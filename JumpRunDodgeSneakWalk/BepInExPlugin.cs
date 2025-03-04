using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace JumpRunDodgeSneakWalk
{
    [BepInPlugin("aedenthorn.JumpRunDodgeSneakWalk", "Jump Run Sneak Walk Swim", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<int> maxJumps;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> fallDamageMult;
        public static ConfigEntry<float> jumpVelocityMult;
        public static ConfigEntry<float> runSpeedMult;
        public static ConfigEntry<float> walkSpeedMult;
        public static ConfigEntry<float> dodgeSpeedMult;
        public static ConfigEntry<float> crouchSpeedMult;
        public static ConfigEntry<float> turnSpeedMult;
        public static ConfigEntry<float> swimSpeedMult;
        public static ConfigEntry<float> swimAccelerationMult;
        public static ConfigEntry<float> swimTurnSpeedMult;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin ).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            maxJumps = Config.Bind<int>("Config", "MaxJumps", 2, "The maximum number of sequential jumps (-1 for infinite)");
            jumpVelocityMult = Config.Bind<float>("Config", "JumpVelocityMult", 1f, "Jump velocity multiplier");
            fallDamageMult = Config.Bind<float>("Config", "FallDamageMult", 1f, "Fall damage multiplier (set to 0 to turn off fall damage)");
            runSpeedMult = Config.Bind<float>("Config", "RunSpeedMult", 1f, "Run speed multiplier");
            walkSpeedMult = Config.Bind<float>("Config", "WalkSpeedMult", 1f, "Walk speed multiplier");
            dodgeSpeedMult = Config.Bind<float>("Config", "DodgeSpeedMult", 1f, "Dodge speed multiplier");
            crouchSpeedMult = Config.Bind<float>("Config", "CrouchSpeedMult", 1f, "Crouch speed multiplier");
            turnSpeedMult = Config.Bind<float>("Config", "TurnSpeedMult", 1f, "Turn speed multiplier");
            swimSpeedMult = Config.Bind<float>("Config", "SwimSpeedMult", 1f, "Swim speed multiplier");
            swimAccelerationMult = Config.Bind<float>("Config", "SwimAccelerationMult", 1f, "Swim acceleration multiplier");
            swimTurnSpeedMult = Config.Bind<float>("Config", "SwimTurnSpeedMult", 1f, "Swim turn speed multiplier");
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 316, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static int JumpNumber { get; public set; }


        [HarmonyPatch(typeof(Player), "GetJogSpeedFactor")]
        public static class GetJogSpeedFactor_Patch
        {
            public static void Postfix(ref float __result)
            {
                if (modEnabled.Value)
                {
                    __result *= walkSpeedMult.Value;
                }
            }
        }
        [HarmonyPatch(typeof(Player), "GetRunSpeedFactor")]
        public static class GetRunSpeedFactor_Patch
        {
            public static void Postfix(ref float __result)
            {
                if (modEnabled.Value)
                {
                    __result *= runSpeedMult.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
        public static class UpdateGroundContact_Patch
        {

            public static void Prefix(Character __instance, ref float ___m_maxAirAltitude)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    ___m_maxAirAltitude = __instance.transform.position.y + (___m_maxAirAltitude - __instance.transform.position.y) * fallDamageMult.Value;
                }
            }
        }
        
        [HarmonyPatch(typeof(Character), "Awake")]
        public static class Character_Awake_Patch
        {

            public static void Prefix(Character __instance)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    __instance.m_crouchSpeed *= crouchSpeedMult.Value;
                    __instance.m_turnSpeed *= turnSpeedMult.Value;
                    __instance.m_jumpForce *= jumpVelocityMult.Value;
                    __instance.m_swimSpeed *= swimSpeedMult.Value;
                    __instance.m_swimAcceleration *= swimAccelerationMult.Value;
                    __instance.m_swimTurnSpeed *= swimTurnSpeedMult.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        public static class Jump_Patch
        {

            public static void Prefix(Character __instance, ref float ___m_lastGroundTouch, ref float ___m_maxAirAltitude)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    if (JumpNumber > 0 && __instance.IsOnGround())
                    {
                        JumpNumber = 0;
                    }

                    if (maxJumps.Value < 0 || JumpNumber < maxJumps.Value)
                    {
                        ___m_maxAirAltitude = __instance.transform.position.y;
                        ___m_lastGroundTouch = 0.1f;
                        JumpNumber++;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CharacterAnimEvent), "Speed")]
        public static class CharacterAnimEvent_Speed_Patch
        {
            public static void Prefix(Animator ___m_animator, Character ___m_character, ref float speedScale)
            {
                if (!modEnabled.Value || !(___m_character is Player))
                    return;

                if (___m_animator?.GetCurrentAnimatorClipInfo(0)?.Any() != true || ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip == null)
                {
                    return;
                }
                //Dbgl($"{___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name} speed {speedScale}");

                if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("dodge"))
                {
                    
                    speedScale *= dodgeSpeedMult.Value;
                    //Dbgl($"Dodge speed after {speedScale}");
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
                if (text.ToLower().Equals("jumprun reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "jumprun config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
