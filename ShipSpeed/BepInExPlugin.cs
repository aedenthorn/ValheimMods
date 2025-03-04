using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShipSpeed
{
    [BepInPlugin("aedenthorn.ShipSpeed", "Ship Speed Mod", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> forceDistanceMult;
        public static ConfigEntry<float> forceMult;
        public static ConfigEntry<float> dampingMult;
        public static ConfigEntry<float> dampingSidewayMult;
        public static ConfigEntry<float> dampingForwardMult;
        public static ConfigEntry<float> angularDampingMult;
        public static ConfigEntry<float> disableLevelMult;
        public static ConfigEntry<float> sailForceFactorMult;
        public static ConfigEntry<float> rudderSpeedMult;
        public static ConfigEntry<float> stearForceOffsetMult;
        public static ConfigEntry<float> stearForceMult;
        public static ConfigEntry<float> stearVelForceFactorMult;
        public static ConfigEntry<float> backwardForceMult;
        public static ConfigEntry<float> rudderRotationMaxMult;
        //public static ConfigEntry<float> rudderRotationSpeedMult;
        public static ConfigEntry<float> minWaterImpactForceMult;
        public static ConfigEntry<float> minWaterImpactIntervalMult;
        public static ConfigEntry<float> waterImpactDamageMult;
        public static ConfigEntry<float> upsideDownDmgIntervalMult;
        public static ConfigEntry<float> upsideDownDmgMult;

        public static ConfigEntry<float> windAngleFactorMin;
        public static ConfigEntry<float> windAngleFactorMult;
        public static ConfigEntry<float> headWindDegrees;
        public static ConfigEntry<float> tailWindMult;
        public static ConfigEntry<float> headWindMult;
        public static ConfigEntry<float> damageMult;

        public Harmony harmony;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 119, "Nexus mod ID for updates");
            forceDistanceMult = Config.Bind<float>("Ships", "ForceDistanceMult", 1f, "Force Distance Multiplier");
            forceMult = Config.Bind<float>("Ships", "ForceMult", 1f, "Force Multiplier");
            dampingMult = Config.Bind<float>("Ships", "DampingMult", 1f, "Damping Multiplier");
            dampingSidewayMult = Config.Bind<float>("Ships", "DampingSidewayMult", 1f, "Damping Sideway Multiplier");
            dampingForwardMult = Config.Bind<float>("Ships", "DampingForwardMult", 1f, "Damping Forward Multiplier");
            angularDampingMult = Config.Bind<float>("Ships", "AngularDampingMult", 1f, "Angular Damping Multiplier");
            disableLevelMult = Config.Bind<float>("Ships", "DisableLevelMult", 1f, "Disable Level Multiplier");
            sailForceFactorMult = Config.Bind<float>("Ships", "SailForceFactorMult", 1f, "Sail Force Factor Multiplier");
            rudderSpeedMult = Config.Bind<float>("Ships", "RudderSpeedMult", 1f, "Rudder Speed Multiplier");
            stearForceOffsetMult = Config.Bind<float>("Ships", "StearForceOffsetMult", 1f, "Steer Force Offset Multiplier");
            stearForceMult = Config.Bind<float>("Ships", "StearForceMult", 1f, "Steer Force Multiplier");
            stearVelForceFactorMult = Config.Bind<float>("Ships", "StearVelForceFactorMult", 1f, "Steer Vel Force Factor Multiplier");
            backwardForceMult = Config.Bind<float>("Ships", "BackwardForceMult", 1f, "Backward Force Multiplier");
            rudderRotationMaxMult = Config.Bind<float>("Ships", "RudderRotationMaxMult", 1f, "Rudder Rotation Max Multiplier");
            //rudderRotationSpeedMult = Config.Bind<float>("Ships", "RudderRotationSpeedMult", 1f, "Rudder Rotation Speed Multiplier");
            minWaterImpactForceMult = Config.Bind<float>("Ships", "MinWaterImpactForceMult", 1f, "Min Water Impact Force Multiplier");
            minWaterImpactIntervalMult = Config.Bind<float>("Ships", "MinWaterImpactIntervalMult", 1f, "Min Water Impact Interval Multiplier");
            waterImpactDamageMult = Config.Bind<float>("Ships", "WaterImpactDamageMult", 1f, "Water Impact Damage Multiplier");
            upsideDownDmgIntervalMult = Config.Bind<float>("Ships", "UpsideDownDmgIntervalMult", 1f, "Upside Down Dmg Interval Multiplier");
            upsideDownDmgMult = Config.Bind<float>("Ships", "UpsideDownDmgMult", 1f, "Upside Down Dmg Multiplier");

            headWindDegrees = Config.Bind<float>("Custom", "HeadWindDegrees", 135f, "Degree difference between heading and wind to call headwind");
            windAngleFactorMin = Config.Bind<float>("Custom", "WindAngleFactorMin", 0.1f, "Wind Angle Factor Minimum");
            windAngleFactorMult = Config.Bind<float>("Custom", "WindAngleFactorMult", 1f, "Wind Angle Factor Multiplier");
            tailWindMult = Config.Bind<float>("Custom", "TailWindMult", 1f, "Tail Wind Multiplier");
            headWindMult = Config.Bind<float>("Custom", "HeadWindMult", 1f, "Head Wind Multiplier");
            damageMult = Config.Bind<float>("Custom", "DamageMult", 1f, "All Damage Multiplier");

            if (!modEnabled.Value)
                return;

            harmony = new Harmony("aedenthorn.ShipSpeed");
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony.UnpatchAll();
        }


        [HarmonyPatch(typeof(Ship), "Awake")]
        public static class Awake_Patch
        {
            public static void Postfix(Ship __instance, ref float ___m_forceDistance, ref float ___m_force, ref float ___m_damping, ref float ___m_dampingSideway, ref float ___m_dampingForward, ref float ___m_angularDamping, ref float ___m_disableLevel, ref float ___m_sailForceFactor, ref float ___m_rudderSpeed, ref float ___m_stearForceOffset, ref float ___m_stearForce, ref float ___m_stearVelForceFactor, ref float ___m_backwardForce, ref float ___m_rudderRotationMax, ref float ___m_minWaterImpactForce, ref float ___m_minWaterImpactInterval, ref float ___m_waterImpactDamage, ref float ___m_upsideDownDmgInterval, ref float ___m_upsideDownDmg)
            {
                ___m_forceDistance *= forceDistanceMult.Value;
                ___m_force *= forceMult.Value;
                ___m_damping *= dampingMult.Value;
                ___m_dampingSideway *= dampingSidewayMult.Value;
                ___m_dampingForward *= dampingForwardMult.Value;
                ___m_angularDamping *= angularDampingMult.Value;
                ___m_disableLevel *= disableLevelMult.Value;
                ___m_sailForceFactor *= sailForceFactorMult.Value;
                ___m_rudderSpeed *= rudderSpeedMult.Value;
                ___m_stearForceOffset *= stearForceOffsetMult.Value;
                ___m_stearForce *= stearForceMult.Value;
                ___m_stearVelForceFactor *= stearVelForceFactorMult.Value;
                ___m_backwardForce *= backwardForceMult.Value;
                ___m_rudderRotationMax *= rudderRotationMaxMult.Value;
                ___m_minWaterImpactForce *= minWaterImpactForceMult.Value;
                ___m_minWaterImpactInterval *= minWaterImpactIntervalMult.Value;
                ___m_waterImpactDamage *= waterImpactDamageMult.Value;
                ___m_upsideDownDmgInterval *= upsideDownDmgIntervalMult.Value;
                ___m_upsideDownDmg *= upsideDownDmgMult.Value;


            }
        }

        [HarmonyPatch(typeof(WearNTear), "Damage")]
        public static class Damage_Patch
        {
            public static void Prefix(WearNTear __instance, ref HitData hit)
            {
                if (__instance.gameObject.GetComponent<Ship>() == null)
                    return;
                MultiplyDamage(ref hit, damageMult.Value);

            }
        }

        public static void MultiplyDamage(ref HitData hit, float value)
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

        [HarmonyPatch(typeof(Ship), "GetSailForce")]
        public static class FixedUpdate_Patch
        {
            public static void Postfix(Ship __instance, ref Vector3 __result)
            {
                
                float degrees = Vector3.Angle(EnvMan.instance.GetWindDir(), __instance.transform.forward);
                if(degrees > headWindDegrees.Value)
                {
                    //Dbgl($"Headwind {(int)degrees}");
                    __result *= headWindMult.Value;
                }
                else
                {
                    //Dbgl($"Tailwind {(int)degrees}");
                    __result *= tailWindMult.Value;
                }
            }
        }
        [HarmonyPatch(typeof(Ship), "GetWindAngleFactor")]
        public static class GetWindAngleFactor_Patch
        {
            public static void Postfix(Ship __instance, ref float __result)
            {
                __result = Math.Max(windAngleFactorMin.Value, __result * windAngleFactorMult.Value);
                __result = Math.Min(1, __result);
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
                if (text.ToLower().Equals("shipspeed reset"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Reloaded ship speed mod config values" }).GetValue();
                    context.Config.Reload();
                    context.Config.Save();
                    return false;
                }
                return true;
            }
        }
    }
}