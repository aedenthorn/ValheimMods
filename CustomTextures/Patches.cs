using HarmonyLib;
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
        static class FejdStartup_SetupObjectDB_Patch
        {
            static void Postfix()
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
        static class ZoneSystem_Awake_Patch
        {
            static void Prefix(ZoneSystem __instance)
            {
                outputDump.Clear();
                ReplaceZoneSystemTextures(__instance);

            }
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix(ZNetScene __instance, Dictionary<int, GameObject> ___m_namedPrefabs)
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


        
        
        [HarmonyPatch(typeof(ClutterSystem), "Awake")]
        static class ClutterSystem_Awake_Patch
        {
            static void Postfix(ClutterSystem __instance)
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
        static class ZoneSystem_Start_Patch
        {
            static void Prefix()
            {
                if (replaceLocationTextures.Value)
                {
                    Dbgl($"Starting ZoneSystem Location prefab replacement");
                    stopwatch.Restart();

                    ReplaceLocationTextures();

                    LogStopwatch("ZoneSystem Locations");
                }
                if (ZNetScene.instance && dumpSceneTextures.Value)
                {
                    string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomTextures", "scene_dump.txt");
                    Dbgl($"Writing {path}");
                    File.WriteAllLines(path, outputDump);
                    dumpSceneTextures.Value = false;
                }
            }
        }
        [HarmonyPatch(typeof(VisEquipment), "Awake")]
        static class VisEquipment_Awake_Patch
        {
            static void Postfix(VisEquipment __instance)
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
    }
}
