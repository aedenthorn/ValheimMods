using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterAITweaks
{
    [BepInPlugin("aedenthorn.MonsterAITweaks", "Monster AI Tweaks", "0.5.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> noMonstersTargetPlayers;
        public static ConfigEntry<bool> noMonstersAlerted;
        public static ConfigEntry<bool> noMonstersFlee;
        public static ConfigEntry<bool> allMonstersTame;
        public static ConfigEntry<bool> noBuildingTargeting;
        public static ConfigEntry<bool> allMonstersFearFire;
        public static ConfigEntry<bool> allMonstersAvoidFire;

        public static ConfigEntry<string> neverTargetPlayersListString;
        public static ConfigEntry<string> neverAlertedListString;
        public static ConfigEntry<string> neverFleeListString;
        public static ConfigEntry<string> defaultTamedListString;
        public static ConfigEntry<string> noBuildingTargetListString;
        public static ConfigEntry<string> fearFireListString;
        public static ConfigEntry<string> avoidFireListString;
        
        public static ConfigEntry<float> viewRangeMult;
        public static ConfigEntry<float> viewAngleMult;
        public static ConfigEntry<float> hearRangeMult;

        public static string[] neverTargetPlayersList;
        public static string[] neverAlertedList;
        public static string[] neverFleeList;
        public static string[] defaultTamedList;
        public static string[] noBuildingTargetList;
        public static string[] fearFireList;
        public static string[] avoidFireList;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 758, "Nexus mod ID for updates");

            noMonstersTargetPlayers = Config.Bind<bool>("Global", "NoMonstersTargetPlayers", false, "No monsters target players.");
            noMonstersAlerted = Config.Bind<bool>("Global", "NoMonstersAlerted", false, "No monsters become alerted.");
            noMonstersFlee = Config.Bind<bool>("Global", "NoMonstersFlee", false, "No monsters flee.");
            allMonstersTame = Config.Bind<bool>("Global", "NoMonstersAlerted", false, "All monsters tamed by default.");
            noBuildingTargeting = Config.Bind<bool>("Global", "NoBuildingTargeting", false, "No monsters target buildings.");
            allMonstersFearFire = Config.Bind<bool>("Global", "AllMonstersFearFire", false, "All monsters fear fire.");
            allMonstersAvoidFire = Config.Bind<bool>("Global", "AllMonstersAvoidFire", false, "All monsters avoid fire.");

            neverTargetPlayersListString = Config.Bind<string>("Lists", "NeverTargetPlayersList", "", "List of monsters that will never target players (comma-separated).");
            neverAlertedListString = Config.Bind<string>("Lists", "NeverAlertedList", "", "List of monsters that will never be alerted (comma-separated).");
            neverFleeListString = Config.Bind<string>("Lists", "NeverFleeListString", "", "List of monsters that will never flee (comma-separated).");
            defaultTamedListString = Config.Bind<string>("Lists", "DefaultTamedList", "", "List of monsters that are tamed by default (comma-separated).");
            noBuildingTargetListString = Config.Bind<string>("Lists", "NoBuildingTargetList", "", "List of monsters that do not target buildings (comma-separated).");
            fearFireListString = Config.Bind<string>("Lists", "FearFireListString", "", "List of monsters that fear fire (comma-separated).");
            avoidFireListString = Config.Bind<string>("Lists", "AvoidFireListString", "", "List of monsters that avoid fire (comma-separated).");
            
            viewRangeMult = Config.Bind<float>("Variables", "ViewRangeMult", 1f, "Monster view range multiplier.");
            viewAngleMult = Config.Bind<float>("Variables", "ViewAngleMult", 1f, "Monster view angle multiplier.");
            hearRangeMult = Config.Bind<float>("Variables", "HearRangeMult", 1f, "Monster hear range multiplier.");

            if (!modEnabled.Value)
                return;

            neverTargetPlayersList = neverTargetPlayersListString.Value.Split(',');
            neverAlertedList = neverAlertedListString.Value.Split(',');
            neverFleeList = neverFleeListString.Value.Split(',');
            defaultTamedList = defaultTamedListString.Value.Split(',');
            noBuildingTargetList = noBuildingTargetListString.Value.Split(',');
            fearFireList = fearFireListString.Value.Split(',');
            avoidFireList = avoidFireListString.Value.Split(',');

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(MonsterAI), "Awake")]
        public static class MonsterAI_Awake_Patch
        {
            public static void Postfix(MonsterAI __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (allMonstersTame.Value || defaultTamedList.Contains(Utils.GetPrefabName(__instance.gameObject)))
                    __instance.MakeTame();
                if (allMonstersAvoidFire.Value || avoidFireList.Contains(Utils.GetPrefabName(__instance.gameObject)))
                    __instance.m_avoidFire = true;
                if (allMonstersFearFire.Value || fearFireList.Contains(Utils.GetPrefabName(__instance.gameObject)))
                    __instance.m_afraidOfFire = true;

            }
        }
        
        [HarmonyPatch(typeof(BaseAI), "Awake")]
        public static class BaseAI_Awake_Patch
        {
            public static void Postfix(BaseAI __instance)
            {
                if (!modEnabled.Value)
                    return;


                __instance.m_viewRange *= viewRangeMult.Value;
                __instance.m_viewAngle *= viewAngleMult.Value;
                __instance.m_hearRange *= hearRangeMult.Value;

            }
        }
        
        [HarmonyPatch(typeof(MonsterAI), "SetTarget", new Type[] { typeof(Character) })]
        public static class MonsterAI_SetTarget_Patch
        {
            public static bool Prefix(MonsterAI __instance, Character attacker)
            {
                if (!modEnabled.Value)
                    return true;

                //Dbgl($"{__instance.name} setting target player {attacker.IsPlayer()} cancel {noMonstersTargetPlayers.Value}");

                if (attacker?.IsPlayer() == true && (noMonstersTargetPlayers.Value || neverTargetPlayersList.Contains(Utils.GetPrefabName(__instance.gameObject))))
                    return false;
                return true;
            }
        }
        
        [HarmonyPatch(typeof(BaseAI), "Flee")]
        public static class BaseAI_Flee_Patch
        {
            public static bool Prefix(BaseAI __instance)
            {
                if (!modEnabled.Value)
                    return true;

                if (noMonstersFlee.Value || neverFleeList.Contains(Utils.GetPrefabName(__instance.gameObject)))
                    return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(BaseAI), "FindEnemy")]
        public static class BaseAI_FindEnemy_Patch
        {
            public static void Postfix(BaseAI __instance, ref Character __result)
            {
                if (!modEnabled.Value)
                    return;

                //Dbgl($"{__instance.name} finding target player {__result?.IsPlayer()} cancel {noMonstersTargetPlayers.Value}");

                if (__result?.IsPlayer() == true && (noMonstersTargetPlayers.Value || neverTargetPlayersList.Contains(Utils.GetPrefabName(__instance.gameObject))))
                {
                    __result = null;
                }
            }
        }

        [HarmonyPatch(typeof(BaseAI), "Alert")]
        public static class BaseAI_Alert_Patch
        {
            public static bool Prefix(BaseAI __instance)
            {
                if (!modEnabled.Value)
                    return true;

                if (noMonstersAlerted.Value || neverAlertedList.Contains(Utils.GetPrefabName(__instance.gameObject)))
                {
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(BaseAI), "FindClosestStaticPriorityTarget")]
        public static class BaseAI_FindClosestStaticPriorityTarget_Patch
        {
            public static bool Prefix(BaseAI __instance, ref StaticTarget __result)
            {
                if (!modEnabled.Value)
                    return true;

                if (noBuildingTargeting.Value || noBuildingTargetList.Contains(Utils.GetPrefabName(__instance.gameObject)))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(BaseAI), "FindRandomStaticTarget")]
        public static class BaseAI_FindRandomStaticTarget_Patch
        {
            public static bool Prefix(BaseAI __instance, ref StaticTarget __result)
            {
                if (!modEnabled.Value)
                    return true;

                if (noBuildingTargeting.Value || noBuildingTargetList.Contains(Utils.GetPrefabName(__instance.gameObject)))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(MonsterAI), "SetAlerted")]
        public static class MonsterAI_SetAlerted_Patch
        {
            public static bool Prefix(MonsterAI __instance, bool alert)
            {
                if (!modEnabled.Value || !alert)
                    return true;

                if (noMonstersAlerted.Value || neverAlertedList.Contains(Utils.GetPrefabName(__instance.gameObject)))
                {
                    return false;
                }
                return true;
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
