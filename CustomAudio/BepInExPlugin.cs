using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace CustomAudio
{
    [BepInPlugin("aedenthorn.CustomAudio", "Custom Audio", "1.6.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> isDebug;

        
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> dumpInfo;
        public static ConfigEntry<bool> overwriteVol;
        public static ConfigEntry<float> sfxVol;
        public static ConfigEntry<float> musicVol;
        public static ConfigEntry<float> ambientVol;

        public static Dictionary<string, AudioClip> customMusic = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Dictionary<string, AudioClip>> customMusicList = new Dictionary<string, Dictionary<string,AudioClip>>();
        public static Dictionary<string, AudioClip> customAmbient = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Dictionary<string, AudioClip>> customAmbientList = new Dictionary<string, Dictionary<string, AudioClip>>();
        public static Dictionary<string, AudioClip> customSFX = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Dictionary<string, AudioClip>> customSFXList = new Dictionary<string, Dictionary<string, AudioClip>>();
        public static ConfigEntry<int> nexusID;

        public static string lastMusicName = "";
        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Show debug log messages in the console");
            dumpInfo = Config.Bind<bool>("General", "DumpInfo", false, "Dump audio info to the console");
            musicVol = Config.Bind<float>("General", "MusicVol", 0.6f, "Music volume, 0.0 - 1.0");
            //sfxVol = Config.Bind<float>("General", "SfxVol", 1f, "SFX volume");
            ambientVol = Config.Bind<float>("General", "AmbientVol", 0.3f, "Ambient volume, 0.0 - 1.0");
            nexusID = Config.Bind<int>("General", "NexusID", 90, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            PreloadAudioClips();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        public void PreloadAudioClips()
        {
            Dbgl("Preloading audio clips.");

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomAudio");
            if (Directory.Exists(path))
            {
                customMusic.Clear();
                customAmbient.Clear();
                customSFX.Clear();
                customMusicList.Clear();
                customAmbientList.Clear();
                customSFXList.Clear();

                if (Directory.Exists(Path.Combine(path, "Music")))
                {
                    CollectAudioFiles(Path.Combine(path, "Music"), customMusic, customMusicList);
                }
                else 
                {
                    Directory.CreateDirectory(Path.Combine(path, "Music"));
                }
                if (Directory.Exists(Path.Combine(path, "SFX")))
                {
                    CollectAudioFiles(Path.Combine(path, "SFX"), customSFX, customSFXList);
                }
                else 
                {
                    Directory.CreateDirectory(Path.Combine(path, "SFX"));
                }
                if (Directory.Exists(Path.Combine(path, "Ambient")))
                {
                    CollectAudioFiles(Path.Combine(path, "Ambient"), customAmbient, customAmbientList);
                }
                else 
                {
                    Directory.CreateDirectory(Path.Combine(path, "Ambient"));
                }
            }
            else
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(Path.Combine(path, "Ambient"));
                Directory.CreateDirectory(Path.Combine(path, "Music"));
                Directory.CreateDirectory(Path.Combine(path, "SFX"));
            }
        }

        public void CollectAudioFiles(string path, Dictionary<string, AudioClip> customDict, Dictionary<string, Dictionary<string, AudioClip>> customDictDict)
        {
            Dbgl($"checking folder {Path.GetFileName(path)}");
            string[] audioFiles = Directory.GetFiles(path);
            foreach (string file in audioFiles)
            {
                //Dbgl($"\tchecking single file {Path.GetFileName(file)}");

                PreloadClipCoroutine(file, AudioType.UNKNOWN, customDict);
            }
            foreach (string folder in Directory.GetDirectories(path))
            {
                //Dbgl($"\tchecking folder {Path.GetFileName(folder)}");
                string folderName = Path.GetFileName(folder);
                audioFiles = Directory.GetFiles(folder);
                customDictDict[folderName] = new Dictionary<string, AudioClip>();
                foreach (string file in audioFiles)
                {
                    //Dbgl($"\tchecking file {Path.GetFileName(file)}");
                    PreloadClipCoroutine(file, AudioType.UNKNOWN, customDictDict[folderName]);
                }
            }
            foreach (string folder in Directory.GetDirectories(path))
            {
                //Dbgl($"\tchecking folder {Path.GetFileName(folder)}");
                string folderName = Path.GetFileName(folder);
                string[] files = Directory.GetFiles(folder);
                if (files.Length != 1 || !files[0].ToLower().EndsWith(".txt"))
                    continue;
                if (customDictDict.ContainsKey(Path.GetFileNameWithoutExtension(files[0])))
                {
                    Dbgl($"\tlinking music folder {Path.GetFileName(folder)} to folder {Path.GetFileNameWithoutExtension(files[0])}");

                    customDictDict[folderName] = customDictDict[Path.GetFileNameWithoutExtension(files[0])];
                }
            }
        }

        public void PreloadClipCoroutine(string path, AudioType audioType, Dictionary<string, AudioClip> whichDict)
        {
            if (path.EndsWith(".txt") || !path.Contains("."))
                return;

            Dbgl($"path: {path}");
            path = "file:///" + path.Replace("\\", "/");
            /*
            try
            {
                AudioClip ac = WaveLoader.WaveLoader.LoadWaveToAudioClip(File.ReadAllBytes(path));
                string name = Path.GetFileNameWithoutExtension(path);
                whichDict[name] = ac;
                Dbgl($"Added audio clip {name} to dict");
            }
            catch (Exception ex)
            {
                Dbgl($"Exception loading {path}\r\n{ex}");
            }
            */

            try
            {
                var www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
                www.SendWebRequest();
                while (!www.isDone)
                {

                }

                //Dbgl($"checking downloaded {filename}");
                if (www != null)
                {
                    //Dbgl("www not null. errors: " + www.error);
                    DownloadHandlerAudioClip dac = ((DownloadHandlerAudioClip)www.downloadHandler);
                    if (dac != null)
                    {
                        AudioClip ac = dac.audioClip;
                        if (ac != null)
                        {
                            string name = Path.GetFileNameWithoutExtension(path);
                            ac.name = name;
                            if (!whichDict.ContainsKey(name))
                                whichDict[name] = ac;
                            Dbgl($"Added audio clip {name} to dict");
                        }
                        else
                        {
                            Dbgl("audio clip is null. data: " + dac.text);
                        }
                    }
                    else
                    {
                        Dbgl("DownloadHandler is null. bytes downloaded: " + www.downloadedBytes);
                    }
                }
                else
                {
                    Dbgl("www is null " + www.url);
                }
            }
            catch { }
        }
        public static string GetZSFXName(ZSFX zfx)
        {
            string name = zfx.name;
            char[] anyOf = new char[]
            {
            '(',
            ' '
            };
            int num = name.IndexOfAny(anyOf);
            if (num != -1)
            {
                return name.Remove(num);
            }
            return name;
        }
        public static void AddMusicList(EnvMan envMan, int index, string which)
        {
            string name = envMan.m_environments[index].m_name + which;
            Dbgl($"Adding music list by name: {name} ({customMusicList[name].Count})");
            switch (which)
            {
                case "Morning":
                    envMan.m_environments[index].m_musicMorning = name;
                    break;
                case "Day":
                    envMan.m_environments[index].m_musicDay = name;
                    break;
                case "Evening":
                    envMan.m_environments[index].m_musicEvening = name;
                    break;
                case "Night":
                    envMan.m_environments[index].m_musicNight = name;
                    break;
            }
            MusicMan.instance.m_music.Add(new MusicMan.NamedMusic() { m_name = name, m_clips = customMusicList[name].Values.ToArray(), m_loop = true, m_ambientMusic = true, m_resume = true });
        }
        [HarmonyPatch(typeof(ZSFX), "Awake")]
        public static class ZSFX_Awake_Patch
        {
            public static void Postfix(ZSFX __instance)
            {
                if (!modEnabled.Value)
                    return;
                string name = GetZSFXName(__instance);
                if (dumpInfo.Value)
                    Dbgl($"Checking SFX: {name}");
                if (customSFXList?.TryGetValue(name, out var dict) == true)
                {
                    if (dumpInfo.Value)
                        Dbgl($"replacing SFX list by name: {name}");
                    __instance.m_audioClips = dict.Values.ToArray();
                    return;
                }
                if(customSFX != null && __instance.m_audioClips != null)
                {
                    for (int i = 0; i < __instance.m_audioClips.Length; i++)
                    {
                        if (__instance.m_audioClips[i] is null)
                            continue;
                        if (dumpInfo.Value)
                            Dbgl($"checking SFX: {name}, clip: {__instance.m_audioClips[i].name}");
                        if (customSFX.TryGetValue(__instance.m_audioClips[i].name, out var dict2))
                        {
                            if (dumpInfo.Value)
                                Dbgl($"replacing SFX: {name}, clip: {__instance.m_audioClips[i].name}");
                            __instance.m_audioClips[i] = dict2;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MusicMan), "Awake")]
        public static class MusicMan_Awake_Patch
        {
            public static void Postfix(MusicMan __instance)
            {
                if (!modEnabled.Value)
                    return;
                List<string> dump = new List<string>();

                for (int i = 0; i < __instance.m_music.Count; i++)
                {
                    dump.Add($"Music list name: {__instance.m_music[i].m_name}");
                    for (int j = 0; j < __instance.m_music[i].m_clips.Length; j++)
                    {
                        if (!__instance.m_music[i].m_clips[j])
                            continue;
                        dump.Add($"\ttrack name: {__instance.m_music[i].m_clips[j].name}");
                        //Dbgl($"checking music: { __instance.m_music[i].m_name}, clip: {__instance.m_music[i].m_clips[j].name}");
                        if (customMusic.ContainsKey(__instance.m_music[i].m_clips[j].name))
                        {
                            dump.Add($"replacing music: { __instance.m_music[i].m_name}, clip: {__instance.m_music[i].m_clips[j].name}");
                            __instance.m_music[i].m_clips[j] = customMusic[__instance.m_music[i].m_clips[j].name];
                        }
                    }
                    if (customMusicList.ContainsKey(__instance.m_music[i].m_name))
                    {
                        dump.Add($"replacing music list by name: {__instance.m_music[i].m_name}");
                        __instance.m_music[i].m_clips = customMusicList[__instance.m_music[i].m_name].Values.ToArray();
                    }
                }
                if (dumpInfo.Value)
                    Dbgl(string.Join("\n", dump));
            }
        }
        [HarmonyPatch(typeof(AudioMan), "Awake")] 
        public static class AudioMan_Awake_Patch
        {
            public static void Postfix(AudioMan __instance, List<AudioMan.BiomeAmbients> ___m_randomAmbients, AudioSource ___m_oceanAmbientSource, AudioSource ___m_windLoopSource)
            {
                if (!modEnabled.Value || ___m_randomAmbients == null)
                    return;
                List<string> dump = new List<string>();

                for (int i = 0; i < ___m_randomAmbients.Count; i++)
                {
                    dump.Add($"Ambient list name: {___m_randomAmbients[i].m_name}");

                    if (___m_randomAmbients[i].m_randomAmbientClips != null)
                    {
                        dump.Add($"\tAmbient tracks: (use {___m_randomAmbients[i].m_name})");
                        if (customAmbientList.TryGetValue(___m_randomAmbients[i].m_name, out var list))
                        {
                            dump.Add($"replacing ambient list by name: {___m_randomAmbients[i].m_name}");
                            ___m_randomAmbients[i].m_randomAmbientClips = list.Values.ToList();
                        }
                        else
                        {
                            for (int j = 0; j < ___m_randomAmbients[i].m_randomAmbientClips.Count; j++)
                            {
                                if (___m_randomAmbients[i].m_randomAmbientClips[j] == null)
                                    continue;
                                dump.Add($"\t\ttrack name: {___m_randomAmbients[i].m_randomAmbientClips[j].name}");

                                if (customAmbient.TryGetValue(___m_randomAmbients[i].m_randomAmbientClips[j].name, out var clip))
                                {
                                    dump.Add($"replacing ambient: {___m_randomAmbients[i].m_name}, clip: {clip.name}");
                                    ___m_randomAmbients[i].m_randomAmbientClips[j] = customAmbient[clip.name];
                                }
                            }

                        }
                    }
                    if (___m_randomAmbients[i].m_randomAmbientClipsDay != null)
                    {
                        dump.Add($"\tAmbient day tracks: (use {___m_randomAmbients[i].m_name}_day)");
                        if (customAmbientList.TryGetValue(___m_randomAmbients[i].m_name + "_day", out var list))
                        {
                            dump.Add($"replacing ambient day list by name: {___m_randomAmbients[i].m_name}_day");
                            ___m_randomAmbients[i].m_randomAmbientClipsDay = list.Values.ToList();
                        }
                        else
                        {
                            for (int j = 0; j < ___m_randomAmbients[i].m_randomAmbientClipsDay.Count; j++)
                            {
                                if (___m_randomAmbients[i].m_randomAmbientClipsDay[j] == null)
                                    continue;
                                dump.Add($"\t\ttrack name: {___m_randomAmbients[i].m_randomAmbientClipsDay[j].name}");

                                //Dbgl($"checking ambient day: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsDay[j].name}");
                                if (customAmbient.TryGetValue(___m_randomAmbients[i].m_randomAmbientClipsDay[j].name, out var clip))
                                {
                                    dump.Add($"replacing ambient day: {___m_randomAmbients[i].m_name}, clip: {clip.name}");
                                    ___m_randomAmbients[i].m_randomAmbientClipsDay[j] = clip;
                                }
                            }
                        }
                    }

                    if (___m_randomAmbients[i].m_randomAmbientClipsNight != null)
                    {
                        dump.Add($"\tAmbient night tracks: (use {___m_randomAmbients[i].m_name}_night)");
                        if (customAmbientList.TryGetValue(___m_randomAmbients[i].m_name + "_night", out var list))
                        {
                            dump.Add($"replacing ambient night list by name: {___m_randomAmbients[i].m_name + "_night"}");
                            ___m_randomAmbients[i].m_randomAmbientClipsNight = list.Values.ToList();
                        }
                        else
                        {
                            for (int j = 0; j < ___m_randomAmbients[i].m_randomAmbientClipsNight.Count; j++)
                            {
                                if (___m_randomAmbients[i].m_randomAmbientClipsNight[j] == null)
                                    continue;
                                dump.Add($"\t\ttrack name: {___m_randomAmbients[i].m_randomAmbientClipsNight[j].name}");

                                //Dbgl($"checking ambient night: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsNight[j].name}");
                                if (customAmbient.TryGetValue(___m_randomAmbients[i].m_randomAmbientClipsNight[j].name, out var clip))
                                {
                                    dump.Add($"replacing ambient night: {___m_randomAmbients[i].m_name}, clip: {clip.name}");
                                    ___m_randomAmbients[i].m_randomAmbientClipsNight[j] = clip;
                                }
                            }
                        }
                    }
                }
                if (dumpInfo.Value)
                    dump.Add(string.Join("\n", dump));

                if (customAmbient.ContainsKey("ocean") && ___m_oceanAmbientSource != null)
                    ___m_oceanAmbientSource.clip = customAmbient["ocean"];
                if (customAmbient.ContainsKey("wind") && ___m_windLoopSource != null)
                    ___m_windLoopSource.clip = customAmbient["wind"];

            }

        }


        [HarmonyPatch(typeof(MusicMan), "UpdateMusic")]
        public static class UpdateMusic_Patch
        {
            public static MusicMan.NamedMusic lastMusic = null;

            public static void Prefix(ref MusicMan.NamedMusic ___m_currentMusic, ref MusicMan.NamedMusic ___m_queuedMusic, AudioSource ___m_musicSource)
            {
                if (!modEnabled.Value)
                    return;

                if (___m_queuedMusic != null && overwriteVol.Value)
                {
                    ___m_queuedMusic.m_volume = musicVol.Value;
                    Dbgl($"Set MusVol {musicVol.Value}");

                }

                if (___m_musicSource?.clip != null && lastMusicName != ___m_musicSource.clip.name)
                {
                    if(dumpInfo.Value)
                        Dbgl($"Switching music from {lastMusicName} to {___m_musicSource.clip.name}");
                    lastMusicName = ___m_musicSource.clip.name;
                }
                if (___m_queuedMusic == null && !___m_musicSource.isPlaying && PlayerPrefs.GetInt("ContinousMusic", 1) == 1)
                {
                    if (lastMusic != null)
                    {
                        lastMusic.m_lastPlayedTime = 0;
                        ___m_queuedMusic = lastMusic;
                        lastMusic = null;
                    }
                    else if (___m_currentMusic != null)
                        lastMusic = ___m_currentMusic;
                    else
                        lastMusic = null;
                }
                else
                    lastMusic = null;
                if (___m_musicSource.isPlaying)
                {
                    if (___m_queuedMusic != null && ___m_musicSource.loop) {
                        Dbgl($"queued {___m_queuedMusic?.m_name}, setting {___m_musicSource.name} loop to false");
                        ___m_musicSource.loop = false;
                    }
                }
            }
        }

        
        [HarmonyPatch(typeof(AudioMan), "QueueAmbientLoop")]
        public static class QueueAmbientLoop_Patch
        {
            public static void Prefix(ref float ___m_queuedAmbientVol, ref float ___m_ambientVol, ref float vol)
            {
                if (!modEnabled.Value || !overwriteVol.Value)
                    return;
                vol = ambientVol.Value;
                ___m_ambientVol = ambientVol.Value;
                ___m_queuedAmbientVol = ambientVol.Value;
                Dbgl($"Set AmbVol {ambientVol.Value}");
            }
        }

        [HarmonyPatch(typeof(EnvMan), "Awake")]
        public static class EnvMan_Awake_Patch
        {
            public static void Postfix(EnvMan __instance)
            {
                if (!modEnabled.Value)
                    return;

                for(int i = 0; i < __instance.m_environments.Count; i++)
                {
                    if (customMusicList.ContainsKey(__instance.m_environments[i].m_name + "Morning"))
                        AddMusicList(__instance, i, "Morning");
                    if (customMusicList.ContainsKey(__instance.m_environments[i].m_name + "Day"))
                        AddMusicList(__instance, i, "Day");
                    if (customMusicList.ContainsKey(__instance.m_environments[i].m_name + "Evening"))
                        AddMusicList(__instance, i, "Evening");
                    if (customMusicList.ContainsKey(__instance.m_environments[i].m_name + "Night"))
                        AddMusicList(__instance, i, "Night");
                }
            }
        }
        
        [HarmonyPatch(typeof(TeleportWorld), "Awake")]
        public static class TeleportWorld_Awake_Patch
        {
            public static void Postfix(TeleportWorld __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (customSFX.ContainsKey("portal"))
                {
                    AudioSource source = __instance.GetComponentInChildren<AudioSource>();
                    source.clip = customSFX["portal"];
                    source.gameObject.SetActive(false);
                    source.gameObject.SetActive(true);

                }
            }
        }
        
        [HarmonyPatch(typeof(Fireplace), "Start")]
        public static class Fireplace_Start_Patch
        {
            public static void Postfix(Fireplace __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (__instance.name.Contains("groundtorch") && customSFX.ContainsKey("groundtorch"))
                {
                    Dbgl("Replacing ground torch audio");
                    __instance.m_enabledObject.GetComponentInChildren<AudioSource>().clip = customSFX["groundtorch"];
                }
                else if(__instance.name.Contains("walltorch") && customSFX.ContainsKey("walltorch"))
                {
                    Dbgl("Replacing walltorch audio");
                    if(__instance.m_enabledObjectHigh?.GetComponentInChildren<AudioSource>())
                        __instance.m_enabledObjectHigh.GetComponentInChildren<AudioSource>().clip = customSFX["walltorch"];
                    else if(__instance.m_enabledObject?.GetComponentInChildren<AudioSource>())
                        __instance.m_enabledObject.GetComponentInChildren<AudioSource>().clip = customSFX["walltorch"];
                }
                else if (__instance.name.Contains("fire_pit") && customSFX.ContainsKey("fire_pit"))
                {
                    Dbgl("Replacing fire_pit audio");
                    __instance.m_enabledObjectHigh.GetComponentInChildren<AudioSource>().clip = customSFX["fire_pit"];
                }
                else if (__instance.name.Contains("bonfire") && customSFX.ContainsKey("bonfire"))
                {
                    Dbgl("Replacing bonfire audio");
                    __instance.m_enabledObjectHigh.GetComponentInChildren<AudioSource>().clip = customSFX["bonfire"];
                }
                else if (__instance.name.Contains("hearth") && customSFX.ContainsKey("hearth"))
                {
                    Dbgl("Replacing hearth audio");
                    __instance.m_enabledObjectHigh.GetComponentInChildren<AudioSource>().clip = customSFX["hearth"];
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
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} music"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    if (EnvMan.instance)
                    {
                        string name;
                        if (Player.m_localPlayer && Player.m_localPlayer.IsSafeInHome())
                        {
                            name = "home";
                        }
                        else
                        {
                            name = EnvMan.instance.GetAmbientMusic();
                        }
                        Dbgl("Current environment: " + EnvMan.instance.GetCurrentEnvironment().m_name);
                        Dbgl("Current music list: " + name + " " + MusicMan.instance.m_music.FirstOrDefault(m => m.m_name == name)?.m_clips.Length);
                    }
                    Dbgl("Vanilla music list names:\n"+string.Join("\n", MusicMan.instance.m_music.Select(m => m.m_name)));
                    if (EnvMan.instance)
                    {

                        List<string> env = new List<string>();
                        for (int i = 0; i < EnvMan.instance.m_environments.Count; i++)
                        {
                            env.Add(EnvMan.instance.m_environments[i].m_name + "Morning");
                            env.Add(EnvMan.instance.m_environments[i].m_name + "Day");
                            env.Add(EnvMan.instance.m_environments[i].m_name + "Evening");
                            env.Add(EnvMan.instance.m_environments[i].m_name + "Night");
                        }
                        Dbgl("Possible music list names:\n" + string.Join("\n", env));
                    }
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped music names" }).GetValue();
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} env"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    if (!EnvMan.instance)
                        Traverse.Create(__instance).Method("AddString", new object[] { "Must be called in-game" }).GetValue();
                    Dbgl("Current environment: " + EnvMan.instance.GetCurrentEnvironment().m_name);
                    return false;
                }
                return true;
            }
        }
    }
    public class UnityWebRequestAwaiter : INotifyCompletion
    {
        public UnityWebRequestAsyncOperation asyncOp;
        public Action continuation;

        public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp)
        {
            this.asyncOp = asyncOp;
            asyncOp.completed += OnRequestCompleted;
            BepInExPlugin.Dbgl("created asyncop");
        }

        public bool IsCompleted 
        { 
            get {
                BepInExPlugin.Dbgl("Is completed get");
                return asyncOp.isDone; 
            } 
        }

        public void GetResult()
        {
            BepInExPlugin.Dbgl("Get Result");
        }

        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;

            BepInExPlugin.Dbgl("on completed");
        }

        public void OnRequestCompleted(AsyncOperation obj)
        {
            continuation();
            BepInExPlugin.Dbgl("on request completed");
        }
    }

    public static class ExtensionMethods
    {
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            return new UnityWebRequestAwaiter(asyncOp);
        }
    }
}
