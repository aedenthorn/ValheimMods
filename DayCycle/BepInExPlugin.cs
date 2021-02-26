using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace DayCycle
{
    [BepInPlugin("aedenthorn.DayCycle", "DayCycle", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> untieSmeltTimes;
        public static ConfigEntry<float> dayStart;
        public static ConfigEntry<float> nightStart;
        public static ConfigEntry<float> dayRate;
        public static ConfigEntry<float> nightRate;
        public static ConfigEntry<int> nexusID;

        private static float currentRate;
        
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            untieSmeltTimes = Config.Bind<bool>("General", "UntieCraftTimes", true, "Make smelting times independent from time scale changes");
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
                float fraction = (float)typeof(EnvMan).GetField("m_smoothDayFraction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(EnvMan.instance);
                if (fraction > dayStart.Value && fraction < nightStart.Value)
                {
                    dt *= dayRate.Value;
                    currentRate = dayRate.Value;
                }
                else
                {
                    dt *= nightRate.Value;
                    currentRate = nightRate.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Smelter), "GetAccumulator")]
        static class Smelter_GetAccumulator_Patch
        {
            static void Postfix(ref float __result)
            {
                if (__result < 0)
                    __result = 0;
            }
        }

        [HarmonyPatch(typeof(Smelter), "GetDeltaTime")]
        static class Smelter_GetDeltaTime_Patch
        {

            static bool Prefix(ZNetView ___m_nview, ref double __result)
            {
                if (!untieSmeltTimes.Value)
                    return true;


                DateTime thisTime = ZNet.instance.GetTime();
                DateTime startTime = new DateTime(___m_nview.GetZDO().GetLong("StartTime", thisTime.Ticks));

                double newTotalSeconds = 0;
                int secondsPerDay = 60 * 60 * 24;

                double startTimeOfDayFraction = (startTime - startTime.Date).TotalSeconds  / (double)secondsPerDay;
                double thisTimeOfDayFraction = (thisTime - thisTime.Date).TotalSeconds / (double)(secondsPerDay);
                double oneDay = (nightStart.Value - dayStart.Value) / dayRate.Value + (1 - (nightStart.Value - dayStart.Value)) / nightRate.Value;

                if (thisTime.Date.CompareTo(startTime.Date) != 0)
                {
                    // modify first day


                    if (startTimeOfDayFraction < dayStart.Value)
                        newTotalSeconds += secondsPerDay * (dayStart.Value - startTimeOfDayFraction) / nightRate.Value + secondsPerDay * (nightStart.Value - dayStart.Value) / dayRate.Value + secondsPerDay * (1 - nightStart.Value) / nightRate.Value;
                    else if (startTimeOfDayFraction < nightStart.Value)
                        newTotalSeconds += secondsPerDay * (nightStart.Value - startTimeOfDayFraction) / dayRate.Value + secondsPerDay * (1 - nightStart.Value) / nightRate.Value;
                    else
                        newTotalSeconds += secondsPerDay * (1 - startTimeOfDayFraction) / nightRate.Value;


                    // modify full days  10 - 5

                    int days = (thisTime.Date - startTime.Date).Days - 1;

                    newTotalSeconds += days * oneDay * secondsPerDay;

                    // modify this day

                    if (thisTimeOfDayFraction > nightStart.Value)
                        newTotalSeconds += secondsPerDay * (thisTimeOfDayFraction - nightStart.Value) / nightRate.Value + secondsPerDay * (nightStart.Value - dayStart.Value) / dayRate.Value + secondsPerDay * (dayStart.Value) / nightRate.Value;
                    else if (thisTimeOfDayFraction > dayStart.Value)
                        newTotalSeconds += secondsPerDay * (thisTimeOfDayFraction - dayStart.Value) / dayRate.Value + secondsPerDay * (dayStart.Value) / nightRate.Value;
                    else
                        newTotalSeconds += secondsPerDay * thisTimeOfDayFraction / nightRate.Value;

                }
                else if(thisTime.Ticks == startTime.Ticks)
                {
                    // less than one tick
                    //Dbgl($"less than one tick passed");
                    startTime.AddTicks(-1);
                    newTotalSeconds = (thisTime - startTime).TotalSeconds;
                }
                else
                {
                    newTotalSeconds = oneDay * secondsPerDay;

                   // Dbgl($"start {startTimeOfDayFraction}, now {thisTimeOfDayFraction}, one day {secondsPerDay} => {oneDay * secondsPerDay}");

                    // subtract seconds in start time

                    if (startTimeOfDayFraction > nightStart.Value)
                        newTotalSeconds -= secondsPerDay * (startTimeOfDayFraction - nightStart.Value) / nightRate.Value + secondsPerDay * (nightStart.Value - dayStart.Value) / dayRate.Value + secondsPerDay * (dayStart.Value) / nightRate.Value;
                    else if (startTimeOfDayFraction > dayStart.Value)
                        newTotalSeconds -= secondsPerDay * (startTimeOfDayFraction - dayStart.Value) / dayRate.Value + secondsPerDay * (dayStart.Value) / nightRate.Value;
                    else
                        newTotalSeconds -= secondsPerDay * startTimeOfDayFraction / nightRate.Value;

                    //Dbgl($"lost from startTime {oneDay * secondsPerDay - newTotalSeconds}");

                    // subtract seconds left in day

                    if (thisTimeOfDayFraction < dayStart.Value)
                        newTotalSeconds -= secondsPerDay * (dayStart.Value - thisTimeOfDayFraction) / nightRate.Value + secondsPerDay * (nightStart.Value - dayStart.Value) / dayRate.Value + secondsPerDay * (1 - nightStart.Value) / nightRate.Value;
                    else if (thisTimeOfDayFraction < nightStart.Value)
                        newTotalSeconds -= secondsPerDay * (nightStart.Value - thisTimeOfDayFraction) / dayRate.Value + secondsPerDay * (1 - nightStart.Value) / nightRate.Value;
                    else
                        newTotalSeconds -= secondsPerDay * (1 - thisTimeOfDayFraction) / nightRate.Value;

                    //Dbgl($"lost from starttime and nowtime {oneDay * secondsPerDay - newTotalSeconds}");
                }

                __result = Math.Max(1, newTotalSeconds);
                //Dbgl($"time passed: {__result}");
                ___m_nview.GetZDO().Set("StartTime", thisTime.Ticks);
                return false;
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
                    return false;
                }
                return true;
            }
        }
    }
}
