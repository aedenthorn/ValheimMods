using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace MiningMod
{
    [BepInPlugin("aedenthorn.MiningMod", "Mining Mod", "0.2.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> damageMult;
        public static ConfigEntry<int> dropMult;
        public static ConfigEntry<float> dropMinMult;
        public static ConfigEntry<float> dropMaxMult;
        public static ConfigEntry<float> dropChanceMult;
        public static ConfigEntry<float> rubyDropChance;
        public static ConfigEntry<float> amberDropChance;
        public static ConfigEntry<float> amberPearlDropChance;
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
            dropChanceMult = Config.Bind<float>("General", "DropChanceMult", 1.5f, "Multiply the drop chance by this amount");
            dropMult = Config.Bind<int>("General", "DropMult", 2, "Multiply the amount dropped by this amount (whole number)");
            dropMinMult = Config.Bind<float>("General", "DropMinMult", 1.1f, "Multiply the minimum amount dropped (before multiplier) by this amount");
            dropMaxMult = Config.Bind<float>("General", "DropMaxMult", 1.1f, "Multiply the maximum amount dropped (before multiplier) by this amount");
            rubyDropChance = Config.Bind<float>("General", "RubyDropChance", 0.05f, "Chance of dropping a ruby");
            amberPearlDropChance = Config.Bind<float>("General", "AmberPearlDropChance", 0.1f, "Chance of dropping amber pearl");
            amberDropChance = Config.Bind<float>("General", "AmberDropChance", 0.2f, "Chance of dropping amber");
            damageMult = Config.Bind<float>("General", "DamageMult", 2f, "Damage multiplier to mining rocks");
            nexusID = Config.Bind<int>("General", "NexusID", 206, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(MineRock), "Start", new Type[] { })]
        static class MineRock_Start_Patch
        {
            static void Postfix(ref MineRock __instance)
            {
                if (Environment.StackTrace.Contains("MineRock"))
                {
                    __instance.m_dropItems.m_dropMin = Mathf.RoundToInt(dropMinMult.Value * __instance.m_dropItems.m_dropMin);
                    __instance.m_dropItems.m_dropMax = Mathf.RoundToInt(dropMaxMult.Value * __instance.m_dropItems.m_dropMax);
                    __instance.m_dropItems.m_dropChance *= dropChanceMult.Value;

                }

            }
        }
        [HarmonyPatch(typeof(MineRock5), "Start", new Type[] { })]
        static class MineRock5_Start_Patch
        {
            static void Postfix(ref MineRock5 __instance)
            {
                if (Environment.StackTrace.Contains("MineRock"))
                {
                    __instance.m_dropItems.m_dropMin = Mathf.RoundToInt(dropMinMult.Value * __instance.m_dropItems.m_dropMin);
                    __instance.m_dropItems.m_dropMax = Mathf.RoundToInt(dropMaxMult.Value * __instance.m_dropItems.m_dropMax);
                    __instance.m_dropItems.m_dropChance *= dropChanceMult.Value;

                }

            }
        }
        [HarmonyPatch(typeof(DropTable), "GetDropList", new Type[] { })]
        static class DropTable_GetDropList_Patch
        {
            static void Postfix(ref List<GameObject> __result)
            {
                if (Environment.StackTrace.Contains("MineRock"))
                {
                    if(dropMult.Value > 1)
                    {
                        int count = __result.Count;
                        for (int i = 0; i < count; i++)
                        {
                            for (int j = 0; j < dropMult.Value - 1; j++)
                            {
                                __result.Add(__result[i]);
                            }
                        }
                    }
                    if (UnityEngine.Random.value < rubyDropChance.Value)
                    {
                        Dbgl("Dropping ruby");
                        GameObject go = ZNetScene.instance.GetPrefab("Ruby");
                        __result.Add(go);
                    }
                    if(UnityEngine.Random.value < amberDropChance.Value)
                    {
                        Dbgl("Dropping amber");
                        GameObject go = ZNetScene.instance.GetPrefab("Amber");
                        __result.Add(go);
                    }
                    if (UnityEngine.Random.value < amberPearlDropChance.Value)
                    {
                        Dbgl("Dropping amber pearl");
                        GameObject go = ZNetScene.instance.GetPrefab("AmberPearl");
                        __result.Add(go);
                    }
                }

            }
        }
        [HarmonyPatch(typeof(DropTable), "GetDropList", new Type[] {typeof(int) })]
        static class DropTable_GetDropList_Patch2
        {
            static void Prefix(ref int amount)
            {

                if (Environment.StackTrace.Contains("MineRock"))
                {
                    Dbgl($"GetDropList2 for mining rock");

                    amount = Mathf.RoundToInt(dropMult.Value * amount);
                }
                    
            }
        }
        [HarmonyPatch(typeof(MineRock), "Damage")]
        static class MineRock_Damage_Patch
        {
            static void Prefix(ref HitData hit)
            {
                hit.m_damage.m_pickaxe *= damageMult.Value;
                hit.m_damage.m_blunt *= damageMult.Value;
                hit.m_damage.m_chop *= damageMult.Value;
                hit.m_damage.m_pierce *= damageMult.Value;
            }
        }
        [HarmonyPatch(typeof(MineRock5), "Damage")]
        static class MineRock5_Damage_Patch
        {
            static void Prefix(ref HitData hit)
            {
                hit.m_damage.m_pickaxe *= damageMult.Value;
                hit.m_damage.m_blunt *= damageMult.Value;
                hit.m_damage.m_chop *= damageMult.Value;
                hit.m_damage.m_pierce *= damageMult.Value;
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
                if (text.ToLower().Equals("mining reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Mining Mod config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
