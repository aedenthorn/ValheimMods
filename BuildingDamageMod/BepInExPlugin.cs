using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace BuildingDamageMod
{
    [BepInPlugin("aedenthorn.BuildingDamageMod", "Building Damage Mod", "0.5.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> creatorDamageMult;
        public static ConfigEntry<float> nonCreatorDamageMult;
        public static ConfigEntry<float> uncreatedDamageMult;
        public static ConfigEntry<float> naturalDamageMult;
        public static ConfigEntry<bool> preventWearDamage;
        public static ConfigEntry<int> nexusID;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            creatorDamageMult = Config.Bind<float>("General", "CreatorDamageMult", 1f, "Multiply damage by creators by this much");
            nonCreatorDamageMult = Config.Bind<float>("General", "NonCreatorDamageMult", 1f, "Multiply damage by non-creators by this much");
            uncreatedDamageMult = Config.Bind<float>("General", "UncreatedDamageMult", 1f, "Multiply damage to uncreated buildings by this much");
            naturalDamageMult = Config.Bind<float>("General", "NaturalDamageMult", 1f, "Multiply natural wear damage to buildings by this much");
            nexusID = Config.Bind<int>("General", "NexusID", 233, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("buildingdamage reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Building Damage config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(WearNTear), "RPC_HealthChanged")]
        static class RPC_HealthChanged_Patch
        {
            static bool Prefix(long peer, Piece ___m_piece)
            {
                if (___m_piece is null)
                    return true;
                //Dbgl($"creator: {___m_piece.GetCreator()} peer {peer}");

                if (uncreatedDamageMult.Value == 0 && ___m_piece.GetCreator() == 0)
                    return false;

                if (nonCreatorDamageMult.Value == 0 && (___m_piece.GetCreator() != 0 && peer != ___m_piece.GetCreator()))
                    return false;

                if (creatorDamageMult.Value == 0 && (___m_piece.GetCreator() != 0 && peer == ___m_piece.GetCreator()))
                    return false;
                return true;
            }
        }
        
        [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
        static class RPC_Damage_Patch
        {
            static void Prefix(ref HitData hit, Piece ___m_piece)
            {
                if (!modEnabled.Value)
                    return;
                
                float mult = 1;
                //Dbgl($"attacker: {hit.m_attacker.userID}, creator { ___m_nview.IsOwner()}");
                if (hit.m_attacker != null && !hit.m_attacker.IsNone())
                {
                    if (___m_piece?.GetCreator() == 0)
                    {
                        mult = uncreatedDamageMult.Value;
                    }
                    else if(hit.m_attacker.UserID == ___m_piece?.GetCreator())
                    {
                        mult = creatorDamageMult.Value;
                    }
                    else
                    {
                        mult = nonCreatorDamageMult.Value;
                    }
                }
                else
                {
                    mult = naturalDamageMult.Value;
                }
                MultiplyDamage(ref hit, mult);
            }
            private static void MultiplyDamage(ref HitData hit, float value)
            {
                value = Math.Max(0, value);
                hit.m_damage.m_damage *= value;
                hit.m_damage.m_blunt *= value;
                hit.m_damage.m_slash *= value;
                hit.m_damage.m_pierce *= value;
                hit.m_damage.m_chop *= value;
                hit.m_damage.m_pickaxe *= value;
                hit.m_damage.m_fire *= value;
                hit.m_damage.m_frost *= value;
                hit.m_damage.m_lightning *= value;
                hit.m_damage.m_poison *= value;
                hit.m_damage.m_spirit *= value;
            }
        }
        
        [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
        static class ApplyDamage_Patch
        {
            static void Prefix(ref float damage)
            {
                if (!modEnabled.Value || Environment.StackTrace.Contains("RPC_Damage"))
                    return;
                damage *= naturalDamageMult.Value;
            }
        }
    }
}