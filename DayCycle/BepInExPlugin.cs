using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace DayCycle
{
    [BepInPlugin("aedenthorn.DayCycle", "DayCycle", "0.7.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> dayStart;
        public static ConfigEntry<float> nightStart;
        public static ConfigEntry<float> dayRate;
        public static ConfigEntry<float> nightRate;
        public static ConfigEntry<int> nexusID;

        private static long vanillaDayLengthSec;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            dayStart = Config.Bind<float>("General", "DayStart", 0.25f, "Fraction of the 24 hours when the day begins");
            nightStart = Config.Bind<float>("General", "NightStart", 0.75f, "Fraction of the 24 hours when the night begins");
            dayRate = Config.Bind<float>("General", "DayRate", 0.5f, "Rate at which the day progresses (0.5 = half speed, etc)");
            nexusID = Config.Bind<int>("General", "NexusID", 98, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            /*
            if(nightStart.Value - dayStart.Value < 0.2)
            {
                Dbgl("Daytime too short, adjusting");
                float diff = 0.2f - (nightStart.Value - dayStart.Value); // 0.99 0.89 = 0.1
                float nightCap = Mathf.Min(0, 1 - (nightStart.Value + diff / 2f)); // 1 - 0.99 - 0.05 = -0.04
                float dayCap = Mathf.Min(0, dayStart.Value - diff / 2f); // 0.89 - 0.05 = 0 clamped
                nightStart.Value = Mathf.Min(1, nightStart.Value + diff / 2f - dayCap); // 1 clamped
                dayStart.Value = Mathf.Max(0, dayStart.Value - diff / 2f + nightCap); // 0.89 - 0.5 +(-0.04) = 0.8
                Config.Save();
            }
            */

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }



        [HarmonyPatch(typeof(EnvMan), "Awake")]
        static class EnvMan_Awake_Patch
        {

            static void Postfix(ref long ___m_dayLengthSec)
            {
                if (!modEnabled.Value)
                    return;
                vanillaDayLengthSec = ___m_dayLengthSec;
                ___m_dayLengthSec = (long)(Mathf.Round(vanillaDayLengthSec / dayRate.Value));
            }
        }

        [HarmonyPatch(typeof(EnvMan), "IsDay")]
        static class EnvMan_IsDay_Patch
        {
            static bool Prefix(float ___m_smoothDayFraction, ref bool __result)
            {
                if (!modEnabled.Value)
                    return true;
                __result = ___m_smoothDayFraction >= dayStart.Value && ___m_smoothDayFraction <= nightStart.Value;
                return false;
            }
        }
        [HarmonyPatch(typeof(EnvMan), "IsAfternoon")]
        static class EnvMan_IsAfternoon_Patch
        {
            static bool Prefix(float ___m_smoothDayFraction, ref bool __result)
            {
                if (!modEnabled.Value)
                    return true;
                __result = ___m_smoothDayFraction >= 0.5f && ___m_smoothDayFraction < nightStart.Value;
                return false;
            }
        }
        [HarmonyPatch(typeof(EnvMan), "IsNight")]
        static class EnvMan_IsNight_Patch
        {
            static bool Prefix(float ___m_smoothDayFraction, ref bool __result)
            {
                if (!modEnabled.Value)
                    return true;
                __result = ___m_smoothDayFraction <= dayStart.Value || ___m_smoothDayFraction >= nightStart.Value;
                return false;
            }
        }

        [HarmonyPatch(typeof(EnvMan), "SetEnv")]
        static class EnvMan_SetEnv_Patch
        {
            static void Prefix(EnvMan __instance, ref float dayInt, ref float nightInt, ref float morningInt, ref float eveningInt, float ___m_smoothDayFraction)
            {
                if (!modEnabled.Value)
                    return;
                nightInt = Mathf.Pow(Mathf.Max(1f - Mathf.Clamp01(___m_smoothDayFraction / dayStart.Value), Mathf.Clamp01((___m_smoothDayFraction - nightStart.Value) / dayStart.Value)), 0.5f);
                dayInt = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(___m_smoothDayFraction - 0.5f) / dayStart.Value), 0.5f);
                morningInt = Mathf.Min(Mathf.Clamp01(1f - (___m_smoothDayFraction - (dayStart.Value + 0.01f)) / -__instance.m_sunHorizonTransitionL), Mathf.Clamp01(1f - (___m_smoothDayFraction - (dayStart.Value + 0.01f)) / __instance.m_sunHorizonTransitionH));
                eveningInt = Mathf.Min(Mathf.Clamp01(1f - (___m_smoothDayFraction - (nightStart.Value - 0.01f)) / -__instance.m_sunHorizonTransitionH), Mathf.Clamp01(1f - (___m_smoothDayFraction - (nightStart.Value - 0.01f)) / __instance.m_sunHorizonTransitionL));
                float num9 = 1f / (nightInt + dayInt + morningInt + eveningInt);
                nightInt *= num9;
                dayInt *= num9;
                morningInt *= num9;
                eveningInt *= num9;
            }
        }

        //[HarmonyPatch(typeof(ZNet), "GetWrappedDayTimeSeconds")]
        static class GetWrappedDayTimeSeconds_Patch
        {

            static bool Prefix(double ___m_netTime, ref float __result)
            {
                int fullDays = (int)(___m_netTime / 86400);
                __result = fullDays * (dayStart.Value + (1 - nightStart.Value)) * 86400 * nightRate.Value + fullDays * (nightStart.Value - dayStart.Value) * 86400 * dayRate.Value;
                float seconds = (float)(___m_netTime % 86400);
                if(seconds > 86400 * nightStart.Value)
                {
                    __result += 86400 * dayStart.Value * nightRate.Value + 86400 * (nightStart.Value - dayStart.Value) * dayRate.Value + (seconds - 86400 * nightStart.Value) * nightRate.Value;

                }
                else if(seconds > 86400 * dayStart.Value)
                {
                    __result += 86400 * dayStart.Value * nightRate.Value + (seconds - 86400 * dayStart.Value) * dayRate.Value;
                }
                else
                {
                    __result += seconds * nightRate.Value;
                }
                return false;
            }
        }
        //[HarmonyPatch(typeof(EnvMan), "RescaleDayFraction")]
        static class RescaleDayFraction_Patch
        {

            static void Postfix(ref float fraction, ref float ___m_smoothDayFraction)
            {
                float newFraction = 0;
                if (fraction > nightStart.Value)
                {
                    newFraction = dayStart.Value * nightRate.Value + (nightStart.Value - dayStart.Value) * dayRate.Value + (fraction - nightStart.Value) * nightRate.Value;

                }
                else if (fraction > dayStart.Value)
                {
                    newFraction += dayStart.Value * nightRate.Value + (fraction - dayStart.Value) * dayRate.Value;
                }
                else
                {
                    newFraction += fraction * nightRate.Value;
                }
                fraction = newFraction;
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
                if (text.ToLower().Equals("daycycle reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(EnvMan.instance).Field("m_dayLengthSec").SetValue((long)Mathf.Round(vanillaDayLengthSec / dayRate.Value));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Day Cycle config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
