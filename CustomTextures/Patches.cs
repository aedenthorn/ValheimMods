using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomTextures
{
    public partial class BepInExPlugin
    {

        [HarmonyPatch(typeof(FejdStartup), "SetupObjectDB")]
        public static class FejdStartup_SetupObjectDB_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                stopwatch.Restart();
                outputDump.Clear();

                Dbgl($"SetupObjectDB postfix");

                ReplaceObjectDBTextures();
                LogStopwatch("SetupObjectDB");
            }

        }

        [HarmonyPatch(typeof(ZoneSystem), "Awake")]
        public static class ZoneSystem_Awake_Patch
        {
            public static void Prefix(ZoneSystem __instance)
            {
                outputDump.Clear();
                ReplaceZoneSystemTextures(__instance);

            }
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {
            public static void Postfix(ZNetScene __instance, Dictionary<int, GameObject> ___m_namedPrefabs)
            {
                Dbgl($"ZNetScene awake");

                stopwatch.Restart();

                ReplaceZNetSceneTextures();

                LogStopwatch("ZNetScene");
                //stopwatch.Restart();

                ReplaceEnvironmentTextures();

                //LogStopwatch("ZNetScene 2");
            }
        }

        
        //[HarmonyPatch(typeof(Player), "Start")]
        public static class Player_Start_Patch
        {
            public static void Prefix(Player __instance)
            {
                if (!modEnabled.Value || Player.m_localPlayer != __instance)
                    return;
                Dbgl($"Player Awake");
                ReloadTextures(replaceLocationTextures.Value);
            }
        }


        
        
        [HarmonyPatch(typeof(ClutterSystem), "Awake")]
        public static class ClutterSystem_Awake_Patch
        {
            public static void Postfix(ClutterSystem __instance)
            {
                Dbgl($"Clutter system awake");

                stopwatch.Restart();

                logDump.Clear();

                Dbgl($"Checking {__instance.m_clutter.Count} clutters");
                foreach (ClutterSystem.Clutter clutter in __instance.m_clutter)
                {
                    ReplaceOneGameObjectTextures(clutter.m_prefab, clutter.m_prefab.name, "object");
                }

                if (logDump.Any())
                    Dbgl("\n" + string.Join("\n", logDump));

                LogStopwatch("Clutter System");

            }
        }

        [HarmonyPatch(typeof(ZoneSystem), "Start")]
        public static class ZoneSystem_Start_Patch
        {
            public static void Prefix()
            {
                if (replaceLocationTextures.Value)
                {
                    Dbgl($"Starting ZoneSystem Location prefab replacement");
                    stopwatch.Restart();

                    ReplaceLocationTextures();

                    LogStopwatch("ZoneSystem Locations");
                }

            }
        }
        [HarmonyPatch(typeof(VisEquipment), "Awake")]
        public static class VisEquipment_Awake_Patch
        {
            public static void Postfix(VisEquipment __instance)
            {
                for (int i = 0; i < __instance.m_models.Length; i++)
                {
                    foreach(string property in __instance.m_models[i].m_baseMaterial.GetTexturePropertyNames())
                    {

                        if (ShouldLoadCustomTexture($"player_model_{i}{property}"))
                        {
                            __instance.m_models[i].m_baseMaterial.SetTexture(property, LoadTexture($"player_model_{i}{property}", __instance.m_models[i].m_baseMaterial.GetTexture(property), false));
                            Dbgl($"set player_model_{i}_texture custom texture.");
                        }
                        else if (property == "_MainTex" && ShouldLoadCustomTexture($"player_model_{i}_texture")) // legacy
                        {
                            __instance.m_models[i].m_baseMaterial.SetTexture(property, LoadTexture($"player_model_{i}_texture", __instance.m_models[i].m_baseMaterial.GetTexture(property), false));
                        }
                        else if (property == "_SkinBumpMap" && ShouldLoadCustomTexture($"player_model_{i}_bump")) // legacy
                        {
                            __instance.m_models[i].m_baseMaterial.SetTexture(property, LoadTexture($"player_model_{i}_bump", __instance.m_models[i].m_baseMaterial.GetTexture(property), true));
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), "SetupVisEquipment")]
        public static class Humanoid_SetupVisEquipment_Patch
        {
            public static void Postfix(Humanoid __instance)
            {
                if (!modEnabled.Value)
                    return;
                SetupVisEquipment(__instance);
            }
        }
    }
}
