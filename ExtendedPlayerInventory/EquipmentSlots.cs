using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ExtendedPlayerInventory
{
    internal static class EquipmentSlots
    {
        private sealed class State
        {
            public bool Arranging;
            public bool Sized = true;
            public bool Loading;
            public ItemDrop.ItemData[] Previous = new ItemDrop.ItemData[5];
        }

        private static readonly ConditionalWeakTable<Player, State> states = new ConditionalWeakTable<Player, State>();
        private static readonly MethodInfo findEmptySlot = typeof(Inventory).GetMethod("FindEmptySlot", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPatch(typeof(Player), "Load")]
        internal static class LoadingPatch
        {
            public static void Prefix(Player __instance)
            {
                State state = states.GetValue(__instance, _ => new State());
                state.Sized = false;
                state.Loading = true;
            }

            public static void Postfix(Player __instance)
            {
                states.GetValue(__instance, _ => new State()).Loading = false;
                Arrange(__instance);
            }

            public static void Finalizer(Player __instance)
            {
                states.GetValue(__instance, _ => new State()).Loading = false;
            }
        }

        [HarmonyPatch(typeof(Player), "SetInventorySize")]
        internal static class SizingPatch
        {
            public static void Postfix(Player __instance)
            {
                states.GetValue(__instance, _ => new State()).Sized = true;
                Arrange(__instance);
            }
        }

        private static ItemDrop.ItemData[] GetEquipment(Player player)
        {
            var equipment = new ItemDrop.ItemData[5];
            for (int i = 0; i < equipment.Length; i++)
                equipment[i] = (ItemDrop.ItemData)BepInExPlugin.fields[i].GetValue(player);
            return equipment;
        }

        public static void Arrange(Player player)
        {
            if (!BepInExPlugin.modEnabled.Value || !BepInExPlugin.addEquipmentRow.Value || !player || player.IsDead())
                return;

            State state = states.GetValue(player, _ => new State());
            // Inventory.Load and EquipInventoryItems run before OnSpawned applies the base size.
            // Do not treat the temporary last ordinary row as an equipment row during loading.
            if (state.Loading || (player == Player.m_localPlayer && !state.Sized))
                return;
            // EquipItem, UnequipItem and DropItem can synchronously call SetupEquipment again.
            if (state.Arranging)
                return;

            state.Arranging = true;
            try
            {
                Inventory inventory = player.GetInventory();
                var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
                ItemDrop.ItemData[] equipment = GetEquipment(player);
                int row = inventory.GetHeight() - 1;

                for (int slot = 0; slot < equipment.Length; slot++)
                {
                    if (equipment[slot] != null && inventory.ContainsItem(equipment[slot]))
                        equipment[slot].m_gridPos = new Vector2i(slot, row);
                }

                foreach (ItemDrop.ItemData item in items)
                {
                    int slot = item.m_gridPos.x;
                    if (!inventory.ContainsItem(item) || item.m_gridPos.y != row || slot < 0 || slot >= equipment.Length)
                        continue;

                    if (item == equipment[slot])
                    {
                        item.m_equipped = true;
                        continue;
                    }

                    if (item.m_shared.m_itemType == BepInExPlugin.typeEnums[slot] && state.Previous[slot] != item)
                    {
                        player.EquipItem(item, false);
                        // The game owns equipped state. A failed attempt must remain retryable,
                        // rather than pretending the armor is worn or dropping it on the next pass.
                        equipment[slot] = (ItemDrop.ItemData)BepInExPlugin.fields[slot].GetValue(player);
                        item.m_equipped = equipment[slot] == item;
                        continue;
                    }

                    // Wrong item type (for example a hammer in Back), or deliberately unequipped gear.
                    Vector2i position = (Vector2i)findEmptySlot.Invoke(inventory, new object[] { true });
                    if (position.x < 0 || position.y < 0 || position.y >= row)
                        player.DropItem(inventory, item, item.m_stack);
                    else
                        item.m_gridPos = position;
                }

                state.Previous = GetEquipment(player);
            }
            finally
            {
                state.Arranging = false;
            }
        }
    }
}
