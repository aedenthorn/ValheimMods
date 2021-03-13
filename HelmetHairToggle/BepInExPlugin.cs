using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace HelmetHairToggle
{
    [BepInPlugin("aedenthorn.HelmetHairToggle", "Helmet Hair Toggle", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<string> toggleString;
        
        public static ConfigEntry<bool> isOn;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            toggleString = Config.Bind<string>("General", "ToggleString", "Show hair with helmet: {0}", "Text to show on toggle. {0} is replaced with true/false");
            toggleKey = Config.Bind<string>("General", "ToggleKey", "h", "Key to toggle behaviour. Leave blank to disable the toggle key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            isOn = Config.Bind<bool>("General", "IsOn", false, "Behaviour is currently on or not");
            
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 470, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses())
                return;
            if (AedenthornUtils.CheckKeyDown(toggleKey.Value))
            {
                isOn.Value = !isOn.Value;
                Config.Save();
                if(toggleString.Value.Length > 0)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(toggleString.Value, isOn.Value), 0, null);

                VisEquipment ve = (VisEquipment)typeof(Humanoid).GetField("m_visEquipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Player.m_localPlayer);
                Traverse.Create(ve).Field("m_helmetHideHair").SetValue(!isOn.Value);
                GameObject helmet = Traverse.Create(ve).Field("m_helmetItemInstance").GetValue<GameObject>();
                if (helmet != null)
                {
                    Traverse.Create(ve).Method("UpdateEquipmentVisuals").GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(VisEquipment), "HelmetHidesHair")]
        static class HelmetHidesHair_Patch
        {
            static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value || !isOn.Value)
                    return true;
                __result = false;
                return false;
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
                if (text.ToLower().Equals("hairtoggle reset"))
                {
                    context.Config.Reload();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Hair toggle config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
