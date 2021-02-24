using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DayCycle
{
    [BepInPlugin("aedenthorn.DayCycle", "DayCycle", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> dayStart;
        public static ConfigEntry<float> nightStart;
        public static ConfigEntry<float> dayRate;
        public static ConfigEntry<float> nightRate;
        public static ConfigEntry<int> nexusID;

        public static float lastTime = 0;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            dayStart = Config.Bind<float>("General", "DayStart", 0.25f, "Fraction of the 24 hours when the day begins");
            nightStart = Config.Bind<float>("General", "NightStart", 0.75f, "Fraction of the 24 hours when the night begins");
            dayRate = Config.Bind<float>("General", "DayRate", 0.5f, "Rate at which the day progresses (0.5 = half speed, etc)");
            nightRate = Config.Bind<float>("General", "NightRate", 1f, "Rate at which the night progresses (0.5 = half speed, etc)");
            nexusID = Config.Bind<int>("General", "NexusID", 98, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        [HarmonyPatch(typeof(ZNet), "UpdateNetTime")]
        static class UpdateNetTime_Patch
        {
            static void Prefix(ref float dt)
            {
                float fraction = AccessTools.FieldRefAccess<EnvMan, float>(EnvMan.instance, "m_smoothDayFraction");
                if (fraction > dayStart.Value && fraction < nightStart.Value)
                {
                    dt *= dayRate.Value;
                }
                else
                {
                    dt *= nightRate.Value;
                }

            }
        }
    }
}
