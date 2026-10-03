using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CustomTextures
{
    [BepInPlugin("aedenthorn.CustomTextures", "Custom Textures", "3.4.5")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> dumpSceneTextures;
        public static ConfigEntry<bool> replaceLocationTextures;
        public static ConfigEntry<bool> reloadLocationTextures;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<float> emissionIntensity;

        public static readonly Regex CreatureLevelSuffixRegex = new Regex(@"^_creaturelevel\d+$", RegexOptions.Compiled);

        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public static Stopwatch stopwatch = new Stopwatch();

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
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            hotKey = Config.Bind<string>("General", "HotKey", "page down", "Key to reload textures");
            replaceLocationTextures = Config.Bind<bool>("General", "ReplaceLocationTextures", true, "Replace textures for special locations (can take a long time)");
            reloadLocationTextures = Config.Bind<bool>("General", "ReloadLocationTextures", false, "Reload textures for special locations on manual reload (can take a long time)");
            dumpSceneTextures = Config.Bind<bool>("General", "DumpSceneTextures", false, "Dump scene textures to BepInEx/plugins/CustomTextures/scene_dump.txt");
            emissionIntensity = Config.Bind<float>("General", "EmissionIntensity", 1f, "Multiplier for _EmissionColor when a custom emission map is applied");
            nexusID = Config.Bind<int>("General", "NexusID", 2796, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            LoadCustomTextures();

            //SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public void Update()
        {
            if (ZNetScene.instance != null && CheckKeyDown(hotKey.Value))
            {
                Dbgl($"Pressed reload key.");

                ReloadTextures(reloadLocationTextures.Value && replaceLocationTextures.Value);
            }
        }
        public static bool CheckKeyDown(string value)
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
        public static void LogStopwatch(string str)
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

        public static bool HasCustomTexture(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            if (customTextures.ContainsKey(id))
                return true;
            string prefix = id + "_";
            return customTextures.Keys.Any(p => p.StartsWith(prefix));
        }
        public static bool ShouldLoadCustomTexture(string id)
        {
            return HasCustomTexture(id) || texturesToLoad.Contains(id) || layersToLoad.Contains(id);
        }
        public static bool IsCreatureLevelVariantKey(string key, string id)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(id) || key.Length <= id.Length || !key.StartsWith(id))
                return false;
            return CreatureLevelSuffixRegex.IsMatch(key.Substring(id.Length));
        }
        public static bool HasLoadableCustomTexture(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            if (customTextures.ContainsKey(id))
                return true;
            string prefix = id + "_";
            return customTextures.Keys.Any(p => p.StartsWith(prefix) && !IsCreatureLevelVariantKey(p, id));
        }
        public static bool ShouldApplyCustomTexture(string id, int? creatureLevel)
        {
            if (HasLoadableCustomTexture(id))
                return true;
            if (creatureLevel.HasValue && creatureLevel.Value > 0 && customTextures.ContainsKey(id + "_creaturelevel" + creatureLevel.Value))
                return true;
            return false;
        }
        public static string ResolveTextureId(string id, int? creatureLevel)
        {
            if (creatureLevel.HasValue && creatureLevel.Value > 0)
            {
                string leveled = id + "_creaturelevel" + creatureLevel.Value;
                if (customTextures.ContainsKey(leveled))
                    return leveled;
            }
            return id;
        }
        public static int? GetLiveCreatureLevel(GameObject go)
        {
            if (go == null)
                return null;
            Character character = go.GetComponent<Character>();
            if (character == null)
                character = go.GetComponentInParent<Character>();
            if (character == null || character is Player)
                return null;
            ZNetView nview = character.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return null;
            return character.GetLevel();
        }
        public static void ApplyCharacterTextures(Character character)
        {
            ApplyCharacterTextures(character, false);
        }
        public static void ApplyCharacterTextures(Character character, bool delayed)
        {
            if (!modEnabled.Value || character == null || character is Player)
                return;
            ZNetView nview = character.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                if (!delayed && character.isActiveAndEnabled)
                    character.StartCoroutine(ApplyCharacterTexturesDelayed(character));
                return;
            }
            ReplaceOneGameObjectTextures(character.gameObject, character.gameObject.name, "object", character.GetLevel(), true);
        }
        public static IEnumerator ApplyCharacterTexturesDelayed(Character character)
        {
            yield return null;
            ApplyCharacterTextures(character, true);
        }
        public static void TryReloadStage(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Dbgl($"Error during {name}: {ex}");
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

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
