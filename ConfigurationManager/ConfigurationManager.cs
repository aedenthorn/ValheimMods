// Based on code made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;

namespace ConfigurationManager
{
    /// <summary>
    /// An easy way to let user configure how a plugin behaves without the need to make your own GUI. The user can change any of the settings you expose, even keyboard shortcuts.
    /// https://github.com/ManlyMarco/BepInEx.ConfigurationManager
    /// </summary>
    [BepInPlugin(GUID, "Valheim Configuration Manager", Version)]
    public class ConfigurationManager : BaseUnityPlugin
    {
        /// <summary>
        /// GUID of this plugin
        /// </summary>
        public const string GUID = "aedenthorn.ConfigurationManager";
        private static bool isDebug = true;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(ConfigurationManager).Namespace + " " : "") + str);
        }
        /// <summary>
        /// Version constant
        /// </summary>
        public const string Version = "0.1.0";
        private static ConfigurationManager context;
        internal static new ManualLogSource Logger;
        private static SettingFieldDrawer _fieldDrawer;

        private const int WindowId = -68;

        private const string SearchBoxName = "searchBox";
        private bool _focusSearchBox;
        private string _searchString = string.Empty;

        /// <summary>
        /// Event fired every time the manager window is shown or hidden.
        /// </summary>
        public event EventHandler<ValueChangedEventArgs<bool>> DisplayingWindowChanged;

        /// <summary>
        /// Disable the hotkey check used by config manager. If enabled you have to set <see cref="DisplayingWindow"/> to show the manager.
        /// </summary>
        public bool OverrideHotkey;

        private bool _displayingWindow;
        private bool _obsoleteCursor;

        private string _modsWithoutSettings;

        private List<SettingEntryBase> _allSettings;
        private List<PluginSettingsData> _filteredSetings = new List<PluginSettingsData>();

        internal Rect DefaultWindowRect { get; private set; }
        private Rect _screenRect;
        private Rect currentWindowRect;
        private Vector2 _settingWindowScrollPos;
        private int _tipsHeight;
        private bool _showDebug;

        private PropertyInfo _curLockState;
        private PropertyInfo _curVisible;
        private int _previousCursorLockState;
        private bool _previousCursorVisible;

        internal static Texture2D WindowBackground { get; private set; }
        internal static Texture2D EntryBackground { get; private set; }
        internal static Texture2D WidgetBackground { get; private set; }

        internal int LeftColumnWidth { get; private set; }
        internal int RightColumnWidth { get; private set; }

        public static ConfigEntry<bool> _showAdvanced;
        public static ConfigEntry<bool> _showKeybinds;
        public static ConfigEntry<bool> _showSettings;
        public static ConfigEntry<KeyboardShortcut> _keybind;
        public static ConfigEntry<bool> _hideSingleSection;
        public static ConfigEntry<bool> _pluginConfigCollapsedDefault;
        public static ConfigEntry<Vector2> _windowPosition;
        public static ConfigEntry<Vector2> _windowSize;
        public static ConfigEntry<int> _textSize;
        public static ConfigEntry<Color> _windowBackgroundColor;
        public static ConfigEntry<Color> _entryBackgroundColor;
        public static ConfigEntry<Color> _fontColor;
        public static ConfigEntry<Color> _widgetBackgroundColor;
        public static ConfigEntry<int> nexusID;


        public static GUIStyle windowStyle;
        public static GUIStyle headerStyle;
        public static GUIStyle entryStyle;
        public static GUIStyle labelStyle;
        public static GUIStyle toggleStyle;
        public static GUIStyle buttonStyle;
        public static GUIStyle boxStyle;
        public static GUIStyle sliderStyle;
        public static GUIStyle thumbStyle;
        public static GUIStyle categoryHeaderSkin;
        public static GUIStyle pluginHeaderSkin;
        public static int fontSize = 14;

        /// <inheritdoc />
        public ConfigurationManager()
        {
            context = this;
            Logger = base.Logger;
            CalculateDefaultWindowRect();
            _fieldDrawer = new SettingFieldDrawer(this);

            _keybind = Config.Bind("General", "Show config manager", new KeyboardShortcut(KeyCode.F1),
                new ConfigDescription("The shortcut used to toggle the config manager window on and off.\n" +
                                      "The key can be overridden by a game-specific plugin if necessary, in that case this setting is ignored."));
            nexusID = Config.Bind<int>("General", "NexusID", 740, "Nexus mod ID for updates");

            _showAdvanced = Config.Bind<bool>("Filtering", "Show advanced", true);
            _showKeybinds = Config.Bind("Filtering", "Show keybinds", true);
            _showSettings = Config.Bind("Filtering", "Show settings", true);
            _hideSingleSection = Config.Bind("General", "Hide single sections", false, new ConfigDescription("Show section title for plugins with only one section"));
            _pluginConfigCollapsedDefault = Config.Bind("General", "Plugin collapsed default", true, new ConfigDescription("If set to true plugins will be collapsed when opening the configuration manager window"));
            _windowPosition = Config.Bind("General", "WindowPosition", new Vector2(55,35), "Window position");
            _windowSize = Config.Bind("General", "WindowSize", DefaultWindowRect.size, "Window size");
            _textSize = Config.Bind("General", "FontSize", 14, "Font Size");
            _windowBackgroundColor = Config.Bind("Colors", "WindowBackgroundColor", new Color(0,0,0,1), "Window background color");
            _entryBackgroundColor = Config.Bind("Colors", "EntryBackgroundColor", new Color(0.557f, 0.502f, 0.502f, 0.871f), "Etnry background color");
            _fontColor = Config.Bind("Colors", "FontColor", new Color(1, 0.714f, 0.361f, 1), "Font color");
            _widgetBackgroundColor = Config.Bind("Colors", "WidgetColor", new Color(0.882f, 0.463f, 0, 0.749f), "Widget color");

            currentWindowRect = new Rect(_windowPosition.Value, _windowSize.Value);
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }


        private void OnGUI()
        {
            if (DisplayingWindow)
            {
                if (Event.current.type == EventType.KeyUp && Event.current.keyCode == _keybind.Value.MainKey)
                {
                    DisplayingWindow = false;
                    return;
                }

                if(_textSize.Value > 9 && _textSize.Value < 100)
                    fontSize = Mathf.Clamp(_textSize.Value, 10, 30);

                CreateBackgrounds();
                CreateStyles();
                SetUnlockCursor(0, true);

                GUI.Box(currentWindowRect, GUIContent.none, new GUIStyle());
                GUI.backgroundColor = _windowBackgroundColor.Value;

                if(_windowSize.Value.x > 100 && _windowSize.Value.x < Screen.width && _windowSize.Value.y > 100 && _windowSize.Value.y < Screen.height)
                    currentWindowRect.size = _windowSize.Value;

                currentWindowRect = GUILayout.Window(WindowId, currentWindowRect, SettingsWindow, "Plugin / mod settings", windowStyle);

                if (!SettingFieldDrawer.SettingKeyboardShortcut)
                    Input.ResetInputAxes();

                if (!Input.GetKey(KeyCode.Mouse0) && (currentWindowRect.x != _windowPosition.Value.x || currentWindowRect.y != _windowPosition.Value.y))
                {
                    _windowPosition.Value = currentWindowRect.position;
                    Config.Save();
                }
            }
        }

        private void SettingsWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, currentWindowRect.width, 20));
            //DrawWindowHeader();

            _settingWindowScrollPos = GUILayout.BeginScrollView(_settingWindowScrollPos, false, true);

            var scrollPosition = _settingWindowScrollPos.y;
            var scrollHeight = currentWindowRect.height;

            GUILayout.BeginVertical();
            {
                if (string.IsNullOrEmpty(SearchString))
                {
                    DrawTips();

                    if (_tipsHeight == 0 && Event.current.type == EventType.Repaint)
                        _tipsHeight = (int)GUILayoutUtility.GetLastRect().height;
                }

                var currentHeight = _tipsHeight;

                foreach (var plugin in _filteredSetings)
                {
                    var visible = plugin.Height == 0 || currentHeight + plugin.Height >= scrollPosition && currentHeight <= scrollPosition + scrollHeight;

                    if (visible)
                    {
                        try
                        {
                            DrawSinglePlugin(plugin);
                        }
                        catch (ArgumentException)
                        {
                            // Needed to avoid GUILayout: Mismatched LayoutGroup.Repaint crashes on large lists
                        }

                        if (plugin.Height == 0 && Event.current.type == EventType.Repaint)
                            plugin.Height = (int)GUILayoutUtility.GetLastRect().height;
                    }
                    else
                    {
                        try
                        {
                            GUILayout.Space(plugin.Height);
                        }
                        catch (ArgumentException)
                        {
                            // Needed to avoid GUILayout: Mismatched LayoutGroup.Repaint crashes on large lists
                        }
                    }

                    currentHeight += plugin.Height;
                }

                if (_showDebug)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Plugins with no options available: " + _modsWithoutSettings, labelStyle);
                }
                else
                {
                    // Always leave some space in case there's a dropdown box at the very bottom of the list
                    GUILayout.Space(70);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (!SettingFieldDrawer.DrawCurrentDropdown())
                DrawTooltip(currentWindowRect);
        }

        private void DrawTips()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Tip: Click plugin names to expand. Click setting and group names to see their descriptions.", labelStyle);

                GUILayout.FlexibleSpace();

                Color color = GUI.backgroundColor;
                GUI.backgroundColor = _widgetBackgroundColor.Value;
                if (GUILayout.Button(_pluginConfigCollapsedDefault.Value ? "Expand" : "Collapse", buttonStyle, GUILayout.ExpandWidth(false)))
                {
                    var newValue = !_pluginConfigCollapsedDefault.Value;
                    _pluginConfigCollapsedDefault.Value = newValue;
                    foreach (var plugin in _filteredSetings)
                        plugin.Collapsed = newValue;
                }
                GUI.backgroundColor = color;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawWindowHeader()
        {
            GUI.backgroundColor = _entryBackgroundColor.Value;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Show: ", labelStyle, GUILayout.ExpandWidth(false));

                GUI.enabled = SearchString == string.Empty;

                var newVal = GUILayout.Toggle(_showSettings.Value, "Normal settings", toggleStyle);
                if (_showSettings.Value != newVal)
                {
                    _showSettings.Value = newVal;
                    BuildFilteredSettingList();
                }

                newVal = GUILayout.Toggle(_showKeybinds.Value, "Keyboard shortcuts", toggleStyle);
                if (_showKeybinds.Value != newVal)
                {
                    _showKeybinds.Value = newVal;
                    BuildFilteredSettingList();
                }

                newVal = GUILayout.Toggle(_showAdvanced.Value, "Advanced settings", toggleStyle);
                if (_showAdvanced.Value != newVal)
                {
                    _showAdvanced.Value = newVal;
                    BuildFilteredSettingList();
                }

                GUI.enabled = true;

                newVal = GUILayout.Toggle(_showDebug, "Debug mode", toggleStyle);
                if (_showDebug != newVal)
                {
                    _showDebug = newVal;
                    BuildSettingList();
                }

                if (GUILayout.Button("Log", buttonStyle, GUILayout.ExpandWidth(false)))
                {
                    try { Utilities.Utils.OpenLog(); }
                    catch (SystemException ex) { Logger.Log(LogLevel.Message | LogLevel.Error, ex.Message); }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Search settings: ", labelStyle, GUILayout.ExpandWidth(false));

                GUI.SetNextControlName(SearchBoxName);
                SearchString = GUILayout.TextField(SearchString, GUILayout.ExpandWidth(true));

                if (_focusSearchBox)
                {
                    GUI.FocusWindow(WindowId);
                    GUI.FocusControl(SearchBoxName);
                    _focusSearchBox = false;
                }
                Color color = GUI.backgroundColor;
                GUI.backgroundColor = _widgetBackgroundColor.Value;
                if (GUILayout.Button("Clear", buttonStyle, GUILayout.ExpandWidth(false)))
                    SearchString = string.Empty;
                GUI.backgroundColor = color;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSinglePlugin(PluginSettingsData plugin)
        {

            var style = new GUIStyle(GUI.skin.box);
            style.normal.textColor = _fontColor.Value;
            style.normal.background = EntryBackground;
            style.fontSize = fontSize;
            GUI.backgroundColor = _entryBackgroundColor.Value;

            GUILayout.BeginVertical(style);

            var categoryHeader = _showDebug ?
                new GUIContent(plugin.Info.Name.TrimStart('!')+" "+plugin.Info.Version, "GUID: " + plugin.Info.GUID) :
                new GUIContent(plugin.Info.Name.TrimStart('!')+" "+plugin.Info.Version);

            var isSearching = !string.IsNullOrEmpty(SearchString);

            if (SettingFieldDrawer.DrawPluginHeader(categoryHeader, plugin.Collapsed && !isSearching) && !isSearching)
                plugin.Collapsed = !plugin.Collapsed;

            if (isSearching || !plugin.Collapsed)
            {
                foreach (var category in plugin.Categories)
                {
                    if (!string.IsNullOrEmpty(category.Name))
                    {
                        if (plugin.Categories.Count > 1 || !_hideSingleSection.Value)
                            SettingFieldDrawer.DrawCategoryHeader(category.Name);
                    }

                    foreach (var setting in category.Settings)
                    {
                        DrawSingleSetting(setting);
                        GUILayout.Space(2);
                    }
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawSingleSetting(SettingEntryBase setting)
        {
            GUILayout.BeginHorizontal();
            {
                try
                {
                    DrawSettingName(setting);
                    _fieldDrawer.DrawSettingValue(setting);
                    DrawDefaultButton(setting);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Failed to draw setting {setting.DispName} - {ex}");
                    GUILayout.Label("Failed to draw this field, check log for details.", labelStyle);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSettingName(SettingEntryBase setting)
        {
            if (setting.HideSettingName) return;


            GUILayout.Label(new GUIContent(setting.DispName.TrimStart('!'), setting.Description), labelStyle,
                GUILayout.Width(LeftColumnWidth), GUILayout.MaxWidth(LeftColumnWidth));

        }

        private static void DrawDefaultButton(SettingEntryBase setting)
        {
            if (setting.HideDefaultButton) return;

            GUI.backgroundColor = _widgetBackgroundColor.Value;

            bool DrawDefaultButton()
            {
                GUILayout.Space(5);
                return GUILayout.Button("Reset", buttonStyle, GUILayout.ExpandWidth(false));
            }

            if (setting.DefaultValue != null)
            {
                if (DrawDefaultButton())
                    setting.Set(setting.DefaultValue);
            }
            else if (setting.SettingType.IsClass)
            {
                if (DrawDefaultButton())
                    setting.Set(null);
            }
        }

        /// <summary>
        /// Is the config manager main window displayed on screen
        /// </summary>
        public bool DisplayingWindow
        {
            get => _displayingWindow;
            set
            {
                if (_displayingWindow == value) return;
                _displayingWindow = value;

                SettingFieldDrawer.ClearCache();

                if (_displayingWindow)
                {
                    CalculateDefaultWindowRect();

                    BuildSettingList();

                    _focusSearchBox = true;

                    // Do through reflection for unity 4 compat
                    if (_curLockState != null)
                    {
                        _previousCursorLockState = _obsoleteCursor ? Convert.ToInt32((bool)_curLockState.GetValue(null, null)) : (int)_curLockState.GetValue(null, null);
                        _previousCursorVisible = (bool)_curVisible.GetValue(null, null);
                    }
                }
                else
                {
                    if (!_previousCursorVisible || _previousCursorLockState != 0) // 0 = CursorLockMode.None
                        SetUnlockCursor(_previousCursorLockState, _previousCursorVisible);
                }

                DisplayingWindowChanged?.Invoke(this, new ValueChangedEventArgs<bool>(value));
            }
        }

        /// <summary>
        /// Register a custom setting drawer for a given type. The action is ran in OnGui in a single setting slot.
        /// Do not use any Begin / End layout methods, and avoid raising height from standard.
        /// </summary>
        public static void RegisterCustomSettingDrawer(Type settingType, Action<SettingEntryBase> onGuiDrawer)
        {
            if (settingType == null) throw new ArgumentNullException(nameof(settingType));
            if (onGuiDrawer == null) throw new ArgumentNullException(nameof(onGuiDrawer));

            if (SettingFieldDrawer.SettingDrawHandlers.ContainsKey(settingType))
                Logger.LogWarning("Tried to add a setting drawer for type " + settingType.FullName + " while one already exists.");
            else
                SettingFieldDrawer.SettingDrawHandlers[settingType] = onGuiDrawer;
        }

        public void BuildSettingList()
        {
            SettingSearcher.CollectSettings(out var results, out var modsWithoutSettings, _showDebug);

            _modsWithoutSettings = string.Join(", ", modsWithoutSettings.Select(x => x.TrimStart('!')).OrderBy(x => x).ToArray());
            _allSettings = results.ToList();

            BuildFilteredSettingList();
        }

        private void BuildFilteredSettingList()
        {
            IEnumerable<SettingEntryBase> results = _allSettings;

            var searchStrings = SearchString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (searchStrings.Length > 0)
            {
                results = results.Where(x => ContainsSearchString(x, searchStrings));
            }
            else
            {
                if (!_showAdvanced.Value)
                    results = results.Where(x => x.IsAdvanced != true);
                if (!_showKeybinds.Value)
                    results = results.Where(x => !IsKeyboardShortcut(x));
                if (!_showSettings.Value)
                    results = results.Where(x => x.IsAdvanced == true || IsKeyboardShortcut(x));
            }

            const string shortcutsCatName = "Keyboard shortcuts";
            string GetCategory(SettingEntryBase eb)
            {

                return eb.Category;
            }

            var settingsAreCollapsed = _pluginConfigCollapsedDefault.Value;

            var nonDefaultCollpasingStateByPluginName = new HashSet<string>();
            foreach (var pluginSetting in _filteredSetings)
            {
                if (pluginSetting.Collapsed != settingsAreCollapsed)
                {
                    nonDefaultCollpasingStateByPluginName.Add(pluginSetting.Info.Name);
                }
            }

            _filteredSetings = results
                .GroupBy(x => x.PluginInfo)
                .Select(pluginSettings =>
                {
                    var categories = pluginSettings
                        .GroupBy(GetCategory)
                        .OrderBy(x => string.Equals(x.Key, shortcutsCatName, StringComparison.Ordinal))
                        .ThenBy(x => x.Key)
                        .Select(x => new PluginSettingsData.PluginSettingsGroupData { Name = x.Key, Settings = x.OrderByDescending(set => set.Order).ThenBy(set => set.DispName).ToList() });

                    return new PluginSettingsData { Info = pluginSettings.Key, Categories = categories.ToList(), Collapsed = nonDefaultCollpasingStateByPluginName.Contains(pluginSettings.Key.Name) ? !settingsAreCollapsed : settingsAreCollapsed };
                })
                .OrderBy(x => x.Info.Name)
                .ToList();
        }

        private static bool IsKeyboardShortcut(SettingEntryBase x)
        {
            return x.SettingType == typeof(BepInEx.Configuration.KeyboardShortcut);
        }

        private static bool ContainsSearchString(SettingEntryBase setting, string[] searchStrings)
        {
            var combinedSearchTarget = setting.PluginInfo.Name + "\n" +
                                       setting.PluginInfo.GUID + "\n" +
                                       setting.DispName + "\n" +
                                       setting.Category + "\n" +
                                       setting.Description + "\n" +
                                       setting.DefaultValue + "\n" +
                                       setting.Get();

            return searchStrings.All(s => combinedSearchTarget.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }

        private void CalculateDefaultWindowRect()
        {
            var width = Mathf.Min(Screen.width, 650);
            var height = Screen.height < 560 ? Screen.height : Screen.height - 100;
            var offsetX = Mathf.RoundToInt((Screen.width - width) / 2f);
            var offsetY = Mathf.RoundToInt((Screen.height - height) / 2f);
            DefaultWindowRect = new Rect(offsetX, offsetY, width, height);

            _screenRect = new Rect(0, 0, Screen.width, Screen.height);

            LeftColumnWidth = Mathf.RoundToInt(DefaultWindowRect.width / 2.5f);
            RightColumnWidth = (int)DefaultWindowRect.width - LeftColumnWidth - 115;
        }

        private static void DrawTooltip(Rect area)
        {
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                var currentEvent = Event.current;

                var style = new GUIStyle(boxStyle)
                {
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };
                var color = GUI.backgroundColor;
                GUI.backgroundColor = _entryBackgroundColor.Value;
                const int width = 400;
                var height = style.CalcHeight(new GUIContent(GUI.tooltip), 400) + 10;

                var x = currentEvent.mousePosition.x + width > area.width
                    ? area.width - width
                    : currentEvent.mousePosition.x;

                var y = currentEvent.mousePosition.y + 25 + height > area.height
                    ? currentEvent.mousePosition.y - height
                    : currentEvent.mousePosition.y + 25;

                GUI.Box(new Rect(x, y, width, height), GUI.tooltip, style);
                GUI.backgroundColor = color;
            }
        }


        /// <summary>
        /// String currently entered into the search box
        /// </summary>
        public string SearchString
        {
            get => _searchString;
            private set
            {
                if (value == null)
                    value = string.Empty;

                if (_searchString == value)
                    return;

                _searchString = value;

                BuildFilteredSettingList();
            }
        }

        private void Start()
        {
            // Use reflection to keep compatibility with unity 4.x since it doesn't have Cursor
            var tCursor = typeof(Cursor);
            _curLockState = tCursor.GetProperty("lockState", BindingFlags.Static | BindingFlags.Public);
            _curVisible = tCursor.GetProperty("visible", BindingFlags.Static | BindingFlags.Public);

            if (_curLockState == null && _curVisible == null)
            {
                _obsoleteCursor = true;
                
                _curLockState = typeof(Screen).GetProperty("lockCursor", BindingFlags.Static | BindingFlags.Public);
                _curVisible = typeof(Screen).GetProperty("showCursor", BindingFlags.Static | BindingFlags.Public);
            }

            // Check if user has permissions to write config files to disk
            try { Config.Save(); }
            catch (IOException ex) { Logger.Log(LogLevel.Message | LogLevel.Warning, "WARNING: Failed to write to config directory, expect issues!\nError message:" + ex.Message); }
            catch (UnauthorizedAccessException ex) { Logger.Log(LogLevel.Message | LogLevel.Warning, "WARNING: Permission denied to write to config directory, expect issues!\nError message:" + ex.Message); }
        }

        private void Update()
        {
            if (DisplayingWindow) SetUnlockCursor(0, true);

            if (OverrideHotkey) return;

            if (!DisplayingWindow && _keybind.Value.IsUp())
            {
                CreateBackgrounds();
               
                DisplayingWindow = true;
            }
        }

        private void CreateStyles()
        {
            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.textColor = _fontColor.Value;
            //windowStyle.fontSize = fontSize;
            windowStyle.active.textColor = _fontColor.Value;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = _fontColor.Value;
            labelStyle.fontSize = fontSize;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.textColor = _fontColor.Value;
            buttonStyle.fontSize = fontSize;

            categoryHeaderSkin = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                stretchWidth = true,
            };
            pluginHeaderSkin = new GUIStyle(categoryHeaderSkin);
            


            toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.normal.textColor = _fontColor.Value;
            toggleStyle.fontSize = fontSize;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.textColor = _fontColor.Value;
            boxStyle.fontSize = fontSize;

            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);

            thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
        }
        private void CreateBackgrounds()
        {
            var background = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            background.SetPixel(0, 0, _windowBackgroundColor.Value);
            background.Apply();
            WindowBackground = background;

            var entryBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            entryBackground.SetPixel(0, 0, _entryBackgroundColor.Value);
            entryBackground.Apply();
            EntryBackground = entryBackground;
        }

        private void LateUpdate()
        {
            if (DisplayingWindow) SetUnlockCursor(0, true);
        }

        private void SetUnlockCursor(int lockState, bool cursorVisible)
        {
            if (_curLockState != null)
            {
                // Do through reflection for unity 4 compat
                //Cursor.lockState = CursorLockMode.None;
                //Cursor.visible = true;
                if(_obsoleteCursor)
                    _curLockState.SetValue(null, Convert.ToBoolean(lockState), null);
                else
                    _curLockState.SetValue(null, lockState, null);
                
                _curVisible.SetValue(null, cursorVisible, null);
            }
        }

        private sealed class PluginSettingsData
        {
            public BepInPlugin Info;
            public List<PluginSettingsGroupData> Categories;
            private bool _collapsed;

            public bool Collapsed
            {
                get => _collapsed;
                set
                {
                    _collapsed = value;
                    Height = 0;
                }
            }

            public sealed class PluginSettingsGroupData
            {
                public string Name;
                public List<SettingEntryBase> Settings;
            }

            public int Height { get; set; }
        }

        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(ConfigurationManager).Namespace.ToLower()} reset"))
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
