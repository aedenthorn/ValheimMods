$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Get-Content -Raw (Join-Path $root 'BepInExPlugin.cs')
$start = $source.IndexOf('public static class Player_SetInventorySize_Patch')
$opening = $source.IndexOf('{', $start)
$depth = 1; $end = $opening + 1
while ($depth -gt 0 -and $end -lt $source.Length) {
    if ($source[$end] -eq '{') { $depth++ }
    if ($source[$end] -eq '}') { $depth-- }
    $end++
}
$sizing = $source.Substring($start, $end - $start)
$fixture = @'
using System;
using System.Collections.Generic;
using System.Reflection;
namespace BepInEx.Configuration {
    public class ConfigEntry<T> { public T Value; }
    public class ConfigFile {
        public int Saves;
        public ConfigEntry<T> Bind<T>(string section, string key, T value, string description) { return new ConfigEntry<T> { Value = value }; }
        public void Save() { Saves++; }
    }
}
namespace HarmonyLib {
    [AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string method) { } }
    public class HarmonyPriority : Attribute { public HarmonyPriority(int priority) { } }
    public static class Priority { public const int First = 800; }
}
namespace UnityEngine { public static class Debug { public static void Log(object message) { } public static void LogError(object message) { } } }
public struct Vector2i {
    public int x, y;
    public Vector2i(int x, int y) { this.x = x; this.y = y; }
}
public class ItemDrop {
    public class ItemData {
        public enum ItemType { Helmet, Chest, Legs, Shoulder, Utility, Tool, Food }
        public class SharedData { public ItemType m_itemType; }
        public SharedData m_shared = new SharedData();
        public Vector2i m_gridPos;
        public int m_stack = 1;
        public bool m_equipped;
        public Dictionary<string,string> Custom = new Dictionary<string,string>();
    }
}
public class Inventory {
    public int Width = 8, Height = 61;
    public List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();
    public int GetWidth() { return Width; }
    public int GetHeight() { return Height; }
    public void SetHeight(int height) { Height = height; }
    public List<ItemDrop.ItemData> GetAllItems() { return Items; }
}
public class Player {
    public static Player m_localPlayer;
    public string Name = "RepairTest", SavedRows = "57";
    public int InvalidItems;
    public Inventory Inventory = new Inventory();
    public ItemDrop.ItemData Head, Chest, Legs, Back, Utility;
    public static implicit operator bool(Player player) { return player != null; }
    public Inventory GetInventory() { return Inventory; }
    public string GetPlayerName() { return Name; }
    public void AddUniqueKeyValue(string key, string value) { SavedRows = value; }
    public bool TryGetUniqueKeyValue(string key, out string value) { value = SavedRows; return value != null; }
    public void DropInvalidItems() { InvalidItems = Inventory.Items.FindAll(i => i.m_gridPos.x < 0 || i.m_gridPos.x >= Inventory.Width || i.m_gridPos.y < 0 || i.m_gridPos.y >= Inventory.Height).Count; }
}
public class InventoryGui {
    public static InventoryGui instance = new InventoryGui();
    public int Height;
    public void SetInventorySize(int height) { Height = height; }
}
namespace ExtendedPlayerInventory {
    public static class BepInExPlugin {
        public static BepInEx.Configuration.ConfigEntry<bool> modEnabled = new BepInEx.Configuration.ConfigEntry<bool> { Value = true };
        public static BepInEx.Configuration.ConfigEntry<bool> addEquipmentRow = new BepInEx.Configuration.ConfigEntry<bool> { Value = true };
        public static BepInEx.Configuration.ConfigEntry<int> extraRows = new BepInEx.Configuration.ConfigEntry<int> { Value = 3 };
        public static ItemDrop.ItemData.ItemType[] typeEnums = { ItemDrop.ItemData.ItemType.Helmet, ItemDrop.ItemData.ItemType.Chest, ItemDrop.ItemData.ItemType.Legs, ItemDrop.ItemData.ItemType.Shoulder, ItemDrop.ItemData.ItemType.Utility };
        public static FieldInfo[] fields = { typeof(Player).GetField("Head"), typeof(Player).GetField("Chest"), typeof(Player).GetField("Legs"), typeof(Player).GetField("Back"), typeof(Player).GetField("Utility") };
        public static void Dbgl(string message) { }
        SIZING_PREFIX
    }
    public static class RepairRegression {
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static ItemDrop.ItemData Item(Player player, int x, int y, ItemDrop.ItemData.ItemType type) {
            var item = new ItemDrop.ItemData { m_gridPos = new Vector2i(x, y), m_stack = 17 };
            item.m_shared.m_itemType = type; item.Custom["sentinel"] = "keep"; player.Inventory.Items.Add(item); return item;
        }
        static BepInEx.Configuration.ConfigFile Request() {
            var config = new BepInEx.Configuration.ConfigFile(); InventoryRepair.Initialize(config);
            InventoryRepair.requestedBaseRows.Value = 5; InventoryRepair.characterName.Value = "repairtest";
            return config;
        }
        public static void Run() {
            var player = new Player(); Player.m_localPlayer = player;
            var original = new Dictionary<ItemDrop.ItemData, Vector2i>();
            for (int i = 0; i < 5; i++) {
                var gear = Item(player, i, 56, BepInExPlugin.typeEnums[i]); gear.m_equipped = true;
                BepInExPlugin.fields[i].SetValue(player, gear);
            }
            for (int i = 5; i < 8; i++) Item(player, i, 60, ItemDrop.ItemData.ItemType.Food);
            var mushrooms = Item(player, 0, 7, ItemDrop.ItemData.ItemType.Food);
            var hammer = Item(player, 3, 60, ItemDrop.ItemData.ItemType.Tool);
            Item(player, 0, 52, ItemDrop.ItemData.ItemType.Food);
            Item(player, 0, 52, ItemDrop.ItemData.ItemType.Tool); // colliding stale coordinates
            foreach (var item in player.Inventory.Items) original[item] = item.m_gridPos;
            var equipped = new ItemDrop.ItemData[] { player.Head, player.Chest, player.Legs, player.Back, player.Utility };
            Dictionary<ItemDrop.ItemData,Vector2i> plan; string error;
            Check(InventoryRepair.TryPlan(player.Inventory, equipped, 8, true, out plan, out error), "repair layout was rejected");
            Check(plan.Count == original.Count && new HashSet<Vector2i>(plan.Values).Count == plan.Count, "items lost or positions collided");
            foreach (var pair in original) Check(pair.Key.m_gridPos.Equals(pair.Value), "planning changed a live position");
            Check(plan[mushrooms].Equals(new Vector2i(0,7)), "valid ordinary position moved");
            Check(plan[hammer].y < 8, "hammer was reserved as Back equipment");
            for (int i = 0; i < 5; i++) Check(plan[equipped[i]].Equals(new Vector2i(i,8)), "actual equipment was not preserved");

            var config = Request(); int rows = 57, state;
            Check(InventoryRepair.RepairSizingPatch.Prefix(player, ref rows, out state) && rows == 5, "requested base not passed to production sizing");
            Check(!BepInExPlugin.Player_SetInventorySize_Patch.Prefix(player, rows, player.Inventory), "production sizing should replace vanilla");
            InventoryRepair.RepairSizingPatch.Postfix(player, state);
            Check(player.SavedRows == "5" && player.Inventory.Height == 9 && InventoryGui.instance.Height == 9 && player.InvalidItems == 0, "repaired size or validation incorrect");
            Check(player.Inventory.Items.Count == original.Count && InventoryRepair.requestedBaseRows.Value == 0 && config.Saves == 1, "one-time request not completed safely");
            foreach (var item in player.Inventory.Items) Check(item.m_stack == 17 && item.Custom["sentinel"] == "keep", "item data was changed");
            for (int spawn = 0; spawn < 100; spawn++) {
                rows = int.Parse(player.SavedRows);
                InventoryRepair.RepairSizingPatch.Prefix(player, ref rows, out state);
                BepInExPlugin.Player_SetInventorySize_Patch.Prefix(player, rows, player.Inventory);
                InventoryRepair.RepairSizingPatch.Postfix(player, state);
                Check(player.Inventory.Height == 9 && player.SavedRows == "5", "growth returned after repair");
            }

            player = new Player(); Player.m_localPlayer = player;
            for (int i = 0; i < 65; i++) Item(player, i%8, i/8, ItemDrop.ItemData.ItemType.Tool);
            original.Clear(); foreach (var item in player.Inventory.Items) original[item] = item.m_gridPos;
            config = Request(); rows = 57;
            Check(!InventoryRepair.RepairSizingPatch.Prefix(player, ref rows, out state), "over-capacity repair should abort sizing");
            Check(state == 0 && rows == 57 && player.Inventory.Height == 61 && player.SavedRows == "57", "aborted repair changed dimensions or persistence");
            foreach (var pair in original) Check(pair.Key.m_gridPos.Equals(pair.Value), "aborted repair moved items");
            Check(InventoryRepair.requestedBaseRows.Value == 5 && config.Saves == 0, "failed repair consumed request");
            player.Name = "Different character";
            Check(InventoryRepair.RepairSizingPatch.Prefix(player, ref rows, out state) && state == 0 && rows == 57, "repair affected another character");
            player.Name = "RepairTest"; InventoryRepair.requestedBaseRows.Value = 3;
            Check(!InventoryRepair.RepairSizingPatch.Prefix(player, ref rows, out state) && rows == 57, "invalid base was accepted");

            player = new Player(); Player.m_localPlayer = player; config = Request(); rows = 57;
            InventoryRepair.RepairSizingPatch.Prefix(player, ref rows, out state);
            InventoryRepair.RepairSizingPatch.Postfix(player, state); // sizing has not yet succeeded
            Check(InventoryRepair.requestedBaseRows.Value == 5 && config.Saves == 0, "request cleared without confirmed persistence");
            var duplicate = Item(player, 0, 0, ItemDrop.ItemData.ItemType.Tool); player.Inventory.Items.Add(duplicate);
            Check(!InventoryRepair.TryPlan(player.Inventory, new ItemDrop.ItemData[5], 8, true, out plan, out error), "duplicate reference should abort");
            player.Inventory.Items.RemoveAt(player.Inventory.Items.Count-1);
            Check(InventoryRepair.TryPlan(player.Inventory, new ItemDrop.ItemData[5], 8, false, out plan, out error) && plan[duplicate].y < 8, "ordinary layout without equipment failed");
            BepInExPlugin.modEnabled.Value = false; rows = 57;
            Check(InventoryRepair.RepairSizingPatch.Prefix(player, ref rows, out state) && state == 0 && rows == 57, "disabled mod applied repair");
        }
    }
}
'@
$fixture = $fixture.Replace('SIZING_PREFIX', $sizing)
$fixturePath = Join-Path ([System.IO.Path]::GetTempPath()) ('epi-repair-tests-' + [guid]::NewGuid().ToString('N') + '.cs')
try {
    [System.IO.File]::WriteAllText($fixturePath, $fixture)
    Add-Type -Path @((Join-Path $root 'InventoryRepair.cs'), $fixturePath)
} finally { Remove-Item -LiteralPath $fixturePath -ErrorAction SilentlyContinue }
[ExtendedPlayerInventory.RepairRegression]::Run()
Write-Output 'PASS: 61-to-9 repair, all equipment and quick slots, overflow/collisions, hammer, item data preserved, no invalid drops, 100 reloads, full-inventory abort, character scope, invalid inputs, persistence verification, disabled mod'
