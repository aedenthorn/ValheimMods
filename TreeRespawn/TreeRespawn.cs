using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace TreeRespawn
{
    [BepInPlugin("aedenthorn.TreeRespawn", "Tree Respawn", "0.5.0")]
    public class TreeRespawn : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(TreeRespawn).Namespace + " " : "") + str);
        }

        private static TreeRespawn context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<float> respawnDelay;

        public static Dictionary<string, string> seedsDic = new Dictionary<string, string>
        {
            {"Beech_Stub", "Beech_Sapling" },
            {"Beech1_Stub", "Beech_Sapling" },
            {"FirTree_Stub", "FirTree_Sapling" },
            {"Pinetree_01_Stub", "PineTree_Sapling" },
            {"BirchStub", "Birch_Sapling" },
            {"OakStub", "Oak_Sapling" },
            {"SwampTree1_Stub", "Ancient_Sapling" }
        };

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
