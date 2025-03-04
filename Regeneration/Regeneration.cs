using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace Regeneration
{
    [BepInPlugin("aedenthorn.Regeneration", "Regeneration", "0.4.1")]
    public class Regeneration : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<float> staminaLossMult;
        public static ConfigEntry<float> sneakStaminaLoss;
        
        public static ConfigEntry<float> swimStaminaLossMin;
        public static ConfigEntry<float> swimStaminaLossMax;

        public static ConfigEntry<float> runStaminaLoss;
        public static ConfigEntry<float> dodgeStaminaLoss;
        public static ConfigEntry<float> jumpStaminaLoss;
        public static ConfigEntry<float> blockStaminaLoss;

        public static ConfigEntry<float> buildStaminaLossMult;
        public static ConfigEntry<float> hookedStaminaPerSec;
        
        public static ConfigEntry<float> encumberedStaminaLoss;
        
        public static ConfigEntry<float> staminaRegenMult;
        public static ConfigEntry<float> staminaRegenCooldownMult;
        public static ConfigEntry<float> eitrRegenRate;
        public static ConfigEntry<float> healthRegenMult;
        public static ConfigEntry<float> healthRegenTimeMult;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(Regeneration).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            staminaLossMult = Config.Bind<float>("Options", "StaminaLossMult", 1f, "General stamina loss multiplier (affects all stamina loss).");

            
            swimStaminaLossMin = Config.Bind<float>("Options", "SwimStaminaLossMin", 5f, "Stamina loss for swimming at minimum swim skill.");
            swimStaminaLossMax = Config.Bind<float>("Options", "SwimStaminaLossMax", 2f, "Stamina loss for swimming at maximum swim skill.");

            dodgeStaminaLoss = Config.Bind<float>("Options", "DodgeStaminaLoss", 10f, "Stamina loss for dodging.");
            jumpStaminaLoss = Config.Bind<float>("Options", "JumpStaminaLoss", 10f, "Stamina loss for jumping.");
            blockStaminaLoss = Config.Bind<float>("Options", "BlockStaminaLoss", 25f, "Stamina loss for blocking.");
            sneakStaminaLoss = Config.Bind<float>("Options", "SneakStaminaLoss", 5f, "Stamina loss for sneaking.");
            runStaminaLoss = Config.Bind<float>("Options", "RunStaminaLoss", 10f, "Stamina loss for running.");

            encumberedStaminaLoss = Config.Bind<float>("Options", "EncumberedStaminaLoss", 10f, "Stamina loss when encumbered.");

            buildStaminaLossMult = Config.Bind<float>("Options", "BuildStaminaLossMult", 1f, "Stamina loss multiplier for building.");
            hookedStaminaPerSec = Config.Bind<float>("Options", "HookedStaminaPerSec", 1f, "Stamina loss per second while reeling in a hooked fish.");

            staminaRegenMult = Config.Bind<float>("Options", "StaminaRegenMult", 1f, "Stamina gain multiplier.");
            staminaRegenCooldownMult = Config.Bind<float>("Options", "StaminaRegenCooldownMult", 1f, "Stamina regen cooldown time multiplier.");

            eitrRegenRate = Config.Bind<float>("Options", "EitrRegenRate", 5f, "Eitr Regen Rate.");
            healthRegenMult = Config.Bind<float>("Options", "HealthRegenMult", 1f, "Health gain multiplier.");
            healthRegenTimeMult = Config.Bind<float>("Options", "HealthRegenTimeMult", 1f, "Health gain delay multiplier.");

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 25, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        [HarmonyPatch(typeof(Player), "UpdateFood")]
        public static class UpdateFood_Patch
        {
            public static void Prefix(float dt, bool forceUpdate, ref float ___m_foodRegenTimer)
            {
                if (modEnabled.Value && !forceUpdate)
                {
                    ___m_foodRegenTimer += dt / healthRegenTimeMult.Value - dt;
                }
            }
        }

        [HarmonyPatch(typeof(Player), "UpdateStats", new Type[] { typeof(float) })]
        public static class UpdateStats_Patch
        {
            public static void Prefix(Player __instance, ref float __state, float ___m_stamina, ref float ___m_staminaRegenTimer, ref float ___m_eiterRegen)
            {
                if (modEnabled.Value)
                {
                    ___m_eiterRegen = eitrRegenRate.Value;
                    __instance.m_dodgeStaminaUsage = dodgeStaminaLoss.Value;
                    __instance.m_jumpStaminaUsage = jumpStaminaLoss.Value;
                    __instance.m_blockStaminaDrain = blockStaminaLoss.Value;
                    __instance.m_sneakStaminaDrain = sneakStaminaLoss.Value;
                    __instance.m_runStaminaDrain = runStaminaLoss.Value;
                    
                    __instance.m_encumberedStaminaDrain = encumberedStaminaLoss.Value;

                    __instance.m_swimStaminaDrainMaxSkill = swimStaminaLossMax.Value;
                    __instance.m_swimStaminaDrainMaxSkill = swimStaminaLossMin.Value;

                    ___m_staminaRegenTimer *= staminaRegenCooldownMult.Value;
                    __state = ___m_stamina;
                }
            }
            public static void Postfix(Player __instance, float __state, ref float ___m_stamina)
            {
                if (modEnabled.Value)
                {
                    if (__state > 0 && ___m_stamina > __state)
                    {
                        ___m_stamina = Mathf.Max(0, __state + (___m_stamina - __state) * staminaRegenMult.Value);
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Player), "UpdatePlacement")]
        public static class UpdatePlacement_Patch
        {
            public static void Prefix(Player __instance, ref float __state, float ___m_stamina)
            {
                if (modEnabled.Value)
                {
                    __state = ___m_stamina;
                }
            }
            public static void Postfix(Player __instance, float __state, ref float ___m_stamina)
            {
                if (modEnabled.Value)
                {
                    if (__state > 0 && ___m_stamina < __state)
                    {
                        ___m_stamina = Mathf.Max(0, __state - (__state- ___m_stamina) * buildStaminaLossMult.Value);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(FishingFloat), "Awake")]
        public static class FishingFloat_Awake_Patch
        {
            public static void Postfix(ref FishingFloat __instance)
            {
                if (modEnabled.Value)
                {
                    __instance.m_hookedStaminaPerSec = hookedStaminaPerSec.Value;
                }
            }
        }


        [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
        public static class UseStamina_Patch
        {
            public static void Prefix(ref float v)
            {
                if (modEnabled.Value)
                {
                    v *= staminaLossMult.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Heal))]
        public static class Heal_Patch
        {
            public static void Prefix(Character __instance, ref float hp)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    hp *= healthRegenMult.Value;
                }
            }
        }

    }
}
