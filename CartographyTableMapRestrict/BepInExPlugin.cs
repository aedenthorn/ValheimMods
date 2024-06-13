﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace CartographyTableMapRestrict
{
    [BepInPlugin("aedenthorn.CartographyTableMapRestrict", "Cartography Table Map Restrict", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> suppressMessage;
        public static ConfigEntry<int> nexusID;

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
            suppressMessage = Config.Bind<bool>("General", "SupressMessage", true, "Supresses message on read");
            nexusID = Config.Bind<int>("General", "NexusID", 1739, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        [HarmonyPatch(typeof(Minimap), "Update")]
        static class Minimap_Update_Patch
        {
            static void Postfix(Minimap __instance, Minimap.MapMode ___m_mode)
            {
                if (!modEnabled.Value || Player.m_localPlayer == null)
                    return;
                __instance.m_smallRoot.SetActive(false);
                if (ZInput.GetButtonDown("Map") || ZInput.GetButtonDown("JoyMap") || ZInput.GetButtonDown("JoyMap"))
                    __instance.SetMapMode(Minimap.MapMode.None);
            }
        }
        [HarmonyPatch(typeof(MapTable), "OnRead", new Type[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData), typeof(bool) })]
        static class MapTable_OnRead_Patch
        {
            static void Prefix(MapTable __instance, ref bool showMessage)
            {
                showMessage = showMessage && !suppressMessage.Value;
            }

            static void Postfix(MapTable __instance, ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || Player.m_localPlayer == null || item != null)
                    return;
                Minimap.instance.SetMapMode(Minimap.MapMode.Large);
            }
        }
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.ShowPointOnMap))]
        static class Minimap_ShowPointOnMap_Patch
        {
            static bool Prefix()
            {
                return false;
            }
        }
    }
}
