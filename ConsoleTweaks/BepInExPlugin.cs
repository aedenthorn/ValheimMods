using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ConsoleTweaks
{
    [BepInPlugin("aedenthorn.ConsoleTweaks", "Console Tweaks", "0.5.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> cheatsEnabled;
        public static ConfigEntry<bool> debugEnabled;
        public static ConfigEntry<bool> skEnabled;
        public static ConfigEntry<bool> aedenthornEnabled;
        public static ConfigEntry<int> nexusID;

        public static string spawnString = "";
        public static string commandString = "";
        public static string spawnSuffix = "";
        public static string commandSuffix = "";

        public static List<string> spawnStrings = new List<string>();
        public static List<string> skCommandStrings = new List<string>()
        {
            "/alt",
            "/coords",
            "/clear",
            "/clearinventory",
            "/detect",
            "/env",
            "/event",
            "/farinteract",
            "/findtomb",
            "/fly",
            "/freecam",
            "/ghost",
            "/give",
            "/god",
            "/heal",
            "/devcommands",
            "/infstam",
            "/killall",
            "/listitems",
            "/listskills",
            "/nocost",
            "/nores",
            "/nosup",
            "/portals",
            "/q",
            "/randomevent",
            "/removedrops",
            "/repair",
            "/resetmap",
            "/resetwind",
            "/revealmap",
            "/seed",
            "/set",
            "/set",
            "/set",
            "/set",
            "/set",
            "/set",
            "/set",
            "/spawn",
            "/stopevent",
            "/td",
            "/tl",
            "/tr",
            "/tu",
            "/tame",
            "/tod",
            "/tp",
            "/wind",
            "/whois",
        };
        public static List<string> basicCommandStrings = new List<string>()
        {
            "help",
            "kick",
            "ban",
            "unban",
            "banned",
            "ping",
            "lodbias",
            "info",
            "devcommands"
        };
        public static List<string> cheatCommandStrings = new List<string>()
        {

            "genloc",
            "debugmode",
            "spawn",
            "pos",
            "goto",
            "exploremap",
            "resetmap",
            "killall",
            "tame",
            "hair",
            "beard",
            "location",
            "raiseskill",
            "resetskill",
            "freefly",
            "ffsmooth",
            "tod",
            "env",
            "resetenv",
            "wind",
            "resetwind",
            "god",
            "event",
            "stopevent",
            "randomevent",
            "save",
            "resetcharacter",
            "removedrops",
            "setkey",
            "resetkeys",
            "listkeys",
            "players",
            "dpsdebug"
        };
        public static List<string> commandStrings = new List<string>();
        
        public static List<string> aedenthornPlugins = new List<string>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            cheatsEnabled = Config.Bind<bool>("General", "CheatsEnabled", true, "Enable cheats by default");
            debugEnabled = Config.Bind<bool>("General", "DebugEnabled", false, "Enable debug mode by default");
            skEnabled = Config.Bind<bool>("General", "SkEnabled", true, "Enable SkToolbox command completion");
            aedenthornEnabled = Config.Bind<bool>("General", "AedenthornEnabled", true, "Enable aedenthorn mod reset completion");
            nexusID = Config.Bind<int>("General", "NexusID", 464, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            if (debugEnabled.Value)
                Player.m_debugMode = true;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Start()
        {
            foreach(var plugin in Chainloader.PluginInfos)
            {
                if (plugin.Key.StartsWith("aedenthorn."))
                {
                    aedenthornPlugins.Add(plugin.Key.Substring("aedenthorn.".Length).ToLower());
                }
            }
        }
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5) && Console.instance.m_chatWindow.gameObject.activeSelf) 
            {
                Dbgl($"Opening console");

                commandStrings = new List<string>(basicCommandStrings);
                if (skEnabled.Value)
                    commandStrings.AddRange(skCommandStrings);

                if (aedenthornEnabled.Value)
                    commandStrings.AddRange(aedenthornPlugins);

                if (cheatsEnabled.Value)
                    Traverse.Create(Console.instance).Field("m_cheat").SetValue(true);

                if(Traverse.Create(Console.instance).Field("m_cheat").GetValue<bool>())
                    commandStrings.AddRange(cheatCommandStrings);

                LoadSpawnStrings();
            }
        }

        [HarmonyPatch(typeof(FejdStartup), "Start")]
        public static class FejdStartup_Start_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                Console.SetConsoleEnabled(true);
            }
        }

        public void LoadSpawnStrings()
        {
            if (ZNetScene.instance == null)
                return;
            spawnStrings.Clear();
            foreach(GameObject go in Traverse.Create(ZNetScene.instance).Field("m_namedPrefabs").GetValue<Dictionary<int, GameObject>>().Values)
            {
                spawnStrings.Add(Utils.GetPrefabName(go));
            }
            Dbgl($"Loaded {spawnStrings.Count} strings");
        }

        [HarmonyPatch(typeof(Console), "Update")]
        public static class UpdateChat_Patch
        {
            public static void Postfix(Console __instance)
            {
                if (!modEnabled.Value || !Console.instance.m_chatWindow.gameObject.activeSelf)
                    return;
                string str = __instance.m_input.text;
                string[] words = str.Split(' ');

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    int caret = __instance.m_input.caretPosition;
                    string afterCaret = str.Substring(caret);
                    int space = afterCaret.IndexOf(' ');
                    if (space == 0)
                        space = 1;
                    else if (space == -1)
                        space = afterCaret.Length;
                    __instance.m_input.caretPosition += space;
                }

                if (words.Length > 1 && !aedenthornPlugins.Contains(words[0]) && words[words.Length - 1] == "reset")
                {
                    words = words.Take(words.Length - 1).ToArray();
                    str = string.Join(" ", words);
                    __instance.m_input.text = str;
                    commandString = "";
                }

                string strToCaret = str.Substring(0, __instance.m_input.caretPosition);
                string[] wordsToCaret = strToCaret.Split(' ');
                string suffix = str.Substring(__instance.m_input.caretPosition).Split(' ')[0];
                if (wordsToCaret.Length == 1)
                {
                    string prefix = wordsToCaret[0];
                    if (suffix == commandSuffix)
                        words[0] = prefix;
                    if (commandString != prefix)
                    {
                        if(prefix.Length > 0)
                        {
                            string exact = commandStrings.Find(s => s.ToLower() == words[0].ToLower());
                            string partial = commandStrings.Find(s => s.ToLower().StartsWith(wordsToCaret[0].ToLower()));
                            if (partial != null && exact == null)
                            {
                                commandSuffix = partial.Substring(prefix.Length);
                                if (commandSuffix.Length > 0)
                                {
                                    words[0] = partial;
                                    if (aedenthornPlugins.Contains(words[0]) && words.Length < 3)
                                        words = new string[] { partial};
                                    
                                }
                            }
                        }
                        __instance.m_input.text = string.Join(" ", words);
                        commandString = prefix;
                    }
                }
                else if (wordsToCaret.Length == 2 && wordsToCaret[0] == "spawn")
                {
                    string prefix = wordsToCaret[1];
                    if (suffix == spawnSuffix)
                        words[1] = prefix;
                    if (spawnString != prefix)
                    {
                        if (prefix.Length > 0)
                        {
                            string exact = spawnStrings.Find(s => s.ToLower() == words[1].ToLower());
                            string partial = spawnStrings.Find(s => s.ToLower().StartsWith(wordsToCaret[1].ToLower()));
                            if (partial != null && exact == null)
                            {
                                spawnSuffix = partial.Substring(prefix.Length);
                                if (spawnSuffix.Length > 0)
                                {
                                    //Dbgl($"got match {partial}");
                                    words[1] = partial;
                                }
                            }
                        }
                        __instance.m_input.text = string.Join(" ", words);
                        spawnString = prefix;
                    }
                }
                else
                    commandString = "";
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
                if (text.ToLower().Equals("consoletweaks reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    if (debugEnabled.Value)
                        Player.m_debugMode = true;
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "console tweaks config reloaded" }).GetValue();
                    return false;
                }
                if (text.ToLower().Equals("consoletweaks spawnlist"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    File.WriteAllLines(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "spawnlist.txt"), spawnStrings);
                    Traverse.Create(__instance).Method("AddString", new object[] { "spawn list dumped to BepInEx\\plugins\\ConsoleTweaks\\spawnlist.txt" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}