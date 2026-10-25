$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$production = @('EquipmentSlots.cs', 'TombstoneRecovery.cs') | ForEach-Object { Join-Path $root $_ }
# Compile the actual production files against deterministic stand-ins for Unity/game callbacks.
# A separate compilation against game assemblies verifies API compatibility.
$fixture = @'
using System;
using System.Collections.Generic;
using System.Reflection;
namespace HarmonyLib {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HarmonyPatch : Attribute {
        public HarmonyPatch(Type type, string method) { }
        public HarmonyPatch(Type type, string method, Type[] args) { }
    }
    public class HarmonyPriority : Attribute { public HarmonyPriority(int priority) { } }
    public static class Priority { public const int First = 800; }
}
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
        public bool m_equipped;
        public int m_stack = 1;
    }
}
public class Inventory {
    public int Width = 8, Height = 8;
    public List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();
    public int GetWidth() { return Width; }
    public int GetHeight() { return Height; }
    public List<ItemDrop.ItemData> GetAllItems() { return Items; }
    public bool ContainsItem(ItemDrop.ItemData item) { return Items.Contains(item); }
    public ItemDrop.ItemData GetItemAt(int x, int y) { return Items.Find(i => i.m_gridPos.x == x && i.m_gridPos.y == y); }
    private Vector2i FindEmptySlot(bool topFirst) {
        for (int y = 0; y < Height - 1; y++) for (int x = 0; x < Width; x++)
            if (GetItemAt(x, y) == null) return new Vector2i(x, y);
        return new Vector2i(-1, -1);
    }
}
public class Humanoid {
    public ItemDrop.ItemData m_helmetItem, m_chestItem, m_legItem, m_shoulderItem, m_utilityItem;
}
public class Player : Humanoid {
    public static Player m_localPlayer;
    public Inventory Inventory = new Inventory();
    public List<ItemDrop.ItemData> Dropped = new List<ItemDrop.ItemData>();
    public bool Dead, RejectEquip, ThrowOnEquip;
    public int EquipCalls;
    public static implicit operator bool(Player p) { return p != null; }
    public bool IsDead() { return Dead; }
    public Inventory GetInventory() { return Inventory; }
    public bool EquipItem(ItemDrop.ItemData item, bool triggerEffects) {
        EquipCalls++;
        if (ThrowOnEquip) throw new InvalidOperationException("simulated equip failure");
        if (RejectEquip) return false;
        int slot = Array.IndexOf(ExtendedPlayerInventory.BepInExPlugin.typeEnums, item.m_shared.m_itemType);
        if (slot < 0) return false;
        ExtendedPlayerInventory.BepInExPlugin.fields[slot].SetValue(this, item);
        item.m_equipped = true;
        ExtendedPlayerInventory.EquipmentSlots.Arrange(this); // SetupEquipment postfix reentry
        return true;
    }
    public bool DropItem(Inventory inventory, ItemDrop.ItemData item, int stack) {
        inventory.Items.Remove(item); Dropped.Add(item);
        ExtendedPlayerInventory.EquipmentSlots.Arrange(this);
        return true;
    }
}
public class TombStone { public static implicit operator bool(TombStone t) { return t != null; } }
public class Container {
    public bool Grave;
    public Inventory Inventory = new Inventory();
    public Inventory GetInventory() { return Inventory; }
    public T GetComponent<T>() where T : class { return Grave ? new TombStone() as T : null; }
}
namespace ExtendedPlayerInventory {
    public class Setting<T> { public T Value; }
    public static class BepInExPlugin {
        public static Setting<bool> modEnabled = new Setting<bool> { Value = true };
        public static Setting<bool> addEquipmentRow = new Setting<bool> { Value = true };
        public static ItemDrop.ItemData.ItemType[] typeEnums = {
            ItemDrop.ItemData.ItemType.Helmet, ItemDrop.ItemData.ItemType.Chest,
            ItemDrop.ItemData.ItemType.Legs, ItemDrop.ItemData.ItemType.Shoulder, ItemDrop.ItemData.ItemType.Utility
        };
        public static FieldInfo[] fields = {
            typeof(Humanoid).GetField("m_helmetItem"), typeof(Humanoid).GetField("m_chestItem"),
            typeof(Humanoid).GetField("m_legItem"), typeof(Humanoid).GetField("m_shoulderItem"), typeof(Humanoid).GetField("m_utilityItem")
        };
    }
    public static class EquipmentRegression {
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static Player NewPlayer() { var p = new Player(); Player.m_localPlayer = p; EquipmentSlots.SizingPatch.Postfix(p); return p; }
        static ItemDrop.ItemData Item(Player p, int slot, ItemDrop.ItemData.ItemType? type = null) {
            var item = new ItemDrop.ItemData { m_gridPos = new Vector2i(slot, p.Inventory.Height - 1) };
            item.m_shared.m_itemType = type ?? BepInExPlugin.typeEnums[slot];
            p.Inventory.Items.Add(item); return item;
        }
        public static void Run() {
            var fresh = new Player(); Player.m_localPlayer = fresh;
            var freshHelmet = Item(fresh, 0);
            EquipmentSlots.Arrange(fresh);
            Check(fresh.m_helmetItem == freshHelmet, "new players that do not load a save remained blocked");
            var loading = new Player(); Player.m_localPlayer = loading; loading.Inventory.Height = 4;
            var loadingHammer = Item(loading, 3, ItemDrop.ItemData.ItemType.Tool);
            var loadingHelmet = Item(loading, 0); loadingHelmet.m_gridPos = new Vector2i(0,8); loading.m_helmetItem = loadingHelmet;
            EquipmentSlots.LoadingPatch.Prefix(loading);
            EquipmentSlots.Arrange(loading);
            Check(loadingHammer.m_gridPos.y == 3 && loadingHelmet.m_gridPos.y == 8 && loading.EquipCalls == 0 && loading.Dropped.Count == 0, "load callbacks rearranged items before sizing");
            EquipmentSlots.LoadingPatch.Postfix(loading);
            EquipmentSlots.Arrange(loading);
            Check(loadingHelmet.m_gridPos.y == 8 && loading.Dropped.Count == 0, "completed load arranged against temporary size");
            loading.Inventory.Height = 9;
            EquipmentSlots.SizingPatch.Postfix(loading);
            Check(loadingHammer.m_gridPos.y == 3 && loadingHelmet.m_gridPos.y == 8 && loading.Dropped.Count == 0, "final sizing treated an ordinary hammer as Back equipment");
            Player.m_localPlayer = null;
            var menu = new Player(); menu.Inventory.Height = 4;
            var menuHammer = Item(menu, 3, ItemDrop.ItemData.ItemType.Tool);
            EquipmentSlots.LoadingPatch.Prefix(menu);
            EquipmentSlots.Arrange(menu);
            Check(menuHammer.m_gridPos.y == 3 && menu.Dropped.Count == 0, "menu load callbacks treated ordinary slots as equipment");
            menu.Inventory.Height = 9;
            EquipmentSlots.LoadingPatch.Postfix(menu);
            Check(menuHammer.m_gridPos.y == 3, "menu load completion moved an ordinary item");
            var p = NewPlayer();
            for (int slot = 0; slot < 5; slot++) Item(p, slot);
            EquipmentSlots.Arrange(p);
            Check(p.EquipCalls == 5 && p.Dropped.Count == 0, "reentrant equipment setup skipped or dropped armor");
            for (int slot = 0; slot < 5; slot++) Check(BepInExPlugin.fields[slot].GetValue(p) == p.Inventory.GetItemAt(slot, 7), "actual equipment field missing");
            EquipmentSlots.Arrange(p);
            Check(p.EquipCalls == 5, "already equipped armor was equipped twice");

            p = NewPlayer(); var helmet = Item(p, 0); p.RejectEquip = true;
            EquipmentSlots.Arrange(p); EquipmentSlots.Arrange(p);
            Check(!helmet.m_equipped && p.m_helmetItem == null && p.Dropped.Count == 0, "failed equip was faked or dropped");
            p.RejectEquip = false; EquipmentSlots.Arrange(p);
            Check(helmet.m_equipped && p.m_helmetItem == helmet, "failed equipment could not retry");
            int calls = p.EquipCalls;
            p.m_helmetItem = null; helmet.m_equipped = false;
            EquipmentSlots.Arrange(p);
            Check(p.EquipCalls == calls && helmet.m_gridPos.y < 7, "manual unequip was overridden");

            p = NewPlayer(); var hammer = Item(p, 3, ItemDrop.ItemData.ItemType.Tool);
            EquipmentSlots.Arrange(p);
            Check(hammer.m_gridPos.y < 7 && p.m_shoulderItem == null && p.EquipCalls == 0 && p.Inventory.Items.Contains(hammer), "hammer remained in Back or was treated as armor");
            p = NewPlayer();
            for (int y = 0; y < 7; y++) for (int x = 0; x < 8; x++)
                p.Inventory.Items.Add(new ItemDrop.ItemData { m_gridPos = new Vector2i(x, y) });
            hammer = Item(p, 3, ItemDrop.ItemData.ItemType.Tool);
            EquipmentSlots.Arrange(p);
            Check(p.Inventory.Items.Count + p.Dropped.Count == 57 && p.Dropped.Count == 1 && p.Dropped[0] == hammer, "full inventory lost or duplicated a wrong-slot item");

            p = NewPlayer(); helmet = Item(p, 0); EquipmentSlots.Arrange(p);
            p.Dead = true; p.m_helmetItem = null; helmet.m_equipped = false;
            EquipmentSlots.Arrange(p);
            Check(helmet.m_gridPos.y == 7 && !helmet.m_equipped, "death unequip moved armor out of grave equipment coordinates");
            p.Inventory.Items.Remove(helmet); p = NewPlayer(); p.Inventory.Items.Add(helmet);
            EquipmentSlots.Arrange(p);
            Check(p.m_helmetItem == helmet, "previous player's cache suppressed recovery");
            p = NewPlayer(); helmet = Item(p, 0); p.ThrowOnEquip = true;
            try { EquipmentSlots.Arrange(p); } catch (InvalidOperationException) { }
            p.ThrowOnEquip = false; EquipmentSlots.Arrange(p);
            Check(p.m_helmetItem == helmet, "exception left arrangement permanently guarded");

            p = NewPlayer();
            var grave = new Container { Grave = true }; grave.Inventory.Height = 12;
            var cloak = new ItemDrop.ItemData { m_gridPos = new Vector2i(3, 11) };
            cloak.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Shoulder; grave.Inventory.Items.Add(cloak);
            Inventory previous;
            TombstoneRecovery.TakeAllPatch.Prefix(grave, true, out previous);
            try {
                int x = 3, y = 11;
                TombstoneRecovery.AddToSlotPatch.Prefix(p.Inventory, cloak, ref x, ref y);
                Check(y == 7 && cloak.m_gridPos.y == 11 && grave.Inventory.Height == 12 && p.Inventory.Height == 8, "grave remapping changed source or capacity");
                var recovered = new ItemDrop.ItemData { m_gridPos = new Vector2i(x, y) };
                recovered.m_shared.m_itemType = cloak.m_shared.m_itemType; p.Inventory.Items.Add(recovered);
                EquipmentSlots.Arrange(p);
                Check(p.m_shoulderItem == recovered && recovered.m_equipped, "old grave coordinates did not restore actual armor");
                y = 11; TombstoneRecovery.AddToSlotPatch.Prefix(p.Inventory, cloak, ref x, ref y);
                Check(y == 11, "occupied equipment slot was overwritten");
                x = 6; y = 11;
                var food = new ItemDrop.ItemData { m_gridPos = new Vector2i(x, y) }; grave.Inventory.Items.Add(food);
                TombstoneRecovery.AddToSlotPatch.Prefix(p.Inventory, food, ref x, ref y);
                Check(y == 7, "grave quick slot was not preserved");
                x = 3; y = 11; hammer = new ItemDrop.ItemData { m_gridPos = new Vector2i(x, y) };
                hammer.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Tool; grave.Inventory.Items.Add(hammer);
                TombstoneRecovery.AddToSlotPatch.Prefix(p.Inventory, hammer, ref x, ref y);
                Check(y == 11, "wrong item type was mapped into Back");
                var chest = new Container(); Inventory nested;
                TombstoneRecovery.TakeAllPatch.Prefix(chest, true, out nested);
                try {
                    x = 6; y = 11; TombstoneRecovery.AddToSlotPatch.Prefix(p.Inventory, food, ref x, ref y);
                    Check(y == 11, "nested chest inherited grave context");
                } finally { TombstoneRecovery.TakeAllPatch.Finalizer(nested); }
                x = 6; y = 11; TombstoneRecovery.AddToSlotPatch.Prefix(p.Inventory, food, ref x, ref y);
                Check(y == 7, "nested transfer did not restore grave context");
            } finally { TombstoneRecovery.TakeAllPatch.Finalizer(previous); }
            int outsideX = 6, outsideY = 11;
            var outsideItem = grave.Inventory.Items[1];
            TombstoneRecovery.AddToSlotPatch.Prefix(p.Inventory, outsideItem, ref outsideX, ref outsideY);
            Check(outsideY == 11, "grave context leaked beyond transfer");
            TombstoneRecovery.TakeAllPatch.Prefix(grave, false, out previous);
            try {
                TombstoneRecovery.AddToSlotPatch.Prefix(p.Inventory, outsideItem, ref outsideX, ref outsideY);
                Check(outsideY == 11, "denied transfer remapped items");
            } finally { TombstoneRecovery.TakeAllPatch.Finalizer(previous); }
            BepInExPlugin.modEnabled.Value = false;
            p = NewPlayer(); helmet = Item(p, 0); EquipmentSlots.Arrange(p);
            Check(p.EquipCalls == 0 && !helmet.m_equipped, "disabled mod still arranged equipment");
        }
    }
}
'@
$fixturePath = Join-Path ([System.IO.Path]::GetTempPath()) ("epi-equipment-tests-" + [guid]::NewGuid().ToString('N') + '.cs')
try {
    [System.IO.File]::WriteAllText($fixturePath, $fixture)
    Add-Type -Path ($production + $fixturePath)
} finally {
    Remove-Item -LiteralPath $fixturePath -ErrorAction SilentlyContinue
}
[ExtendedPlayerInventory.EquipmentRegression]::Run()
Write-Output 'PASS: load-before-sizing and menu loading, all equipment types, reentry, equip failures/retries, manual unequip, hammer in Back, full inventory, death, player isolation, exceptions, old graves, quick slots, occupied slots, denied/nested transfers, disabled mod'
