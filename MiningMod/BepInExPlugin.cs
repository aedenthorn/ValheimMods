using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MiningMod
{
    [BepInPlugin("aedenthorn.MiningMod", "Mining Mod", "0.8.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = false;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> damageMult;
        public static ConfigEntry<float> stoneDropMult;
        public static ConfigEntry<float> oreDropMult;
        public static ConfigEntry<float> dropMinMult;
        public static ConfigEntry<float> dropMaxMult;
        public static ConfigEntry<float> dropChanceMult;
        public static ConfigEntry<float> rubyDropChance;
        public static ConfigEntry<float> amberDropChance;
        public static ConfigEntry<float> amberPearlDropChance;
        public static ConfigEntry<float> flintDropChance;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            dropChanceMult = Config.Bind<float>("General", "DropChanceMult", 1f, "Multiply the drop chance by this amount");
            stoneDropMult = Config.Bind<float>("General", "StoneDropMult", 1f, "Multiply the amount of stone dropped by this amount");
            oreDropMult = Config.Bind<float>("General", "OreDropMult", 1f, "Multiply the amount of ore dropped by this amount");
            dropMinMult = Config.Bind<float>("General", "DropMinMult", 1f, "Multiply the minimum amount dropped (before multiplier) by this amount");
            dropMaxMult = Config.Bind<float>("General", "DropMaxMult", 1f, "Multiply the maximum amount dropped (before multiplier) by this amount");
            rubyDropChance = Config.Bind<float>("General", "RubyDropChance", 0.05f, "Chance of dropping a ruby");
            amberPearlDropChance = Config.Bind<float>("General", "AmberPearlDropChance", 0.1f, "Chance of dropping amber pearl");
            amberDropChance = Config.Bind<float>("General", "AmberDropChance", 0.2f, "Chance of dropping amber");
            flintDropChance = Config.Bind<float>("General", "FlintDropChance", 0.15f, "Chance of dropping flint");
            damageMult = Config.Bind<float>("General", "DamageMult", 1f, "Damage multiplier to mining rocks");
            nexusID = Config.Bind<int>("General", "NexusID", 206, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static string dropTableObject = "";

        [HarmonyPatch(typeof(DropOnDestroyed), "Awake")]
        public static class DropOnDestroyed_Awake_Patch
        {
            public static void Postfix(ref DropOnDestroyed __instance)
            {
                if (__instance.gameObject.name.Contains("Rock"))
                {
                    __instance.m_dropWhenDestroyed.m_dropMin = Mathf.RoundToInt(dropMinMult.Value * __instance.m_dropWhenDestroyed.m_dropMin);
                    __instance.m_dropWhenDestroyed.m_dropMax = Mathf.RoundToInt(dropMaxMult.Value * __instance.m_dropWhenDestroyed.m_dropMax);
                    __instance.m_dropWhenDestroyed.m_dropChance *= dropChanceMult.Value;
                }

            }
        }
        [HarmonyPatch(typeof(DropOnDestroyed), "OnDestroyed")]
        public static class DropOnDestroyed_OnDestroyed_Patch
        {
            public static void Prefix(ref DropOnDestroyed __instance)
            {
                dropTableObject = __instance.gameObject.name;
            }
            public static void Postfix(ref DropOnDestroyed __instance)
            {
                dropTableObject = "";
            }
        }
        [HarmonyPatch(typeof(MineRock), "Start", new Type[] { })]
        public static class MineRock_Start_Patch
        {
            public static void Postfix(ref MineRock __instance)
            {
                __instance.m_dropItems.m_dropMin = Mathf.RoundToInt(dropMinMult.Value * __instance.m_dropItems.m_dropMin);
                __instance.m_dropItems.m_dropMax = Mathf.RoundToInt(dropMaxMult.Value * __instance.m_dropItems.m_dropMax);
                __instance.m_dropItems.m_dropChance *= dropChanceMult.Value;

            }
        }
        [HarmonyPatch(typeof(MineRock5), "Awake", new Type[] { })]
        public static class MineRock5_Start_Patch
        {
            public static void Postfix(ref MineRock5 __instance)
            {
                __instance.m_dropItems.m_dropMin = Mathf.RoundToInt(dropMinMult.Value * __instance.m_dropItems.m_dropMin);
                __instance.m_dropItems.m_dropMax = Mathf.RoundToInt(dropMaxMult.Value * __instance.m_dropItems.m_dropMax);
                __instance.m_dropItems.m_dropChance *= dropChanceMult.Value;
            }
        }
        [HarmonyPatch(typeof(DropTable), "GetDropList", new Type[] { })]
        public static class DropTable_GetDropList_Patch
        {
            public static void Postfix(ref List<GameObject> __result)
            {
                if (Environment.StackTrace.Contains("MineRock") || (Environment.StackTrace.Contains("DropOnDestroyed") && dropTableObject.Contains("Rock")))
                {
                    Dictionary<string, List<GameObject>> typeLootDict = new Dictionary<string, List<GameObject>>();
                    foreach (GameObject go in __result)
                    {
                        if (go == null)
                            continue;
                        if (!typeLootDict.ContainsKey(go.name))
                            typeLootDict.Add(go.name, new List<GameObject>());
                        typeLootDict[go.name].Add(go);
                    }

                    foreach(var kvp in typeLootDict)
                    {
                        string name = Utils.GetPrefabName(kvp.Value[0]);
                        int count = kvp.Value.Count;
                        if (kvp.Key == "Stone")
                        {
                            count = Mathf.RoundToInt(count * stoneDropMult.Value);
                        }
                        else if (kvp.Key.EndsWith("Ore"))
                        {
                            count = Mathf.RoundToInt(count * oreDropMult.Value);
                        }
                        Dbgl($"loot drop had {kvp.Value.Count} {(kvp.Value.Count > 0? kvp.Value[0].name :"")} - changed amount to {count}");
                        if(kvp.Value.Count < count)
                        {
                            for (int i = kvp.Value.Count; i < count; i++)
                            {
                                __result.Add(kvp.Value[0]);
                            }
                        }
                        else if(count < kvp.Value.Count)
                        {
                            for (int i = count; i < kvp.Value.Count; i++)
                            {
                                __result.Remove(kvp.Value[i]);
                            }
                        }
                    }

                    if (UnityEngine.Random.value < rubyDropChance.Value)
                    {
                        GameObject go = ZNetScene.instance.GetPrefab("Ruby");
                        __result.Add(go);
                    }
                    if(UnityEngine.Random.value < amberDropChance.Value)
                    {
                        GameObject go = ZNetScene.instance.GetPrefab("Amber");
                        __result.Add(go);
                    }
                    if (UnityEngine.Random.value < amberPearlDropChance.Value)
                    {
                        GameObject go = ZNetScene.instance.GetPrefab("AmberPearl");
                        __result.Add(go);
                    }
                    if (UnityEngine.Random.value < flintDropChance.Value)
                    {
                        GameObject go = ZNetScene.instance.GetPrefab("Flint");
                        __result.Add(go);
                    }
                }

            }
        }
        //[HarmonyPatch(typeof(DropTable), "GetDropList", new Type[] {typeof(int) })]
        public static class DropTable_GetDropList_Patch2
        {
            public static void Prefix(ref int amount)
            {
                if (Environment.StackTrace.Contains("MineRock") || (Environment.StackTrace.Contains("DropOnDestroyed") && dropTableObject.Contains("Rock")))
                {
                    //Dbgl($"Getting drops for {dropTableObject}");
                    //amount = Mathf.RoundToInt(dropMult.Value * amount);
                }
            }
        }

        [HarmonyPatch(typeof(MineRock), "Damage")]
        public static class MineRock_Damage_Patch
        {
            public static void Prefix(MineRock __instance, ref HitData hit)
            {
                Dbgl($"Damaging {__instance.gameObject.name}");
                hit.m_damage.m_pickaxe *= damageMult.Value;
                hit.m_damage.m_blunt *= damageMult.Value;
                hit.m_damage.m_chop *= damageMult.Value;
                hit.m_damage.m_pierce *= damageMult.Value;
            }
        }
        [HarmonyPatch(typeof(MineRock5), "Damage")]
        public static class MineRock5_Damage_Patch
        {
            public static void Prefix(MineRock5 __instance, ref HitData hit)
            {
                Dbgl($"Damaging {__instance.gameObject.name}");

                hit.m_damage.m_pickaxe *= damageMult.Value;
                hit.m_damage.m_blunt *= damageMult.Value;
                hit.m_damage.m_chop *= damageMult.Value;
                hit.m_damage.m_pierce *= damageMult.Value;
            }
        }
        [HarmonyPatch(typeof(Destructible), "Damage")]
        public static class DropOnDestroyed_Damage_Patch
        {
            public static void Prefix(Destructible __instance, ref HitData hit)
            {
                if (__instance.GetComponent<DropOnDestroyed>() && __instance.gameObject.name.Contains("Rock"))
                {
                    Dbgl($"Damaging {__instance.gameObject.name}");
                    hit.m_damage.m_pickaxe *= damageMult.Value;
                    hit.m_damage.m_blunt *= damageMult.Value;
                    hit.m_damage.m_chop *= damageMult.Value;
                    hit.m_damage.m_pierce *= damageMult.Value;
                }
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
                if (text.ToLower().Equals("mining reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    __instance.AddString(text);
                    __instance.AddString("Mining Mod config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
