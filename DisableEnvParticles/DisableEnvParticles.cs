using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DisableEnvParticles
{
    [BepInPlugin("aedenthorn.DisableEnvParticles", "Disable Env Particles", "0.1.1")]
    public class DisableEnvParticles: BaseUnityPlugin
    {

        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }

        private static DisableEnvParticles context;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> autoLoad;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;


        private void Awake()
        {
            context = this;
            hotKey = Config.Bind<string>("General", "HotKey", "f7", "Hot key code to perform quick load.");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 2060, "Nexus mod ID for updates");
            nexusID.Value = 2060;
            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(EnvMan), "SetParticleArrayEnabled")]
        static class EnvMan_SetParticleArrayEnabled_Patch
        {
            static void Prefix(GameObject[] psystems, ref bool enabled)
            {
                if(!modEnabled.Value)
                    return;
                enabled = false;
            }
        }
    }
}
