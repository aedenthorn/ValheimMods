using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BuildRestrictionTweaks
{
    [BepInPlugin("aedenthorn.BuildRestrictionTweaks", "Build Restriction Tweaks", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private enum PlacementStatus
        {
            Valid,
            Invalid,
            BlockedbyPlayer,
            NoBuildZone,
            PrivateZone,
            MoreSpace,
            NoTeleportArea,
            ExtensionMissingStation,
            WrongBiome,
            NeedCultivated,
            NotInDungeon
        }

        private static readonly bool isDebug = true;

        public static ConfigEntry<string> modKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> alwaysValid;
        public static ConfigEntry<bool> ignoreInvalid;
        public static ConfigEntry<bool> ignoreBlockedbyPlayer;
        public static ConfigEntry<bool> ignoreBuildZone;
        public static ConfigEntry<bool> ignoreSpaceRestrictions;
        public static ConfigEntry<bool> ignoreTeleportAreaRestrictions;
        public static ConfigEntry<bool> ignoreMissingStation;
        public static ConfigEntry<bool> ignoreBiomeRestrictions;
        public static ConfigEntry<bool> ignoreCultivationRestrictions;
        public static ConfigEntry<bool> ignoreDungeonRestrictions;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 1573, "Nexus mod ID for updates");

            alwaysValid = Config.Bind<bool>("Options", "AlwaysValid", false, "Remove all build restrictions.");
            ignoreBlockedbyPlayer = Config.Bind<bool>("Options", "ignoreBlockedbyPlayer", false, "Ignore player blocking build.");
            ignoreInvalid = Config.Bind<bool>("Options", "IgnoreInvalid", false, "Prevent misc build restrictions.");
            ignoreBuildZone = Config.Bind<bool>("Options", "IgnoreInvalid", false, "Ignore zone restrictions.");
            ignoreSpaceRestrictions = Config.Bind<bool>("Options", "ignoreSpaceRestrictions", false, "Ignore space restrictions.");
            ignoreTeleportAreaRestrictions = Config.Bind<bool>("Options", "ignoreTeleportAreaRestrictions", false, "Ignore teleport area restrictions.");
            ignoreMissingStation = Config.Bind<bool>("Options", "ignoreMissingStation", false, "Ignore missing station.");
            ignoreBiomeRestrictions = Config.Bind<bool>("Options", "ignoreBiomeRestrictions", false, "Ignore biome restrictions.");
            ignoreCultivationRestrictions = Config.Bind<bool>("Options", "ignoreCultivationRestrictions", false, "Ignore need for cultivated ground.");
            ignoreDungeonRestrictions = Config.Bind<bool>("Options", "ignoreDungeonRestrictions", false, "Ignore indoor restrictions.");
            
            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID );

        }

        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        static class Player_UpdatePlacementGhost_Patch
        {
            static void Postfix(Player __instance, GameObject ___m_placementGhost)
            {
                if (!modEnabled.Value || ___m_placementGhost == null)
                    return;

                PlacementStatus placementStatus = (PlacementStatus)AccessTools.Field(typeof(Player), "m_placementStatus").GetValue(__instance);

                if (
                    (placementStatus != PlacementStatus.Valid && placementStatus != PlacementStatus.PrivateZone && alwaysValid.Value)
                    || (placementStatus == PlacementStatus.Invalid && ignoreInvalid.Value)
                    || (placementStatus == PlacementStatus.BlockedbyPlayer && ignoreBlockedbyPlayer.Value)
                    || (placementStatus == PlacementStatus.NoBuildZone && ignoreBuildZone.Value)
                    || (placementStatus == PlacementStatus.MoreSpace && ignoreSpaceRestrictions.Value)
                    || (placementStatus == PlacementStatus.NoTeleportArea && ignoreTeleportAreaRestrictions.Value)
                    || (placementStatus == PlacementStatus.ExtensionMissingStation && ignoreBiomeRestrictions.Value)
                    || (placementStatus == PlacementStatus.NeedCultivated && ignoreCultivationRestrictions.Value)
                    || (placementStatus == PlacementStatus.NotInDungeon && ignoreDungeonRestrictions.Value)
                )
                {
                    AccessTools.Field(typeof(Player), "m_placementStatus").SetValue(__instance, (int)PlacementStatus.Valid);
                    AccessTools.Method(typeof(Player), "SetPlacementGhostValid").Invoke(__instance, new object[] { true });
                }
            }
        }
    }
}
