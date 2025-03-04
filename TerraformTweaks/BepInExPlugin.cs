using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TerraformTweaks
{
    [BepInPlugin("aedenthorn.TerraformTweaks", "Terraform Tweaks", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> modKey;
        
        public static ConfigEntry<float> levelRadius;
        public static ConfigEntry<float> smoothRadius;
        public static ConfigEntry<float> paintRadius;
        public static ConfigEntry<float> smoothPower;

        public static BepInExPlugin context;
        public static List<string> typeEnums = new List<string>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            
            foreach (int i in Enum.GetValues(typeof(ItemDrop.ItemData.ItemType)))
            {
                typeEnums.Add(Enum.GetName(typeof(ItemDrop.ItemData.ItemType), i));
            }

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1068, "Nexus mod ID for updates");
            modKey = Config.Bind<string>("General", "ModKey", "left ctrl", "Mod key to change radius");

            levelRadius = Config.Bind<float>("Values", "LevelRadius", 2f, "Level radius");
            smoothRadius = Config.Bind<float>("Values", "SmoothRadius", 2f, "Smooth radius");
            paintRadius = Config.Bind<float>("Values", "PaintRadius", 2f, "Paint radius");
            smoothPower = Config.Bind<float>("Values", "SmoothPower", 3f, "Smooth power");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(TerrainModifier), "Awake")]
        public static class TerrainModifier_Patch
        {
            public static void Prefix(TerrainModifier __instance)
            {
                SetVariables(__instance);
            }
        }

        //[HarmonyPatch(typeof(TerrainModifier), "OnDrawGizmosSelected")]
        public static class OnDrawGizmosSelected_Patch
        {
            public static void Prefix(TerrainModifier __instance)
            {
                SetVariables(__instance);
            }
        }

        //[HarmonyPatch(typeof(TerrainModifier), "GetRadius")]
        public static class GetRadius_Patch
        {
            public static void Prefix(TerrainModifier __instance)
            {
                SetVariables(__instance);
            }
        }

        public static void SetVariables(TerrainModifier __instance)
        {
            if (!modEnabled.Value)
                return;
            __instance.m_smoothRadius = smoothRadius.Value;
            __instance.m_smoothPower = smoothPower.Value;
            __instance.m_levelRadius = levelRadius.Value;
            __instance.m_paintRadius = paintRadius.Value;
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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
