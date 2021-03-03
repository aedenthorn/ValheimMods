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
                    Dbgl($"{___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name} {AnimatorExtensions.GetCurrentStateName(___m_animator, 0)}");
                    if (___m_animator.GetCurrentAnimatorStateInfo(0).fullPathHash == Animator.StringToHash("swing_longsword0") || ___m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_longsword1") || ___m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_longsword2"))
                    {
                        Dbgl($"animation {0}: sword");
                        ___m_animator.speed = swordSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_axe0") || ___m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_axe1") || ___m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_axe2"))
                    {
                        Dbgl($"animation {0}: axe");
                        ___m_animator.speed = axeSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("knife_stab0") || ___m_animator.GetCurrentAnimatorStateInfo(0).IsName("knife_stab1") || ___m_animator.GetCurrentAnimatorStateInfo(0).IsName("knife_stab2"))
                    {
                        Dbgl($"animation {0}: axe");
                        ___m_animator.speed = knifeSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("atgeir_attack0") || ___m_animator.GetCurrentAnimatorStateInfo(0).IsName("atgeir_attack1") || ___m_animator.GetCurrentAnimatorStateInfo(0).IsName("atgeir_attack2"))
                    {
                        Dbgl($"animation {0}: atgeir");
                        ___m_animator.speed = atgeirSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("interact"))
                    {
                        Dbgl($"animation {0}: interact");
                        ___m_animator.speed = interactSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("jump"))
                    {
                        Dbgl($"animation {0}: jump");
                        ___m_animator.speed = jumpSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("bow_fire"))
                    {
                        Dbgl($"animation {0}: bow_fire");
                        ___m_animator.speed = bowFireSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("spear_poke"))
                    {
                        Dbgl($"animation {0}: spear_poke");
                        ___m_animator.speed = spearSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_pickaxe"))
                    {
                        Dbgl($"animation {0}: swing_pickaxe");
                        ___m_animator.speed = pickAxeSpeed.Value;
                    }
                    
                }

                return;

                // this works on all animations!


                switch (name)
                {
                    case "interact":
                        ___m_animator.speed = interactSpeed.Value;
                        break;
                    case "jump":
                        ___m_animator.speed = jumpSpeed.Value;
                        break;
                    case "bow_fire":
                        ___m_animator.speed = bowFireSpeed.Value;
                        break;
                    case "swing_pickaxe":
                        ___m_animator.speed = pickAxeSpeed.Value;
                        break;
                    case "swing_axe0":
                    case "swing_axe1":
                    case "swing_axe2":
                        ___m_animator.speed = axeSpeed.Value;
                        break;
                    case "spear_poke":
                        ___m_animator.speed = spearSpeed.Value;
                        break;
                    case "atgeir_attack0":
                    case "atgeir_attack1":
                    case "atgeir_attack2":
                        ___m_animator.speed = atgeirSpeed.Value;
                        break;
                    case "swing_longsword0":
                    case "swing_longsword1":
                    case "swing_longsword2":
                        ___m_animator.speed = swordSpeed.Value;
                        break;
                    case "knife_stab0":
                    case "knife_stab1":
                    case "knife_stab2":
                        ___m_animator.speed = knifeSpeed.Value;
                        break;
                    default:
                        ___m_animator.speed = 1f;
                        break;
                }
                Dbgl($"animation: {name} new speed {___m_animator.speed} ");
            }
        }

    }
}
