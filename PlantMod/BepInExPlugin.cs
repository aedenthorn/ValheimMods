using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlantMod
{
    [BepInPlugin("aedenthorn.PlantMod", "Plant Mod", "0.2.2")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> plantAnywhere;
        public static ConfigEntry<bool> ignoreBiome;
        public static ConfigEntry<bool> ignoreSun;
        public static ConfigEntry<bool> preventPlantTooClose;
        public static ConfigEntry<bool> preventDestroyIfCantGrow;
        public static ConfigEntry<float> growthTimeMultTree;
        public static ConfigEntry<float> growRadiusMultTree;
        public static ConfigEntry<float> minScaleMultTree;
        public static ConfigEntry<float> maxScaleMultTree;
        public static ConfigEntry<float> growthTimeMultPlant;
        public static ConfigEntry<float> growRadiusMultPlant;
        public static ConfigEntry<float> minScaleMultPlant;
        public static ConfigEntry<float> maxScaleMultPlant;

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 273, "Nexus mod ID for updates");

            plantAnywhere = Config.Bind<bool>("General", "PlantAnywhere", false, "Don't require cultivated ground to plant anything");
            ignoreBiome = Config.Bind<bool>("General", "IgnoreBiome", false, "Allow planting anything in any biome.");
            ignoreSun = Config.Bind<bool>("General", "IgnoreSun", false, "Allow planting under roofs.");
            preventPlantTooClose = Config.Bind<bool>("General", "PreventPlantTooClose", true, "Prevent plants from being planted if they are too close together to grow.");
            preventDestroyIfCantGrow = Config.Bind<bool>("General", "PreventDestroyIfCantGrow", false, "Prevent destruction of plants that normally are destroyed if they can't grow.");
            growthTimeMultTree = Config.Bind<float>("General", "GrowthTimeMultTree", 1f, "Multiply time taken to grow by this amount.");
            growRadiusMultTree = Config.Bind<float>("General", "GrowthRadiusMultTree", 1f, "Multiply required space to grow by this amount.");
            minScaleMultTree = Config.Bind<float>("General", "MinScaleMultTree", 1f, "Multiply minimum size by this amount.");
            maxScaleMultTree = Config.Bind<float>("General", "MaxScaleMultTree", 1f, "Multiply maximum size by this amount.");
            growthTimeMultPlant = Config.Bind<float>("General", "GrowthTimeMultPlant", 1f, "Multiply time taken to grow by this amount.");
            growRadiusMultPlant = Config.Bind<float>("General", "GrowthRadiusMultPlant", 1f, "Multiply required space to grow by this amount.");
            minScaleMultPlant = Config.Bind<float>("General", "MinScaleMultPlant", 1f, "Multiply minimum size by this amount.");
            maxScaleMultPlant = Config.Bind<float>("General", "MaxScaleMultPlant", 1f, "Multiply maximum size by this amount.");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        static class Player_UpdatePlacementGhost_Patch
        {
            static void Postfix(Player __instance, ref GameObject ___m_placementGhost, GameObject ___m_placementMarkerInstance)
            {
                if (___m_placementMarkerInstance != null && ___m_placementGhost?.GetComponent<Plant>() != null)
                {
                    if (preventPlantTooClose.Value)
                    {
                        Plant plant = ___m_placementGhost.GetComponent<Plant>();
                        if (!HaveGrowSpace(plant))
                        {
                            typeof(Player).GetField("m_placementStatus", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, 5);
                            typeof(Player).GetMethod("SetPlacementGhostValid", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { false });
                        }
                    }
                }
            }
        }

        private static bool HaveGrowSpace(Plant plant)
        {
            var spaceMask = LayerMask.GetMask(new string[]
            {
                "Default",
                "static_solid",
                "Default_small",
                "piece",
                "piece_nonsolid"
            });
            Collider[] array = Physics.OverlapSphere(plant.transform.position, plant.m_growRadius, spaceMask);
            for (int i = 0; i < array.Length; i++)
            {
                Plant component = array[i].GetComponent<Plant>();
                if (Input.GetKey("left shift"))
                    Dbgl($"{Vector3.Distance(plant.transform.position, component.transform.position)} {Math.Max(component.m_growRadius, plant.m_growRadius)}");
                if (component && component != plant)
                {
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(Piece), "Awake")]
        static class Piece_Awake_Patch
        {
            static void Postfix(ref Piece __instance)
            {
                if (__instance.gameObject.GetComponent<Plant>() != null)
                {
                    if (plantAnywhere.Value)
                    {

                        __instance.m_cultivatedGroundOnly = false;
                        __instance.m_groundOnly = false;
                    }
                }
            }
        }
        
        
        [HarmonyPatch(typeof(Plant), "GetHoverText")]
        static class Plant_GetHoverText_Patch
        {
            static void Postfix(ref Plant __instance, ref string __result)
            {
                double timeSincePlanted = Traverse.Create(__instance).Method("TimeSincePlanted").GetValue<double>();
                float growTime = Traverse.Create(__instance).Method("GetGrowTime").GetValue<float>();
                if(timeSincePlanted < growTime)
                    __result += $"\n{Mathf.RoundToInt((float)timeSincePlanted)}/{Mathf.RoundToInt(growTime)}";
            }
        }

                        
        [HarmonyPatch(typeof(Humanoid), "UpdateEquipment")]
        static class UpdateEquipment_Patch
        {
            static void Prefix(ItemDrop.ItemData ___m_rightItem, ItemDrop.ItemData ___m_leftItem)
            {
                if(___m_rightItem != null)
                    ___m_rightItem.m_durability = Math.Min(1, ___m_rightItem.m_durability);
                if (___m_leftItem != null)
                    ___m_leftItem.m_durability = Math.Min(1, ___m_leftItem.m_durability);
            }
        }
        
        
        [HarmonyPatch(typeof(Plant), "Awake")]
        static class Plant_Awake_Patch
        {
            static void Postfix(ref Plant __instance)
            {
                if (plantAnywhere.Value)
                {
                    __instance.m_needCultivatedGround = false;
                }
                if (preventDestroyIfCantGrow.Value)
                {
                    __instance.m_destroyIfCantGrow = false;
                }
                if (ignoreBiome.Value)
                {
                    Heightmap.Biome biome = 0;
                    foreach(Heightmap.Biome b in Enum.GetValues(typeof(Heightmap.Biome)))
                    {
                        biome |= b;
                    }

                    __instance.m_biome = biome;
                }
                if (__instance.name.ToLower().Contains("tree"))
                {
                    __instance.m_growTime *= growthTimeMultTree.Value;
                    __instance.m_growTimeMax *= growthTimeMultTree.Value;
                    __instance.m_growRadius *= growRadiusMultTree.Value;
                    __instance.m_minScale *= minScaleMultTree.Value;
                    __instance.m_maxScale *= maxScaleMultTree.Value;

                }
                else
                {
                    __instance.m_growTime *= growthTimeMultPlant.Value;
                    __instance.m_growTimeMax *= growthTimeMultPlant.Value;
                    __instance.m_growRadius *= growRadiusMultPlant.Value;
                    __instance.m_minScale *= minScaleMultPlant.Value;
                    __instance.m_maxScale *= maxScaleMultPlant.Value;
                }
            }
        }
        
          
        [HarmonyPatch(typeof(Plant), "GetGrowTime")]
        static class Plant_GetGrowTime_Patch
        {
            static void Postfix(ref Plant __instance, ref float __result)
            {
                if (modEnabled.Value)
                {
                    __result *= (__instance.name.ToLower().Contains("tree") ? growthTimeMultTree.Value : growthTimeMultPlant.Value);
                }
            }
        }               
        [HarmonyPatch(typeof(Plant), "HaveRoof")]
        static class Plant_HaveRoof_Patch
        {
            static bool Prefix(ref bool __result)
            {
                if (modEnabled.Value && ignoreSun.Value)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }          
        [HarmonyPatch(typeof(Plant), "HaveGrowSpace")]
        static class Plant_HaveGrowSpace_Patch
        {
            static bool Prefix(Plant __instance, ref bool __result)
            {
                //Dbgl($"checking too close?");

                if (modEnabled.Value && ((__instance.name.ToLower().Contains("tree") && growRadiusMultTree.Value == 0) ||(!__instance.name.ToLower().Contains("tree") && growRadiusMultPlant.Value == 0)))
                {
                    __result = true;
                    return false;
                }
                return true;
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
                if (text.ToLower().Equals("plantmod reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Plant Mod config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
