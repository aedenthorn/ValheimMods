using BepInEx;
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
    [BepInPlugin("aedenthorn.NexusUpdate", "Nexus Update", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<Vector2> updatesPosition;
        public static ConfigEntry<int> updateTextWidth;
        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<Color> fontColor;
        public static ConfigEntry<int> betweenSpace;
        public static ConfigEntry<int> buttonWidth;
        public static ConfigEntry<int> buttonHeight;
        public static ConfigEntry<string> updateText;
        public static ConfigEntry<string> buttonText;
        public static ConfigEntry<int> nexusID;

        private List<NexusUpdatable> nexusUpdatables = new List<NexusUpdatable>();
        private bool finishedChecking = false;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            updatesPosition = Config.Bind<Vector2>("General", "UpdatesPosition", new Vector2(40, 40), "Position of the updates list on the screen");
            updateTextWidth = Config.Bind<int>("General", "UpdateTextWidth", 500, "Width of the update text (will wrap if it is too long)");
            buttonWidth = Config.Bind<int>("General", "ButtonWidth", 100, "Width of the update button");
            buttonHeight = Config.Bind<int>("General", "ButtonHeight", 30, "Height of the update button");
            betweenSpace = Config.Bind<int>("General", "BetweenSpace", 10, "Vertical space between each update in list");
            fontSize = Config.Bind<int>("General", "FontSize", 18, "Size of the text in the updates list");
            fontColor = Config.Bind<Color>("General", "FontColor", Color.white, "Color of the text in the updates list");
            updateText = Config.Bind<string>("General", "UpdateText", "<b>{0}</b> (v. {1}) has an updated version: <b>{2}</b>", "Text to show for each update. {0} is replaced by the mod name, {1} is replaced by the current version, and {2} is replaced by the remote version");
            buttonText = Config.Bind<string>("General", "ButtonText", "<b>Visit</b>", "Text to show for each update button");
            nexusID = Config.Bind<int>("General", "NexusID", 102, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            StartCoroutine(CheckPlugins());

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void OnGUI()
        {
            if (modEnabled.Value && FejdStartup.instance?.enabled == true && nexusUpdatables.Any() && finishedChecking)
            {
                GUIStyle style = new GUIStyle
                {
                    richText = true,
                    fontSize = fontSize.Value,
                    wordWrap = true,
                    alignment = TextAnchor.LowerLeft
                };
                style.normal.textColor = fontColor.Value;

                GUILayout.BeginArea(new Rect(updatesPosition.Value.x, updatesPosition.Value.y, updateTextWidth.Value + buttonWidth.Value + 20, Screen.height - updatesPosition.Value.y));
                for (int i = 0; i < nexusUpdatables.Count; i++)
                {
                    GUILayout.BeginHorizontal( new GUILayoutOption[] { GUILayout.Height(30) });
                    GUILayout.Label(string.Format(updateText.Value, nexusUpdatables[i].name, nexusUpdatables[i].currentVersion, nexusUpdatables[i].version), style, new GUILayoutOption[]{
                        GUILayout.Width(updateTextWidth.Value),
                        GUILayout.Height(buttonHeight.Value)
                    });
                    if(GUILayout.Button("<b>Visit</b>", new GUILayoutOption[]{
                        GUILayout.Width(buttonWidth.Value),
                        GUILayout.Height(buttonHeight.Value)
                    }))
                    {
                        Application.OpenURL($"https://www.nexusmods.com/valheim/mods/{nexusUpdatables[i].id}/?tab=files");
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(betweenSpace.Value);
                }
                GUILayout.EndArea();

            }
        }
        private IEnumerator CheckPlugins()
        {

            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
            {

                Version currentVersion = null;
                string pluginName = null;
                string guid = null;
                try
                {
                    var DLL = Assembly.LoadFile(file);

                    Type[] types = DLL.GetTypes();
                    foreach (Type type in types)
                    {
                        MethodInfo awake = type.GetMethod("Awake", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (awake != null)
                        {
                            Version version = type.GetCustomAttribute<BepInPlugin>()?.Version;
                            if (version != null)
                            {
                                currentVersion = version;
                                pluginName = type.GetCustomAttribute<BepInPlugin>()?.Name;
                                guid = type.GetCustomAttribute<BepInPlugin>()?.GUID;
                                break;
                            }
                        }
                    }

                }
                catch
                {
                    continue;
                }
                if (currentVersion == null)
                {
                    continue;
                }


                string cfgFile = Path.Combine(new string[]{ Directory.GetParent(Path.GetDirectoryName(file)).FullName, "config",$"{guid}.cfg"});
                Dbgl($"{cfgFile}");
                if (!File.Exists(cfgFile))
                {
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

                Dbgl($"{pluginName} {id} {file} current version: {currentVersion}");

                WWWForm form = new WWWForm();

                UnityWebRequest uwr = UnityWebRequest.Get($"https://www.nexusmods.com/valheim/mods/{id}");
                yield return uwr.SendWebRequest();

                if (uwr.isNetworkError)
                {
                    Debug.Log("Error While Sending: " + uwr.error);
                }
                else
                {
                    string[] lines = uwr.downloadHandler.text.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    bool check = false;
                    foreach (string line in lines)
                    {
                        if (check && line.Contains("<div class=\"stat\">"))
                        {
                            Match match = Regex.Match(line, @"<[^>]+>([0-9.]+)<[^>]+>");
                            if (!match.Success)
                                break;

                            Version version = new Version(match.Groups[1].Value);
                            Dbgl($"remote version: {version}.");
                            if (version > currentVersion)
                            {
                                Dbgl($"new remote version: {version}!");
                                nexusUpdatables.Add(new NexusUpdatable(pluginName, id, currentVersion, version));
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
