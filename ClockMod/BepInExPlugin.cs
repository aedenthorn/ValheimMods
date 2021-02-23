using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = System.Object;

namespace ClockMod
{
    [BepInPlugin("aedenthorn.ClockMod", "Clock Mod", "0.2.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> showingClock;
        public static ConfigEntry<bool> toggleClockKeyOnPress;
        public static ConfigEntry<bool> clockUseOSFont;
        public static ConfigEntry<Vector2> clockLocation;
        public static ConfigEntry<Color> clockFontColor;
        public static ConfigEntry<int> clockFontSize;
        public static ConfigEntry<string> toggleClockKey;
        public static ConfigEntry<string> clockFontName;
        public static ConfigEntry<string> clockFormat;
        public static ConfigEntry<string> clockString;
        public static ConfigEntry<string> clockFuzzyStrings;
        public static Text clockText;
        private static Font clockFont;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            showingClock = Config.Bind<bool>("General", "ShowClock", true, "Show the clock?");
            clockLocation = Config.Bind<Vector2>("General", "ClockLocation", new Vector2(Screen.width / 2, 80), "Location on the screen in pixels to show the clock");
            clockUseOSFont = Config.Bind<bool>("General", "ClockUseOSFont", false, "Set to true to specify the name of a font from your OS; otherwise limited to fonts in the game resources");
            clockFontName = Config.Bind<string>("General", "ClockFontName", "AveriaSerifLibre-Bold", "Name of the font to use");
            clockFontSize = Config.Bind<int>("General", "ClockFontSize", 24, "Location on the screen in pixels to show the clock");
            clockFontColor = Config.Bind<Color>("General", "ClockFontColor", Color.white, "Font color for the clock");
            toggleClockKey = Config.Bind<string>("General", "ShowClockKey", "home", "Key used to toggle the clock display");
            toggleClockKeyOnPress = Config.Bind<bool>("General", "ShowClockKeyOnPress", false, "If true, limit clock display to when the hotkey is down");
            clockFormat = Config.Bind<string>("General", "ClockFormat", "HH:mm", "Time format; set to 'fuzzy' for fuzzy time");
            clockString = Config.Bind<string>("General", "ClockString", "<b>{0}</b>", "Formatted clock string - {0} is replaced by the actual time string");
            clockFuzzyStrings = Config.Bind<string>("General", "ClockFuzzyStrings", "Night,Early Morning,Morning,Late Morning,Midday,Early Afternoon,Afternoon,Late Afternoon,Early Evening,Evening,Late Evening,Night", "Fuzzy time strings to split up the day into custom periods if ClockFormat is set to 'fuzzy'; comma-separated");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        [HarmonyPatch(typeof(FejdStartup), "Awake")]
        static class Awake_Patch
        {
            static void Postfix(FejdStartup __instance)
            {
                if (clockUseOSFont.Value)
                    clockFont = Font.CreateDynamicFontFromOSFont(clockFontName.Value, clockFontSize.Value);
                else
                {
                    Debug.Log($"getting fonts");
                    Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                    foreach (Font font in fonts)
                    {
                        if(font.name == clockFontName.Value)
                        {
                            clockFont = font;
                            Debug.Log($"got font {font.name}");
                            break;
                        }
                    }
                }
            }
        }
        private void Update()
        {
            if (!toggleClockKeyOnPress.Value && Input.GetKeyDown(toggleClockKey.Value))
            {
                bool show = showingClock.Value;
                showingClock.Value = !show;
                Dbgl($"show clock: {showingClock.Value}");
            }
        }
        private void OnGUI()
        {
            if (modEnabled.Value && Player.m_localPlayer)
            {
                if ((!toggleClockKeyOnPress.Value && showingClock.Value) || (toggleClockKeyOnPress.Value && Input.GetKey(toggleClockKey.Value)))
                {
                    GUIStyle style = new GUIStyle
                    {
                        richText = true,
                        fontSize = clockFontSize.Value,
                        alignment = TextAnchor.MiddleCenter,
                        font = clockFont
                    };
                    style.normal.textColor = clockFontColor.Value;
                
                    GUI.Label(new Rect(clockLocation.Value, new Vector2(0,0)), $"{string.Format(clockString.Value, GetCurrentTimeString())}", style);
                }
            }
        }

        private string GetCurrentTimeString()
        {
            float fraction = AccessTools.FieldRefAccess<EnvMan, float>(EnvMan.instance, "m_smoothDayFraction");

            if (clockFormat.Value != "fuzzy")
            {

                int hour = (int)(fraction * 24);
                int minute = (int)((fraction * 24 - hour) * 60);
                int second = (int)((((fraction * 24 - hour) * 60) - minute) * 60);

                DateTime now = DateTime.Now;
                DateTime theTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, second);

                return theTime.ToString(clockFormat.Value);
            }
            string[] fuzzyStringArray = clockFuzzyStrings.Value.Split(',');
            int idx = Math.Min((int)(fuzzyStringArray.Length * fraction), fuzzyStringArray.Length - 1);
            return fuzzyStringArray[idx];
        }

    }
}