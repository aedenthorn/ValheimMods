using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CustomToolbarHotkeys
{
    [BepInPlugin("aedenthorn.CustomToolbarHotkeys", "Custom Toolbar Hotkeys", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private static bool usingHotkey = false;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<bool> hideNumbers;
        public static ConfigEntry<bool> showHotkeys;
        
        public static ConfigEntry<string> hotKey1;
        public static ConfigEntry<string> hotKey2;
        public static ConfigEntry<string> hotKey3;
        public static ConfigEntry<string> hotKey4;
        public static ConfigEntry<string> hotKey5;
        public static ConfigEntry<string> hotKey6;
        public static ConfigEntry<string> hotKey7;
        public static ConfigEntry<string> hotKey8;

        private static ConfigEntry<string>[] hotkeys;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 683, "Nexus mod ID for updates");

            hideNumbers = Config.Bind<bool>("General", "HideNumbers", false, "Hide hotkey numbers on toolbar");
            showHotkeys = Config.Bind<bool>("General", "ShowHotkeys", false, "Show new hotkey strings on toolbar. Must set HideNumbers to true.");
            hotKey1 = Config.Bind<string>("Hotkeys", "HotKey1", "1", "Hotkey 1 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey2 = Config.Bind<string>("Hotkeys", "HotKey2", "2", "Hotkey 2 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey3 = Config.Bind<string>("Hotkeys", "HotKey3", "3", "Hotkey 3 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey4 = Config.Bind<string>("Hotkeys", "HotKey4", "4", "Hotkey 4 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey5 = Config.Bind<string>("Hotkeys", "HotKey5", "5", "Hotkey 5 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey6 = Config.Bind<string>("Hotkeys", "HotKey6", "6", "Hotkey 6 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey7 = Config.Bind<string>("Hotkeys", "HotKey7", "7", "Hotkey 7 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey8 = Config.Bind<string>("Hotkeys", "HotKey8", "8", "Hotkey 8 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");

            hotkeys = new ConfigEntry<string>[]
            {
                hotKey1,
                hotKey2,
                hotKey3,
                hotKey4,
                hotKey5,
                hotKey6,
                hotKey7,
                hotKey8
            };

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
        static class HotkeyBar_UpdateIcons_Patch
        {
            static void Postfix(HotkeyBar __instance)
            {
                if (!modEnabled.Value || !hideNumbers.Value || __instance.name != "HotKeyBar")
                    return;

                int count = __instance.transform.childCount;
                for(int i = 0; i < count; i++)
                {
                    if (__instance.transform.GetChild(i).Find("binding")) { }
                        __instance.transform.GetChild(i).Find("binding").GetComponent<Text>().text = showHotkeys.Value ? hotkeys[i].Value : "";
                }
            }
        }
        
        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static bool Prefix(Player __instance)
            {
                if (!modEnabled.Value)
                    return true;

                if (Input.GetKeyDown(hotKey1.Value))
                    __instance.UseHotbarItem(1);
                else if (Input.GetKeyDown(hotKey2.Value))
                    __instance.UseHotbarItem(2);
                else if (Input.GetKeyDown(hotKey3.Value))
                    __instance.UseHotbarItem(3);
                else if (Input.GetKeyDown(hotKey4.Value))
                    __instance.UseHotbarItem(4);
                else if (Input.GetKeyDown(hotKey5.Value))
                    __instance.UseHotbarItem(5);
                else if (Input.GetKeyDown(hotKey6.Value))
                    __instance.UseHotbarItem(6);
                else if (Input.GetKeyDown(hotKey7.Value))
                    __instance.UseHotbarItem(7);
                else if (Input.GetKeyDown(hotKey8.Value))
                    __instance.UseHotbarItem(8);
                else return true;

                usingHotkey = true;

                return false;
            }
        }
        
        [HarmonyPatch(typeof(Player), "UseHotbarItem")]
        static class Player_UseHotbarItem_Patch
        {
            static bool Prefix(int index)
            {
                if (!modEnabled.Value)
                    return true;

                if (!usingHotkey)
                    return false;

                usingHotkey = false;

                return true;
            }
        }

                
        [HarmonyPatch(typeof(InventoryGui), "Update")]
        static class InventoryGui_Update_Patch
        {
            static void Postfix(InventoryGrid ___m_playerGrid, Animator ___m_animator)
            {
                if (!modEnabled.Value || !hideNumbers.Value || !___m_animator.GetBool("visible"))
                    return;

                List<object> elements = typeof(InventoryGrid).GetField("m_elements", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(___m_playerGrid) as List<object>;
                for(int i = 0; i < 8; i++)
                {
                    if(___m_playerGrid.m_gridRoot.transform.GetChild(i).Find("binding"))
                        ___m_playerGrid.m_gridRoot.transform.GetChild(i).Find("binding").GetComponent<Text>().text = showHotkeys.Value ? hotkeys[i].Value : "";
                }

            }
        }



        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}