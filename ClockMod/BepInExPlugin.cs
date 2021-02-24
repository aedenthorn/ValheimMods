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
    [BepInPlugin("aedenthorn.ClockMod", "Clock Mod", "0.4.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> showingClock;
        public static ConfigEntry<bool> toggleClockKeyOnPress;
        public static ConfigEntry<bool> clockUseOSFont;
        public static ConfigEntry<bool> clockUseShadow;
        public static ConfigEntry<Color> clockFontColor;
        public static ConfigEntry<Color> clockShadowColor;
        public static ConfigEntry<int> clockShadowOffset;
        public static ConfigEntry<Vector2> clockLocation;
        public static ConfigEntry<int> clockFontSize;
        public static ConfigEntry<string> toggleClockKey;
        public static ConfigEntry<string> clockFontName;
        public static ConfigEntry<string> clockFormat;
        public static ConfigEntry<string> clockString;
        public static ConfigEntry<string> clockFuzzyStrings;
        public static ConfigEntry<int> nexusID;

        public static Text text;
        private static Font clockFont;
        private static GameObject canvasGO;
        private static GUIStyle style;
        private static GUIStyle style2;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            showingClock = Config.Bind<bool>("General", "ShowClock", true, "Show the clock?");
            clockLocation = Config.Bind<Vector2>("General", "ClockLocation", new Vector2(Screen.width / 2, 40), "Location on the screen in pixels to show the clock");
            clockUseOSFont = Config.Bind<bool>("General", "ClockUseOSFont", false, "Set to true to specify the name of a font from your OS; otherwise limited to fonts in the game resources");
            clockUseShadow = Config.Bind<bool>("General", "ClockUseShadow", false, "Add a shadow behind the text");
            clockShadowOffset = Config.Bind<int>("General", "ClockShadowOffset", 2, "Shadow offset in pixels");
            clockFontName = Config.Bind<string>("General", "ClockFontName", "AveriaSerifLibre-Bold", "Name of the font to use");
            clockFontSize = Config.Bind<int>("General", "ClockFontSize", 24, "Location on the screen in pixels to show the clock");
            clockFontColor = Config.Bind<Color>("General", "ClockFontColor", Color.white, "Font color for the clock");
            clockShadowColor = Config.Bind<Color>("General", "ClockShadowColor", Color.black, "Color for the shadow");
            toggleClockKey = Config.Bind<string>("General", "ShowClockKey", "home", "Key used to toggle the clock display");
            toggleClockKeyOnPress = Config.Bind<bool>("General", "ShowClockKeyOnPress", false, "If true, limit clock display to when the hotkey is down");
            clockFormat = Config.Bind<string>("General", "ClockFormat", "HH:mm", "Time format; set to 'fuzzy' for fuzzy time");
            clockString = Config.Bind<string>("General", "ClockString", "<b>{0}</b>", "Formatted clock string - {0} is replaced by the actual time string");
            clockFuzzyStrings = Config.Bind<string>("General", "ClockFuzzyStrings", "Night,Early Morning,Morning,Late Morning,Midday,Early Afternoon,Afternoon,Late Afternoon,Early Evening,Evening,Late Evening,Night", "Fuzzy time strings to split up the day into custom periods if ClockFormat is set to 'fuzzy'; comma-separated");
            nexusID = Config.Bind<int>("General", "NexusID", 85, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;



            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
            if (modEnabled.Value && !toggleClockKeyOnPress.Value && Input.GetKeyDown(toggleClockKey.Value))
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
                    if (clockUseShadow.Value)
                    {

                        GUI.Label(new Rect(clockLocation.Value + new Vector2(-clockShadowOffset.Value, clockShadowOffset.Value), new Vector2(0, 0)), $"{string.Format(clockString.Value, GetCurrentTimeString())}", style2);
                    }

                    GUI.Label(new Rect(clockLocation.Value, new Vector2(0, 0)), $"{string.Format(clockString.Value, GetCurrentTimeString())}", style);
                }
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
                if (text.ToLower().Equals("clockmod reset"))
                {
                    context.Config.Reload();
                    SetStyles();
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                SetStyles();

                /*
                // Load the Arial font from the Unity Resources folder.
                Font arial;
                arial = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

                // Create Canvas GameObject.
                canvasGO = new GameObject();
                canvasGO.name = "ClockObject";
                canvasGO.AddComponent<Canvas>();
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                Instantiate(canvasGO, new Vector3(0, -5, 0), Quaternion.identity);

                // Get canvas from the GameObject.
                Canvas canvas;
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                // Create the Text GameObject.
                GameObject textGO = new GameObject();
                textGO.transform.parent = canvasGO.transform;
                textGO.AddComponent<Text>();

                // Set Text component properties.
                text = textGO.GetComponent<Text>();
                text.font = arial;
                text.text = "Press space key";
                text.fontSize = 48;
                text.alignment = TextAnchor.MiddleCenter;

                // Provide Text position and size using RectTransform.
                RectTransform rectTransform;
                rectTransform = text.GetComponent<RectTransform>();
                rectTransform.localPosition = new Vector3(0, 0, 0);
                rectTransform.sizeDelta = new Vector2(600, 200);
                */
            }


        }
        private static void SetStyles()
        {
            if (clockUseOSFont.Value)
                clockFont = Font.CreateDynamicFontFromOSFont(clockFontName.Value, clockFontSize.Value);
            else
            {
                Debug.Log($"getting fonts");
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                foreach (Font font in fonts)
                {
                    if (font.name == clockFontName.Value)
                    {
                        clockFont = font;
                        Debug.Log($"got font {font.name}");
                        break;
                    }
                }
            }
            style = new GUIStyle
            {
                richText = true,
                fontSize = clockFontSize.Value,
                alignment = TextAnchor.MiddleCenter,
                font = clockFont
            };
            style.normal.textColor = clockFontColor.Value;
            style2 = new GUIStyle
            {
                richText = true,
                fontSize = clockFontSize.Value,
                alignment = TextAnchor.MiddleCenter,
                font = clockFont
            };
            style2.normal.textColor = clockShadowColor.Value;
        }

        private string GetCurrentTimeString()
        {
            float fraction = (float)typeof(EnvMan).GetField("m_smoothDayFraction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(EnvMan.instance);

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