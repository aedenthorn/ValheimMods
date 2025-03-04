using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MapCoordinateDisplay
{
    [BepInPlugin("aedenthorn.MapCoordinateDisplay", "Map Coordinate Display", "0.4.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<bool> showPlayerCoordinates;
        public static ConfigEntry<bool> showCursorCoordinates;
        public static ConfigEntry<bool> showCursorCoordinatesFirst;
        public static ConfigEntry<int> coordFontSize;
        public static ConfigEntry<bool> coordsUseShadow;
        public static ConfigEntry<float> clockShadowOffset;
        public static ConfigEntry<Vector2> coordPosition;
        public static ConfigEntry<string> titleString;
        public static ConfigEntry<string> cursorString;
        public static ConfigEntry<string> playerString;
        public static ConfigEntry<string> fontName;
        public static ConfigEntry<Color> playerCoordFontColor;
        public static ConfigEntry<Color> cursorCoordFontColor;
        public static ConfigEntry<Color> windowBackgroundColor;
        public static ConfigEntry<TextAnchor> alignment;

        public Rect windowRect;
        public int windowId = 5318008;
        public Rect coordRect;
        public Rect doubleSize;
        public Rect secondRect;
        public GUIStyle cursorStyle;
        public GUIStyle playerStyle;
        public GUIStyle windowStyle;

        public static string playerPos = "";
        public static string cursorPos = "";
        public string lastFontName;
        public Font currentFont;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 907, "Nexus mod ID for updates");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug log");
            showPlayerCoordinates = Config.Bind<bool>("General", "ShowPlayerCoordinates", true, "Show player coordinates.");
            showCursorCoordinates = Config.Bind<bool>("General", "ShowCursorCoordinates", true, "Show cursor coordinates.");
            showCursorCoordinatesFirst = Config.Bind<bool>("General", "ShowCursorCoordinatesFirst", true, "Show cursor coordinates above player coordinates.");

            titleString = Config.Bind<string>("Display", "TitleString", "Map Coordinates", "Title string");
            cursorString = Config.Bind<string>("Display", "CursorString", "Cursor {0}", "Cursor coordinates text. {0} is replaced by the coordinates.");
            playerString = Config.Bind<string>("Display", "PlayerString", "Player {0}", "Player coordinates text. {0} is replaced by the coordinates.");
            coordPosition = Config.Bind<Vector2>("Display", "CoordPosition", new Vector2(Screen.width / 2f, 0), "Coordinates text current screen position (draggable)");
            coordFontSize = Config.Bind<int>("Display", "CoordFontSize", 20, "Coordinate font size");
            playerCoordFontColor = Config.Bind<Color>("Display", "PlayerCoordFontColor", Color.white, "Player coordinate font color");
            cursorCoordFontColor = Config.Bind<Color>("Display", "CursorCoordFontColor", Color.white, "Cursor coordinate font color");
            windowBackgroundColor = Config.Bind<Color>("Display", "windowBackgroundColor", Color.clear, "Window background color");
            fontName = Config.Bind<string>("Display", "FontName", "AveriaSerifLibre-Bold", "Font name");
            alignment = Config.Bind<TextAnchor>("Display", "TextAlignment", TextAnchor.UpperCenter, "Text alignment");

            coordPosition.SettingChanged += CoordPosition_SettingChanged;

            windowRect = new Rect(coordPosition.Value, new Vector2(1000, 100));

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void CoordPosition_SettingChanged(object sender, System.EventArgs e)
        {
            windowRect = new Rect(coordPosition.Value, new Vector2(1000, 100));
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }
        public static Font GetFont(string fontName, int fontSize)
        {
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            foreach (Font font in fonts)
            {
                if (font.name == fontName)
                {
                    return font;
                }
            }
            return Font.CreateDynamicFontFromOSFont(fontName, fontSize);
        }

        public void OnGUI()
        {
            if (!modEnabled.Value || !Player.m_localPlayer || ((!Hud.instance || !Traverse.Create(Hud.instance).Method("IsVisible").GetValue<bool>()) && !Minimap.IsOpen()))
                return;
            cursorStyle = new GUIStyle
            {
                richText = true,
                fontSize = coordFontSize.Value,
                alignment = alignment.Value
            };
            cursorStyle.normal.textColor = cursorCoordFontColor.Value;

            playerStyle = new GUIStyle
            {
                richText = true,
                fontSize = coordFontSize.Value,
                alignment = alignment.Value
            };
            playerStyle.normal.textColor = playerCoordFontColor.Value;

            windowStyle = GUI.skin.window;

            if (lastFontName != fontName.Value) // call when config changes
            {
                lastFontName = fontName.Value;
                Dbgl($"new font {fontName.Value}");
                Font font = GetFont(fontName.Value, 20);
                if (font == null)
                    Dbgl($"new font not found");
                else
                    currentFont = font;
            }
            if (currentFont != null && cursorStyle?.font?.name != currentFont.name)
            {
                Dbgl($"setting font {currentFont.name}");
                cursorStyle.font = currentFont;
                playerStyle.font = currentFont;
                windowStyle.font = currentFont;
            }

            playerPos = showPlayerCoordinates.Value ? string.Format(playerString.Value, new Vector3(Player.m_localPlayer.transform.position.x, Player.m_localPlayer.transform.position.z, Player.m_localPlayer.transform.position.y)) : "";

            if (Minimap.IsOpen() && showCursorCoordinates.Value)
            {
                Vector3 cursorV = Traverse.Create(Minimap.instance).Method("ScreenToWorldPoint", new object[] { Input.mousePosition }).GetValue<Vector3>();
                cursorPos = string.Format(cursorString.Value, new Vector2(cursorV.x, cursorV.z));
            }
            else
                cursorPos = "";

            if (playerPos.Length + cursorPos.Length == 0)
                return;

            GUI.backgroundColor = windowBackgroundColor.Value;
            windowRect = GUILayout.Window(windowId, new Rect(windowRect.position, coordRect.position + (playerPos.Length > 0 && cursorPos.Length > 0 ? doubleSize.size : coordRect.size)), new GUI.WindowFunction(WindowBuilder), titleString.Value, windowStyle);
            if (!Input.GetKey(KeyCode.Mouse0) && (windowRect.x != coordPosition.Value.x || windowRect.y != coordPosition.Value.y))
            {
                coordPosition.Value = new Vector2(windowRect.x, windowRect.y);
                //Dbgl($"{cursorPos} {playerPos} {coordRect} {secondRect} {doubleSize} {windowRect}");
            }

        }

        public void WindowBuilder(int id)
        {
            if (cursorPos.Length > playerPos.Length)
                coordRect = GUILayoutUtility.GetRect(new GUIContent(cursorPos), cursorStyle);
            else
                coordRect = GUILayoutUtility.GetRect(new GUIContent(playerPos), playerStyle);

            doubleSize = new Rect(coordRect.position, new Vector2(coordRect.width, coordRect.height * 2));
            secondRect = new Rect(coordRect.position + new Vector2(0, coordRect.height), coordRect.size);


            if (cursorPos.Length > 0 && playerPos.Length > 0)
            {
                GUI.DragWindow(doubleSize);
                if (showCursorCoordinatesFirst.Value)
                {
                    GUI.Label(coordRect, cursorPos, cursorStyle);
                    GUI.Label(secondRect, playerPos, playerStyle);
                }
                else
                {
                    GUI.Label(coordRect, playerPos, playerStyle);
                    GUI.Label(secondRect, cursorPos, cursorStyle);
                }
            }
            else
            {
                GUI.DragWindow(coordRect);
                if (cursorPos.Length > 0)
                {
                    GUI.Label(coordRect, cursorPos, cursorStyle);
                }
                else if (playerPos.Length > 0)
                {
                    GUI.Label(coordRect, playerPos, playerStyle);
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
