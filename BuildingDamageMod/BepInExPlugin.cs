using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BuildingDamageMod
{
    [BepInPlugin("aedenthorn.BuildingDamageMod", "Building Damage Mod", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> preventCreatorDamage;
        public static ConfigEntry<bool> preventNonCreatorDamage;
        public static ConfigEntry<bool> preventUncreatedDamage;
        public static ConfigEntry<bool> preventWearDamage;
        public static ConfigEntry<int> nexusID;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            preventCreatorDamage = Config.Bind<bool>("General", "PreventCreatorDamage", false, "Prevent creators from damaging their own structures.");
            preventNonCreatorDamage = Config.Bind<bool>("General", "PreventNonCreatorDamage", false, "Prevent damaging structures created by others.");
            preventUncreatedDamage = Config.Bind<bool>("General", "PreventUncreatedDamage", false, "Prevent damaging structures not created by players.");
            nexusID = Config.Bind<int>("General", "NexusID", 85, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;



            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("buildingdamagemod reset"))
                {
                    context.Config.Reload();
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(WearNTear), "Damage")]
        static class Damage_Patch
        {
            static bool Prefix(WearNTear __instance, HitData hit, ZNetView ___m_nview, Piece ___m_piece)
            {
                if (!modEnabled.Value)
                    return true;

                Dbgl($"attacker: {hit.m_attacker.userID}, creator { ___m_nview.IsOwner()}");
                if (!hit.m_attacker.IsNone() && 
                    (hit.m_attacker.userID != ___m_piece.GetCreator() && preventNonCreatorDamage.Value)
                    ||
                    (hit.m_attacker.userID == ___m_piece.GetCreator() && preventCreatorDamage.Value)
                    ||
                    (___m_piece.GetCreator() == 0 && preventUncreatedDamage.Value)
                )
                {
                    Dbgl("Preventing damage");
                    return false;
                }
                return true;
            }
        }

    }
}