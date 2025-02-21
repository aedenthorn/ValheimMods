using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace ClockMod
{
    [BepInPlugin("aedenthorn.ClockMod", "Clock Mod", "1.7.0")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {

        public static string debugName = "clockmod";
        public static int windowId = 434343;
        public void Awake()
        {
            nexusID = Config.Bind<int>("General", "NexusID", 85, "Nexus mod ID for updates");
            toggleClockKeyMod = Config.Bind<string>("General", "ShowClockKeyMod", "", "Extra modifier key used to toggle the clock display. Leave blank to not require one. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            toggleClockKey = Config.Bind<string>("General", "ShowClockKey", "home", "Key used to toggle the clock display. use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            clockLocationString = Config.Bind<string>("General", "ClockLocationString", "50%,3%", "Location on the screen to show the clock (x,y) or (x%,y%)");

            clockLocationString.SettingChanged += ClockLocationString_SettingChanged;

            LoadConfig();

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public void ClockLocationString_SettingChanged(object sender, EventArgs e)
        {
            string[] split = clockLocationString.Value.Split(',');
            clockPosition = new Vector2(split[0].Trim().EndsWith("%") ? (float.Parse(split[0].Trim().Substring(0, split[0].Trim().Length - 1)) / 100f) * Screen.width : float.Parse(split[0].Trim()), split[1].Trim().EndsWith("%") ? (float.Parse(split[1].Trim().Substring(0, split[1].Trim().Length - 1)) / 100f) * Screen.height : float.Parse(split[1].Trim()));

            windowRect = new Rect(clockPosition, new Vector2(1000, 100));
        }

        public string GetCurrentTimeString()
        {
            if (!EnvMan.instance)
                return "";
            float fraction = (float)typeof(EnvMan).GetField("m_smoothDayFraction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(EnvMan.instance);

            int hour = (int)(fraction * 24);
            int minute = (int)((fraction * 24 - hour) * 60);
            int second = (int)((((fraction * 24 - hour) * 60) - minute) * 60);

            DateTime now = DateTime.Now;
            DateTime theTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, second);
            int days = Traverse.Create(EnvMan.instance).Method("GetCurrentDay").GetValue<int>();
            return GetCurrentTimeString(theTime, fraction, days);
        }
    }
}