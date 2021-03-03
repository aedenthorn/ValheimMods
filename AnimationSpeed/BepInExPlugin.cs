using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace AnimationSpeed
{
    [BepInPlugin("aedenthorn.AnimationSpeed", "Animation Speed", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> interactSpeed;
        public static ConfigEntry<float> jumpSpeed;
        public static ConfigEntry<float> bowFireSpeed;
        public static ConfigEntry<float> axeSpeed;
        public static ConfigEntry<float> pickAxeSpeed;
        public static ConfigEntry<float> spearSpeed;
        public static ConfigEntry<float> atgeirSpeed;
        public static ConfigEntry<float> swordSpeed;
        public static ConfigEntry<float> knifeSpeed;

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 240, "Nexus mod ID for updates");

            interactSpeed = Config.Bind<float>("General", "interactSpeed", 2f, "Speed for interactions");
            jumpSpeed = Config.Bind<float>("General", "jumpSpeed", 2f, "Speed for jumping");
            bowFireSpeed = Config.Bind<float>("General", "bowFireSpeed", 2f, "Speed for firing bows");
            axeSpeed = Config.Bind<float>("General", "axeSpeed", 2f, "Speed for axe swing");
            pickAxeSpeed = Config.Bind<float>("General", "PickAxeSpeed", 2f, "Speed for pick axe swing");
            spearSpeed = Config.Bind<float>("General", "spearSpeed", 2f, "Speed for spear stab");
            atgeirSpeed = Config.Bind<float>("General", "atgeirSpeed", 2f, "Speed for atgeir thrust");
            swordSpeed = Config.Bind<float>("General", "SwordSpeed", 2f, "Speed for sword swing");
            knifeSpeed = Config.Bind<float>("General", "SwordSpeed", 2f, "Speed for knife stab");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(CharacterAnimEvent), "FixedUpdate")]
        static class CharacterAnimEvent_FixedUpdate_Patch
        {
            static void Postfix(ref Animator ___m_animator, Character ___m_character)
            {
                if (!modEnabled.Value || ___m_character == null || Player.m_localPlayer == null || !(___m_character is Player) || (___m_character as Player).GetPlayerID() != Player.m_localPlayer.GetPlayerID())
                    return;

                string name = ___m_animator.name;
                if (___m_character.InAttack())
                {
                    //Dbgl($"{___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name} {AnimatorExtensions.GetCurrentStateName(___m_animator, 0)}");
                    if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("Attack"))
                    {
                        ___m_animator.speed = swordSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "axe_swing")
                    {
                        ___m_animator.speed = axeSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("knife_slash"))
                    {
                        ___m_animator.speed = knifeSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("2Hand-Spear-"))
                    {
                        ___m_animator.speed = atgeirSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("bow_fire"))
                    {
                        ___m_animator.speed = bowFireSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("spear_poke"))
                    {
                        ___m_animator.speed = spearSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_pickaxe"))
                    {
                        ___m_animator.speed = pickAxeSpeed.Value;
                    }
                    
                }

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
                if (text.ToLower().Equals("animationspeed reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Animation Speed config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
