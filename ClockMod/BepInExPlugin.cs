using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ClockMod
{
    [BepInPlugin("aedenthorn.ClockMod", "Clock Mod", "0.7.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> showingClock;
        public static ConfigEntry<bool> showClockOnChange;
        public static ConfigEntry<float> showClockOnChangeFadeTime;
        public static ConfigEntry<float> showClockOnChangeFadeLength;
        public static ConfigEntry<bool> toggleClockKeyOnPress;
        public static ConfigEntry<bool> clockUseOSFont;
        public static ConfigEntry<bool> clockUseShadow;
        public static ConfigEntry<Color> clockFontColor;
        public static ConfigEntry<Color> clockShadowColor;
        public static ConfigEntry<int> clockShadowOffset;
        public static ConfigEntry<Vector2> clockLocation;
        public static ConfigEntry<string> clockLocationString;
        public static ConfigEntry<int> clockFontSize;
        public static ConfigEntry<string> toggleClockKeyMod;
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
        private static Vector2 clockPosition;
        private static float shownTime = 0;
        private static string lastTimeString = "";
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
            showClockOnChange = Config.Bind<bool>("General", "ShowClockOnChange", true, "Only show the clock when the time changes?");
            showClockOnChangeFadeTime = Config.Bind<float>("General", "ShowClockOnChangeFadeTime", 5f, "If only showing on change, length in seconds to show the clock before begining to fade");
            showClockOnChangeFadeLength = Config.Bind<float>("General", "ShowClockOnChangeFadeLength", 1f, "How long fade should take in seconds");
            clockLocation = Config.Bind<Vector2>("General", "ClockLocation", new Vector2(Screen.width / 2, 40), "obsolete");
            clockLocationString = Config.Bind<string>("General", "ClockLocationString", "50%,3%", "Location on the screen to show the clock (x,y) or (x%,y%)");
            clockUseOSFont = Config.Bind<bool>("General", "ClockUseOSFont", false, "Set to true to specify the name of a font from your OS; otherwise limited to fonts in the game resources");
            clockUseShadow = Config.Bind<bool>("General", "ClockUseShadow", false, "Add a shadow behind the text");
            clockShadowOffset = Config.Bind<int>("General", "ClockShadowOffset", 2, "Shadow offset in pixels");
            clockFontName = Config.Bind<string>("General", "ClockFontName", "AveriaSerifLibre-Bold", "Name of the font to use");
            clockFontSize = Config.Bind<int>("General", "ClockFontSize", 24, "Location on the screen in pixels to show the clock");
            clockFontColor = Config.Bind<Color>("General", "ClockFontColor", Color.white, "Font color for the clock");
            clockShadowColor = Config.Bind<Color>("General", "ClockShadowColor", Color.black, "Color for the shadow");
            toggleClockKeyMod = Config.Bind<string>("General", "ShowClockKeyMod", "", "Extra modifier key used to toggle the clock display. Leave blank to not require one. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            toggleClockKey = Config.Bind<string>("General", "ShowClockKey", "home", "Key used to toggle the clock display. use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            toggleClockKeyOnPress = Config.Bind<bool>("General", "ShowClockKeyOnPress", false, "If true, limit clock display to when the hotkey is down");
            clockFormat = Config.Bind<string>("General", "ClockFormat", "HH:mm", "Time format; set to 'fuzzy' for fuzzy time");
            clockString = Config.Bind<string>("General", "ClockString", "<b>{0}</b>", "Formatted clock string - {0} is replaced by the actual time string, and {1} is replaced by the fuzzy string (if you want both)");
            clockFuzzyStrings = Config.Bind<string>("General", "ClockFuzzyStrings", "Midnight,Before Dawn,Before Dawn,Dawn,Dawn,Morning,Morning,Late Morning,Late Morning,Midday,Midday,Afternoon,Afternoon,Evening,Evening,Night,Night,Late Night,Late Night,Midnight", "Fuzzy time strings to split up the day into custom periods if ClockFormat is set to 'fuzzy'; comma-separated");
            nexusID = Config.Bind<int>("General", "NexusID", 85, "Nexus mod ID for updates");

            if(clockLocation.Value.y != 40 && clockLocationString.Value == "50%,3%")
            {
                clockLocationString.Value = $"{clockLocation.Value.x},{clockLocation.Value.y}";
                Config.Save();
            }

            if (clockFuzzyStrings.Value == "Night,Early Morning,Morning,Late Morning,Midday,Early Afternoon,Afternoon,Late Afternoon,Early Evening,Evening,Late Evening,Night")
                clockFuzzyStrings.Value = "Midnight,Before Dawn,Before Dawn,Dawn,Dawn,Morning,Morning,Late Morning,Late Morning,Midday,Midday,Afternoon,Afternoon,Evening,Evening,Night,Night,Late Night,Late Night,Midnight";
            Config.Save();
            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
            if (modEnabled.Value && !toggleClockKeyOnPress.Value && PressedToggleKey())
            {
                bool show = showingClock.Value;
                showingClock.Value = !show;
                Config.Save();
                Dbgl($"show clock: {showingClock.Value}");
            }
        }

        private void OnGUI()
        {
            if (modEnabled.Value && Player.m_localPlayer)
            {
                float alpha = 1f;
                string newTimeString = GetCurrentTimeString();
                if (showClockOnChange.Value)
                {
                    if (newTimeString == lastTimeString)
                    {
                        shownTime = 0;
                        return;
                    }
                    if (shownTime > showClockOnChangeFadeTime.Value)
                    {
                        if (shownTime > showClockOnChangeFadeTime.Value + showClockOnChangeFadeLength.Value)
                        {
                            shownTime = 0;
                            lastTimeString = newTimeString;
                            return;
                        }
                        alpha = (showClockOnChangeFadeLength.Value + showClockOnChangeFadeTime.Value - shownTime) / showClockOnChangeFadeLength.Value;
                    }
                    shownTime += Time.deltaTime;
                }
                style.normal.textColor = new Color(clockFontColor.Value.r, clockFontColor.Value.g, clockFontColor.Value.b, clockFontColor.Value.a * alpha); 
                style2.normal.textColor = new Color(clockShadowColor.Value.r, clockShadowColor.Value.g, clockShadowColor.Value.b, clockShadowColor.Value.a * alpha); 
                if ((!toggleClockKeyOnPress.Value && showingClock.Value) || (toggleClockKeyOnPress.Value && CheckKeyHeld(toggleClockKey.Value)))
                {
                    if (clockUseShadow.Value)
                    {

                        GUI.Label(new Rect(clockPosition + new Vector2(-clockShadowOffset.Value, clockShadowOffset.Value), new Vector2(0, 0)), newTimeString, style2);
                    }

                    GUI.Label(new Rect(clockPosition, new Vector2(0, 0)), newTimeString, style);
                }
            }
        }
        private static bool CheckKeyHeld(string value)
        {
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return true;
            }
        }
        private bool PressedToggleKey()
        {
            try
            {
                return Input.GetKeyDown(toggleClockKey.Value.ToLower()) && CheckKeyHeld(toggleClockKeyMod.Value);
            }
            catch
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                ApplyConfig();

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
        private static void ApplyConfig()
        {
            string[] split = clockLocationString.Value.Split(',');
            clockPosition = new Vector2(split[0].Trim().EndsWith("%") ? (float.Parse(split[0].Trim().Substring(0, split[0].Trim().Length - 1)) / 100f) * Screen.width : float.Parse(split[0].Trim()), split[1].Trim().EndsWith("%") ? (float.Parse(split[1].Trim().Substring(0, split[1].Trim().Length - 1)) / 100f) * Screen.height : float.Parse(split[1].Trim()));

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
            style2 = new GUIStyle
            {
                richText = true,
                fontSize = clockFontSize.Value,
                alignment = TextAnchor.MiddleCenter,
                font = clockFont
            };
        }

        private string GetCurrentTimeString()
        {
            float fraction = (float)typeof(EnvMan).GetField("m_smoothDayFraction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(EnvMan.instance);

            int hour = (int)(fraction * 24);
            int minute = (int)((fraction * 24 - hour) * 60);
            int second = (int)((((fraction * 24 - hour) * 60) - minute) * 60);

            DateTime now = DateTime.Now;
            DateTime theTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, second);

            if (!clockString.Value.Contains("{1}") && clockFormat.Value != "fuzzy")
            {
                return string.Format(clockString.Value, theTime.ToString(clockFormat.Value));
            }
            string[] fuzzyStringArray = clockFuzzyStrings.Value.Split(',');
            int idx = Math.Min((int)(fuzzyStringArray.Length * fraction), fuzzyStringArray.Length - 1);

            if(clockFormat.Value == "fuzzy")
                return string.Format(clockString.Value, fuzzyStringArray[idx]);

            return string.Format(clockString.Value, theTime.ToString(clockFormat.Value), fuzzyStringArray[idx]);
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
                    context.Config.Save();
                    ApplyConfig();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "ClockMod config reloaded" }).GetValue();
                    return false;
                }
                if (text.ToLower().Equals("clockmod osfonts"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "OS Fonts dumped to Player.log" }).GetValue();
                    string[] fonts = Font.GetOSInstalledFontNames();
                    foreach (string str in fonts)
                    {
                        Dbgl(str);
                    }
                    return false;
                }
                return true;
            }
        }
    }
}