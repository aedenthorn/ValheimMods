using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace QuickLoad
{
    [BepInPlugin("aedenthorn.QuickLoad", "Quick Load", "0.4.0")]
    public class QuickLoad: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(QuickLoad).Namespace + " " : "") + str);
        }

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> autoLoad;
        public static ConfigEntry<int> nexusID;


        private void Awake()
        {
            hotKey = Config.Bind<string>("General", "HotKey", "f7", "Hot key code to perform quick load.");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            autoLoad = Config.Bind<bool>("General", "AutoLoad", false, "Automatically load into last world");
            nexusID = Config.Bind<int>("General", "NexusID", 7, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(FejdStartup), "Start")]
        static class Start_Patch
        {
            static void Postfix(FejdStartup __instance)
            {
                if (autoLoad.Value)
                {
                    Dbgl("performing auto load");
                    DoQuickLoad();
                }
            }
        }

        [HarmonyPatch(typeof(FejdStartup), "Update")]
        static class Update_Patch
        {
            static void Postfix(FejdStartup __instance)
            {
                if (CheckKeyDown(hotKey.Value))
                {
                    Dbgl("pressed hot key");

                    DoQuickLoad();
                }
            }
        }
        private static void DoQuickLoad()
        {

            string worldName = PlayerPrefs.GetString("world");
            Game.SetProfile(PlayerPrefs.GetString("profile"));

            if (worldName == null || worldName.Length == 0)
                return;

            Dbgl($"got world name {worldName}");

            typeof(FejdStartup).GetMethod("UpdateCharacterList", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(FejdStartup.instance, new object[] { });
            typeof(FejdStartup).GetMethod("UpdateWorldList", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(FejdStartup.instance, new object[] { true });

            bool isOn = FejdStartup.instance.m_publicServerToggle.isOn;
            bool isOn2 = FejdStartup.instance.m_openServerToggle.isOn;
            string text = FejdStartup.instance.m_serverPassword.text;
            World world = (World)typeof(FejdStartup).GetMethod("FindWorld", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(FejdStartup.instance, new object[] { worldName });

            if (world == null)
                return;

            Dbgl($"got world");


            ZNet.SetServer(true, isOn2, isOn, worldName, text, world);
            ZNet.ResetServerHost();

            Dbgl($"Set server");
            try
            {
                string eventLabel = "open:" + isOn2.ToString() + ",public:" + isOn.ToString();
                Gogan.LogEvent("Menu", "WorldStart", eventLabel, 0L);
            }
            catch
            {
                Dbgl($"Error calling Gogan... oh well");
            }

            Dbgl($"transitioning...");

            typeof(FejdStartup).GetMethod("TransitionToMainScene", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(FejdStartup.instance, new object[] { });
        }
    }
}
