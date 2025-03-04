using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace EitrRegenRate
{
    [BepInPlugin("aedenthorn.EitrRegenRate", "EitrRegenRate", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<float> regenRate;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            regenRate = Config.Bind<float>("Options", "RegenRate", 5f, "Eitr regeneration rate");

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 25, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        [HarmonyPatch(typeof(Player), "UpdateStats")]
        public static class UpdateFood_Patch
        {
            public static void Prefix(ref float ___m_eiterRegen)
            {
                if (modEnabled.Value)
                {
                    ___m_eiterRegen = regenRate.Value;
                }
            }
        }
    }
}
