using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace RemoveBossTrophy
{
    [BepInPlugin("aedenthorn.RemoveBossTrophy", "Remove Boss Trophy", "0.1.0")]
    public class RemoveBossTrophy : BaseUnityPlugin
    {

        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }

        public static RemoveBossTrophy context;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> autoLoad;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;


        public void Awake()
        {
            context = this;
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 2060, "Nexus mod ID for updates");
            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.GetHoverText))]
        public static class ItemStand_GetHoverText_Patch
        {
            public static void Prefix(ItemStand __instance)
            {
                if(!modEnabled.Value || !Player.m_localPlayer)
                    return;
                __instance.m_canBeRemoved = true;
            }
        }
    }
}
