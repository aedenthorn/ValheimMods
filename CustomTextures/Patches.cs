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

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix(Dictionary<int, GameObject> ___m_namedPrefabs)
            {
                Dbgl($"ZNetScene awake");

                outputDump.Clear();

                Dbgl($"Checking {___m_namedPrefabs.Values.Count} prefabs");

                LoadSceneTextures(___m_namedPrefabs.Values.ToArray());
            }
        }

        [HarmonyPatch(typeof(ClutterSystem), "Awake")]
        static class ClutterSystem_Awake_Patch
        {
            static void Postfix(ClutterSystem __instance)
            {
                Dbgl($"Clutter system awake");

                List<GameObject> gos = new List<GameObject>();
                foreach (ClutterSystem.Clutter clutter in __instance.m_clutter)
                {
                    gos.Add(clutter.m_prefab);
                }
                Dbgl($"Checking {gos.Count} clutters");
                LoadSceneTextures(gos.ToArray());
                if (dumpSceneTextures.Value)
                {
                    string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomTextures", "scene_dump.txt");
                    File.WriteAllLines(path, outputDump);
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
                    if (HasCustomTexture($"player_model_{i}_texture"))
                    {
                        __instance.m_models[i].m_baseMaterial.mainTexture = LoadTexture($"player_model_{i}_texture", __instance.m_models[i].m_baseMaterial.mainTexture);
                        Dbgl($"set player_model_{i}_texture custom texture.");
                    }
                    if (HasCustomTexture($"player_model_{i}_bump"))
                    {
                        __instance.m_models[i].m_baseMaterial.SetTexture("_SkinBumpMap", LoadTexture($"player_model_{i}_bump", __instance.m_models[i].m_baseMaterial.mainTexture));
                        Dbgl($"set player_model_{i}_bump custom skin bump map.");
                    }
                }
            }
        }


        [HarmonyPatch(typeof(VisEquipment), "SetLeftHandEquiped")]
        static class VisEquipment_SetLeftHandEquiped_Patch
        {
            static void Postfix(bool __result, string ___m_leftItem, GameObject ___m_leftItemInstance)
            {
                if (!__result)
                    return;

                SetEquipmentTexture(___m_leftItem, ___m_leftItemInstance);
            }
        }

        [HarmonyPatch(typeof(VisEquipment), "SetRightHandEquiped")]
        static class VisEquipment_SetRightHandEquiped_Patch
        {
            static void Postfix(bool __result, string ___m_rightItem, GameObject ___m_rightItemInstance)
            {
                if (!__result)
                    return;

                SetEquipmentTexture(___m_rightItem, ___m_rightItemInstance);
            }
        }

        [HarmonyPatch(typeof(VisEquipment), "SetHelmetEquiped")]
        static class VisEquipment_SetHelmetEquiped_Patch
        {
            static void Postfix(bool __result, string ___m_helmetItem, GameObject ___m_helmetItemInstance)
            {
                if (!__result)
                    return;

                SetEquipmentTexture(___m_helmetItem, ___m_helmetItemInstance);
            }
        }

        [HarmonyPatch(typeof(VisEquipment), "SetBackEquiped")]
        static class VisEquipment_SetBackEquiped_Patch
        {
            static void Postfix(bool __result, string ___m_leftBackItem, GameObject ___m_leftBackItemInstance, string ___m_rightBackItem, GameObject ___m_rightBackItemInstance)
            {
                if (!__result)
                    return;

                SetEquipmentTexture(___m_leftBackItem, ___m_leftBackItemInstance);
                SetEquipmentTexture(___m_rightBackItem, ___m_rightBackItemInstance);


            }
        }

        [HarmonyPatch(typeof(VisEquipment), "SetShoulderEquiped")]
        static class VisEquipment_SetShoulderEquiped_Patch
        {
            static void Postfix(bool __result, string ___m_shoulderItem, List<GameObject> ___m_shoulderItemInstances)
            {
                if (!__result)
                    return;

                SetEquipmentListTexture(___m_shoulderItem, ___m_shoulderItemInstances);

            }
        }

        [HarmonyPatch(typeof(VisEquipment), "SetUtilityEquiped")]
        static class VisEquipment_SetUtilityEquiped_Patch
        {
            static void Postfix(bool __result, string ___m_utilityItem, List<GameObject> ___m_utilityItemInstances)
            {
                if (!__result)
                    return;

                SetEquipmentListTexture(___m_utilityItem, ___m_utilityItemInstances);
            }
        }

        [HarmonyPatch(typeof(VisEquipment), "SetLegEquiped")]
        static class VisEquipment_SetLegEquiped_Patch
        {
            static void Postfix(bool __result, string ___m_legItem, SkinnedMeshRenderer ___m_bodyModel, List<GameObject> ___m_legItemInstances)
            {
                if (!__result)
                    return;

                SetBodyEquipmentTexture(___m_legItem, ___m_bodyModel, ___m_legItemInstances, "_Legs");
            }
        }

        [HarmonyPatch(typeof(VisEquipment), "SetChestEquiped")]
        static class VisEquipment_SetChestEquiped_Patch
        {
            static void Postfix(bool __result, string ___m_chestItem, SkinnedMeshRenderer ___m_bodyModel, List<GameObject> ___m_chestItemInstances)
            {
                if (!__result)
                    return;

                SetBodyEquipmentTexture(___m_chestItem, ___m_bodyModel, ___m_chestItemInstances, "_Chest");

            }
        }

    }
}
