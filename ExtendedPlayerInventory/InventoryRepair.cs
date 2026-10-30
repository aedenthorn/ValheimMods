using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ExtendedPlayerInventory
{
    internal static class InventoryRepair
    {
        internal const string CheckedKey = "ExtendedPlayerInventory_rows_checked_v1";
        internal static ConfigEntry<bool> automaticRepair;
        internal static ConfigEntry<int> requestedBaseRows;
        internal static ConfigEntry<string> characterName;
        private static ConfigFile settings;
        private sealed class SpawnState { internal int Depth; }
        private static readonly ConditionalWeakTable<Player, SpawnState> spawning = new ConditionalWeakTable<Player, SpawnState>();

        internal struct RepairState
        {
            internal int BaseRows;
            internal bool Automatic;
            internal bool Changed;
        }

        internal static void Initialize(ConfigFile config)
        {
            settings = config;
            automaticRepair = config.Bind("Maintenance", "AutoRepairSavedRows", true, "Once per character at spawn, repair inflated saved base rows using the vanilla invslot1/invslot2 purchase flags (4 base rows plus one per purchase). Items are repositioned first; repair aborts if they do not fit. Disable if another mod adds base capacity beyond these purchases. ExtraRows is preserved.");
            requestedBaseRows = config.Bind("Maintenance", "RepairBaseRows", 0, "Manual one-time repair override. Set to the intended vanilla base rows, including purchased upgrades but excluding ExtraRows/equipment. 0 leaves automatic detection in control. Requires RepairCharacter. Items are repositioned first; repair aborts if they do not fit. Resets to 0 after success.");
            characterName = config.Bind("Maintenance", "RepairCharacter", "", "Exact character name for the one-time repair. Required when RepairBaseRows is nonzero.");
        }

        internal static int PurchasedBaseRows(Player player)
        {
            return 4 + (player.HaveUniqueKey("invslot1") ? 1 : 0) + (player.HaveUniqueKey("invslot2") ? 1 : 0);
        }

        // Scope detection to spawn sizing. A trader's SetInventorySize call can happen
        // before its purchase flag is added, and must never be mistaken for inflation.
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        internal static class SpawnPatch
        {
            public static void Prefix(Player __instance, out bool __state)
            {
                __state = false;
                if (!BepInExPlugin.modEnabled.Value || !automaticRepair.Value || !Player.m_localPlayer || __instance != Player.m_localPlayer || __instance.HaveUniqueKey(CheckedKey))
                    return;
                spawning.GetValue(__instance, _ => new SpawnState()).Depth++;
                __state = true;
            }

            public static void Finalizer(Player __instance, bool __state)
            {
                if (__state)
                    spawning.GetValue(__instance, _ => new SpawnState()).Depth--;
            }
        }

        // Build a complete layout before changing any live item, dimension or saved key.
        internal static bool TryPlan(Inventory inventory, IList<ItemDrop.ItemData> equipped, int ordinaryRows, bool equipmentRow,
            out Dictionary<ItemDrop.ItemData, Vector2i> plan, out string error)
        {
            plan = null;
            error = null;
            int width = inventory.GetWidth();
            if (ordinaryRows < 1 || width < (equipmentRow ? 8 : 1))
            {
                error = "Invalid target dimensions.";
                return false;
            }

            var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            var uniqueItems = new HashSet<ItemDrop.ItemData>();
            foreach (var item in items)
            {
                if (item == null || !uniqueItems.Add(item))
                {
                    error = "Inventory contains a missing or duplicate item reference.";
                    return false;
                }
            }

            var positions = new Dictionary<ItemDrop.ItemData, Vector2i>();
            var occupied = new HashSet<Vector2i>();
            int oldEquipmentRow = inventory.GetHeight() - 1;
            if (equipmentRow)
            {
                // Equipped fields are authoritative even when load/spawn already moved the row.
                for (int slot = 0; slot < 5; slot++)
                {
                    var item = equipped[slot];
                    if (item != null && uniqueItems.Contains(item) && item.m_shared != null && item.m_shared.m_itemType == BepInExPlugin.typeEnums[slot] && !positions.ContainsKey(item))
                    {
                        var position = new Vector2i(slot, ordinaryRows);
                        positions.Add(item, position);
                        occupied.Add(position);
                    }
                }

                foreach (var item in items)
                {
                    int slot = item.m_gridPos.x;
                    if (positions.ContainsKey(item) || item.m_gridPos.y != oldEquipmentRow || slot < 0 || slot >= 8)
                        continue;
                    if (slot < 5 && (item.m_shared == null || item.m_shared.m_itemType != BepInExPlugin.typeEnums[slot]))
                        continue;
                    var position = new Vector2i(slot, ordinaryRows);
                    if (occupied.Add(position))
                        positions.Add(item, position);
                }
            }

            // Keep valid ordinary positions. Repack only overflow, collisions and wrong-slot items.
            foreach (var item in items)
            {
                var position = item.m_gridPos;
                if (!positions.ContainsKey(item) && position.x >= 0 && position.x < width && position.y >= 0 && position.y < ordinaryRows && occupied.Add(position))
                    positions.Add(item, position);
            }

            foreach (var item in items)
            {
                if (positions.ContainsKey(item))
                    continue;
                bool found = false;
                for (int y = 0; y < ordinaryRows && !found; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var position = new Vector2i(x, y);
                        if (!occupied.Add(position))
                            continue;
                        positions.Add(item, position);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    error = "Items do not fit in the requested ordinary rows. Nothing was changed; move items to a chest before retrying.";
                    return false;
                }
            }

            plan = positions;
            return true;
        }

        [HarmonyPatch(typeof(Player), "SetInventorySize")]
        internal static class RepairSizingPatch
        {
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(Player __instance, ref int rows, out RepairState __state)
            {
                __state = default(RepairState);
                if (!BepInExPlugin.modEnabled.Value || !Player.m_localPlayer || __instance != Player.m_localPlayer)
                    return true;

                bool manual = requestedBaseRows.Value != 0 && !string.IsNullOrWhiteSpace(characterName.Value) && string.Equals(__instance.GetPlayerName(), characterName.Value, StringComparison.OrdinalIgnoreCase);
                bool automatic = !manual && automaticRepair.Value && !__instance.HaveUniqueKey(CheckedKey) && spawning.TryGetValue(__instance, out var spawn) && spawn.Depth > 0;
                if (!manual && !automatic)
                    return true;

                int baseRows = manual ? requestedBaseRows.Value : PurchasedBaseRows(__instance);
                if (automatic && rows <= baseRows)
                {
                    // A healthy save needs no migration. Confirm sizing before marking it
                    // checked, and leave an undersized/unknown save to the ordinary path.
                    if (rows == baseRows)
                        __state = new RepairState { BaseRows = baseRows, Automatic = true };
                    return true;
                }
                long ordinaryRows = (long)baseRows + BepInExPlugin.extraRows.Value;
                if (baseRows < 4 || baseRows > 9 || ordinaryRows < baseRows || ordinaryRows >= int.MaxValue)
                {
                    Debug.LogError("ExtendedPlayerInventory repair cancelled: invalid base or extra row count.");
                    return false;
                }

                var inventory = __instance.GetInventory();
                var equipped = new ItemDrop.ItemData[5];
                for (int slot = 0; slot < equipped.Length; slot++)
                    equipped[slot] = (ItemDrop.ItemData)BepInExPlugin.fields[slot].GetValue(__instance);
                if (!TryPlan(inventory, equipped, (int)ordinaryRows, BepInExPlugin.addEquipmentRow.Value, out var plan, out var error))
                {
                    Debug.LogError("ExtendedPlayerInventory repair cancelled: " + error);
                    // Skip both vanilla sizing and the ordinary sizing prefix, preserving capacity.
                    return false;
                }

                foreach (var entry in plan)
                    entry.Key.m_gridPos = entry.Value;
                rows = baseRows;
                __state = new RepairState { BaseRows = baseRows, Automatic = automatic, Changed = true };
                return true;
            }

            public static void Postfix(Player __instance, RepairState __state)
            {
                if (__state.BaseRows == 0)
                    return;
                int height = __state.BaseRows + BepInExPlugin.extraRows.Value + (BepInExPlugin.addEquipmentRow.Value ? 1 : 0);
                if (__instance.GetInventory().GetHeight() != height || !__instance.TryGetUniqueKeyValue("invrows", out string saved) || saved != __state.BaseRows.ToString())
                {
                    Debug.LogError("ExtendedPlayerInventory repair has not completed sizing; the repair request remains enabled.");
                    return;
                }
                __instance.AddUniqueKey(CheckedKey);
                if (!__state.Automatic)
                {
                    requestedBaseRows.Value = 0;
                    settings.Save();
                }
                if (__state.Changed)
                    Debug.Log($"ExtendedPlayerInventory repaired saved base rows to {__state.BaseRows}, total height to {height}; {__instance.GetInventory().GetAllItems().Count} items preserved. One-time repair completed.");
            }
        }
    }
}
