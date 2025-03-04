using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BuildRestrictionTweaks
{
    [BepInPlugin("aedenthorn.BuildRestrictionTweaks", "Build Restriction Tweaks", "0.6.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public enum PlacementStatus
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
            NeedDirt,
            NotInDungeon
        }

        public static readonly bool isDebug = true;

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
        public static ConfigEntry<bool> ignoreMissingStationExtension;
        public static ConfigEntry<bool> ignoreBiomeRestrictions;
        public static ConfigEntry<bool> ignoreCultivationRestrictions;
        public static ConfigEntry<bool> ignoreDirtRestrictions;
        public static ConfigEntry<bool> ignoreDungeonRestrictions;

        public static BepInExPlugin context;
        public static GameObject craftingStationObject;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1606, "Nexus mod ID for updates");

            alwaysValid = Config.Bind<bool>("Options", "AlwaysValid", false, "Remove all build restrictions.");
            ignoreBlockedbyPlayer = Config.Bind<bool>("Options", "ignoreBlockedbyPlayer", false, "Ignore player blocking build.");
            ignoreInvalid = Config.Bind<bool>("Options", "IgnoreInvalid", false, "Prevent misc build restrictions.");
            ignoreBuildZone = Config.Bind<bool>("Options", "ignoreBuildZone", false, "Ignore zone restrictions.");
            ignoreSpaceRestrictions = Config.Bind<bool>("Options", "ignoreSpaceRestrictions", false, "Ignore space restrictions.");
            ignoreTeleportAreaRestrictions = Config.Bind<bool>("Options", "ignoreTeleportAreaRestrictions", false, "Ignore teleport area restrictions.");
            ignoreMissingStationExtension = Config.Bind<bool>("Options", "ignoreMissingStationExtension", false, "Ignore missing station extension.");
            ignoreMissingStation = Config.Bind<bool>("Options", "ignoreMissingStation", false, "Ignore missing station.");
            ignoreBiomeRestrictions = Config.Bind<bool>("Options", "ignoreBiomeRestrictions", false, "Ignore biome restrictions.");
            ignoreCultivationRestrictions = Config.Bind<bool>("Options", "ignoreCultivationRestrictions", false, "Ignore need for cultivated ground.");
            ignoreDirtRestrictions = Config.Bind<bool>("Options", "ignoreDirtRestrictions", false, "Ignore need for dirt.");
            ignoreDungeonRestrictions = Config.Bind<bool>("Options", "ignoreDungeonRestrictions", false, "Ignore indoor restrictions.");

            nexusID.Value = 1606;

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID );

        }

        [HarmonyPatch(typeof(Location), nameof(Location.IsInsideNoBuildLocation))]
        public static class Location_IsInsideNoBuildLocation_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (!modEnabled.Value || (!ignoreBuildZone.Value && !alwaysValid.Value))
                    return;
                __result = false;
            }
        }
        [HarmonyPatch(typeof(CraftingStation), "HaveBuildStationInRange")]
        public static class CraftingStation_HaveBuildStationInRange_Patch
        {
            public static void Postfix(ref CraftingStation __result, string name)
            {
                if (!modEnabled.Value || (!ignoreMissingStation.Value && !alwaysValid.Value) || __result != null)
                    return;
                if (craftingStationObject)
                    __result = craftingStationObject.GetComponent<CraftingStation>();
                else
                {
                    craftingStationObject = new GameObject();
                    DontDestroyOnLoad(craftingStationObject);
                    __result = craftingStationObject.AddComponent<CraftingStation>();
                }
            }
        }
        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        public static class Player_UpdatePlacementGhost_Patch
        {
            public static void Postfix(Player __instance, GameObject ___m_placementGhost)
            {
                if (!modEnabled.Value || ___m_placementGhost == null)
                    return;

                PlacementStatus placementStatus = (PlacementStatus)(int)AccessTools.Field(typeof(Player), "m_placementStatus").GetValue(__instance);

                if (
                    (placementStatus != PlacementStatus.Valid && placementStatus != PlacementStatus.PrivateZone && alwaysValid.Value)
                    || (placementStatus == PlacementStatus.Invalid && ignoreInvalid.Value)
                    || (placementStatus == PlacementStatus.BlockedbyPlayer && ignoreBlockedbyPlayer.Value)
                    || (placementStatus == PlacementStatus.NoBuildZone && ignoreBuildZone.Value)
                    || (placementStatus == PlacementStatus.MoreSpace && ignoreSpaceRestrictions.Value)
                    || (placementStatus == PlacementStatus.NoTeleportArea && ignoreTeleportAreaRestrictions.Value)
                    || (placementStatus == PlacementStatus.ExtensionMissingStation && ignoreMissingStationExtension.Value)
                    || (placementStatus == PlacementStatus.WrongBiome && ignoreBiomeRestrictions.Value)
                    || (placementStatus == PlacementStatus.NeedCultivated && ignoreCultivationRestrictions.Value)
                    || (placementStatus == PlacementStatus.NeedDirt && ignoreDirtRestrictions.Value)
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
