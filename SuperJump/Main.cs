using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace SuperJump
{
    public class Main
    {
        private static readonly bool isDebug = true;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(Main).Namespace + " " : "") + str);
        }

        public static int JumpNumber { get; private set; }

        public static Settings settings { get; private set; }
        public static bool enabled;
        private static void Load(UnityModManager.ModEntry modEntry)
        {
            settings = Settings.Load<Settings>(modEntry);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnToggle = OnToggle;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return;
        }

        // Called when the mod is turned to on/off.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            enabled = value;
            return true; // Permit or not.
        }
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label(string.Format("Maximum Jumps: <b>{0}</b>", settings.MaxJumps), new GUILayoutOption[0]);
            settings.MaxJumps = (int)GUILayout.HorizontalSlider(settings.MaxJumps, -1f, 10f, new GUILayoutOption[0]);
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        static class Jump_Patch
        {

            static void Prefix(Character __instance)
            {
                if (enabled)
                {
                    if (JumpNumber > 0 && __instance.IsOnGround())
                    {
                        JumpNumber = 0;
                    }

                    if (settings.MaxJumps < 0 || JumpNumber < settings.MaxJumps)
                    {
                        typeof(Character).GetField("m_lastGroundTouch", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, 0.1f);
                        JumpNumber++;
                    }
                }
            }
        }
    }
}
