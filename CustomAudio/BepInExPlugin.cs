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
    [BepInPlugin("aedenthorn.CustomAudio", "Custom Audio", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> dumpSFX;
        public static Dictionary<string, AudioClip> customMusic = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> customAmbient = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> customSFX = new Dictionary<string, AudioClip>();
        public static Dictionary<string, List<AudioClip>> customSFXList = new Dictionary<string, List<AudioClip>>();
        public static ConfigEntry<int> nexusID;

        private static string[] audioFiles;
        private static BepInExPlugin instance;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            instance = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            dumpSFX = Config.Bind<bool>("General", "DumpSFX", false, "Dump sound effect names to the console");
            nexusID = Config.Bind<int>("General", "NexusID", 90, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            PreloadAudioClips();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private static void PreloadAudioClips()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomAudio");
            if (!Directory.Exists(path))
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
                return;
            }
            if (Directory.Exists(path))
            {
                audioFiles = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);
                customMusic.Clear();
                customAmbient.Clear();
                foreach (string file in audioFiles)
                {
                    instance.StartCoroutine(PreloadClipCoroutine(file));
                }
            }
        }

        public static IEnumerator PreloadClipCoroutine(string filename)
        {
            filename = "file:///" + filename.Replace("\\", "/");

            //Dbgl($"filename: {filename}");

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filename, AudioType.WAV))
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
                            string name = Path.GetFileNameWithoutExtension(filename);
                            Dbgl($"Adding audio clip {name}");
                            if (name.StartsWith("music_"))
                                customMusic.Add(name.Substring(6), ac);
                            else if(name.StartsWith("ambient_"))
                                customAmbient.Add(name.Substring(8), ac);
                            else if(name.StartsWith("effect_"))
                                customSFX.Add(name.Substring(7), ac);
                            else if (name.StartsWith("list_"))
                            {
                                string[] vars = name.Split('_');
                                string key = string.Join("_", vars.Skip(2));
                                if (!customSFXList.ContainsKey(key))
                                    customSFXList[key] = new List<AudioClip>();
                                customSFXList[key].Add(ac);
                            }
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
                if(dumpSFX.Value)
                    Dbgl($"Checking SFX: { __instance.name}");
                if (customSFXList.ContainsKey(__instance.name.Replace("(Clone)", "")))
                {
                    Dbgl($"replacing SFX list by name: { __instance.name}");
                    __instance.m_audioClips = customSFXList[__instance.name].ToArray();
                    return;
                }
                for (int i = 0; i < __instance.m_audioClips.Length; i++)
                {
                    if (dumpSFX.Value)
                        Dbgl($"checking SFX: { __instance.name}, clip: {__instance.m_audioClips[i].name}");
                    if (customSFX.ContainsKey(__instance.m_audioClips[i].name))
                    {
                        Dbgl($"replacing SFX: { __instance.name}, clip: {__instance.m_audioClips[i].name}");
                        __instance.m_audioClips[i] = customSFX[__instance.m_audioClips[i].name];
                    }
                }
            }
        }
        [HarmonyPatch(typeof(MusicMan), "Awake")]
        static class MusicMan_Awake_Patch
        {
            static void Postfix(MusicMan __instance, List<MusicMan.NamedMusic> ___m_music)
            {
                for (int i = 0; i < ___m_music.Count; i++)
                {
                    for(int j = 0; j < ___m_music[i].m_clips.Length; j++)
                    {
                        if (!___m_music[i].m_clips[j])
                            continue;
                        //Dbgl($"checking music: { ___m_music[i].m_name}, clip: {___m_music[i].m_clips[j].name}");
                        if (customMusic.ContainsKey(___m_music[i].m_clips[j].name))
                        {
                            Dbgl($"replacing music: { ___m_music[i].m_name}, clip: {___m_music[i].m_clips[j].name}");
                            ___m_music[i].m_clips[j] = customMusic[___m_music[i].m_clips[j].name];
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(AudioMan), "Awake")] 
        static class AudioMan_Awake_Patch
        {
            static void Postfix(MusicMan __instance, List<AudioMan.BiomeAmbients> ___m_randomAmbients)
            {
                for(int i = 0; i <___m_randomAmbients.Count; i++)
                {
                    for (int j = 0; j < ___m_randomAmbients[i].m_randomAmbientClips.Count; j++)
                    {
                        //Dbgl($"checking ambient: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClips[j].name}");
                        if (customAmbient.ContainsKey(___m_randomAmbients[i].m_randomAmbientClips[j].name))
                        {
                            Dbgl($"replacing ambient: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClips[j].name}");
                            ___m_randomAmbients[i].m_randomAmbientClips[j] = customMusic[___m_randomAmbients[i].m_randomAmbientClips[j].name];
                        }
                    }
                    for (int j = 0; j < ___m_randomAmbients[i].m_randomAmbientClipsDay.Count; j++)
                    {
                        //Dbgl($"checking ambient day: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsDay[j].name}");
                        if (customAmbient.ContainsKey(___m_randomAmbients[i].m_randomAmbientClipsDay[j].name))
                        {
                            Dbgl($"replacing ambient day: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsDay[j].name}");
                            ___m_randomAmbients[i].m_randomAmbientClipsDay[j] = customMusic[___m_randomAmbients[i].m_randomAmbientClipsDay[j].name];
                        }
                    }
                    for (int j = 0; j < ___m_randomAmbients[i].m_randomAmbientClipsNight.Count; j++)
                    {
                        //Dbgl($"checking ambient night: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsNight[j].name}");
                        if (customAmbient.ContainsKey(___m_randomAmbients[i].m_randomAmbientClipsNight[j].name))
                        {
                            Dbgl($"replacing ambient night: { ___m_randomAmbients[i].m_name}, clip: {___m_randomAmbients[i].m_randomAmbientClipsNight[j].name}");
                            ___m_randomAmbients[i].m_randomAmbientClipsNight[j] = customMusic[___m_randomAmbients[i].m_randomAmbientClipsNight[j].name];
                        }
                    }
                }
            }

        }
    }
}
