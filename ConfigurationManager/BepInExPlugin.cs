// Based on code made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ConfigurationManager
{
    /// <summary>
    /// An easy way to let user configure how a plugin behaves without the need to make your own GUI. The user can change any of the settings you expose, even keyboard shortcuts.
    /// </summary>
    [BepInPlugin(GUID, "Valheim Configuration Manager", "0.5.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// GUID of this plugin
        /// </summary>
        public const string GUID = "aedenthorn.ConfigurationManager";
        public static bool isDebug = true;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }

        internal static BepInExPlugin context;
        internal static new ManualLogSource Logger;
        public static SettingFieldDrawer _fieldDrawer;

        public const int WindowId = -68;

        public const string SearchBoxName = "searchBox";
        public bool _focusSearchBox;
        public string _searchString = string.Empty;

        /// <summary>
        /// Event fired every time the manager window is shown or hidden.
        /// </summary>
        public event EventHandler<ValueChangedEventArgs<bool>> DisplayingWindowChanged;

        /// <summary>
        /// Disable the hotkey check used by config manager. If enabled you have to set <see cref="DisplayingWindow"/> to show the manager.
        /// </summary>
        public bool OverrideHotkey;

        public bool _displayingWindow;
        public bool _obsoleteCursor;

        public string _modsWithoutSettings;

        public List<SettingEntryBase> _allSettings;
        public List<PluginSettingsData> _filteredSetings = new List<PluginSettingsData>();

        internal Rect DefaultWindowRect { get; public set; }
        public Rect _screenRect;
        public Rect currentWindowRect;
        public Vector2 _settingWindowScrollPos;
        public int _tipsHeight;
        public bool _showDebug;

        public PropertyInfo _curLockState;
        public PropertyInfo _curVisible;
        public int _previousCursorLockState;
        public bool _previousCursorVisible;

        internal static Texture2D WindowBackground { get; public set; }
        internal static Texture2D EntryBackground { get; public set; }
        internal static Texture2D WidgetBackground { get; public set; }

        internal int LeftColumnWidth { get; public set; }
        internal int RightColumnWidth { get; public set; }

        public static ConfigEntry<bool> _showAdvanced;
        public static ConfigEntry<bool> _showKeybinds;
        public static ConfigEntry<bool> _showSettings;
        public static ConfigEntry<bool> _showMenuButton;

        public static ConfigEntry<KeyboardShortcut> _keybind;
        public static ConfigEntry<bool> _hideSingleSection;
        public static ConfigEntry<bool> _pluginConfigCollapsedDefault;
        public static ConfigEntry<Vector2> _windowPosition;
        public static ConfigEntry<Vector2> _windowSize;
        
        public static ConfigEntry<string> _windowTitle;
        public static ConfigEntry<string> _normalText;
        public static ConfigEntry<string> _shortcutsText;
        public static ConfigEntry<string> _advancedText;
        public static ConfigEntry<string> _searchText;
        public static ConfigEntry<string> _reloadText;
        public static ConfigEntry<string> _resetText;
        public static ConfigEntry<string> _resetSettingText;
        public static ConfigEntry<string> _expandText;
        public static ConfigEntry<string> _collapseText;
        public static ConfigEntry<string> _tipText;
        public static ConfigEntry<string> _clearText;
        public static ConfigEntry<string> _openMenuText;

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
        public static GUIStyle textStyle;
        public static GUIStyle toggleStyle;
        public static GUIStyle buttonStyle;
        public static GUIStyle boxStyle;
        public static GUIStyle sliderStyle;
        public static GUIStyle thumbStyle;
        public static GUIStyle categoryHeaderSkin;
        public static GUIStyle pluginHeaderSkin;
        public static int fontSize = 14;

        /// <inheritdoc />
        public BepInExPlugin()
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
            _showMenuButton = Config.Bind("General", "Show Menu Button", true, new ConfigDescription("Show the menu button on the start menu"));

            _windowTitle = Config.Bind("Text", "WindowTitle", "Configuration Manager", new ConfigDescription("Window title text"));
            _normalText = Config.Bind("Text", "NormalText", "Normal", new ConfigDescription("Normal settings toggle text"));
            _shortcutsText = Config.Bind("Text", "ShortcutsText", "Keybinds", new ConfigDescription("Shortcut key settings toggle text"));
            _advancedText = Config.Bind("Text", "AdvancedText", "Advanced", new ConfigDescription("Advanced settings toggle text"));
            _searchText = Config.Bind("Text", "SearchText", "Search Settings: ", new ConfigDescription("Search label text"));
            _reloadText = Config.Bind("Text", "ReloadText", "Reload From File", new ConfigDescription("Reload mod config from file text"));
            _resetText = Config.Bind("Text", "ResetText", "Reset To Default", new ConfigDescription("Reset mod config to default text"));
            _resetSettingText = Config.Bind("Text", "ResetSettingText", "Reset", new ConfigDescription("Reset setting text"));
            _expandText = Config.Bind("Text", "ExpandText", "Expand", new ConfigDescription("Expand button text"));
            _collapseText = Config.Bind("Text", "CollapseText", "Collapse", new ConfigDescription("Collapse button text"));
            _tipText = Config.Bind("Text", "TipText", "Tip: Click plugin names to expand. Hover over setting names to see their descriptions.", new ConfigDescription("Tip text"));
            _clearText = Config.Bind("Text", "ClearText", "Clear", new ConfigDescription("Clear search text"));
            _openMenuText = Config.Bind("Text", "OpenMenuText", "Open Config Menu", new ConfigDescription("Open Menu Button text"));

            _pluginConfigCollapsedDefault = Config.Bind("General", "Plugin collapsed default", true, new ConfigDescription("If set to true plugins will be collapsed when opening the configuration manager window"));
            _windowPosition = Config.Bind("General", "WindowPosition", new Vector2(55, 35), "Window position");
            _windowSize = Config.Bind("General", "WindowSize", DefaultWindowRect.size, "Window size");
            _textSize = Config.Bind("General", "FontSize", 14, "Font Size");
            _windowBackgroundColor = Config.Bind("Colors", "WindowBackgroundColor", new Color(0, 0, 0, 1), "Window background color");
            _entryBackgroundColor = Config.Bind("Colors", "EntryBackgroundColor", new Color(0.557f, 0.502f, 0.502f, 0.871f), "Entry background color");
            _fontColor = Config.Bind("Colors", "FontColor", new Color(1, 0.714f, 0.361f, 1), "Font color");
            _widgetBackgroundColor = Config.Bind("Colors", "WidgetColor", new Color(0.882f, 0.463f, 0, 0.749f), "Widget color");

            currentWindowRect = new Rect(_windowPosition.Value, _windowSize.Value);

            Patches.ApplyPatches();
        }

        public void OnGUI()
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

                if(_windowSize.Value.x > 200 && _windowSize.Value.x < Screen.width && _windowSize.Value.y > 200 && _windowSize.Value.y < Screen.height)
                    currentWindowRect.size = _windowSize.Value;

                RightColumnWidth = Mathf.RoundToInt(currentWindowRect.width / 2.5f * fontSize / 12f);
                LeftColumnWidth = Mathf.RoundToInt(currentWindowRect.width - RightColumnWidth - 115);


                currentWindowRect = GUILayout.Window(WindowId, currentWindowRect, SettingsWindow, _windowTitle.Value, windowStyle);

                if (!SettingFieldDrawer.SettingKeyboardShortcut)
                    Input.ResetInputAxes();

                if (!Input.GetKey(KeyCode.Mouse0) && (currentWindowRect.x != _windowPosition.Value.x || currentWindowRect.y != _windowPosition.Value.y))
                {
                    _windowPosition.Value = currentWindowRect.position;
                    Config.Save();
                }
            }
        }

        public void SettingsWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, currentWindowRect.width, 20));
            DrawWindowHeader();

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

        public void DrawTips()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(_tipText.Value, labelStyle);

                GUILayout.FlexibleSpace();

                Color color = GUI.backgroundColor;
                GUI.backgroundColor = _widgetBackgroundColor.Value;
                if (GUILayout.Button(_pluginConfigCollapsedDefault.Value ? _expandText.Value : _collapseText.Value, buttonStyle, GUILayout.ExpandWidth(false)))
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

        public void DrawWindowHeader()
        {
            GUI.backgroundColor = _entryBackgroundColor.Value;
            GUILayout.BeginHorizontal();
            {
                GUI.enabled = SearchString == string.Empty;

                var newVal = GUILayout.Toggle(_showSettings.Value, _normalText.Value, toggleStyle);
                if (_showSettings.Value != newVal)
                {
                    _showSettings.Value = newVal;
                    BuildFilteredSettingList();
                }

                newVal = GUILayout.Toggle(_showKeybinds.Value, _shortcutsText.Value, toggleStyle);
                if (_showKeybinds.Value != newVal)
                {
                    _showKeybinds.Value = newVal;
                    BuildFilteredSettingList();
                }

                newVal = GUILayout.Toggle(_showAdvanced.Value, _advancedText.Value, toggleStyle);
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

                if (GUILayout.Button("Close", buttonStyle, GUILayout.ExpandWidth(false)))
                    DisplayingWindow = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(_searchText.Value, labelStyle, GUILayout.ExpandWidth(false));

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
                if (GUILayout.Button(_clearText.Value, buttonStyle, GUILayout.ExpandWidth(false)))
                    SearchString = string.Empty;
                GUI.backgroundColor = color;
            }
            GUILayout.EndHorizontal();
        }

        public void DrawSinglePlugin(PluginSettingsData plugin)
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
                GUILayout.BeginHorizontal();
                var color = GUI.backgroundColor;
                GUI.backgroundColor = _widgetBackgroundColor.Value;
                if (GUILayout.Button(_reloadText.Value, buttonStyle, GUILayout.ExpandWidth(true)))
                {
                    foreach (var category in plugin.Categories)
                    {
                        foreach (var setting in category.Settings)
                        {
                            setting.PluginInstance.Config.Reload();
                            break;
                        }
                        break;
                    }
                    BuildFilteredSettingList();
                }
                if (GUILayout.Button(_resetText.Value, buttonStyle, GUILayout.ExpandWidth(true)))
                {
                    foreach (var category in plugin.Categories)
                    {
                        foreach (var setting in category.Settings)
                        {
                            setting.Set(setting.DefaultValue);
                        }
                    }
                    BuildFilteredSettingList();
                }
                GUI.backgroundColor = color;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        public void DrawSingleSetting(SettingEntryBase setting)
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

        public void DrawSettingName(SettingEntryBase setting)
        {
            if (setting.HideSettingName) return;


            GUILayout.Label(new GUIContent(setting.DispName.TrimStart('!'), setting.Description), labelStyle,
                GUILayout.Width(LeftColumnWidth), GUILayout.MaxWidth(LeftColumnWidth));

        }

        public static void DrawDefaultButton(SettingEntryBase setting)
        {
            if (setting.HideDefaultButton) return;

            GUI.backgroundColor = _widgetBackgroundColor.Value;

            bool DrawDefaultButton()
            {
                GUILayout.Space(5);
                return GUILayout.Button(_resetSettingText.Value, buttonStyle, GUILayout.ExpandWidth(false));
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

        public void BuildFilteredSettingList()
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
                    return new PluginSettingsData {Info = pluginSettings.Key, Categories = categories.ToList(), Collapsed = nonDefaultCollpasingStateByPluginName.Contains(pluginSettings.Key.Name) ? !settingsAreCollapsed : settingsAreCollapsed };
                })
                .OrderBy(x => x.Info.Name)
                .ToList();
        }

        public static bool IsKeyboardShortcut(SettingEntryBase x)
        {
            return x.SettingType == typeof(KeyboardShortcut);
        }

        public static bool ContainsSearchString(SettingEntryBase setting, string[] searchStrings)
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

        public void CalculateDefaultWindowRect()
        {
            var width = Mathf.Min(Screen.width, 650);
            var height = Screen.height < 800 ? Screen.height : 800;
            var offsetX = Mathf.RoundToInt((Screen.width - width) / 2f);
            var offsetY = Mathf.RoundToInt((Screen.height - height) / 2f);
            DefaultWindowRect = new Rect(offsetX, offsetY, width, height);

            _screenRect = new Rect(0, 0, Screen.width, Screen.height);

            LeftColumnWidth = Mathf.RoundToInt(DefaultWindowRect.width / 2.5f);
            RightColumnWidth = (int)DefaultWindowRect.width - LeftColumnWidth - 115;
        }

        public static void DrawTooltip(Rect area)
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
            public set
            {
                if (value == null)
                    value = string.Empty;

                if (_searchString == value)
                    return;

                _searchString = value;

                BuildFilteredSettingList();
            }
        }

        public void Start()
        {

            try
            {
                Dbgl("Searching for vanilla Config Manager.");
                var vanilla = Chainloader.PluginInfos.First(p => p.Key == "com.bepis.bepinex.configurationmanager");
                vanilla.Value.Instance.enabled = false;
                Dbgl("Disabled Vanilla Config Manager");
            }
            catch
            {
                Dbgl("Vanilla Config Manager not found.");
            }


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

        public void Update()
        {
            if (DisplayingWindow) SetUnlockCursor(0, true);

            if (OverrideHotkey) return;

            if (!DisplayingWindow && _keybind.Value.IsUp())
            {
                CreateBackgrounds();
               
                DisplayingWindow = true;
            }
        }

        public void CreateStyles()
        {
            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.textColor = _fontColor.Value;
            //windowStyle.fontSize = fontSize;
            windowStyle.active.textColor = _fontColor.Value;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = _fontColor.Value;
            labelStyle.fontSize = fontSize;

            textStyle = new GUIStyle(GUI.skin.textArea);
            textStyle.normal.textColor = _fontColor.Value;
            textStyle.fontSize = fontSize;

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
        public void CreateBackgrounds()
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

        public void LateUpdate()
        {
            if (DisplayingWindow) SetUnlockCursor(0, true);
        }

        public void SetUnlockCursor(int lockState, bool cursorVisible)
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

        public sealed class PluginSettingsData
        {
            public BepInPlugin Info;
            public List<PluginSettingsGroupData> Categories;
            public bool _collapsed;

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
    }
}
