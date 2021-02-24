using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace AutoSplitStack
{
    [BepInPlugin("aedenthorn.AutoSplitStack", "AutoSplitStack", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static bool autoSplitting = false;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 76, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(InventoryGui), "OnRightClickItem")]
        static class OnRightClickItem_Patch
        {
            static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, int ___m_dragAmount)
            {
                if (ZInput.GetButton("JoyLTrigger") || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if(item != null && item.m_stack > 1 && Player.m_localPlayer)
                    {
                        int amount = ___m_dragAmount > 0 ? (item.m_stack - ___m_dragAmount) / 2 + ___m_dragAmount : item.m_stack / 2;
                        Dbgl($"auto stacking: {amount}/{ item.m_stack } {item?.m_shared.m_name}");
                        __instance.GetType().GetMethod("SetupDragItem", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { item, grid.GetInventory(), amount });
                        autoSplitting = true;
                    }
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(InventoryGui), "SetupDragItem")]
        static class InventoryGui_SetupDragItem_Patch
        {
            static bool Prefix(ItemDrop.ItemData item, Inventory inventory, int amount)
            {
                //Dbgl($"setupdragitem {autoSplitting} {amount} {item?.m_shared.m_name} {Environment.StackTrace}");
                if (autoSplitting)
                {
                    return false;
                }
                return true;

            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        static class InventoryGui_Update_Patch
        {
            static void Postfix()
            {
                if (!Input.GetMouseButton(1))
                    autoSplitting = false;
            }
        }
    }
}
