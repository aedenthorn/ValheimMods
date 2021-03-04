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
    [BepInPlugin("aedenthorn.AnimationSpeed", "Animation Speed", "0.4.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> interactSpeed;
        public static ConfigEntry<float> jumpSpeed;
        public static ConfigEntry<float> bowFireSpeed;
        public static ConfigEntry<float> axeSpeed;
        public static ConfigEntry<float> battleAxeSpeed;
        public static ConfigEntry<float> clubSpeed;
        public static ConfigEntry<float> pickAxeSpeed;
        public static ConfigEntry<float> spearSpeed;
        public static ConfigEntry<float> atgeirSpeed;
        public static ConfigEntry<float> swordSpeed;
        public static ConfigEntry<float> knifeSpeed;
        public static ConfigEntry<float> hammerSpeed;
        public static ConfigEntry<float> unarmedSpeed;

        public static ConfigEntry<float> interactEnemySpeed;
        public static ConfigEntry<float> jumpEnemySpeed;
        public static ConfigEntry<float> bowFireEnemySpeed;
        public static ConfigEntry<float> axeEnemySpeed;
        public static ConfigEntry<float> battleAxeEnemySpeed;
        public static ConfigEntry<float> clubEnemySpeed;
        public static ConfigEntry<float> pickAxeEnemySpeed;
        public static ConfigEntry<float> spearEnemySpeed;
        public static ConfigEntry<float> atgeirEnemySpeed;
        public static ConfigEntry<float> swordEnemySpeed;
        public static ConfigEntry<float> knifeEnemySpeed;
        public static ConfigEntry<float> hammerEnemySpeed;
        public static ConfigEntry<float> unarmedEnemySpeed;

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

            bowFireSpeed = Config.Bind<float>("General", "BowFireSpeed", 1f, "Speed for firing bows");
            axeSpeed = Config.Bind<float>("General", "AxeSpeed", 1f, "Speed for axe swing");
            battleAxeSpeed = Config.Bind<float>("General", "BattleAxeSpeed", 1f, "Speed for battle axe swing");
            clubSpeed = Config.Bind<float>("General", "ClubSpeed", 1f, "Speed for club swing");
            pickAxeSpeed = Config.Bind<float>("General", "PickAxeSpeed", 1f, "Speed for pick axe swing");
            spearSpeed = Config.Bind<float>("General", "SpearSpeed", 1f, "Speed for spear stab");
            atgeirSpeed = Config.Bind<float>("General", "AtgeirSpeed", 1f, "Speed for atgeir thrust");
            swordSpeed = Config.Bind<float>("General", "SwordSpeed", 1f, "Speed for sword swing");
            hammerSpeed = Config.Bind<float>("General", "HammerSpeed", 1f, "Speed for hammer swing");
            knifeSpeed = Config.Bind<float>("General", "KnifeSpeed", 1f, "Speed for knife stab");
            unarmedSpeed = Config.Bind<float>("General", "UnarmedSpeed", 1f, "Speed for punch attacks");

            bowFireEnemySpeed = Config.Bind<float>("General", "BowFireEnemySpeed", 1f, "Enemy speed for firing bows");
            axeEnemySpeed = Config.Bind<float>("General", "AxeEnemySpeed", 1f, "Enemy speed for axe swing");
            battleAxeEnemySpeed = Config.Bind<float>("General", "BattleAxeEnemySpeed", 1f, "Enemy speed for battle axe swing");
            clubEnemySpeed = Config.Bind<float>("General", "ClubEnemySpeed", 1f, "Enemy speed for club swing");
            pickAxeEnemySpeed = Config.Bind<float>("General", "PickAxeEnemySpeed", 1f, "Enemy speed for pick axe swing");
            spearEnemySpeed = Config.Bind<float>("General", "SpearEnemySpeed", 1f, "Enemy speed for spear stab");
            atgeirEnemySpeed = Config.Bind<float>("General", "AtgeirEnemySpeed", 1f, "Enemy speed for atgeir thrust");
            swordEnemySpeed = Config.Bind<float>("General", "SwordEnemySpeed", 1f, "Enemy speed for sword swing");
            hammerEnemySpeed = Config.Bind<float>("General", "HammerEnemySpeed", 1f, "Speed for hammer swing");
            knifeEnemySpeed = Config.Bind<float>("General", "KnifeEnemySpeed", 1f, "Enemy speed for knife attacks");
            unarmedEnemySpeed = Config.Bind<float>("General", "UnarmedEnemySpeed", 1f, "Enemy speed for punch attacks");

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
                if (!modEnabled.Value || !(___m_character is Humanoid || ___m_character is Player) || Player.m_localPlayer == null || (___m_character is Player &&  (___m_character as Player).GetPlayerID() != Player.m_localPlayer.GetPlayerID()))
                    return;
                
                bool enemy = !(___m_character is Player);

                string name = ___m_animator.name;
                if (___m_character.InAttack())
                {
                    /*
                    try
                    {
                        if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true)
                            Dbgl($"{___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");
                    }
                    catch { }
                    try
                    {
                        if ((___m_character as Humanoid).GetCurrentWeapon() != null)
                            Dbgl($"{(___m_character as Humanoid).GetCurrentWeapon()?.m_dropPrefab.name} {(___m_character as Humanoid).GetCurrentWeapon()?.m_shared.m_skillType}");

                    }
                    catch { }
                    */
                    if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("Attack"))
                    {
                        if((___m_character as Humanoid).GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Clubs)
                            ___m_animator.speed = enemy ? clubEnemySpeed.Value : clubSpeed.Value;
                        else if((___m_character as Humanoid).GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Swords)
                            ___m_animator.speed = enemy ? swordEnemySpeed.Value : swordSpeed.Value;
                        else
                            ___m_animator.speed = enemy ? swordEnemySpeed.Value : swordSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("Sledge-Attack"))
                    {
                        ___m_animator.speed = enemy ? hammerEnemySpeed.Value : hammerSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && (___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("BattleAxe")))
                    {
                        ___m_animator.speed = enemy ? battleAxeEnemySpeed.Value : battleAxeSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && (___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "axe_swing" || ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("Axe")))
                    {
                        ___m_animator.speed = enemy ? axeEnemySpeed.Value : axeSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("knife_slash"))
                    {
                        ___m_animator.speed = enemy ? knifeEnemySpeed.Value : knifeSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("2Hand-Spear-"))
                    {
                        ___m_animator.speed = enemy ? atgeirEnemySpeed.Value : atgeirSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("bow_fire"))
                    {
                        ___m_animator.speed = enemy ? bowFireEnemySpeed.Value : bowFireSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("spear_poke"))
                    {
                        ___m_animator.speed = enemy ? spearEnemySpeed.Value : spearSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_pickaxe"))
                    {
                        ___m_animator.speed = enemy ? pickAxeEnemySpeed.Value : pickAxeSpeed.Value;
                    }
                    else if (___m_animator.GetCurrentAnimatorClipInfo(0)?.Any() == true && ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("Punchstep"))
                    {
                        ___m_animator.speed = enemy ? unarmedEnemySpeed.Value : unarmedSpeed.Value;
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
