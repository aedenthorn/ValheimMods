using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace QuickLoad
{
    public class Main
    {
        private static readonly bool isDebug = true;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(Main).Namespace + " " : "") + str);
        }

        public static Settings settings { get; private set; }
        public static bool enabled;
        private static void Load(UnityModManager.ModEntry modEntry)
        {
            settings = Settings.Load<Settings>(modEntry);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnToggle = OnToggle;            

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return;
        }

        // Called when the mod is turned to on/off.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            enabled = value;
            return true; // Permit or not.
        }
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
        }

        [HarmonyPatch(typeof(FejdStartup), "Update")]
        static class Update_Patch
        {
            static void Postfix(FejdStartup __instance)
            {
                if (!Input.GetKeyDown(settings.HotKey))
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
