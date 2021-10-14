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
        private static void LoadCustomTextures()
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

        private static void ReloadTextures()
        {

            outputDump.Clear();
            logDump.Clear();

            LoadCustomTextures();

            //Dbgl($"textures to load \n\n{string.Join("\n", texturesToLoad)}");

            ReplaceObjectDBTextures();

            var zones = SceneManager.GetActiveScene().GetRootGameObjects().Where(go => go.name.StartsWith("_Zone"));

            Dbgl($"Replacing textures for {zones.Count()} zones");
            foreach (var go in zones)
            {
                ReplaceOneZoneTextures("_GameMain", go);
            }

            ReplaceZoneSystemTextures((ZoneSystem)typeof(ZoneSystem).GetField("m_instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));

            ReplaceHeightmapTextures();

            ReplaceEnvironmentTextures();

            List<GameObject> gos = new List<GameObject>();

            SkinnedMeshRenderer[] smrs = FindObjectsOfType<SkinnedMeshRenderer>();
            MeshRenderer[] mrs = FindObjectsOfType<MeshRenderer>();
            ParticleSystemRenderer[] psrs = FindObjectsOfType<ParticleSystemRenderer>();
            LineRenderer[] lrs = FindObjectsOfType<LineRenderer>();

            foreach(var r in smrs)
            {
                if(!gos.Contains(r.gameObject))
                    gos.Add(r.gameObject);
            }
            foreach(var r in mrs)
            {
                if(!gos.Contains(r.gameObject))
                    gos.Add(r.gameObject);
            }
            foreach(var r in psrs)
            {
                if(!gos.Contains(r.gameObject))
                    gos.Add(r.gameObject);
            }
            foreach(var r in lrs)
            {
                if(!gos.Contains(r.gameObject))
                    gos.Add(r.gameObject);
            }


            foreach (ClutterSystem.Clutter clutter in ClutterSystem.instance.m_clutter)
            {
                if (!gos.Contains(clutter.m_prefab))
                    gos.Add(clutter.m_prefab);
            }

            gos.AddRange(Traverse.Create(ZNetScene.instance).Field("m_namedPrefabs").GetValue<Dictionary<int, GameObject>>().Values);

            ReplaceSceneTextures(gos.ToArray());

            foreach (Player player in Player.GetAllPlayers())
            {
                VisEquipment ve = (VisEquipment)typeof(Humanoid).GetField("m_visEquipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);
                if (ve != null)
                {
                    SetEquipmentTexture(Traverse.Create(ve).Field("m_leftItem").GetValue<string>(), Traverse.Create(ve).Field("m_leftItemInstance").GetValue<GameObject>());
                    SetEquipmentTexture(Traverse.Create(ve).Field("m_rightItem").GetValue<string>(), Traverse.Create(ve).Field("m_rightItemInstance").GetValue<GameObject>());
                    SetEquipmentTexture(Traverse.Create(ve).Field("m_helmetItem").GetValue<string>(), Traverse.Create(ve).Field("m_helmetItemInstance").GetValue<GameObject>());
                    SetEquipmentTexture(Traverse.Create(ve).Field("m_leftBackItem").GetValue<string>(), Traverse.Create(ve).Field("m_leftBackItemInstance").GetValue<GameObject>());
                    SetEquipmentTexture(Traverse.Create(ve).Field("m_rightBackItem").GetValue<string>(), Traverse.Create(ve).Field("m_rightBackItemInstance").GetValue<GameObject>());
                    SetEquipmentListTexture(Traverse.Create(ve).Field("m_shoulderItem").GetValue<string>(), Traverse.Create(ve).Field("m_shoulderItemInstances").GetValue<List<GameObject>>());
                    SetEquipmentListTexture(Traverse.Create(ve).Field("m_utilityItem").GetValue<string>(), Traverse.Create(ve).Field("m_utilityItemInstances").GetValue<List<GameObject>>());
                    SetBodyEquipmentTexture(ve, Traverse.Create(ve).Field("m_legItem").GetValue<string>(), ve.m_bodyModel, Traverse.Create(ve).Field("m_legItemInstances").GetValue<List<GameObject>>());
                    SetBodyEquipmentTexture(ve, Traverse.Create(ve).Field("m_chestItem").GetValue<string>(), ve.m_bodyModel, Traverse.Create(ve).Field("m_chestItemInstances").GetValue<List<GameObject>>());
                }
            }

            if (logDump.Any())
                Dbgl("\n" + string.Join("\n", logDump));
            if (dumpSceneTextures.Value)
            {
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomTextures", "scene_dump.txt");
                Dbgl($"Writing {path}");
                File.WriteAllLines(path, outputDump);
            }
        }

    }
}
