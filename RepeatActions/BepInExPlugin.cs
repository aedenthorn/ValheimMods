using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace RepeatActions
{
    [BepInPlugin("aedenthorn.RepeatActions", "Repeat Actions", "0.4.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<string> toggleModKey;
        
        public static ConfigEntry<bool> repeatAttacks;
        public static ConfigEntry<bool> repeatPlanting;
        public static ConfigEntry<bool> repeatTerrainMod;
        public static ConfigEntry<bool> repeatBuildPlace;
        
        public static ConfigEntry<float> repeatPlantingDelay;
        public static ConfigEntry<float> repeatTerrainModDelay;
        public static ConfigEntry<float> repeatBuildPlaceDelay;

        public static bool wasSelecting = false;
        public static float placeDelay;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1248, "Nexus mod ID for updates");
            toggleModKey = Config.Bind<string>("General", "ToggleModKey", "", "Hotkey to toggle mod");
            
            repeatAttacks = Config.Bind<bool>("Toggles", "RepeatAttacks", true, "Enable attack repeating");
            repeatPlanting = Config.Bind<bool>("Toggles", "RepeatPlanting", true, "Enable plant repeating");
            repeatTerrainMod = Config.Bind<bool>("Toggles", "RepeatTerrainMod", true, "Enable terrain mod repeating");
            repeatBuildPlace = Config.Bind<bool>("Toggles", "RepeatBuildPlace", true, "Enable build place repeating");

            repeatPlantingDelay = Config.Bind<float>("Toggles", "RepeatPlantingDelay", 0.4f, "Plant repeating delay");
            repeatTerrainModDelay = Config.Bind<float>("Toggles", "RepeatTerrainModDelay", 0.4f, "Terrain mod repeating delay");
            repeatBuildPlaceDelay = Config.Bind<float>("Toggles", "RepeatBuildPlaceDelay", 0.4f, "Build place repeating delay");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
            if (!AedenthornUtils.IgnoreKeyPresses() && AedenthornUtils.CheckKeyDown(toggleModKey.Value))
            {
                modEnabled.Value = !modEnabled.Value;
                Dbgl($"Repeat Actions: {modEnabled.Value}");
                if (Player.m_localPlayer)
                    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"Repeat Actions: {modEnabled.Value}");
            }
        }


        [HarmonyPatch(typeof(Player), "Awake")]
        public static class Player_Awake_Patch
        {

            public static void Prefix(Player __instance)
            {
                placeDelay = __instance.m_placeDelay;
            }
        }
        
        [HarmonyPatch(typeof(Player), "UpdatePlacement")]
        public static class UpdatePlacement_Patch
        {
            public static void Prefix(Player __instance, float ___m_lastToolUseTime, ref float ___m_placePressedTime)
            {
                __instance.m_placeDelay = placeDelay;
                
                if (!modEnabled.Value)
                    return;

                string rightItem = __instance.GetRightItem()?.m_shared.m_name;

                if (rightItem == "$item_hammer")
                {
                    if (!repeatBuildPlace.Value)
                        return;
                    __instance.m_placeDelay = repeatBuildPlaceDelay.Value;
                }
                else if (rightItem == "$item_hoe")
                {
                    if (!repeatTerrainMod.Value)
                        return;
                    __instance.m_placeDelay = repeatTerrainModDelay.Value;
                }
                else if (rightItem == "$item_cultivator")
                {
                    if (!repeatPlanting.Value)
                        return;
                    __instance.m_placeDelay = repeatPlantingDelay.Value;
                }

                if (!wasSelecting && (ZInput.GetButton("Attack") || ZInput.GetButton("JoyPlace")) && Time.time - ___m_lastToolUseTime > __instance.m_placeDelay)
                {
                    if (InventoryGui.IsVisible() || Minimap.IsOpen() || StoreGui.IsVisible() || Hud.IsPieceSelectionVisible())
                        wasSelecting = true;
                    else
                        ___m_placePressedTime = Time.time;
                }
                else if(wasSelecting && !ZInput.GetButton("Attack") && !ZInput.GetButton("JoyPlace"))
                {
                    wasSelecting = false;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
        public static class FixedUpdate_Patch
        {
            public static void Prefix(PlayerController __instance, ref bool ___m_attackWasPressed, Player ___m_character)
            {
                if (!modEnabled.Value || !repeatAttacks.Value)
                    return;

                if (___m_attackWasPressed && (((Attack)___m_character.GetType().GetField("m_currentAttack", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(___m_character))?.CanStartChainAttack() == true || !___m_character.InAttack()) && (ZInput.GetButton("Attack") || ZInput.GetButton("JoyAttack")) && !(bool)__instance.GetType().GetMethod("InInventoryEtc", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { }))
                {
                    ___m_attackWasPressed = false;
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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
