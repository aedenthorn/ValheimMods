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
    [BepInPlugin("aedenthorn.AnimationSpeed", "Animation Speed", "1.0.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static Dictionary<long, string> lastAnims = new Dictionary<long, string>();

        public static readonly bool isDebug = false;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> bowFireSpeedMult;
        public static ConfigEntry<float> axeSpeedMult;
        public static ConfigEntry<float> battleAxeSpeedMult;
        public static ConfigEntry<float> clubSpeedMult;
        public static ConfigEntry<float> pickAxeSpeedMult;
        public static ConfigEntry<float> spearSpeedMult;
        public static ConfigEntry<float> polearmSpeedMult;
        public static ConfigEntry<float> swordSpeedMult;
        public static ConfigEntry<float> knifeSpeedMult;
        public static ConfigEntry<float> hammerSpeedMult;
        public static ConfigEntry<float> unarmedSpeedMult;
        public static ConfigEntry<float> basicAttackSpeedMult;

        public static ConfigEntry<float> interactEnemySpeedMult;
        public static ConfigEntry<float> jumpEnemySpeedMult;
        public static ConfigEntry<float> bowFireEnemySpeedMult;
        public static ConfigEntry<float> axeEnemySpeedMult;
        public static ConfigEntry<float> battleAxeEnemySpeedMult;
        public static ConfigEntry<float> clubEnemySpeedMult;
        public static ConfigEntry<float> pickAxeEnemySpeedMult;
        public static ConfigEntry<float> spearEnemySpeedMult;
        public static ConfigEntry<float> polearmEnemySpeedMult;
        public static ConfigEntry<float> swordEnemySpeedMult;
        public static ConfigEntry<float> knifeEnemySpeedMult;
        public static ConfigEntry<float> hammerEnemySpeedMult;
        public static ConfigEntry<float> unarmedEnemySpeedMult;
        public static ConfigEntry<float> basicEnemyAttackSpeedMult;

        public static BepInExPlugin context;
        
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 240, "Nexus mod ID for updates");

            bowFireSpeedMult = Config.Bind<float>("General", "BowFireSpeedMult", 1f, "Speed multiplier for firing bows");
            axeSpeedMult = Config.Bind<float>("General", "AxeSpeedMult", 1f, "Speed multiplier for axe swing");
            battleAxeSpeedMult = Config.Bind<float>("General", "BattleAxeSpeedMult", 1f, "Speed multiplier for battle axe swing");
            clubSpeedMult = Config.Bind<float>("General", "ClubSpeedMult", 1f, "Speed multiplier for club swing");
            pickAxeSpeedMult = Config.Bind<float>("General", "PickAxeSpeedMult", 1f, "Speed multiplier for pick axe swing");
            spearSpeedMult = Config.Bind<float>("General", "SpearSpeedMult", 1f, "Speed multiplier for spear stab");
            polearmSpeedMult = Config.Bind<float>("General", "AtgeirSpeedMult", 1f, "Speed multiplier for atgeir thrust");
            swordSpeedMult = Config.Bind<float>("General", "SwordSpeedMult", 1f, "Speed multiplier for sword swing");
            hammerSpeedMult = Config.Bind<float>("General", "HammerSpeedMult", 1f, "Speed multiplier for hammer swing");
            knifeSpeedMult = Config.Bind<float>("General", "KnifeSpeedMult", 1f, "Speed multiplier for knife stab");
            unarmedSpeedMult = Config.Bind<float>("General", "UnarmedSpeedMult", 1f, "Speed multiplier for punch attacks");
            basicAttackSpeedMult = Config.Bind<float>("General", "BasicAttackSpeedMult", 1f, "Speed multiplier for basic (unnamed) parts of attacks");

            bowFireEnemySpeedMult = Config.Bind<float>("General", "BowFireEnemySpeedMult", 1f, "Enemy speed for firing bows");
            axeEnemySpeedMult = Config.Bind<float>("General", "AxeEnemySpeedMult", 1f, "Enemy speed for axe swing");
            battleAxeEnemySpeedMult = Config.Bind<float>("General", "BattleAxeEnemySpeedMult", 1f, "Enemy speed for battle axe swing");
            clubEnemySpeedMult = Config.Bind<float>("General", "ClubEnemySpeedMult", 1f, "Enemy speed for club swing");
            pickAxeEnemySpeedMult = Config.Bind<float>("General", "PickAxeEnemySpeedMult", 1f, "Enemy speed for pick axe swing");
            spearEnemySpeedMult = Config.Bind<float>("General", "SpearEnemySpeedMult", 1f, "Enemy speed for spear stab");
            polearmEnemySpeedMult = Config.Bind<float>("General", "AtgeirEnemySpeedMult", 1f, "Enemy speed for atgeir thrust");
            swordEnemySpeedMult = Config.Bind<float>("General", "SwordEnemySpeedMult", 1f, "Enemy speed for sword swing");
            hammerEnemySpeedMult = Config.Bind<float>("General", "HammerEnemySpeedMult", 1f, "Speed multiplier for hammer swing");
            knifeEnemySpeedMult = Config.Bind<float>("General", "KnifeEnemySpeedMult", 1f, "Enemy speed for knife attacks");
            unarmedEnemySpeedMult = Config.Bind<float>("General", "UnarmedEnemySpeedMult", 1f, "Enemy speed for punch attacks");
            basicEnemyAttackSpeedMult = Config.Bind<float>("General", "BasicEnemyAttackSpeedMult", 1f, "Enemy speed multiplier for basic (unnamed) parts of attacks");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static bool CheckKeyDown(string value)
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
        [HarmonyPatch(typeof(CharacterAnimEvent), "Speed")]
        public static class CharacterAnimEvent_Speed_Patch
        {
            public static void Postfix(Character ___m_character)
            {
                if (___m_character is Player)
                    lastAnims.Remove((___m_character as Player).GetPlayerID());
            }
        }

        [HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.CustomFixedUpdate))]
        public static class CharacterAnimEvent_CustomFixedUpdate_Patch
        {
            public static void Prefix(ref Animator ___m_animator, Character ___m_character)
            {
                if (!modEnabled.Value || !(___m_character is Humanoid) || Player.m_localPlayer == null || (___m_character is Player && (___m_character as Player).GetPlayerID() != Player.m_localPlayer.GetPlayerID()))
                    return;

                if (___m_animator?.GetCurrentAnimatorClipInfo(0)?.Any() != true || ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip == null)
                {
                    //Dbgl($"current clip is null");
                    return;
                }

                if (___m_character.InAttack() && ___m_character is Player && (___m_character as Player).GetPlayerID() == Player.m_localPlayer.GetPlayerID())
                {
                    Dbgl($"{___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name} speed {___m_animator.speed}");
                    if ((___m_character as Humanoid).GetCurrentWeapon() != null)
                        Dbgl($"{(___m_character as Humanoid).GetCurrentWeapon()?.m_dropPrefab?.name} {(___m_character as Humanoid).GetCurrentWeapon()?.m_shared?.m_skillType}");
                }

                bool enemy = !(___m_character is Player);

                if (___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("Attack"))
                {
                    if ((___m_character as Humanoid).GetCurrentWeapon()?.m_shared?.m_skillType == Skills.SkillType.Clubs)
                        ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? clubEnemySpeedMult.Value : clubSpeedMult.Value);
                    else if ((___m_character as Humanoid).GetCurrentWeapon()?.m_shared?.m_skillType == Skills.SkillType.Swords)
                        ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? swordEnemySpeedMult.Value : swordSpeedMult.Value);
                    else
                        ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? swordEnemySpeedMult.Value : swordSpeedMult.Value);
                }
                else if (___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("Sledge-Attack"))
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? hammerEnemySpeedMult.Value : hammerSpeedMult.Value);
                }
                else if ((___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("BattleAxe")))
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? battleAxeEnemySpeedMult.Value : battleAxeSpeedMult.Value);
                }
                else if ((___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("axe_swing") || ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("Axe")))
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? axeEnemySpeedMult.Value : axeSpeedMult.Value);
                }
                else if (___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("knife_slash"))
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? knifeEnemySpeedMult.Value : knifeSpeedMult.Value);
                }
                else if (___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("2Hand-Spear-"))
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? polearmEnemySpeedMult.Value : polearmSpeedMult.Value);
                }
                else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("bow_fire"))
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? bowFireEnemySpeedMult.Value : bowFireSpeedMult.Value);
                }
                else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("spear_poke"))
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? spearEnemySpeedMult.Value : spearSpeedMult.Value);
                }
                else if (___m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_pickaxe"))
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? pickAxeEnemySpeedMult.Value : pickAxeSpeedMult.Value);
                }
                else if (___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("Punchstep"))
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? unarmedEnemySpeedMult.Value : unarmedSpeedMult.Value);
                }
                else if (___m_character.InAttack())
                {
                    ___m_animator.speed = ChangeSpeed(___m_character, ___m_animator, enemy ? basicEnemyAttackSpeedMult.Value : basicAttackSpeedMult.Value);
                }
                else if (___m_character is Player)
                    lastAnims.Remove((___m_character as Player).GetPlayerID());

            }
        }

        public static float ChangeSpeed(Character character, Animator animator, float speed)
        {
            if(character is Player)
            {
                long id = (character as Player).GetPlayerID();
                string name = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
                float newSpeed = animator.speed * speed;
                if (!lastAnims.ContainsKey(id) || lastAnims[id] != name)
                {
                    Dbgl($"setting speed for {name} from {speed} to {newSpeed}");
                    lastAnims[id] = name;
                    return newSpeed;
                }
                else
                {
                    Dbgl($"not changing speed for {name} {lastAnims.ContainsKey(id)}");
                }
            }
            return animator.speed;
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

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
