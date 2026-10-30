using HarmonyLib;
using System;

namespace ExtendedPlayerInventory
{
    internal static class TombstoneRecovery
    {
        private static Inventory source;

        [HarmonyPatch(typeof(Container), "RPC_TakeAllResponse")]
        internal static class TakeAllPatch
        {
            public static void Prefix(Container __instance, bool granted, out Inventory __state)
            {
                __state = source;
                source = null;
                if (BepInExPlugin.modEnabled.Value && BepInExPlugin.addEquipmentRow.Value && granted && Player.m_localPlayer && __instance.GetComponent<TombStone>())
                    source = __instance.GetInventory();
            }

            // Always clear the transfer context, including when another patch or vanilla throws.
            public static void Finalizer(Inventory __state)
            {
                source = __state;
            }
        }

        [HarmonyPatch(typeof(Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) })]
        internal static class AddToSlotPatch
        {
            // Remap before the existing compatibility prefix can expand the destination height.
            [HarmonyPriority(Priority.First)]
            public static void Prefix(Inventory __instance, ItemDrop.ItemData item, ref int x, ref int y)
            {
                if (!BepInExPlugin.modEnabled.Value || !BepInExPlugin.addEquipmentRow.Value || source == null || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                    return;

                if (!source.ContainsItem(item) || y != source.GetHeight() - 1 || x < 0 || x >= 8 || x >= __instance.GetWidth())
                    return;

                if (x < 5 && item.m_shared.m_itemType != BepInExPlugin.typeEnums[x])
                    return;

                int equipmentRow = __instance.GetHeight() - 1;
                if (__instance.GetItemAt(x, equipmentRow) == null)
                    y = equipmentRow;
            }
        }
    }
}
