using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DamageMod
{
    [BepInPlugin("aedenthorn.DamageMod", "Damage Mod", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> tameDamageMult;
        public static ConfigEntry<float> wildDamageMult;
        public static ConfigEntry<float> playerDamageMult;
        
        public static ConfigEntry<string> customAttackerDamageMult;
        public static ConfigEntry<string> customDefenderDamageMult;

        public static Dictionary<string, float> attackerMults = new Dictionary<string, float>();
        public static Dictionary<string, float> defenderMults = new Dictionary<string, float>();


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1239, "Nexus mod ID for updates");

            tameDamageMult = Config.Bind<float>("Variables", "TameDamageMult", 1f, "Multiplier for damage taken by tame creatures");
            wildDamageMult = Config.Bind<float>("Variables", "WildDamageMult", 1f, "Multiplier for damage taken by wild creatures");
            playerDamageMult = Config.Bind<float>("Variables", "PlayerDamageMult", 1f, "Multiplier for damage taken by players");
            
            customAttackerDamageMult = Config.Bind<string>("Variables", "CustomAttackerDamageMult", "", "Custom attacker damage multipliers. Use comma-separated list of pairs separated by colon (:), e.g. Boar:1.5,Wolf:0.5");
            customDefenderDamageMult = Config.Bind<string>("Variables", "CustomDefenderDamageMult", "", "Custom defender damage multipliers. Use comma-separated list of pairs separated by colon (:), e.g. Boar:1.5,Wolf:0.5");

            if (!modEnabled.Value)
                return;

            SetCustomDamages();

            customAttackerDamageMult.SettingChanged += SettingChanged;
            customDefenderDamageMult.SettingChanged += SettingChanged;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void SettingChanged(object sender, System.EventArgs e)
        {
            SetCustomDamages();
        }

        public static void SetCustomDamages()
        {
            Dbgl(customAttackerDamageMult.Value);
            Dbgl(customDefenderDamageMult.Value);
            foreach (string pair in customAttackerDamageMult.Value.Split(','))
            {
                if (!pair.Contains(":") || !float.TryParse(pair.Split(':')[1], NumberStyles.Any, CultureInfo.InvariantCulture,  out float result))
                    continue;

                attackerMults.Add(pair.Split(':')[0], result);
            }

            foreach (string pair in customDefenderDamageMult.Value.Split(','))
            {
                if (!pair.Contains(":") || !float.TryParse(pair.Split(':')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                    continue;

                defenderMults.Add(pair.Split(':')[0], result);
            }
        }

        [HarmonyPatch(typeof(Character), "RPC_Damage")]
        public static class RPC_Damage_Patch
        {
            public static void Prefix(Character __instance, ref HitData hit)
            {
                if (!modEnabled.Value)
                    return;

                var attacker = hit.GetAttacker() ? Utils.GetPrefabName(hit.GetAttacker().gameObject) : "";
                var defender = Utils.GetPrefabName(__instance.gameObject);

                if (__instance.IsPlayer())
                    hit.ApplyModifier(playerDamageMult.Value);
                else if (__instance.IsTamed())
                    hit.ApplyModifier(tameDamageMult.Value);
                else 
                    hit.ApplyModifier(wildDamageMult.Value);

                if (defenderMults.TryGetValue(defender, out float mult1))
                {
                    Dbgl($"Applying mult of {mult1} for defender {defender}");
                    hit.ApplyModifier(mult1);
                }
                else if (hit.GetAttacker() && attackerMults.TryGetValue(attacker, out float mult2))
                {
                    Dbgl($"Applying mult of {mult2} for attacker {attacker}");
                    hit.ApplyModifier(mult2);
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

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
