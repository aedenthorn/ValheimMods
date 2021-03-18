using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RememberIP
{
    [BepInPlugin("aedenthorn.RememberIP", "Remember IP Address", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<bool> rememberIP;
        public static ConfigEntry<bool> rememberPort;

        public static ConfigEntry<string> lastIPAddress;
        public static ConfigEntry<string> lastPort;

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 0, "Nexus mod ID for updates");

            rememberIP = Config.Bind<bool>("General", "RememberIP", true, "Remember IP address");
            rememberPort = Config.Bind<bool>("General", "RememberPort", true, "Remember port");

            lastIPAddress = Config.Bind<string>("General", "lastIPAddress", "", "Last used IP Address");
            lastPort = Config.Bind<string>("General", "LastPort", "", "Last used port");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(FejdStartup), "OnJoinIPOpen")]
        static class FejdStartup_OnJoinIPOpen_Patch
        {
            static void Postfix(FejdStartup __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (rememberIP.Value)
                {
                    string text = lastIPAddress.Value;
                    if (rememberPort.Value)
                    {
                        text += ":" + lastPort.Value;
                    }
                    __instance.m_joinIPAddress.text = text;
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
                if (text.ToLower().Equals("playermodelswitch reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "player model switch config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
