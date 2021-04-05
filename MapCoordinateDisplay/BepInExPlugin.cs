using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MapCoordinateDisplay
{
    [BepInPlugin("aedenthorn.MapCoordinateDisplay", "Map Coordinate Display", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<int> coordFontSize;
        public static ConfigEntry<bool> coordsUseShadow;
        public static ConfigEntry<float> clockShadowOffset;
        public static ConfigEntry<Vector2> coordPosition;
        public static ConfigEntry<string> titleString;
        public static ConfigEntry<Color> coordFontColor;
        public static ConfigEntry<Color> windowBackgroundColor;

        private Rect windowRect;
        private int windowId = 5318008;
        private Rect coordRect;
        private GUIStyle style;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 907, "Nexus mod ID for updates");
            titleString = Config.Bind<string>("General", "TitleString", "Map Coordinates", "Title string");
            coordPosition = Config.Bind<Vector2>("General", "CoordPosition", new Vector2(Screen.width / 2f, 0), "Coordinates current position");
            coordFontSize = Config.Bind<int>("General", "CoordFontSize", 20, "Coordinate font size");
            coordFontColor = Config.Bind<Color>("General", "CoordFontColor", Color.white, "Coordinate font color");
            windowBackgroundColor = Config.Bind<Color>("General", "windowBackgroundColor", Color.clear, "Coordinate font color");

            style = new GUIStyle
            {
                richText = true,
                fontSize = coordFontSize.Value,
            };
            style.normal.textColor = coordFontColor.Value;

            windowRect = new Rect(coordPosition.Value, new Vector2(1000, 100));

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        private void OnGUI()
        {
            if (!modEnabled.Value || !Minimap.IsOpen())
                return;
            GUI.backgroundColor = windowBackgroundColor.Value;
            windowRect = GUILayout.Window(windowId, new Rect(windowRect.position, coordRect.size), new GUI.WindowFunction(WindowBuilder), titleString.Value);
            if (!Input.GetKey(KeyCode.Mouse0) && (windowRect.x != coordPosition.Value.x || windowRect.y != coordPosition.Value.y))
            {
                coordPosition.Value = new Vector2(windowRect.x, windowRect.y);
            }
        }
        private void WindowBuilder(int id)
        {

            Vector3 pos = Traverse.Create(Minimap.instance).Method("ScreenToWorldPoint", new object[] { Input.mousePosition }).GetValue<Vector3>();

            coordRect = GUILayoutUtility.GetRect(new GUIContent(pos+""), style);

            GUI.DragWindow(coordRect);

            GUI.Label(coordRect, pos+"", style);

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
