using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MonsterAITweaks
{
    [BepInPlugin("aedenthorn.MonsterAITweaks", "Monster AI Tweaks", "0.4.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static Dictionary<string, AIData> aiDataDict = new Dictionary<string, AIData>();

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

                if (aiDataDict.ContainsKey(Utils.GetPrefabName(__instance.gameObject)))
                {
                    SetMonsterAIData(__instance, aiDataDict[Utils.GetPrefabName(__instance.gameObject)]);
                   
                }
            }
        }

        [HarmonyPatch(typeof(AnimalAI), "Awake")]
        public static class AnimalAI_Awake_Patch
        {
            public static void Postfix(AnimalAI __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (aiDataDict.ContainsKey(Utils.GetPrefabName(__instance.gameObject)))
                {
                    SetAnimalAIData(__instance, aiDataDict[Utils.GetPrefabName(__instance.gameObject)]);

                }

            }
        }

        [HarmonyPatch(typeof(BaseAI), "Awake")]
        public static class BaseAI_Awake_Patch
        {
            public static void Postfix(BaseAI __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (aiDataDict.ContainsKey(Utils.GetPrefabName(__instance.gameObject)))
                {
                    SetBaseAIData(__instance, aiDataDict[Utils.GetPrefabName(__instance.gameObject)]);
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
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reload"))
                {
                    aiDataDict = GetAIDataFromFiles();
                    if (ZNetScene.instance)
                        LoadAllAIData(ZNetScene.instance);
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} reloaded armor stats from files" }).GetValue();
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} save "))
                {
                    var t = text.Split(' ');
                    string ai = t[t.Length - 1];
                    AIData aiData = GetAIDataByName(ai);
                    if (aiData == null)
                        return false;
                    CheckModFolder();
                    File.WriteAllText(Path.Combine(assetPath, aiData.name + ".json"), JsonUtility.ToJson(aiData, true));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} saved armor data to {ai}.json" }).GetValue();
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} dump "))
                {
                    var t = text.Split(' ');
                    string armor = t[t.Length - 1];
                    AIData aiData = GetAIDataByName(armor);
                    if (aiData == null)
                        return false;
                    Dbgl(JsonUtility.ToJson(aiData));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped {armor}" }).GetValue();
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()}"))
                {
                    string output = $"{context.Info.Metadata.Name} reset\r\n"
                    + $"{context.Info.Metadata.Name} reload\r\n"
                    + $"{context.Info.Metadata.Name} dump <ArmorName>\r\n"
                    + $"{context.Info.Metadata.Name} save <ArmorName>\r\n"
                    + $"{context.Info.Metadata.Name} damagetypes\r\n"
                    + $"{context.Info.Metadata.Name} damagemods";

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { output }).GetValue();
                    return false;
                }
                return true;
            }
        }

        public static void SetMonsterAIData(MonsterAI instance, AIData data)
        {
            instance.m_alertRange = data.alertRange;
            instance.m_fleeIfHurtWhenTargetCantBeReached = data.fleeIfHurtWhenTargetCantBeReached;
            instance.m_fleeIfNotAlerted = data.fleeIfNotAlerted;
            instance.m_fleeIfLowHealth = data.fleeIfLowHealth;
            instance.m_circulateWhileCharging = data.circulateWhileCharging;
            instance.m_circulateWhileChargingFlying = data.circulateWhileChargingFlying;
            instance.m_enableHuntPlayer = data.enableHuntPlayer;
            instance.m_attackPlayerObjects = data.attackPlayerObjects;
            instance.m_interceptTimeMax = data.interceptTimeMax;
            instance.m_interceptTimeMin = data.interceptTimeMin;
            instance.m_maxChaseDistance = data.maxChaseDistance;
            instance.m_minAttackInterval = data.minAttackInterval;
            instance.m_circleTargetInterval = data.circleTargetInterval;
            instance.m_circleTargetDuration = data.circleTargetDuration;
            instance.m_circleTargetDistance = data.circleTargetDistance;
            instance.m_sleeping = data.sleeping;
            instance.m_noiseWakeup = data.noiseWakeup;
            instance.m_noiseRangeScale = data.noiseRangeScale;
            instance.m_wakeupRange = data.wakeupRange;
            instance.m_avoidLand = data.avoidLand;
            instance.m_consumeRange = data.consumeRange;
            instance.m_consumeSearchRange = data.consumeSearchRange;
            instance.m_consumeSearchInterval = data.consumeSearchInterval;
            AccessTools.Field(typeof(MonsterAI), "m_despawnInDay").SetValue(instance, data.despawnInDay);
            AccessTools.Field(typeof(MonsterAI), "m_eventCreature").SetValue(instance, data.eventCreature);
            SetBaseAIData(instance, data);
        }

        public static void SetAnimalAIData(AnimalAI instance, AIData data)
        {
            instance.m_timeToSafe = data.timeToSafe;
        }

        public static void SetBaseAIData(BaseAI instance, AIData data)
        {
            instance.m_viewRange = data.viewRange;
            instance.m_viewAngle = data.viewAngle;
            instance.m_hearRange = data.hearRange;
            instance.m_idleSoundInterval = data.idleSoundInterval;
            instance.m_idleSoundChance = data.idleSoundChance;
            instance.m_moveMinAngle = data.moveMinAngle;
            instance.m_smoothMovement = data.smoothMovement;
            instance.m_serpentMovement = data.serpentMovement;
            instance.m_serpentTurnRadius = data.serpentTurnRadius;
            instance.m_jumpInterval = data.jumpInterval;
            instance.m_randomCircleInterval = data.randomCircleInterval;
            instance.m_randomMoveInterval = data.randomMoveInterval;
            instance.m_randomMoveRange = data.randomMoveRange;
            instance.m_randomFly = data.randomFly;
            instance.m_chanceToTakeoff = data.chanceToTakeoff;
            instance.m_chanceToLand = data.chanceToLand;
            instance.m_groundDuration = data.groundDuration;
            instance.m_airDuration = data.airDuration;
            instance.m_maxLandAltitude = data.maxLandAltitude;
            instance.m_flyAltitudeMin = data.flyAltitudeMin;
            instance.m_flyAltitudeMax = data.flyAltitudeMax;
            instance.m_takeoffTime = data.takeoffTime;
            instance.m_avoidFire = data.avoidFire;
            instance.m_afraidOfFire = data.afraidOfFire;
            instance.m_avoidWater = data.avoidWater;
            instance.m_spawnMessage = data.spawnMessage;
            instance.m_deathMessage = data.deathMessage;
        }
    }
}
