using BepInEx;
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
        public static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> suppressMessage;
        public static ConfigEntry<int> nexusID;

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
            suppressMessage = Config.Bind<bool>("General", "SupressMessage", true, "Supresses message on read");
            nexusID = Config.Bind<int>("General", "NexusID", 1739, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        [HarmonyPatch(typeof(Minimap), "Update")]
        public static class Minimap_Update_Patch
        {
            public static void Postfix(Minimap __instance, Minimap.MapMode ___m_mode)
            {
                if (!modEnabled.Value || Player.m_localPlayer == null)
                    return;
                __instance.m_smallRoot.SetActive(false);
                if (ZInput.GetButtonDown("Map") || ZInput.GetButtonDown("JoyMap") || ZInput.GetButtonDown("JoyMap"))
                    __instance.SetMapMode(Minimap.MapMode.None);
            }
        }
        [HarmonyPatch(typeof(MapTable), "OnRead", new Type[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData), typeof(bool) })]
        public static class MapTable_OnRead_Patch
        {
            public static void Prefix(MapTable __instance, ref bool showMessage)
            {
                showMessage = showMessage && !suppressMessage.Value;
            }

            public static void Postfix(MapTable __instance, ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || Player.m_localPlayer == null || item != null)
                    return;
                Minimap.instance.SetMapMode(Minimap.MapMode.Large);
            }
        }
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.ShowPointOnMap))]
        public static class Minimap_ShowPointOnMap_Patch
        {
            public static bool Prefix()
            {
                return false;
            }
        }
    }
}
