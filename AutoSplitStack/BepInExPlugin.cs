using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace AutoSplitStack
{
    [BepInPlugin("aedenthorn.AutoSplitStack", "AutoSplitStack", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<string> modKey;

        public static bool autoSplitting = false;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 76, "Nexus mod ID for updates");
            modKey = Config.Bind<string>("General", "ModKey", "left shift", "Modifier key to split stack");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public static bool CheckModKey(string value)
        {
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "OnRightClickItem")]
        public static class OnRightClickItem_Patch
        {
            public static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, int ___m_dragAmount)
            {
                if (ZInput.GetButton("JoyLTrigger") || CheckModKey(modKey.Value))
                {
                    if(item != null && item.m_stack > 1 && Player.m_localPlayer)
                    {
                        int amount = ___m_dragAmount > 0 ? (item.m_stack - ___m_dragAmount) / 2 + ___m_dragAmount : item.m_stack / 2;
                        //Dbgl($"auto stacking: {amount}/{ item.m_stack } {item?.m_shared.m_name}");
                        __instance.GetType().GetMethod("SetupDragItem", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { item, grid.GetInventory(), amount });
                        autoSplitting = true;
                    }
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(InventoryGui), "SetupDragItem")]
        public static class InventoryGui_SetupDragItem_Patch
        {
            public static bool Prefix(ItemDrop.ItemData item, Inventory inventory, int amount)
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
        public static class InventoryGui_Update_Patch
        {
            public static void Postfix()
            {
                if (!Input.GetMouseButton(1))
                    autoSplitting = false;
            }
        }
        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
