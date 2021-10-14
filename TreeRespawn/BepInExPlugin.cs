using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TreeRespawn
{
    [BepInPlugin("aedenthorn.TreeRespawn", "Tree Respawn", "0.8.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }

        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<float> respawnDelay;

        public static Dictionary<string, string> seedsDic = new Dictionary<string, string>();

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 37, "Nexus mod ID for updates");
            respawnDelay = Config.Bind<float>("General", "RespawnDelay", 2.5f, "Delay in seconds to spawn sapling");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Start()
        {
            string jsonFile = "tree_dict.json";
            if (Chainloader.PluginInfos.ContainsKey("advize.PlantEverything"))
                jsonFile = "tree_dict_Plant_Everything.json";
            else if (Chainloader.PluginInfos.ContainsKey("com.Defryder.Plant_all_trees"))
                jsonFile = "tree_dict_Plant_all_trees.json";
            else if (Chainloader.PluginInfos.ContainsKey("com.bkeyes93.PlantingPlus"))
                jsonFile = "tree_dict_PlantingPlus.json";

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TreeRespawn", jsonFile);


            string json = File.ReadAllText(path);

            SeedData seeds = JsonUtility.FromJson<SeedData>(json);

            foreach (string seed in seeds.seeds)
            {
                string[] split = seed.Split(':');
                seedsDic.Add(split[0], split[1]);
            }

            Dbgl($"Loaded {seedsDic.Count} seeds from {path}");
        }

        [HarmonyPatch(typeof(Destructible), "Destroy")]
        static class Destroy_Patch
        {
            static void Prefix(Destructible __instance)
            {
                Dbgl($"destroyed destructible {__instance.name}");

                string name = seedsDic.FirstOrDefault(s => __instance.name.StartsWith(s.Key)).Value;

                if (name != null)
                {
                    Dbgl($"destroyed trunk {__instance.name}, trying to spawn {name}");
                    GameObject prefab = ZNetScene.instance.GetPrefab(name);
                    if (prefab != null)
                    {
                        Dbgl($"trying to spawn new tree");
                        context.StartCoroutine(SpawnTree(prefab, __instance.transform.position));
                    }
                    else
                    {
                        Dbgl($"prefab is null");
                    }
                }
            }
        }

        private static IEnumerator SpawnTree(GameObject prefab, Vector3 position)
        {
            Dbgl($"spawning new tree");
            yield return new WaitForSeconds(respawnDelay.Value);
            Instantiate(prefab, position, Quaternion.identity);
            Dbgl($"created new {prefab.name}");
        }
    }
}
