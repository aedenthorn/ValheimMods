using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CustomTextures
{
    [BepInPlugin("aedenthorn.CustomTextures", "Custom Textures", "2.0.1")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> dumpSceneTextures;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<int> nexusID;

        private static readonly bool isDebug = true;
        private static Stopwatch stopwatch = new Stopwatch();

        public static bool dumpOutput = false;
        public static Dictionary<string, string> customTextures = new Dictionary<string, string>();
        public static Dictionary<string, DateTime> fileWriteTimes = new Dictionary<string, DateTime>();
        public static List<string> texturesToLoad = new List<string>();
        public static List<string> layersToLoad = new List<string>();
        public static Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();
        public static List<string> outputDump = new List<string>();
        public static List<string> logDump = new List<string>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            hotKey = Config.Bind<string>("General", "HotKey", "page down", "Key to reload textures");
            dumpSceneTextures = Config.Bind<bool>("General", "DumpSceneTextures", false, "Dump scene textures to BepInEx/plugins/CustomTextures/scene_dump.txt");
            nexusID = Config.Bind<int>("General", "NexusID", 48, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            dumpOutput = dumpSceneTextures.Value;

            LoadCustomTextures();

            //SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (ZNetScene.instance != null && CheckKeyDown(hotKey.Value))
            {
                Dbgl($"Pressed reload key.");

                ReloadTextures();

            }

        }
        private static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }
        private static void LogStopwatch(string str)
        {
            stopwatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopwatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Dbgl($"{str} RunTime " + elapsedTime);
        }

        private static bool HasCustomTexture(string id)
        {
            return customTextures.ContainsKey(id) || customTextures.Keys.ToList().Exists(p => p.StartsWith(id));
        }
        private static bool ShouldLoadCustomTexture(string id)
        {
            return texturesToLoad.Contains(id) || layersToLoad.Contains(id);
        }
    }
}
