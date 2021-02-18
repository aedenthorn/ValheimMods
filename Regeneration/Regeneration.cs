using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace Regeneration
{
    [BepInPlugin("aedenthorn.Regeneration", "Regeneration", "0.2.0")]
    public class Regeneration : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> staminaLossMult;
        public static ConfigEntry<float> sneakStaminaLoss;
        
        public static ConfigEntry<float> swimStaminaLossMin;
        public static ConfigEntry<float> swimStaminaLossMax;

        public static ConfigEntry<float> runStaminaLoss;
        public static ConfigEntry<float> dodgeStaminaLoss;
        public static ConfigEntry<float> jumpStaminaLoss;
        public static ConfigEntry<float> blockStaminaLoss;
        public static ConfigEntry<float> buildStaminaLossMult;
        
        public static ConfigEntry<float> weightStaminaFactor;
        public static ConfigEntry<float> encumberedStaminaLoss;
        
        public static ConfigEntry<float> staminaRegenMult;
        public static ConfigEntry<float> staminaRegenCooldownMult;
        public static ConfigEntry<float> healthRegenMult;
        public static ConfigEntry<float> healthRegenTimeMult;

        public static ConfigEntry<bool> modEnabled; 

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(Regeneration).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            staminaLossMult = Config.Bind<float>("Stamina", "StaminaLossMult", 1f, "General stamina loss multiplier (affects all stamina loss).");

            
            swimStaminaLossMin = Config.Bind<float>("Stamina", "SwimStaminaLossMin", 5f, "Stamina loss for swimming at minimum swim skill.");
            swimStaminaLossMax = Config.Bind<float>("Stamina", "SwimStaminaLossMax", 2f, "Stamina loss for swimming at maximum swim skill.");

            dodgeStaminaLoss = Config.Bind<float>("Stamina", "DodgeStaminaLoss", 10f, "Stamina loss for dodging.");
            jumpStaminaLoss = Config.Bind<float>("Stamina", "JumpStaminaLoss", 10f, "Stamina loss for jumping.");
            blockStaminaLoss = Config.Bind<float>("Stamina", "BlockStaminaLoss", 25f, "Stamina loss for blocking.");
            sneakStaminaLoss = Config.Bind<float>("Stamina", "SneakStaminaLoss", 5f, "Stamina loss for running.");
            runStaminaLoss = Config.Bind<float>("Stamina", "RunStaminaLoss", 10f, "Stamina loss for running.");

            weightStaminaFactor = Config.Bind<float>("Stamina", "WeightStaminaFactor", 0.1f, "Stamina loss weight factor.");
            encumberedStaminaLoss = Config.Bind<float>("Stamina", "EncumberedStaminaLoss", 10f, "Stamina loss when encumbered.");

            buildStaminaLossMult = Config.Bind<float>("Stamina", "BuildStaminaLossMult", 1f, "Stamina loss multiplier for building.");

            staminaRegenMult = Config.Bind<float>("Stamina", "StaminaRegenMult", 1f, "Stamina gain multiplier.");
            staminaRegenCooldownMult = Config.Bind<float>("Stamina", "StaminaRegenCooldownMult", 1f, "Stamina regen cooldown time multiplier.");

            healthRegenMult = Config.Bind<float>("Stamina", "HealthRegenMult", 1f, "Health gain multiplier.");
            healthRegenTimeMult = Config.Bind<float>("Stamina", "HealthRegenTimeMult", 1f, "Health gain delay multiplier.");

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        [HarmonyPatch(typeof(Player), "UpdateFood")]
        static class UpdateFood_Patch
        {
            static void Prefix(float dt, bool forceUpdate, ref float ___m_foodRegenTimer)
            {
                if (modEnabled.Value && !forceUpdate)
                {
                    ___m_foodRegenTimer += dt / healthRegenTimeMult.Value - dt;
                }
            }
        }

        [HarmonyPatch(typeof(Player), "UpdateStats", new Type[] { typeof(float) })]
        static class UpdateStats_Patch
        {
            static void Prefix(Player __instance, ref float __state, float ___m_stamina, ref float ___m_staminaRegenTimer)
            {
                if (modEnabled.Value)
                {
                    __instance.m_dodgeStaminaUsage = dodgeStaminaLoss.Value;
                    __instance.m_jumpStaminaUsage = jumpStaminaLoss.Value;
                    __instance.m_blockStaminaDrain = blockStaminaLoss.Value;
                    __instance.m_sneakStaminaDrain = sneakStaminaLoss.Value;
                    __instance.m_runStaminaDrain = runStaminaLoss.Value;
                    
                    __instance.m_encumberedStaminaDrain = encumberedStaminaLoss.Value;
                    __instance.m_weightStaminaFactor = weightStaminaFactor.Value;

                    __instance.m_swimStaminaDrainMaxSkill = swimStaminaLossMax.Value;
                    __instance.m_swimStaminaDrainMaxSkill = swimStaminaLossMin.Value;

                    ___m_staminaRegenTimer *= staminaRegenCooldownMult.Value;
                    __state = ___m_stamina;
                }
            }
            static void Postfix(Player __instance, float __state, ref float ___m_stamina)
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
        static class UpdatePlacement_Patch
        {
            static void Prefix(Player __instance, ref float __state, float ___m_stamina)
            {
                if (modEnabled.Value)
                {
                    __state = ___m_stamina;
                }
            }
            static void Postfix(Player __instance, float __state, ref float ___m_stamina)
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


        [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
        static class UseStamina_Patch
        {
            static void Prefix(ref float v)
            {
                if (modEnabled.Value)
                {
                    v *= staminaLossMult.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Heal))]
        static class Heal_Patch
        {
            static void Prefix(Character __instance, ref float hp)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    hp *= healthRegenMult.Value;
                }
            }
        }

    }
}
