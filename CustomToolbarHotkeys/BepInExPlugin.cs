using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomToolbarHotkeys
{
    [BepInPlugin("aedenthorn.CustomToolbarHotkeys", "Custom Toolbar Hotkeys", "0.4.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = false;
        public static BepInExPlugin context;
        public static bool usingHotkey = false;

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

        public static ConfigEntry<string>[] hotkeys;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 683, "Nexus mod ID for updates");

            hideNumbers = Config.Bind<bool>("General", "HideNumbers", false, "Hide hotkey numbers on toolbar");
            showHotkeys = Config.Bind<bool>("General", "ShowHotkeys", false, "Show new hotkey strings on toolbar (takes priority over numbers or hidden)");
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
        public static class HotkeyBar_UpdateIcons_Patch
        {
            public static void Postfix(HotkeyBar __instance)
            {
                if (!modEnabled.Value || __instance.name != "HotKeyBar")
                    return;

                int count = __instance.transform.childCount;
                if (showHotkeys.Value)
                {
                    Dbgl("Switching to Hotkeys");
                    for (int i = 0; i < count; i++)
                    {
                        if (__instance.transform.GetChild(i).Find("binding"))
                        {
                            __instance.transform.GetChild(i).Find("binding").GetComponent<TextMeshProUGUI>().text = hotkeys[i].Value;
                        }
                    }
                }
                else if (hideNumbers.Value)
                {
                    Dbgl("Switching to Nothing");
                    for (int i = 0; i < count; i++)
                    {
                        if (__instance.transform.GetChild(i).Find("binding"))
                        {
                            __instance.transform.GetChild(i).Find("binding").GetComponent<TextMeshProUGUI>().text = "";
                        }
                    }
                }
                else
                {
                    Dbgl("Switching to Numbers");
                    for (int i = 0; i < count; i++)
                    {
                        if (__instance.transform.GetChild(i).Find("binding"))
                        {
                            __instance.transform.GetChild(i).Find("binding").GetComponent<TextMeshProUGUI>().text = (i+1).ToString();
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Player), "Update")]
        public static class Player_Update_Patch
        {
            public static bool Prefix(Player __instance)
            {
                if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses(true))
                    return true;

                int which;
                if (AedenthornUtils.CheckKeyDown(hotKey1.Value))
                    which = 1;
                else if (AedenthornUtils.CheckKeyDown(hotKey2.Value))
                    which = 2;
                else if (AedenthornUtils.CheckKeyDown(hotKey3.Value))
                    which = 3;
                else if (AedenthornUtils.CheckKeyDown(hotKey4.Value))
                    which = 4;
                else if (AedenthornUtils.CheckKeyDown(hotKey5.Value))
                    which = 5;
                else if (AedenthornUtils.CheckKeyDown(hotKey6.Value))
                    which = 6;
                else if (AedenthornUtils.CheckKeyDown(hotKey7.Value))
                    which = 7;
                else if (AedenthornUtils.CheckKeyDown(hotKey8.Value))
                    which = 8;
                else return true;

                usingHotkey = true;
                __instance.UseHotbarItem(which);
                return false;
            }
        }
        
        [HarmonyPatch(typeof(Player), "UseHotbarItem")]
        public static class Player_UseHotbarItem_Patch
        {
            public static bool Prefix(int index)
            {
                if (!modEnabled.Value || !usingHotkey)
                    return false;

                usingHotkey = false;

                return true;
            }
        }

                
        [HarmonyPatch(typeof(InventoryGui), "Update")]
        public static class InventoryGui_Update_Patch
        {
            public static void Postfix(InventoryGrid ___m_playerGrid, Animator ___m_animator)
            {
                if (!modEnabled.Value || ___m_playerGrid.m_gridRoot.transform.childCount < 8 || !___m_animator.GetBool("visible"))
                    return;

                if (showHotkeys.Value)
                {
                    Dbgl("Switching to Hotkeys");
                    for (int i = 0; i < 8; i++)
                    {
                        if (___m_playerGrid.m_gridRoot.transform.GetChild(i)?.Find("binding"))
                        {
                            ___m_playerGrid.m_gridRoot.transform.GetChild(i).Find("binding").GetComponent<TMP_Text>().text = hotkeys[i].Value;
                        }
                    }
                }
                else if (hideNumbers.Value)
                {
                    Dbgl("Switching to Nothing");
                    for (int i = 0; i < 8; i++)
                    {
                        if (___m_playerGrid.m_gridRoot.transform.GetChild(i)?.Find("binding"))
                        {
                            ___m_playerGrid.m_gridRoot.transform.GetChild(i).Find("binding").GetComponent<TMP_Text>().text = "";
                        }
                    }
                }
                else
                {
                    Dbgl("Switching to Numbers");
                    for (int i = 0; i < 8; i++)
                    {
                        if (___m_playerGrid.m_gridRoot.transform.GetChild(i)?.Find("binding"))
                        {
                            ___m_playerGrid.m_gridRoot.transform.GetChild(i).Find("binding").GetComponent<TMP_Text>().text = (i + 1).ToString();
                        }
                    }
                }
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