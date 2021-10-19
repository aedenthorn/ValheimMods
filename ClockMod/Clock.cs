using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using System.IO;

namespace ClockMod
{
    public partial class BepInExPlugin
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
        public static ConfigEntry<TextAnchor> clockTextAlignment;
        public static ConfigEntry<string> clockFuzzyStrings;
        public static ConfigEntry<int> nexusID;

        private static Font clockFont;
        private static GUIStyle style;
        private static GUIStyle style2;
        private static bool configApplied = false;
        private static Vector2 clockPosition;
        private static float shownTime = 0;
        private static string lastTimeString = "";
        private static Rect windowRect;
        private string newTimeString;
        private static Rect timeRect;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void LoadConfig()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            showingClock = Config.Bind<bool>("General", "ShowClock", true, "Show the clock?");
            showClockOnChange = Config.Bind<bool>("General", "ShowClockOnChange", false, "Only show the clock when the time changes?");
            showClockOnChangeFadeTime = Config.Bind<float>("General", "ShowClockOnChangeFadeTime", 5f, "If only showing on change, length in seconds to show the clock before begining to fade");
            showClockOnChangeFadeLength = Config.Bind<float>("General", "ShowClockOnChangeFadeLength", 1f, "How long fade should take in seconds");
            clockUseOSFont = Config.Bind<bool>("General", "ClockUseOSFont", false, "Set to true to specify the name of a font from your OS; otherwise limited to fonts in the game resources");
            clockUseShadow = Config.Bind<bool>("General", "ClockUseShadow", false, "Add a shadow behind the text");
            clockShadowOffset = Config.Bind<int>("General", "ClockShadowOffset", 2, "Shadow offset in pixels");
            clockFontName = Config.Bind<string>("General", "ClockFontName", "AveriaSerifLibre-Bold", "Name of the font to use");
            clockFontSize = Config.Bind<int>("General", "ClockFontSize", 24, "Font size of clock text");
            clockFontColor = Config.Bind<Color>("General", "ClockFontColor", Color.white, "Font color for the clock");
            clockShadowColor = Config.Bind<Color>("General", "ClockShadowColor", Color.black, "Color for the shadow");
            toggleClockKeyMod = Config.Bind<string>("General", "ShowClockKeyMod", "", "Extra modifier key used to toggle the clock display. Leave blank to not require one. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            toggleClockKeyOnPress = Config.Bind<bool>("General", "ShowClockKeyOnPress", false, "If true, limit clock display to when the hotkey is down");
            clockFormat = Config.Bind<string>("General", "ClockFormat", "HH:mm", "Time format; set to 'fuzzy' for fuzzy time");
            clockString = Config.Bind<string>("General", "ClockString", "<b>{0}</b>", "Formatted clock string - {0} is replaced by the actual time string, {1} is replaced by the fuzzy string, {2} is replaced by the current day");
            clockTextAlignment = Config.Bind<TextAnchor>("General", "ClockTextAlignment", TextAnchor.MiddleCenter, "Clock text alignment.");
            clockFuzzyStrings = Config.Bind<string>("General", "ClockFuzzyStrings", "Midnight,Early Morning,Early Morning,Before Dawn,Before Dawn,Dawn,Dawn,Morning,Morning,Late Morning,Late Morning,Midday,Midday,Early Afternoon,Early Afternoon,Afternoon,Afternoon,Evening,Evening,Night,Night,Late Night,Late Night,Midnight", "Fuzzy time strings to split up the day into custom periods if ClockFormat is set to 'fuzzy'; comma-separated");

            newTimeString = "";
            style = new GUIStyle
            {
                richText = true,
                fontSize = clockFontSize.Value,
                alignment = clockTextAlignment.Value,
            };
            style2 = new GUIStyle
            {
                richText = true,
                fontSize = clockFontSize.Value,
                alignment = clockTextAlignment.Value,
            };

            
            
    }
        private void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses() || toggleClockKeyOnPress.Value || !PressedToggleKey())
                return;

            bool show = showingClock.Value;
            showingClock.Value = !show;
            Config.Save();
        }

        private void OnGUI()
        {
            if (modEnabled.Value && configApplied && Player.m_localPlayer && Hud.instance)
            {
                float alpha = 1f;
                newTimeString = GetCurrentTimeString();
                if (showClockOnChange.Value)
                {
                    if (newTimeString == lastTimeString)
                    {
                        shownTime = 0;

                        if (!toggleClockKeyOnPress.Value || !CheckKeyHeld(toggleClockKey.Value))
                            return;
                    }
                    if (shownTime > showClockOnChangeFadeTime.Value)
                    {
                        if (shownTime > showClockOnChangeFadeTime.Value + showClockOnChangeFadeLength.Value)
                        {
                            shownTime = 0;
                            lastTimeString = newTimeString;
                            if (!toggleClockKeyOnPress.Value || !CheckKeyHeld(toggleClockKey.Value))
                                return;
                        }
                        alpha = (showClockOnChangeFadeLength.Value + showClockOnChangeFadeTime.Value - shownTime) / showClockOnChangeFadeLength.Value;
                    }
                    shownTime += Time.deltaTime;
                }
                style.normal.textColor = new Color(clockFontColor.Value.r, clockFontColor.Value.g, clockFontColor.Value.b, clockFontColor.Value.a * alpha);
                style2.normal.textColor = new Color(clockShadowColor.Value.r, clockShadowColor.Value.g, clockShadowColor.Value.b, clockShadowColor.Value.a * alpha);
                if (((!toggleClockKeyOnPress.Value && showingClock.Value) || (toggleClockKeyOnPress.Value && (showClockOnChange.Value || CheckKeyHeld(toggleClockKey.Value)))) && Traverse.Create(Hud.instance).Method("IsVisible").GetValue<bool>())
                {
                    GUI.backgroundColor = Color.clear;
                    windowRect = GUILayout.Window(windowId, new Rect(windowRect.position, timeRect.size), new GUI.WindowFunction(WindowBuilder), "");
                    //Dbgl(""+windowRect.size);
                }
            }
            if (!Input.GetKey(KeyCode.Mouse0) && (windowRect.x != clockPosition.x || windowRect.y != clockPosition.y))
            {
                clockPosition = new Vector2(windowRect.x, windowRect.y);
                clockLocationString.Value = $"{windowRect.x},{windowRect.y}";
                Config.Save();
            }
        }


        private void WindowBuilder(int id)
        {

            timeRect = GUILayoutUtility.GetRect(new GUIContent(newTimeString), style);

            GUI.DragWindow(timeRect);

            if (clockUseShadow.Value)
            {
                GUI.Label(new Rect(timeRect.position + new Vector2(-clockShadowOffset.Value, clockShadowOffset.Value), timeRect.size), newTimeString, style2);
            }
            GUI.Label(timeRect, newTimeString, style);
        }

        private static void ApplyConfig()
        {

            string[] split = clockLocationString.Value.Split(',');
            clockPosition = new Vector2(split[0].Trim().EndsWith("%") ? (float.Parse(split[0].Trim().Substring(0, split[0].Trim().Length - 1)) / 100f) * Screen.width : float.Parse(split[0].Trim()), split[1].Trim().EndsWith("%") ? (float.Parse(split[1].Trim().Substring(0, split[1].Trim().Length - 1)) / 100f) * Screen.height : float.Parse(split[1].Trim()));

            windowRect = new Rect(clockPosition, new Vector2(1000, 100));

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
                alignment = clockTextAlignment.Value,
                font = clockFont
            };
            style2 = new GUIStyle
            {
                richText = true,
                fontSize = clockFontSize.Value,
                alignment = clockTextAlignment.Value,
                font = clockFont
            };

            configApplied = true;
        }

        private string GetCurrentTimeString(DateTime theTime, float fraction, int days)
        {

            string[] fuzzyStringArray = clockFuzzyStrings.Value.Split(',');

            int idx = Math.Min((int)(fuzzyStringArray.Length * fraction), fuzzyStringArray.Length - 1);

            try
            {
                if (clockFormat.Value == "fuzzy")
                    return string.Format(clockString.Value, fuzzyStringArray[idx]);

                return string.Format(clockString.Value, theTime.ToString(clockFormat.Value), fuzzyStringArray[idx], days.ToString());
            }
            catch
            {
                return clockString.Value.Replace("{0}", theTime.ToString(clockFormat.Value)).Replace("{1}", fuzzyStringArray[idx]).Replace("{2}", days.ToString());
            }
        }

        private static string GetFuzzyFileName(string lang)
        {            
            return context.Info.Location.Replace("ClockMod.dll","") + string.Format("clockmod.lang.{0}.cfg",lang);
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
        static class ZNetScene_Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                ApplyConfig();

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
                if (text.ToLower().Equals($"{debugName} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    ApplyConfig();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                if (text.ToLower().Equals($"{debugName} osfonts"))
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
