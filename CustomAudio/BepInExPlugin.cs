using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace CustomAudio
{
    [BepInPlugin("aedenthorn.CustomAudio", "Custom Audio", "0.9.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> dumpInfo;
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

        private static string lastMusicName = "";
        private static string[] audioFiles;
        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Show debug log messages in the console");
            dumpInfo = Config.Bind<bool>("General", "DumpInfo", true, "Dump audio info to the console");
            musicVol = Config.Bind<float>("General", "MusicVol", 0.6f, "Music volume, 0.0 - 1.0");
            //sfxVol = Config.Bind<float>("General", "SfxVol", 1f, "SFX volume");
            ambientVol = Config.Bind<float>("General", "AmbientVol", 0.3f, "Ambient volume");
            nexusID = Config.Bind<int>("General", "NexusID", 90, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            PreloadAudioClips();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private static void PreloadAudioClips()
        {
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

        private static void CollectAudioFiles(string path, Dictionary<string, AudioClip> customDict, Dictionary<string, Dictionary<string, AudioClip>> customDictDict)
        {
            Dbgl($"checking folder {Path.GetFileName(path)}");
            audioFiles = Directory.GetFiles(path);
            foreach (string file in audioFiles)
            {
                Dbgl($"\tchecking single file {Path.GetFileName(file)}");

                if (Path.GetExtension(file).ToLower().Equals(".ogg"))
                    context.StartCoroutine(PreloadClipCoroutine(file, AudioType.OGGVORBIS, customDict));
                else if(Path.GetExtension(file).ToLower().Equals(".wav"))
                    context.StartCoroutine(PreloadClipCoroutine(file, AudioType.WAV, customDict));
            }
            foreach(string folder in Directory.GetDirectories(path))
            {
                Dbgl($"\tchecking folder {Path.GetFileName(folder)}");
                string folderName = Path.GetFileName(folder);
                audioFiles = Directory.GetFiles(folder);
                customDictDict[folderName] = new Dictionary<string, AudioClip>();
                foreach (string file in audioFiles)
                {
                    Dbgl($"\tchecking file {Path.GetFileName(file)}");
                    if (Path.GetExtension(file).ToLower().Equals(".ogg"))
                        context.StartCoroutine(PreloadClipCoroutine(file, AudioType.OGGVORBIS, customDictDict[folderName]));
                    else if (Path.GetExtension(file).ToLower().Equals(".wav"))
                        context.StartCoroutine(PreloadClipCoroutine(file, AudioType.WAV, customDictDict[folderName]));
                }
            }
        }

        public static IEnumerator PreloadClipCoroutine(string path, AudioType audioType, Dictionary<string, AudioClip> whichDict)
        {
            Dbgl($"\t\tpath: {path}");
            
            path = "file:///" + path.Replace("\\", "/");


            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, audioType))
            {
                www.SendWebRequest();
                yield return null;
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
        }

        [HarmonyPatch(typeof(ZSFX), "Awake")]
        static class ZSFX_Awake_Patch
        {
            static void Postfix(ZSFX __instance)
            {
                string name = GetZSFXName(__instance);
                if (dumpInfo.Value)
                    Dbgl($"Checking SFX: {name}");
                if (customSFXList.ContainsKey(name))
                {
                    if (dumpInfo.Value)
                        Dbgl($"replacing SFX list by name: {name}");
                    __instance.m_audioClips = customSFXList[name].Values.ToArray();
                    return;
                }
                for (int i = 0; i < __instance.m_audioClips.Length; i++)
                {
                    if (dumpInfo.Value)
                        Dbgl($"checking SFX: {name}, clip: {__instance.m_audioClips[i].name}");
                    if (customSFX.ContainsKey(__instance.m_audioClips[i].name))
                    {
                        if (dumpInfo.Value)
                            Dbgl($"replacing SFX: {name}, clip: {__instance.m_audioClips[i].name}");
                        __instance.m_audioClips[i] = customSFX[__instance.m_audioClips[i].name];
                    }
                }
            }
        }
        [HarmonyPatch(typeof(MusicMan), "Awake")]
        static class MusicMan_Awake_Patch
        {
            static void Postfix(MusicMan __instance, List<MusicMan.NamedMusic> ___m_music, ref float ___m_musicVolume)
            {
                List<string> dump = new List<string>();

                for (int i = 0; i < ___m_music.Count; i++)
                {
                    dump.Add($"Music list name: {___m_music[i].m_name}");
                    for (int j = 0; j < ___m_music[i].m_clips.Length; j++)
                    {
                        if (!___m_music[i].m_clips[j])
                            continue;
                        dump.Add($"\ttrack name: {___m_music[i].m_clips[j].name}");
                        //Dbgl($"checking music: { ___m_music[i].m_name}, clip: {___m_music[i].m_clips[j].name}");
                        if (customMusic.ContainsKey(___m_music[i].m_clips[j].name))
                        {
                            Dbgl($"replacing music: { ___m_music[i].m_name}, clip: {___m_music[i].m_clips[j].name}");
                            ___m_music[i].m_clips[j] = customMusic[___m_music[i].m_clips[j].name];
                        }
                    }
                    if (customMusicList.ContainsKey(___m_music[i].m_name))
                    {
                        Dbgl($"replacing music list by name: {___m_music[i].m_name}");
                        ___m_music[i].m_clips = customMusicList[___m_music[i].m_name].Values.ToArray();
                    }
                }
                if (dumpInfo.Value)
                    Dbgl(string.Join("\n", dump));
            }
        }
        [HarmonyPatch(typeof(AudioMan), "Awake")] 
        static class AudioMan_Awake_Patch
        {
            static void Postfix(AudioMan __instance, List<AudioMan.BiomeAmbients> ___m_randomAmbients, AudioSource ___m_oceanAmbientSource, AudioSource ___m_windLoopSource)
            {
                List<string> dump = new List<string>();

                for (int i = 0; i <___m_randomAmbients.Count; i++)
                {
                    dump.Add($"Ambient list name: {___m_randomAmbients[i].m_name}");

                    dump.Add($"\tAmbient tracks: (use {___m_randomAmbients[i].m_name})");
                    for (int j = 0; j < ___m_randomAmbients[i].m_randomAmbientClips.Count; j++)
                    {
                        dump.Add($"\t\ttrack name: {___m_randomAmbients[i].m_randomAmbientClips[j].name}");

                        //Dbgl($"checking ambient: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClips[j].name}");
                        if (customAmbient.ContainsKey(___m_randomAmbients[i].m_randomAmbientClips[j].name))
                        {
                            Dbgl($"replacing ambient: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClips[j].name}");
                            ___m_randomAmbients[i].m_randomAmbientClips[j] = customAmbient[___m_randomAmbients[i].m_randomAmbientClips[j].name];
                        }
                    }
                    dump.Add($"\tAmbient day tracks: (use {___m_randomAmbients[i].m_name}_day)");
                    for (int j = 0; j < ___m_randomAmbients[i].m_randomAmbientClipsDay.Count; j++)
                    {
                        dump.Add($"\t\ttrack name: {___m_randomAmbients[i].m_randomAmbientClipsDay[j].name}");

                        //Dbgl($"checking ambient day: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsDay[j].name}");
                        if (customAmbient.ContainsKey(___m_randomAmbients[i].m_randomAmbientClipsDay[j].name))
                        {
                            Dbgl($"replacing ambient day: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsDay[j].name}");
                            ___m_randomAmbients[i].m_randomAmbientClipsDay[j] = customAmbient[___m_randomAmbients[i].m_randomAmbientClipsDay[j].name];
                        }
                    }
                    dump.Add($"\tAmbient night tracks: (use {___m_randomAmbients[i].m_name}_night)");
                    for (int j = 0; j < ___m_randomAmbients[i].m_randomAmbientClipsNight.Count; j++)
                    {
                        dump.Add($"\t\ttrack name: {___m_randomAmbients[i].m_randomAmbientClipsNight[j].name}");

                        //Dbgl($"checking ambient night: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsNight[j].name}");
                        if (customAmbient.ContainsKey(___m_randomAmbients[i].m_randomAmbientClipsNight[j].name))
                        {
                            Dbgl($"replacing ambient night: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsNight[j].name}");
                            ___m_randomAmbients[i].m_randomAmbientClipsNight[j] = customAmbient[___m_randomAmbients[i].m_randomAmbientClipsNight[j].name];
                        }
                    }
                    if (customAmbientList.ContainsKey(___m_randomAmbients[i].m_name + "_day"))
                    {
                        Dbgl($"replacing ambient day list by name: {___m_randomAmbients[i].m_name}");
                        ___m_randomAmbients[i].m_randomAmbientClipsDay = new List<AudioClip>(customAmbientList[___m_randomAmbients[i].m_name].Values.ToList());
                    }
                    else if (customAmbientList.ContainsKey(___m_randomAmbients[i].m_name + "_night"))
                    {
                        Dbgl($"replacing ambient night list by name: {___m_randomAmbients[i].m_name}");
                        ___m_randomAmbients[i].m_randomAmbientClipsNight = new List<AudioClip>(customAmbientList[___m_randomAmbients[i].m_name].Values.ToList());
                    }
                    else if (customAmbientList.ContainsKey(___m_randomAmbients[i].m_name))
                    {
                        Dbgl($"replacing ambient list by name: {___m_randomAmbients[i].m_name}");
                        ___m_randomAmbients[i].m_randomAmbientClips = new List<AudioClip>(customAmbientList[___m_randomAmbients[i].m_name].Values.ToList());
                    }
                }
                if (dumpInfo.Value)
                    Dbgl(string.Join("\n", dump));

                if (customAmbient.ContainsKey("ocean"))
                    ___m_oceanAmbientSource.clip = customAmbient["ocean"];
                if (customAmbient.ContainsKey("wind"))
                    ___m_windLoopSource.clip = customAmbient["wind"];

            }

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

        [HarmonyPatch(typeof(MusicMan), "UpdateMusic")]
        static class UpdateMusic_Patch
        {
            private static MusicMan.NamedMusic lastMusic = null;

            static void Prefix(ref MusicMan.NamedMusic ___m_currentMusic, ref MusicMan.NamedMusic ___m_queuedMusic, AudioSource ___m_musicSource)
            {

                if(___m_queuedMusic != null)
                {
                    ___m_queuedMusic.m_volume = musicVol.Value;

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
                    if(___m_musicSource.loop)
                        Dbgl($"queued {___m_queuedMusic?.m_name}, setting {___m_musicSource.name} loop to false");
                    ___m_musicSource.loop = false;
                }
            }
        }

        
        [HarmonyPatch(typeof(AudioMan), "QueueAmbientLoop")]
        static class QueueAmbientLoop_Patch
        {
            static void Prefix(ref float ___m_queuedAmbientVol, ref float ___m_ambientVol, ref float vol)
            {
                vol = ambientVol.Value;
                ___m_ambientVol = ambientVol.Value;
                ___m_queuedAmbientVol = ambientVol.Value;
            }
        }

        [HarmonyPatch(typeof(Fireplace), "Start")]
        static class Fireplace_Start_Patch
        {
            static void Postfix(Fireplace __instance)
            {
                if (__instance.name.Contains("groundtorch") && customSFX.ContainsKey("groundtorch"))
                {
                    Dbgl("Replacing ground torch audio");
                    __instance.m_enabledObjectHigh.GetComponentInChildren<AudioSource>().clip = customSFX["groundtorch"];
                }
                else if(__instance.name.Contains("walltorch") && customSFX.ContainsKey("walltorch"))
                {
                    Dbgl("Replacing walltorch audio");
                    __instance.m_enabledObjectHigh.GetComponentInChildren<AudioSource>().clip = customSFX["walltorch"];
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

        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("customaudio reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    PreloadAudioClips();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Reloaded custom audio mod" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
