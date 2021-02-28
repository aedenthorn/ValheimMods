using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace MiningMod
{
    [BepInPlugin("aedenthorn.MiningMod", "Mining Mod", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> showAllManagedMods;
        public static ConfigEntry<bool> createEmptyConfigFiles;
        public static ConfigEntry<Vector2> updatesPosition;
        public static ConfigEntry<int> updateTextWidth;
        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<Color> updateFontColor;
        public static ConfigEntry<Color> nonUpdateFontColor;
        public static ConfigEntry<Color> backgroundColor;
        public static ConfigEntry<int> betweenSpace;
        public static ConfigEntry<int> buttonWidth;
        public static ConfigEntry<int> buttonHeight;
        public static ConfigEntry<string> updateText;
        public static ConfigEntry<string> nonUpdateText;
        public static ConfigEntry<string> checkingUpdatesText;
        public static ConfigEntry<string> buttonText;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            showAllManagedMods = Config.Bind<bool>("General", "ShowAllManagedMods", false, "Show all mods that have a nexus ID in the list, even if they are up-to-date");
            createEmptyConfigFiles = Config.Bind<bool>("General", "CreateEmptyConfigFiles", false, "Create empty GUID-based config files for mods that don't have them (may cause there to be duplicate config files)");
            updatesPosition = Config.Bind<Vector2>("General", "UpdatesPosition", new Vector2(40, 40), "Position of the updates list on the screen");
            updateTextWidth = Config.Bind<int>("General", "UpdateTextWidth", 600, "Width of the update text (will wrap if it is too long)");
            buttonWidth = Config.Bind<int>("General", "ButtonWidth", 100, "Width of the update button");
            buttonHeight = Config.Bind<int>("General", "ButtonHeight", 30, "Height of the update button");
            betweenSpace = Config.Bind<int>("General", "BetweenSpace", 10, "Vertical space between each update in list");
            fontSize = Config.Bind<int>("General", "FontSize", 16, "Size of the text in the updates list");
            updateFontColor = Config.Bind<Color>("General", "UpdateFontColor", Color.white, "Color of the text in the updateable list");
            nonUpdateFontColor = Config.Bind<Color>("General", "NonUpdateFontColor", new Color(0.7f, 0.7f, 0.7f, 1f), "Color of the text in the non-updateable list");
            updateText = Config.Bind<string>("General", "UpdateText", "<b>{0}</b> (v. {1}) has an updated version: <b>{2}</b>", "Text to show for each update. {0} is replaced by the mod name, {1} is replaced by the current version, and {2} is replaced by the remote version");
            nonUpdateText = Config.Bind<string>("General", "NonUpdateText", "<b>{0}</b> (v. {1}) is up-to-date!", "Text to show for each update. {0} is replaced by the mod name, {1} is replaced by the current version, and {2} is replaced by the remote version");
            checkingUpdatesText = Config.Bind<string>("General", "CheckingUpdatesText", "<b>Checking for mod updates...</b>", "Text to show while checking for updates");
            buttonText = Config.Bind<string>("General", "ButtonText", "<b>Visit</b>", "Text to show for each update button");
            nexusID = Config.Bind<int>("General", "NexusID", 102, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(Smelter), "GetAccumulator")]
        static class Smelter_GetAccumulator_Patch
        {
            static void Postfix(ref float __result)
            {
                if (__result < 0)
                    __result = 0;
            }
        }
    }
}
