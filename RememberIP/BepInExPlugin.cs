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
        public static readonly bool isDebug = true;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<bool> rememberIP;
        public static ConfigEntry<bool> rememberPort;

        public static ConfigEntry<string> lastIPAddress;
        public static ConfigEntry<string> lastPort;

        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 572, "Nexus mod ID for updates");

            rememberIP = Config.Bind<bool>("General", "RememberIP", true, "Remember IP address");
            rememberPort = Config.Bind<bool>("General", "RememberPort", true, "Remember port");

            lastIPAddress = Config.Bind<string>("General", "lastIPAddress", "", "Last used IP Address");
            lastPort = Config.Bind<string>("General", "LastPort", "", "Last used port");

            if (!modEnabled.Value)
                return;

            //Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        //[HarmonyPatch(typeof(FejdStartup), "OnJoinIPOpen")]
        public static class FejdStartup_OnJoinIPOpen_Patch
        {
            public static void Postfix(FejdStartup __instance)
            {
                if (!modEnabled.Value)
                    return;

                //__instance.m_joinIPAddress.onValueChanged.RemoveListener(SaveIPAddress);

                string text = "";
                if (rememberIP.Value)
                {
                    text += lastIPAddress.Value;
                }
                if (rememberPort.Value && lastPort.Value.Length > 0)
                {
                    text += ":" + lastPort.Value;
                }
                //__instance.m_joinIPAddress.text = text;

                //__instance.m_joinIPAddress.onValueChanged.AddListener(SaveIPAddress);
            }

        }
        public static void SaveIPAddress(string text)
        {
            if (!modEnabled.Value)
                return;

            string[] splitText = text.Split(':');
            if (rememberIP.Value)
            {
                lastIPAddress.Value = splitText[0];
            }
            if (rememberPort.Value && splitText.Length > 1)
            {
                lastPort.Value = splitText[1];
            }
            context.Config.Save();
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("rememberip reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Remember IP config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
