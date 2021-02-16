using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace QuickLoad
{
    [BepInPlugin("aedenthorn.QuickLoad", "Quick Load", "0.2.0")]
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


        private void Awake()
        {
            hotKey = Config.Bind<string>("General", "HotKey", "f7", "Hot key code to perform quick load.");
            modEnabled = Config.Bind<bool>("General", "enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(FejdStartup), "Update")]
        static class Update_Patch
        {
            static void Postfix(FejdStartup __instance)
            {
                if (!Input.GetKeyDown(hotKey.Value))
                    return;

                Dbgl("pressed hot key");

                string worldName = PlayerPrefs.GetString("world");
                Game.SetProfile(PlayerPrefs.GetString("profile"));

                if (worldName == null || worldName.Length == 0)
                    return;

                Dbgl($"got world name {worldName}");

                typeof(FejdStartup).GetMethod("UpdateCharacterList", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { });
                typeof(FejdStartup).GetMethod("UpdateWorldList", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { true });

                bool isOn = __instance.m_publicServerToggle.isOn;
                bool isOn2 = __instance.m_openServerToggle.isOn;
                string text = __instance.m_serverPassword.text;
                World world = (World)typeof(FejdStartup).GetMethod("FindWorld", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { worldName });
                
                if (world == null)
                    return;

                Dbgl($"got world");

                ZNet.SetServer(true, isOn2, isOn, worldName, text, world);
                ZNet.SetServerHost("", 0);
                string eventLabel = "open:" + isOn2.ToString() + ",public:" + isOn.ToString();
                GoogleAnalyticsV4.instance.LogEvent("Menu", "WorldStart", eventLabel, 0L);

                Dbgl($"transitioning...");

                typeof(FejdStartup).GetMethod("TransitionToMainScene", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { });
            }
        }
    }
}
