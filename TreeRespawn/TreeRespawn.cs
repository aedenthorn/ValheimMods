using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TreeRespawn
{
    [BepInPlugin("aedenthorn.TreeRespawn", "Tree Respawn", "0.1.1")]
    public class TreeRespawn : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(TreeRespawn).Namespace + " " : "") + str);
        }

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static Dictionary<string, string> seedsDic = new Dictionary<string, string>
        {
            {"Beech_Stub", "Beech_Sapling" },
            {"FirTree_Stub", "FirTree_Sapling" },
            {"Pinetree_01_Stub", "PineTree_Sapling" },
        };

        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 37, "Nexus mod ID for updates");

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
                    Dbgl($"destroyed trunk {__instance.name}");
                    GameObject prefab = ZNetScene.instance.GetPrefab(name);
                    if (prefab != null)
                    {
                        Instantiate(prefab, __instance.transform.position, Quaternion.identity);
                        Dbgl($"created new {name}");
                    }
                }
            }
        }
    }
}
