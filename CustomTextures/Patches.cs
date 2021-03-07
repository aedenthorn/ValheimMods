using HarmonyLib;
using System.IO;
using System.Reflection;

namespace CustomTextures
{
    public partial class BepInExPlugin
    {

        [HarmonyPatch(typeof(FejdStartup), "SetupObjectDB")]
        static class FejdStartup_SetupObjectDB_Patch
        {
            static void Prefix()
            {
                if (!modEnabled.Value)
                    return;

                Dbgl($"SetupObjectDB prefix");


                ReplaceObjectDBTextures();

            }
        }

        [HarmonyPatch(typeof(ClutterSystem), "Awake")]
        static class ClutterSystem_Awake_Patch
        {
            static void Postfix(ClutterSystem __instance)
            {
                Dbgl($"Clutter system awake");

                outputDump.Clear();

                Dbgl($"Checking {__instance.m_clutter.Count} clutters");
                foreach (ClutterSystem.Clutter clutter in __instance.m_clutter)
                {
                    LoadOneTexture(clutter.m_prefab, clutter.m_prefab.name, "object");
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
    }
}
