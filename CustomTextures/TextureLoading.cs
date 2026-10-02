using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomTextures
{
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        public static void LoadCustomTextures()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"CustomTextures");

            if (!Directory.Exists(path))
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
                return;
            }


            texturesToLoad.Clear();

            foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                string id = Path.GetFileNameWithoutExtension(fileName);

                
                if (!fileWriteTimes.ContainsKey(id) || (cachedTextures.ContainsKey(id) && !DateTime.Equals(File.GetLastWriteTimeUtc(file), fileWriteTimes[id])))
                {
                    cachedTextures.Remove(id);
                    texturesToLoad.Add(id);
                    layersToLoad.Add(Regex.Replace(id, @"_[^_]+\.", "."));
                    fileWriteTimes[id] = File.GetLastWriteTimeUtc(file);
                    //Dbgl($"adding new {fileName} custom texture.");
                }
                
                customTextures[id] = file;
            }
        }
        public static List<int> reloadedObjects = new List<int>();
        public static void ReloadTextures(bool locations)
        {
            reloadedObjects.Clear();
            outputDump.Clear();
            logDump.Clear();

            TryReloadStage("LoadCustomTextures", LoadCustomTextures);
            TryReloadStage("ReplaceObjectDBTextures", ReplaceObjectDBTextures);
            TryReloadStage("ReplaceSceneObjects", ReplaceSceneObjects);

            Dbgl($"Replaced textures for {reloadedObjects.Count()} found unique objects");

            TryReloadStage("ReplaceZoneObjects", () =>
            {
                var zones = SceneManager.GetActiveScene().GetRootGameObjects().Where(go => go != null && go.name.StartsWith("_Zone"));

                Dbgl($"Replacing textures for {zones.Count()} zones");
                foreach (var go in zones)
                {
                    ReplaceOneZoneTextures("_GameMain", go);
                }
            });

            TryReloadStage("ReplaceZoneSystemTextures", () =>
            {
                if (ZoneSystem.instance != null)
                    ReplaceZoneSystemTextures(ZoneSystem.instance);
            });

            TryReloadStage("ReplaceHeightmapTextures", ReplaceHeightmapTextures);
            TryReloadStage("ReplaceEnvironmentTextures", ReplaceEnvironmentTextures);
            TryReloadStage("ReplaceZNetSceneTextures", ReplaceZNetSceneTextures);

            if (locations)
            {
                TryReloadStage("ReplaceLocationTextures", () =>
                {
                    Dbgl($"Starting ZoneSystem Location prefab replacement");
                    stopwatch.Restart();

                    ReplaceLocationTextures();

                    LogStopwatch("ZoneSystem Locations");
                });
            }

            TryReloadStage("SetupVisEquipment", () =>
            {
                foreach (Player player in Player.GetAllPlayers())
                {
                    SetupVisEquipment(player);
                }
            });

            if (logDump.Any())
                Dbgl("\n" + string.Join("\n", logDump));

            Dbgl($"Checked {reloadedObjects.Count} objects total");

            reloadedObjects.Clear();
            if (dumpSceneTextures.Value)
            {
                TryReloadStage("DumpSceneTextures", () =>
                {
                    string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomTextures", "scene_dump.txt");
                    Dbgl($"Writing {path}");
                    File.WriteAllLines(path, outputDump);
                    dumpSceneTextures.Value = false;
                });
            }
        }

        public static void SetupVisEquipment(Humanoid humanoid)
        {
            if (humanoid == null)
                return;
            VisEquipment ve = humanoid.GetComponent<VisEquipment>();
            if (ve == null)
                ve = humanoid.GetComponentInChildren<VisEquipment>(true);
            if (ve != null)
            {
                var veTraverse = Traverse.Create(ve);
                SetEquipmentTexture(veTraverse.Field("m_leftItem").GetValue<int>(), veTraverse.Field("m_leftItemInstance").GetValue<GameObject>());
                SetEquipmentTexture(veTraverse.Field("m_rightItem").GetValue<int>(), veTraverse.Field("m_rightItemInstance").GetValue<GameObject>());
                SetEquipmentTexture(veTraverse.Field("m_helmetItem").GetValue<int>(), veTraverse.Field("m_helmetItemInstance").GetValue<GameObject>());
                SetEquipmentTexture(veTraverse.Field("m_leftBackItem").GetValue<int>(), veTraverse.Field("m_leftBackItemInstance").GetValue<GameObject>());
                SetEquipmentTexture(veTraverse.Field("m_rightBackItem").GetValue<int>(), veTraverse.Field("m_rightBackItemInstance").GetValue<GameObject>());
                SetEquipmentListTexture(veTraverse.Field("m_shoulderItem").GetValue<int>(), veTraverse.Field("m_shoulderItemInstances").GetValue<List<GameObject>>());
                SetEquipmentListTexture(veTraverse.Field("m_utilityItem").GetValue<int>(), veTraverse.Field("m_utilityItemInstances").GetValue<List<GameObject>>());
                SetBodyEquipmentTexture(ve, veTraverse.Field("m_legItem").GetValue<int>(), ve.m_bodyModel, veTraverse.Field("m_legItemInstances").GetValue<List<GameObject>>());
                SetBodyEquipmentTexture(ve, veTraverse.Field("m_chestItem").GetValue<int>(), ve.m_bodyModel, veTraverse.Field("m_chestItemInstances").GetValue<List<GameObject>>());
            }
        }
    }
}
