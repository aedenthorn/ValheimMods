using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CombatMusic
{
    [BepInPlugin("aedenthorn.CombatMusic", "Combat Music", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<float> combatVolume;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            //nexusID = Config.Bind<int>("General", "NexusID", 758, "Nexus mod ID for updates");
            
            combatVolume = Config.Bind<float>("Options", "CombatVolume", 1f, "Combat music volume");

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(MusicMan), "HandleSailingMusic")]
        static class MusicMan_HandleSailingMusic_Patch
        {
            static bool Prefix(MusicMan __instance, string currentMusic, ref bool __result)
            {
                if (!modEnabled.Value)
                    return true;

                __instance.m_music.Find(m => m.m_name == "combat").m_volume = combatVolume.Value;

                if ((bool)AccessTools.Method(typeof(MusicMan), "HandleCombatMusic").Invoke(__instance, new object[] { currentMusic }))
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(MusicMan), "StartMusic", new Type[] { typeof(string) })]
        static class MusicMan_StartMusic_Patch
        {
            static void Prefix(MusicMan __instance, string name)
            {
                if (!modEnabled.Value || name != "combat")
                    return;
                __instance.m_music.Find(m => m.m_name == "combat").m_enabled = true;
            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
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
