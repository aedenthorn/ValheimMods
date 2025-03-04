using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace NexusUpdate
{
    [BepInPlugin("aedenthorn.NexusUpdate", "Nexus Update", "1.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> showAllManagedMods;
        public static ConfigEntry<bool> createEmptyConfigFiles;
        public static ConfigEntry<bool> showIgnoreButton;
        public static ConfigEntry<bool> updateButtonFirst;

        public static ConfigEntry<Vector2> updatesPosition;
        public static ConfigEntry<int> updateTextWidth;
        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<Color> updateFontColor;
        public static ConfigEntry<int> buttonWidth;
        public static ConfigEntry<int> buttonHeight;
        public static ConfigEntry<int> betweenSpace;
        public static ConfigEntry<float> windowHeight;
        public static ConfigEntry<Color> windowBackgroundColor;

        public static ConfigEntry<Color> nonUpdateFontColor;

        public static ConfigEntry<string> updateText;
        public static ConfigEntry<string> nonUpdateText;
        public static ConfigEntry<string> checkingUpdatesText;
        public static ConfigEntry<string> buttonText;
        public static ConfigEntry<string> ignoreButtonText;
        public static ConfigEntry<string> windowTitleText;
        public static ConfigEntry<string> windowTitleTextChecking;

        public static ConfigEntry<string> ignoreList;

        public static ConfigEntry<int> nexusID;

        public static List<NexusUpdatable> nexusUpdatables = new List<NexusUpdatable>();
        public static List<NexusUpdatable> nexusNonupdatables = new List<NexusUpdatable>();
        public static Vector2 scrollPosition;
        public static bool finishedChecking = false;
        public static GUIStyle style;
        public static GUIStyle style2;
        public static float rowWidth;
        public static Rect windowRect;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            showAllManagedMods = Config.Bind<bool>("General", "ShowAllManagedMods", false, "Show all mods that have a nexus ID in the list, even if they are up-to-date");
            createEmptyConfigFiles = Config.Bind<bool>("General", "CreateEmptyConfigFiles", false, "Create empty GUID-based config files for mods that don't have them (may cause there to be duplicate config files)");
            showIgnoreButton = Config.Bind<bool>("General", "ShowIgnoreButton", true, "If true, will add a button to ignore an existing update - only works if ShowAllManagedMods is false");
            updateButtonFirst = Config.Bind<bool>("General", "UpdateButtonFirst", false, "If false, will put the button on the right side");
            ignoreList = Config.Bind<string>("General", "IgnoreList", "", "Comma-separated list of updates to ignore, format: NexusModId:VersionNumber");
            
            updatesPosition = Config.Bind<Vector2>("UI", "UpdatesPosition", new Vector2(40, 40), "Position of the updates list on the screen");
            updateTextWidth = Config.Bind<int>("UI", "UpdateTextWidth", Screen.width / 6, "Width of the update text (will wrap if it is too long)");
            buttonWidth = Config.Bind<int>("UI", "ButtonWidth", 100, "Width of the update button");
            buttonHeight = Config.Bind<int>("UI", "ButtonHeight", 30, "Height of the update button");
            betweenSpace = Config.Bind<int>("UI", "BetweenSpace", 10, "Vertical space between each update in list");
            windowHeight = Config.Bind<float>("UI", "WindowHeight", Screen.height / 3, "Height of the update window");
            windowBackgroundColor = Config.Bind<Color>("UI", "WindowBackgroundColor", new Color(1,1,1,0.25f), "Color of the window background");
            
            fontSize = Config.Bind<int>("Text", "FontSize", 14, "Size of the text in the updates list");
            updateFontColor = Config.Bind<Color>("Text", "UpdateFontColor", new Color(1,1,0.7f,1), "Color of the text in the updateable list");
            nonUpdateFontColor = Config.Bind<Color>("Text", "NonUpdateFontColor", Color.white, "Color of the text in the non-updateable list");
            windowTitleText = Config.Bind<string>("Text", "WindowTitleText", "<b>Nexus Updates</b>", "Window title when not checking for updates");
            windowTitleTextChecking = Config.Bind<string>("Text", "WindowTitleTextChecking", "<b>Nexus Updates - checking for updates...</b>", "Text to show for each ignore button");
            updateText = Config.Bind<string>("Text", "UpdateText", "<b>{0}</b> (v. {1}) has an updated version: <b>{2}</b>", "Text to show for each update. {0} is replaced by the mod name, {1} is replaced by the current version, and {2} is replaced by the remote version");
            nonUpdateText = Config.Bind<string>("Text", "NonUpdateText", "<b>{0}</b> (v. {1}) is up-to-date!", "Text to show for each update. {0} is replaced by the mod name, {1} is replaced by the current version, and {2} is replaced by the remote version");
            buttonText = Config.Bind<string>("Text", "ButtonText", "<b>Visit</b>", "Text to show for each update button");
            ignoreButtonText = Config.Bind<string>("Text", "IgnoreButtonText", "<b>Ignore</b>", "Text to show for each ignore button");
            
            
            
            nexusID = Config.Bind<int>("General", "NexusID", 102, "Nexus mod ID for updates");

            if (!modEnabled.Value) 
                return;

            ApplyConfig();


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static void ApplyConfig()
        {
            rowWidth = updateTextWidth.Value + buttonWidth.Value * (!showAllManagedMods.Value && showIgnoreButton.Value ? 2 : 1);

            windowRect = new Rect(updatesPosition.Value.x, updatesPosition.Value.y, rowWidth + 50, windowHeight.Value);

            style = new GUIStyle
            {
                richText = true,
                fontSize = fontSize.Value,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            style.normal.textColor = updateFontColor.Value;
            style2 = new GUIStyle
            {
                richText = true,
                fontSize = fontSize.Value,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            style2.normal.textColor = nonUpdateFontColor.Value;

        }

        public void Start()
        {
            if(modEnabled.Value)
                StartCoroutine(CheckPlugins());
        }

        public void OnGUI()
        {
            if (modEnabled.Value && FejdStartup.instance?.enabled == true)
            {
                GUI.backgroundColor = windowBackgroundColor.Value;


                if (!finishedChecking || nexusUpdatables.Any() || (showAllManagedMods.Value && nexusNonupdatables.Any()))
                    windowRect = GUI.Window(424242, windowRect, new GUI.WindowFunction(WindowBuilder), finishedChecking ? windowTitleText.Value : windowTitleTextChecking.Value);
                if(!Input.GetKey(KeyCode.Mouse0) && ( windowRect.x != updatesPosition.Value.x || windowRect.y != updatesPosition.Value.y))
                {
                    updatesPosition.Value = new Vector2(windowRect.x, windowRect.y);
                    Config.Save();
                }
            }
        }

        public void WindowBuilder(int id)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(new Rect(0, 0, rowWidth + 50, 20));

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, new GUILayoutOption[] { GUILayout.Width(rowWidth + 40), GUILayout.Height(windowHeight.Value - 30) });
            List<NexusUpdatable> reverseNUL = new List<NexusUpdatable>(nexusUpdatables);
            reverseNUL.Reverse();
            for (int i = reverseNUL.Count - 1; i >= 0; i--)
            {

                if (!showAllManagedMods.Value && IsIgnored(reverseNUL[i]))
                    continue;

                GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(buttonHeight.Value), GUILayout.Width(rowWidth + 20) });
                if (updateButtonFirst.Value)
                {
                    if (GUILayout.Button(buttonText.Value, new GUILayoutOption[]{
                            GUILayout.Width(buttonWidth.Value),
                            GUILayout.Height(buttonHeight.Value)
                        }))
                    {
                        Application.OpenURL($"https://www.nexusmods.com/valheim/mods/{reverseNUL[i].id}/?tab=files");
                    }
                    if (showIgnoreButton.Value && !showAllManagedMods.Value)
                    {
                        if (GUILayout.Button(ignoreButtonText.Value, new GUILayoutOption[]{
                            GUILayout.Width(buttonWidth.Value),
                            GUILayout.Height(buttonHeight.Value)
                        }))
                        {
                            AddIgnore($"{reverseNUL[i].id}:{reverseNUL[i].version}");
                            reverseNUL.RemoveAt(i);
                        }
                    }
                }
                GUILayout.Label(string.Format(updateText.Value, reverseNUL[i].name, reverseNUL[i].currentVersion, reverseNUL[i].version), style, new GUILayoutOption[]{
                        GUILayout.Width(updateTextWidth.Value),
                        GUILayout.Height(buttonHeight.Value)
                    });
                if (!updateButtonFirst.Value)
                {
                    if (GUILayout.Button(buttonText.Value, new GUILayoutOption[]{
                            GUILayout.Width(buttonWidth.Value),
                            GUILayout.Height(buttonHeight.Value)
                        }))
                    {
                        Application.OpenURL($"https://www.nexusmods.com/valheim/mods/{reverseNUL[i].id}/?tab=files");
                    }
                    if (showIgnoreButton.Value && !showAllManagedMods.Value)
                    {
                        if (GUILayout.Button(ignoreButtonText.Value, new GUILayoutOption[]{
                            GUILayout.Width(buttonWidth.Value),
                            GUILayout.Height(buttonHeight.Value)
                        }))
                        {
                            AddIgnore($"{reverseNUL[i].id}:{reverseNUL[i].version}");
                            reverseNUL.RemoveAt(i);
                        }
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(betweenSpace.Value);
            }

            nexusUpdatables = new List<NexusUpdatable>(reverseNUL);
            nexusUpdatables.Reverse();

            if (showAllManagedMods.Value)
            {
                for (int i = 0; i < nexusNonupdatables.Count; i++)
                {
                    GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(buttonHeight.Value), GUILayout.Width(rowWidth + 20) });
                    if (updateButtonFirst.Value)
                    {
                        if (GUILayout.Button(buttonText.Value, new GUILayoutOption[]{
                                GUILayout.Width(buttonWidth.Value),
                                GUILayout.Height(buttonHeight.Value)
                            }))
                        {
                            Application.OpenURL($"https://www.nexusmods.com/valheim/mods/{nexusNonupdatables[i].id}/?tab=files");
                        }
                    }
                    GUILayout.Label(string.Format(nonUpdateText.Value, nexusNonupdatables[i].name, nexusNonupdatables[i].currentVersion, nexusNonupdatables[i].version), style2, new GUILayoutOption[]{
                            GUILayout.Width(updateTextWidth.Value),
                            GUILayout.Height(buttonHeight.Value)
                        });
                    if (!updateButtonFirst.Value)
                    {
                        if (GUILayout.Button(buttonText.Value, new GUILayoutOption[]{
                                GUILayout.Width(buttonWidth.Value),
                                GUILayout.Height(buttonHeight.Value)
                            }))
                        {
                            Application.OpenURL($"https://www.nexusmods.com/valheim/mods/{nexusNonupdatables[i].id}/?tab=files");
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(betweenSpace.Value);
                }

            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        public bool IsIgnored(NexusUpdatable nexusUpdatable)
        {
            var dict = GetIgnores();
            return dict.ContainsKey("" + nexusUpdatable.id) && dict["" + nexusUpdatable.id] == nexusUpdatable.version.ToString();
        }

        public void AddIgnore(string str)
        {
            Dictionary<string, string> dict = GetIgnores();

            string[] ignore = str.Split(':');
            if (ignore.Length == 2 && int.TryParse(ignore[0], out int id) && Version.TryParse(ignore[1], out Version version))
                dict.Add(ignore[0], ignore[1]);
            MakeIgnores(dict);
        }
        public void RemoveIgnore(string id)
        {
            Dictionary<string, string> dict = GetIgnores();
            dict.Remove(id);
            MakeIgnores(dict);
        }

        public Dictionary<string, string> GetIgnores()
        {
            if (ignoreList.Value == null || ignoreList.Value.Length == 0)
                return new Dictionary<string, string>();

            string[] ignores = ignoreList.Value.Split(',');
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach(string str in ignores)
            {
                string[] ignore = str.Split(':');
                ignore[0] = ignore[0].Trim();
                ignore[1] = ignore[1].Trim();
                if (ignore.Length == 2 && int.TryParse(ignore[0], out int id) && Version.TryParse(ignore[1], out Version version))
                    dict[ignore[0]] = ignore[1];
            }
            MakeIgnores(dict);

            return dict;
        }

        public void MakeIgnores(Dictionary<string, string> dict)
        {
            List<string> strings = new List<string>();

            foreach (var kvp in dict)
                strings.Add($"{kvp.Key}:{kvp.Value}");

            string newIgnores = string.Join(",", strings);
            if (newIgnores != ignoreList.Value)
            {
                ignoreList.Value = newIgnores;
                Config.Save();
            }
        }

        public IEnumerator CheckPlugins()
        {

            Dictionary<string, PluginInfo> pluginInfos = Chainloader.PluginInfos;
            Dictionary<string, string> ignores = GetIgnores();
            foreach (KeyValuePair<string, PluginInfo> kvp in pluginInfos)
            {

                Version currentVersion = kvp.Value.Metadata.Version;
                string pluginName = kvp.Value.Metadata.Name;
                string guid = kvp.Value.Metadata.GUID;

                string cfgFile = Path.Combine(new string[]{ Directory.GetParent(Path.GetDirectoryName(typeof(BepInProcess).Assembly.Location)).FullName, "config", $"{guid}.cfg"});
                //Dbgl($"{cfgFile}");
                if (!File.Exists(cfgFile))
                {
                    if(createEmptyConfigFiles.Value)
                        File.Create(cfgFile);
                    continue;
                }

                int id = -1;
                string[] cfgLines = File.ReadAllLines(cfgFile);
                foreach(string line in cfgLines)
                {
                    if (line.Trim().ToLower().StartsWith("nexusid"))
                    {
                        Match match = Regex.Match(line, @"[0-9]+");
                        if (match.Success)
                            id = int.Parse(match.Value);
                        break;
                    }
                }
                if (id == -1)
                    continue;

                Dbgl($"{pluginName} {id} current version: {currentVersion}");

                WWWForm form = new WWWForm();

                UnityWebRequest uwr = UnityWebRequest.Get($"https://www.nexusmods.com/valheim/mods/{id}");
                yield return uwr.SendWebRequest();

                if (uwr.isNetworkError)
                {
                    Debug.Log("Error While Sending: " + uwr.error);
                }
                else
                {
                    //Dbgl($"entire text: {uwr.downloadHandler.text}.");

                    string[] lines = uwr.downloadHandler.text.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    bool check = false;
                    foreach (string line in lines)
                    {
                        if (check && line.Contains("<div class=\"stat\">"))
                        {
                            Match match = Regex.Match(line, @"<[^>]+>[^0-9.]*([0-9.]+)[^0-9.]*<[^>]+>");
                            if (!match.Success)
                                break;

                            Version version = new Version(match.Groups[1].Value);
                            Dbgl($"remote version: {version}.");

                            if(ignores.ContainsKey("" + id))
                            {
                                if(ignores["" + id] == version.ToString())
                                {
                                    if (!showAllManagedMods.Value)
                                    {
                                        Dbgl($"ignoring {pluginName} {id} version: {version}");
                                        break;
                                    }
                                }
                                else
                                {
                                    Dbgl($"new version {version}, removing ignore {ignores[""+id]}");
                                    RemoveIgnore(""+id);
                                }
                            }


                            if (version > currentVersion)
                            {
                                Dbgl($"new remote version: {version}!");
                                nexusUpdatables.Add(new NexusUpdatable(pluginName, id, currentVersion, version));
                            }
                            else if (showAllManagedMods.Value)
                            {
                                nexusNonupdatables.Add(new NexusUpdatable(pluginName, id, currentVersion, version));
                            }
                            break;
                        }
                        if (line.Contains("<li class=\"stat-version\">"))
                            check = true;
                    }
                }
            }
            finishedChecking = true;
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("nexusupdate reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    ApplyConfig();

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Nexus Update Check config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
