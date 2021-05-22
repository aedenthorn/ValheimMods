using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DamageMod
{
    [BepInPlugin("aedenthorn.DamageMod", "Damage Mod", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> tameDamageMult;
        public static ConfigEntry<float> wildDamageMult;
        public static ConfigEntry<float> playerDamageMult;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1239, "Nexus mod ID for updates");


            tameDamageMult = Config.Bind<float>("Variables", "TameDamageMult", 1f, "Multiplier for damage taken by tame creatures");
            wildDamageMult = Config.Bind<float>("Variables", "WildDamageMult", 1f, "Multiplier for damage taken by wild creatures");
            playerDamageMult = Config.Bind<float>("Variables", "PlayerDamageMult", 1f, "Multiplier for damage taken by players");

            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Character), "RPC_Damage")]
        static class RPC_Damage_Patch
        {
            static void Prefix(Character __instance, ref HitData hit)
            {
                if (!modEnabled.Value)
                    return;
                if (__instance.IsPlayer())
                    hit.ApplyModifier(playerDamageMult.Value);
                else if (__instance.IsTamed())
                    hit.ApplyModifier(tameDamageMult.Value);
                else
                    hit.ApplyModifier(wildDamageMult.Value);
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
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
