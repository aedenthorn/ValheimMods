using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace MerchantSpawnTweaks
{
    [BepInPlugin("aedenthorn.MerchantSpawnTweaks", "Merchant Spawn Tweaks", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> relocateInterval;
        public static ConfigEntry<int> lastRelocateDay;
        public static ConfigEntry<Vector3> merchantPosition;
        
        public static GameObject merchantObject;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            //nexusID = Config.Bind<int>("General", "NexusID", 858, "Nexus mod ID for updates");


            relocateInterval = Config.Bind<int>("Merchant", "RelocateInterval", 0, "Number of days before merchant relocates. Sit to 0 to disable relocation.");
            lastRelocateDay = Config.Bind<int>("Merchant", "LastRelocateDay", 0, "Number of days before merchant relocates. Sit to 0 to disable relocation.");
            merchantPosition = Config.Bind<Vector3>("Merchant", "MerchantPosition", Vector3.zero, "Current merchant position.");

            if (!modEnabled.Value)
                return;

            

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        [HarmonyPatch(typeof(ZoneSystem), "Start")]
        public static class ZoneSystem_Start_Patch
        {
            public static void Prefix(ZoneSystem __instance, Dictionary<Vector2i, ZoneSystem.LocationInstance> ___m_locationInstances)
            {

                if (!modEnabled.Value)
                    return;

                Dbgl($"Starting ZoneSystem, relocating merchant");

                for(int i = 0; i < ___m_locationInstances.Count; i++)
                {
                    Vector2i v = ___m_locationInstances.Keys.ToArray()[i];
                    ZoneSystem.LocationInstance loc = ___m_locationInstances[v];
                    if (loc.m_location.m_prefabName == "Vendor_BlackForest")
                    {
                        if (merchantPosition.Value != Vector3.zero)
                        {
                            ___m_locationInstances.Remove(v);
                            loc.m_position = merchantPosition.Value;
                            Vector2i zone = __instance.GetZone(merchantPosition.Value);

                        }

                        break;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(EnvMan), "OnMorning")]
        public static class OnMorning_Patch
        {
            public static void Prefix(EnvMan __instance, double ___m_totalSeconds)
            {
                if (!modEnabled.Value)
                    return;
                int day = (int)(___m_totalSeconds / (double)__instance.m_dayLengthSec);
                if (relocateInterval.Value > 0 && day - lastRelocateDay.Value >= relocateInterval.Value)
                {
                    RelocateMerchant();
                }
            }
        }
        public static void RelocateMerchant()
        {
            float size = Minimap.instance.m_textureSize / 2f;
            Vector2 pos = Vector2.zero;
            while(WorldGenerator.instance.GetBiome(pos.x, pos.y) != Heightmap.Biome.BlackForest)
            {
                pos = new Vector2(Random.Range(-size, size), Random.Range(-size, size));
            }
            RelocateMerchant(pos);
        }
        public static void RelocateMerchant(Vector2 coords)
        {
            if (merchantObject != null)
            {
                if (WorldGenerator.instance.GetBiome(coords.x, coords.y) != Heightmap.Biome.BlackForest)
                {
                    Dbgl("Coordinates not in Black Forest");
                    return;
                }

                Vector3 position = new Vector3(coords.x, 0, coords.y);

                HeightmapBuilder.HMBuildData data = new HeightmapBuilder.HMBuildData(position, 1, 1, false, WorldGenerator.instance);
                Traverse.Create(HeightmapBuilder.instance).Method("Build", new object[] { data }).GetValue();

                position.y = data.m_baseHeights[0];

                merchantPosition.Value = position;
                merchantObject.transform.position = position;
                Dbgl($"Merchant relocated to position {position}");
            }
        }


        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"merchant relocate"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Merchant randomly relocated." }).GetValue();
                    return false;
                }
                if (text.ToLower().StartsWith($"merchant relocate "))
                {

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    string[] split = text.Split(' ');
                    
                    try
                    {
                        float x = float.Parse(split[2]);
                        float y = float.Parse(split[3]);
                        Player localPlayer2 = Player.m_localPlayer;
                        Vector2 pos = new Vector2(x, y);
                        RelocateMerchant(pos);
                    }
                    catch
                    {
                        Traverse.Create(__instance).Method("AddString", new object[] { "Error parsing coordinates." }).GetValue();
                    }
                    return false;
                }
                if (text.ToLower().Equals($"merchant summon"))
                {
                    Vector3 pos = Player.m_localPlayer.transform.position + Vector3.forward * 3;
                    RelocateMerchant(new Vector2(pos.x, pos.z));
                    return false;
                }
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
