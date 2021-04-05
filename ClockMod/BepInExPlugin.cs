using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;

namespace ClockMod
{
    [BepInPlugin("aedenthorn.ClockMod", "Clock Mod", "1.4.3")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {

        private static string debugName = "clockmod";
        private static int windowId = 434343;
        private void Awake()
        {
            nexusID = Config.Bind<int>("General", "NexusID", 85, "Nexus mod ID for updates");
            toggleClockKeyMod = Config.Bind<string>("General", "ShowClockKeyMod", "", "Extra modifier key used to toggle the clock display. Leave blank to not require one. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            toggleClockKey = Config.Bind<string>("General", "ShowClockKey", "home", "Key used to toggle the clock display. use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            clockLocationString = Config.Bind<string>("General", "ClockLocationString", "50%,3%", "Location on the screen to show the clock (x,y) or (x%,y%)");

            LoadConfig();

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private string GetCurrentTimeString()
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