using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace NexusUpdate
{
    [BepInPlugin("aedenthorn.NexusUpdate", "Nexus Update", "0.8.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> showAllManagedMods;
        public static ConfigEntry<bool> createEmptyConfigFiles;
        public static ConfigEntry<bool> updateButtonFirst;
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

        private List<NexusUpdatable> nexusUpdatables = new List<NexusUpdatable>();
        private List<NexusUpdatable> nexusNonupdatables = new List<NexusUpdatable>();
        private Vector2 scrollPosition;
        private bool finishedChecking = false;
        private GUIStyle style;
        private GUIStyle style2;
        private GUIStyle backStyle;

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
            updateButtonFirst = Config.Bind<bool>("General", "updateButtonFirst", false, "If false, will put the button on the right side");
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

            backStyle = new GUIStyle();

            /*

                        int width = updateTextWidth.Value + buttonWidth.Value + 20;
                        int height = buttonHeight.Value;
                        Color col = Color.black;
                        Color[] pix = new Color[width * height];

                        for (int i = 0; i < pix.Length; i++)
                            pix[i] = col;

                        Texture2D result = new Texture2D(width, height);
                        result.SetPixels(pix);
                        result.Apply();

                        backStyle.normal.background = result;
            */
        }

        private void Start()
        {
            StartCoroutine(CheckPlugins());
        }

        private void OnGUI()
        {
            if (modEnabled.Value && FejdStartup.instance?.enabled == true)
            {

                GUILayout.BeginArea(new Rect(updatesPosition.Value.x, updatesPosition.Value.y, updateTextWidth.Value + buttonWidth.Value + 50, Screen.height - updatesPosition.Value.y - 80));
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, new GUILayoutOption[] { GUILayout.Width(updateTextWidth.Value + buttonWidth.Value + 40), GUILayout.Height(Screen.height - updatesPosition.Value.y - 80) });
                for (int i = 0; i < nexusUpdatables.Count; i++)
                {
                    GUILayout.BeginHorizontal(backStyle, new GUILayoutOption[] { GUILayout.Height(buttonHeight.Value), GUILayout.Width(updateTextWidth.Value + buttonWidth.Value + 20) });
                    if (updateButtonFirst.Value)
                    {
                        if (GUILayout.Button(buttonText.Value, new GUILayoutOption[]{
                            GUILayout.Width(buttonWidth.Value),
                            GUILayout.Height(buttonHeight.Value)
                        }))
                        {
                            Application.OpenURL($"https://www.nexusmods.com/valheim/mods/{nexusUpdatables[i].id}/?tab=files");
                        }

                    }
                    GUILayout.Label(string.Format(updateText.Value, nexusUpdatables[i].name, nexusUpdatables[i].currentVersion, nexusUpdatables[i].version), style, new GUILayoutOption[]{
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
                            Application.OpenURL($"https://www.nexusmods.com/valheim/mods/{nexusUpdatables[i].id}/?tab=files");
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(betweenSpace.Value);
                }
                if (showAllManagedMods.Value)
                {
                    for (int i = 0; i < nexusNonupdatables.Count; i++)
                    {
                        GUILayout.BeginHorizontal(backStyle, new GUILayoutOption[] { GUILayout.Height(buttonHeight.Value), GUILayout.Width(updateTextWidth.Value + buttonWidth.Value + 20) });
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
                if (!finishedChecking)
                {
                    GUILayout.Label(string.Format(checkingUpdatesText.Value), style, new GUILayoutOption[]{
                        GUILayout.Width(updateTextWidth.Value),
                        GUILayout.Height(buttonHeight.Value)
                    });
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();

            }
        }
        private IEnumerator CheckPlugins()
        {

            Dictionary<string, PluginInfo> pluginInfos = Chainloader.PluginInfos;

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
    }
}
