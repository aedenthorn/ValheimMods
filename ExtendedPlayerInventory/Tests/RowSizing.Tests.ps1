$ErrorActionPreference = 'Stop'

# Exercise the production prefixes without loading Unity or modifying a game installation.
$source = Get-Content -Raw (Join-Path $PSScriptRoot '../BepInExPlugin.cs')
function Get-PatchClass([string] $name) {
    $start = $source.IndexOf("public static class $name")
    if ($start -lt 0) { throw "Missing production class $name" }
    $opening = $source.IndexOf('{', $start)
    $depth = 1
    $end = $opening + 1
    while ($depth -gt 0 -and $end -lt $source.Length) {
        if ($source[$end] -eq '{') { $depth++ }
        if ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    if ($depth -ne 0) { throw "Unbalanced production class $name" }
    $source.Substring($start, $end - $start)
}
$sizing = Get-PatchClass 'Player_SetInventorySize_Patch'
$insertion = Get-PatchClass 'Inventory_AddItem_Patch2'
Add-Type -TypeDefinition @"
using System;
namespace RowSizingRegression {
    public class Setting<T> { public T Value; }
    public class Inventory {
        public int Width = 8, Height = 4;
        public void SetHeight(int value) { Height = value; }
        public bool Insert(int x, int y, bool loading = false) {
            Plugin.Inventory_AddItem_Patch2.Prefix(this, ref Width, ref Height, x, y, loading);
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }
    }
    public class Player {
        public static Player m_localPlayer;
        public readonly Inventory Inventory = new Inventory();
        public string SavedRows;
        public int DropChecks;
        public Inventory GetInventory() { return Inventory; }
        public void AddUniqueKeyValue(string key, string value) { SavedRows = value; }
        public void DropInvalidItems() { DropChecks++; }
    }
    public class InventoryGui {
        public static InventoryGui instance = new InventoryGui();
        public int Height;
        public void SetInventorySize(int value) { Height = value; }
    }
    public class Plugin {
        public static Setting<bool> modEnabled = new Setting<bool> { Value = true };
        public static Setting<bool> addEquipmentRow = new Setting<bool>();
        public static Setting<bool> displayEquipmentRowSeparate = new Setting<bool>();
        public static Setting<int> extraRows = new Setting<int>();
        public static void Dbgl(string value) { }
        $sizing
        $insertion
    }
    public static class Tests {
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run() {
            foreach (int extras in new[] { 0, 3 }) foreach (bool equipment in new[] { false, true }) foreach (bool separate in new[] { false, true }) {
                var player = new Player(); Player.m_localPlayer = player;
                Plugin.extraRows.Value = extras; Plugin.addEquipmentRow.Value = equipment;
                Plugin.displayEquipmentRowSeparate.Value = separate;
                int baseRows = 4;
                for (int spawn = 0; spawn < 100; spawn++) {
                    Check(!Plugin.Player_SetInventorySize_Patch.Prefix(player, baseRows, player.Inventory), "vanilla sizing must be replaced");
                    Check(player.Inventory.Height == 4 + extras + (equipment ? 1 : 0), "height grew across repeated spawns");
                    Check(player.SavedRows == "4", "mod rows leaked into vanilla persistence");
                    Check(InventoryGui.instance.Height == player.Inventory.Height - (equipment && separate ? 1 : 0), "panel includes a row whose slots are displayed separately");
                    baseRows = int.Parse(player.SavedRows);
                }
                // A purchased row must survive subsequent respawns without being counted twice.
                Plugin.Player_SetInventorySize_Patch.Prefix(player, baseRows + 1, player.Inventory);
                Check(player.SavedRows == "5", "capacity upgrade was not preserved");
                int upgradedHeight = player.Inventory.Height;
                Plugin.Player_SetInventorySize_Patch.Prefix(player, int.Parse(player.SavedRows), player.Inventory);
                Check(player.Inventory.Height == upgradedHeight, "upgraded inventory grew on respawn");
                Check(InventoryGui.instance.Height == upgradedHeight - (equipment && separate ? 1 : 0), "purchased row or separate equipment panel sized incorrectly");
                int width = player.Inventory.Width, height = player.Inventory.Height;
                Check(!player.Inventory.Insert(width, height + 3), "out-of-bounds grave insertion should fail for fallback");
                Check(player.Inventory.Width == width && player.Inventory.Height == height, "grave coordinates changed player capacity");
                Check(player.Inventory.Insert(0, 0), "valid insertion was rejected");
                Check(player.Inventory.Insert(7, 60, true) && player.Inventory.Height == 61, "saved positions were rejected before final sizing");
                Plugin.Player_SetInventorySize_Patch.Prefix(player, int.Parse(player.SavedRows), player.Inventory);
                Check(player.Inventory.Height == upgradedHeight && player.SavedRows == "5", "temporary load height leaked into final sizing");
            }
            var container = new Inventory();
            Check(container.Insert(9, 12), "existing non-player compatibility changed");
            Plugin.modEnabled.Value = false;
            var untouched = new Player();
            Check(Plugin.Player_SetInventorySize_Patch.Prefix(untouched, 6, untouched.Inventory), "disabled mod must defer to vanilla");
            Check(untouched.SavedRows == null && untouched.DropChecks == 0 && untouched.Inventory.Height == 4, "disabled prefix changed player state");
        }
    }
}
"@
[RowSizingRegression.Tests]::Run()
Write-Output 'PASS: repeated spawns, capacity upgrades, GUI sizing, grave bounds, container compatibility, disabled mod'
