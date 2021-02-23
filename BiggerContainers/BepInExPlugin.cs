using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BiggerContainers
{
    [BepInPlugin("aedenthorn.BiggerContainers", "Bigger Containers", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> chestWidth;
        public static ConfigEntry<int> chestHeight;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "enabled", true, "Enable this mod");
            chestWidth = Config.Bind<int>("General", "ChestWidth", 8, "Number of slots wide for chests");
            chestHeight = Config.Bind<int>("General", "ChestHeight", 8, "Number of slots tall for chests");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Container), "CheckForChanges")]
        static class Container_Update_Patch
        {
            static void Postfix(Container __instance, Inventory ___m_inventory)
            {
                if(__instance.m_name.StartsWith("Chest"))
                {
                    AccessTools.FieldRefAccess<Inventory, int>(___m_inventory, "m_width") = chestWidth.Value;
                    AccessTools.FieldRefAccess<Inventory, int>(___m_inventory, "m_height") = chestHeight.Value;
                }
            }
        }
    }
}
