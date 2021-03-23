using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EnableConsole
{
    [BepInPlugin("aedenthorn.EnableConsole", "Enable Console", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<int> nexusID;

        public static int itemSize = 48;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            nexusID = Config.Bind<int>("General", "NexusID", 669, "Nexus mod ID for updates");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");


            if (!modEnabled.Value)
                return;
            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(FejdStartup), "Start")]
        static class FejdStartup_Start_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                Console.SetConsoleEnabled(true);
            }
        }
    }
}
