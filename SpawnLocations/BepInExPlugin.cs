using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SpawnLocations
{
    [BepInPlugin("aedenthorn.SpawnLocations", "Spawn Locations", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            //nexusID = Config.Bind<int>("General", "NexusID", 1113, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(TerrainOp), "OnPlaced")]
        static class OnPlaced_Patch
        {
            static bool Prefix(TerrainOp.Settings ___m_settings)
            {
                //Dbgl($"{___m_settings.m_smooth} {AedenthornUtils.CheckKeyHeld(modKey.Value)}");
                if (!modEnabled.Value || !___m_settings.m_smooth || !AedenthornUtils.CheckKeyHeld(modKey.Value))
                    return true;
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
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} spawn "))
                {
                    string[] parts = text.Split(' ');
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    if(parts.Length == 4 && int.TryParse(parts[3], out int amount))
                    {

                        Traverse.Create(__instance).Method("AddString", new object[] { $"spawned {parts[2]} x{parts[3]}" }).GetValue();
                    }
                    else 
                        Traverse.Create(__instance).Method("AddString", new object[] { "Syntax error." }).GetValue();

                    return false;
                }
                return true;
            }
        }
    }
}
