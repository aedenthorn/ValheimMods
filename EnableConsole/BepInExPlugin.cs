using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace EnableConsole
{
    [BepInPlugin("aedenthorn.EnableConsole", "Enable Console", "0.4.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<int> nexusID;

        public static int itemSize = 48;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            nexusID = Config.Bind<int>("General", "NexusID", 669, "Nexus mod ID for updates");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");


            if (!modEnabled.Value)
                return;
            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(FejdStartup), "Start")]
        public static class FejdStartup_Start_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                Console.SetConsoleEnabledForThisSession();
            }
        }
        [HarmonyPatch(typeof(Console), nameof(Console.SetConsoleEnabled))]
        public static class Console_SetConsoleEnabled_Patch
        {
            public static void Prefix(ref bool enabled)
            {
                if (modEnabled.Value)
                    enabled = true;
            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
