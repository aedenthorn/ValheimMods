using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CustomControls
{
    [BepInPlugin("aedenthorn.CustomControls", "Custom Controls", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;
        private Rect windowRect;
        public static Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> menuEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<Color> fontColor;
        public static ConfigEntry<string> fontName;
        public static ConfigEntry<string> titleString;
        public static ConfigEntry<Color> windowBackgroundColor;
        public static ConfigEntry<Vector2> windowPosition;
        public static ConfigEntry<Vector2> windowSize;

        public static Dictionary<string, ControlInfo> allControls = new Dictionary<string, ControlInfo>();
        public static Dictionary<string, ControlInfo> customControls = new Dictionary<string, ControlInfo>();
        
        public static GUIStyle textStyle;
        public static GUIStyle messageStyle;
        public static GUIStyle buttonStyle;
        public static GUIStyle windowStyle;
        public string lastFontName;
        public static Font currentFont;
        public static int windowId = "CustomControls".GetHashCode();

        public static string message = " ";
        public static Color messageColor = Color.green;

        public enum WindowState
        {
            Hidden,
            Button,
            Menu,
            Control
        }

        public static WindowState windowState;
        public static void Dbgl(object str, LogLevel level = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(level, str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("Config", "Enabled", true, "Enable this mod");
            menuEnabled = Config.Bind<bool>("Config", "MenuEnabled", true, "Enable in-game menu");
            isDebug = Config.Bind<bool>("Config", "Debug", true, "Enable Debug Logs");
            nexusID = Config.Bind<int>("Config", "NexusID", 3161, "Nexus mod ID for updates");
            
            fontSize = Config.Bind<int>("Display", "FontSize", 16, "Font size");
            fontColor = Config.Bind<Color>("Display", "FontColor", Color.white, "Font color");
            fontName = Config.Bind<string>("Display", "FontName", "AveriaSerifLibre-Bold", "Font name");
            windowBackgroundColor = Config.Bind<Color>("Display", "WindowBackgroundColor", Color.black, "Window background color");
            windowPosition = Config.Bind<Vector2>("Display", "WindowPosition", new Vector2(0, 0), "Window current screen position (draggable)");
            windowSize = Config.Bind<Vector2>("Display", "WindowSize", new Vector2(300, 500), "Window size");
            titleString = Config.Bind<string>("Display", "TitleString", "Custom Controls", "Title string");

            windowPosition.SettingChanged += WindowPosition_SettingChanged;

            windowRect = new Rect(windowPosition.Value, windowSize.Value);
            
            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();

        }

        private void WindowPosition_SettingChanged(object sender, EventArgs e)
        {
            windowRect = new Rect(windowPosition.Value, windowSize.Value);
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
            if (!modEnabled.Value || !menuEnabled.Value || Settings.instance == null || ZInput.instance == null)
            {
                message = " ";
                windowState = WindowState.Hidden;
                return;
            }
            if (windowState == WindowState.Hidden)
            {
                windowState = WindowState.Button;
            }

            textStyle = new GUIStyle
            {
                richText = true,
                fontSize = fontSize.Value,
                alignment = TextAnchor.MiddleCenter
            };
            textStyle.normal.textColor = fontColor.Value;

            messageStyle = new GUIStyle
            {
                richText = true,
                fontSize = fontSize.Value,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            
            messageStyle.normal.textColor = messageColor;


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
            if (currentFont != null && textStyle?.font?.name != currentFont.name)
            {
                Dbgl($"setting font {currentFont.name}");
                textStyle.font = currentFont;
            }
            GUI.backgroundColor = windowState != WindowState.Button ? windowBackgroundColor.Value : Color.clear;

            windowRect = GUILayout.Window(windowId, windowRect, new GUI.WindowFunction(WindowBuilder), windowState == WindowState.Button ? "" : titleString.Value);
            if (!Input.GetKey(KeyCode.Mouse0) && (windowRect.x != windowPosition.Value.x || windowRect.y != windowPosition.Value.y))
            {
                windowPosition.Value = new Vector2(windowRect.x, windowRect.y);
                //Dbgl($"{cursorPos} {playerPos} {coordRect} {secondRect} {doubleSize} {windowRect}");
            }

        }

        public static string currentControl = "";
        public static Vector2 scrollPosition;

        public void WindowBuilder(int id)
        {
            GUI.tooltip = null;
            var rect = new Rect(0, 0, windowSize.Value.x, fontSize.Value);
            GUI.DragWindow(rect);
            switch (windowState)
            {
                case WindowState.Button:
                    message = null;
                    GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(titleString.Value))
                        {
                            scrollPosition = Vector2.zero;
                            windowState = WindowState.Menu;
                            SetControls(ZInput.instance);
                        }
                        GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    return;
                case WindowState.Menu:
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(message, new GUILayoutOption[] { GUILayout.Width(windowSize.Value.x) });
                        var newControl = GUILayout.TextField(currentControl, new GUILayoutOption[] { GUILayout.Width(windowSize.Value.x)});
                        if (newControl != currentControl)
                        {
                            currentControl = newControl;
                            message = null;
                        }

                        scrollPosition = GUILayout.BeginScrollView(scrollPosition, new GUILayoutOption[] { GUILayout.Width(windowSize.Value.x), GUILayout.ExpandHeight(true) });
                        {

                            foreach (var kvp in customControls)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    if (GUILayout.Button(new GUIContent(kvp.Key, kvp.Value.path), new GUILayoutOption[] { GUILayout.Width(windowSize.Value.x - 40 - 30) }))
                                    {
                                        currentControl = kvp.Key;
                                        message = " ";
                                        windowState = WindowState.Control;
                                        return;
                                    }
                                    if (GUILayout.Button("x", new GUILayoutOption[] { GUILayout.Width(35) }))
                                    {
                                        message = null;
                                        customControls.Remove(kvp.Key);
                                        SaveCustomControls();
                                        SetControls(ZInput.instance);
                                        return;
                                    }
                                }
                                GUILayout.EndHorizontal();

                            }

                            var cl = currentControl.ToLower();
                            foreach (var kvp in allControls)
                            {
                                if ((currentControl.Length == 0 || kvp.Key.ToLower().Contains(cl)) && !customControls.ContainsKey(kvp.Key))
                                {
                                    if (GUILayout.Button(new GUIContent(kvp.Key, kvp.Value.path), new GUILayoutOption[] { GUILayout.Width(windowSize.Value.x - 30) } ))
                                    {
                                        currentControl = kvp.Key;
                                        message = " ";
                                        windowState = WindowState.Control;
                                        return;
                                    }
                                }
                            }
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();
                    DrawTooltip(windowRect);
                    return;
                case WindowState.Control:
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(currentControl, textStyle);
                        GUILayout.Label("Press any key or button", textStyle);
                    }
                    GUILayout.EndVertical();
                    return;
            }
        }
        public static void DrawTooltip(Rect area)
        {
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                var currentEvent = Event.current;

                const int width = 200;
                var height = textStyle.CalcHeight(new GUIContent(GUI.tooltip), 200) + 10;

                var x = currentEvent.mousePosition.x + width > area.width
                    ? area.width - width
                    : currentEvent.mousePosition.x;

                var y = currentEvent.mousePosition.y + 25 + height > area.height
                    ? currentEvent.mousePosition.y - height
                    : currentEvent.mousePosition.y + 25;

                GUI.Box(new Rect(x, y, width, height), GUI.tooltip, textStyle);
            }
        }


        public static void LoadControls()
        {
            customControls.Clear();
            var path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "controls.json");
            if (File.Exists(path))
            {
                try
                {
                    customControls = JsonConvert.DeserializeObject<Dictionary<string, ControlInfo>>(File.ReadAllText(path));
                    Dbgl($"Finished loading {customControls.Count} controls");
                }
                catch (Exception ex)
                {
                    Dbgl($"Error loading buttons:\n\n {ex}", LogLevel.Error);
                }

                path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "all_controls.json");
                if (!File.Exists(path))
                {
                    SaveAllControls();
                }
            }
            else
            {
                SaveAllControls();
            }
        }

        public static void SaveCustomControls()
        {
            var path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "controls.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(customControls, Formatting.Indented));
            Dbgl($"Finished saving {customControls.Count} controls");
        }

        public static void SaveAllControls()
        {
            var path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "all_controls.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(allControls, Formatting.Indented));
            Dbgl($"Finished saving {allControls.Count} buttons");

            path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "all_paths.txt");
            if (!File.Exists(path))
            {
                List<string> list = new List<string>();
                MethodInfo KeyCodeToPath = AccessTools.Method(typeof(ZInput), "KeyCodeToPath");
                foreach (var k in Enum.GetValues(typeof(KeyCode)))
                {
                    var str = (string)KeyCodeToPath.Invoke(null, new object[] { k, false });
                    if(str != "<Keyboard>/None")
                        list.Add(str);
                }
                File.WriteAllLines(path, list);
            }
        }

        public static void SetControls(ZInput zInput)
        {
            if (!modEnabled.Value)
                return;

            LoadControls();

            Dictionary<string, ZInput.ButtonDef> m_buttons = AccessTools.FieldRefAccess<ZInput, Dictionary<string, ZInput.ButtonDef>>(zInput, "m_buttons");
            MethodInfo UnsubscribeButton = AccessTools.Method(typeof(ZInput), "UnsubscribeButton");
            MethodInfo AddButton = AccessTools.Method(typeof(ZInput), "AddButton");

            using (var enumerator = customControls.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    try
                    {
                        var kvp = enumerator.Current;
                        if (m_buttons.TryGetValue(kvp.Key, out var buttonDef))
                        {
                            UnsubscribeButton.Invoke(zInput, new object[] { buttonDef });
                            buttonDef.ButtonAction.Disable();
                            m_buttons.Remove(buttonDef.Name);
                            AddButton.Invoke(zInput, new object[] { kvp.Value.name, kvp.Value.path, kvp.Value.altKey, kvp.Value.showHints, kvp.Value.rebindable, kvp.Value.repeatDelay, kvp.Value.repeatInterval });
                            //Dbgl($"Set button {enumerator.Current.Key}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Dbgl($"Error setting button {enumerator.Current.Key}: {ex}");
                    }
                }
            }
            zInput.Save();
            Dbgl("Finished setting buttons");
        }
        [HarmonyPatch(typeof(ZInput), "OnActionPerformed")]
        public static class ZInput_OnActionPerformed_Patch
        {
            public static bool Prefix(InputAction.CallbackContext ctx)
            {
                if (!modEnabled.Value)
                    return true;
                if (windowState == WindowState.Control)
                {
                    windowState = WindowState.Menu;
                    if (ctx.action.bindings.Count == 0)
                    {
                        message = $"Invalid key press {ctx.action.name}";
                        messageColor = Color.red;
                        return false;
                    }
                    if (allControls.TryGetValue(currentControl, out var info))
                    {
                        var path = ctx.action.bindings[0].path;
                        message = $"Control for <b>{currentControl}</b> set to <b>{path}</b>!";
                        messageColor = Color.green;
                        if (!customControls.TryGetValue(currentControl, out var cinfo))
                        {
                            cinfo = info;
                        }
                        cinfo.path = path;
                        customControls[currentControl] = cinfo;
                        SaveCustomControls();
                        SetControls(ZInput.instance);
                        currentControl = "";
                        scrollPosition = Vector2.zero;
                    }
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(ZInput), "AddButton")]
        public static class ZInput_AddButton_Patch
        {
            public static void Postfix(string name, string path, bool altKey, bool showHints, bool rebindable, float repeatDelay, float repeatInterval)
            {
                if (modEnabled.Value)
                {
                    allControls[name] = new ControlInfo()
                    {
                        name = name,
                        path = path,
                        altKey = altKey,
                        showHints = showHints,
                        rebindable = rebindable,
                        repeatDelay = repeatDelay,
                        repeatInterval = repeatInterval
                    };
                }
            }
        }
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.ChangeLayout))]
        public static class ZInput_ChangeLayout_Patch
        {
            public static void Postfix(ZInput __instance)
            {
                if (!modEnabled.Value)
                    return;

                SetControls(__instance);
            }
        }
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.Reset))]
        public static class ZInput_Reset_Patch
        {
            public static void Postfix(ZInput __instance)
            {
                if (!modEnabled.Value)
                    return;
                SetControls(__instance);
            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value || ZInput.instance is null)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    SetControls(ZInput.instance);
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
    
}
