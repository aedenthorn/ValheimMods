using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace HereFishy
{
    [BepInPlugin("aedenthorn.HereFishy", "Here Fishy", "0.6.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> playHereFishy;
        public static ConfigEntry<bool> playWeeee;
        public static ConfigEntry<float> hereFishyVolume;
        public static ConfigEntry<float> weeVolume;
        public static ConfigEntry<float> maxFishyDistance;
        public static ConfigEntry<float> jumpSpeed;
        public static ConfigEntry<float> jumpHeight;

        public static BepInExPlugin context;
        public static AudioClip fishyClip;
        public static AudioClip weeClip;
        public static AudioSource fishAudio;
        public static float lastHereFishy;


        public static List<GameObject> hereFishyFishies = new List<GameObject>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 218, "Nexus mod ID for updates");
            
            hotKey = Config.Bind<string>("General", "HotKey", "g", "Heeeeeeeeeeeeeeeeeeeeeeeeeeere Fishy Fishy Fishy key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            maxFishyDistance = Config.Bind<float>("General", "MaxFishyDistance", 100f, "Max distance Heeeeeeeeeeeeeeeeeeeeeeeeeeere Fishy Fishy Fishy can be heard");
            playHereFishy = Config.Bind<bool>("General", "PlayHereFishy", true, "Heeeeeeeeeeeeeeeeeeeeeeeeeeere Fishy Fishy Fishy");
            playWeeee = Config.Bind<bool>("General", "PlayWeeee", true, "Weeeeeeeeeeeeeeeeeeeee");
            hereFishyVolume = Config.Bind<float>("General", "HereFishyVolume", 1f, "Heeeeeeeeeeeeeeeeeeeeeeeeeeere Fishy Fishy Fishy volume");
            weeVolume = Config.Bind<float>("General", "WeeVolume", 1f, "Weeeeeeeeeeeeeeeeeeeee volume");
            jumpSpeed = Config.Bind<float>("General", "JumpSpeed", 0.1f, "Fishy jump speed");
            jumpHeight = Config.Bind<float>("General", "JumpHeight", 6f, "Fishy jump height");

            if (!modEnabled.Value)
                return;
            
            StartCoroutine(PreloadClipsCoroutine());

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }
        public void Update()
        {
            if (!modEnabled.Value || Player.m_localPlayer == null || !Traverse.Create(Player.m_localPlayer).Method("TakeInput").GetValue<bool>())
                return;

            if (AedenthornUtils.CheckKeyDown(hotKey.Value))
            {
                Dbgl($"pressed hotkey");
                lastHereFishy = Time.realtimeSinceStartup;
                ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Player.m_localPlayer)).SetTrigger("gpower");
                if (playHereFishy.Value)
                {
                    Destroy(Player.m_localPlayer.gameObject.GetComponent<AudioSource>());
                    AudioSource playerAudio = Player.m_localPlayer.gameObject.AddComponent<AudioSource>();
                    playerAudio.volume = Mathf.Clamp(hereFishyVolume.Value, 0.1f, 1f);
                    playerAudio.clip = fishyClip;
                    playerAudio.loop = false;
                    playerAudio.spatialBlend = 1f;
                    playerAudio.Play();
                }
                float closest = maxFishyDistance.Value;
                Fish closestFish = null;
                foreach (Fish fish in Fish.Instances)
                {
                    if (Vector3.Distance(Player.m_localPlayer.transform.position, fish.transform.position) < closest)
                    {
                        float distance = Vector3.Distance(Player.m_localPlayer.transform.position, fish.gameObject.transform.position);
                        if (distance < closest && !hereFishyFishies.Contains(fish.gameObject))
                        {
                            //Dbgl($"got closer fishy at {fish.gameObject.transform.position} ({which})");
                            closest = distance;
                            closestFish = fish;
                        }
                    }
                }
                if (closestFish != null)
                {
                    Dbgl($"got closest fishy at {closestFish.gameObject.transform.position}");

                    StartCoroutine(FishJump(closestFish, fishyClip.length));
                }
            }
        }

        public static IEnumerator FishJump(Fish fish, float secs)
        {
            Vector3 origPos = fish.gameObject.transform.position;
            Vector3 flatPos = origPos;

            if (fish == null)
                yield break;

            hereFishyFishies.Add(fish.gameObject);

            yield return new WaitForSeconds(secs);

            Dbgl("starting fish jump");

            if (playWeeee.Value)
            {
                fishAudio = fish.gameObject.AddComponent<AudioSource>();
                fishAudio.volume = Mathf.Clamp(weeVolume.Value, 0.1f, 1f);
                fishAudio.clip = weeClip;
                fishAudio.loop = false;
                fishAudio.spatialBlend = 1f;
                fishAudio.Play();
            }

            for (; ; )
            {
                flatPos = Vector3.MoveTowards(flatPos, Player.m_localPlayer.transform.position, jumpSpeed.Value);

                Vector3 playerPos = Player.m_localPlayer.transform.position;

                float travelled = Vector3.Distance(flatPos, origPos);
                float total = Vector3.Distance(playerPos, origPos);

                float height = (float)Math.Sin(travelled * Math.PI / total) * jumpHeight.Value;

                try
                {
                    fish.gameObject.transform.position = new Vector3(flatPos.x, flatPos.y + height, flatPos.z);
                }
                catch
                {
                    break;
                }

                if (Vector3.Distance(playerPos, fish.gameObject.transform.position) < jumpSpeed.Value * 20)
                {
                    
                }

                if (Vector3.Distance(playerPos, fish.gameObject.transform.position) < jumpSpeed.Value)
                {
                    Dbgl("taking fish");
                    Destroy(fishAudio);
                    hereFishyFishies.Remove(fish.gameObject);
                    fish.Pickup(Player.m_localPlayer);
                    break;
                }
                yield return null;
            }
        }



        public static IEnumerator PreloadClipsCoroutine()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "HereFishy", "herefishy.wav");

            if (!File.Exists(path))
            {
                Dbgl($"file {path} does not exist!");
                yield break;
            }
            string filename = "file:///" + path.Replace("\\", "/");
            Dbgl($"getting audio clip from filename: {filename}");



            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filename, AudioType.WAV))
            {
                www.SendWebRequest();
                yield return null;

                if (www != null)
                {

                    DownloadHandlerAudioClip dac = ((DownloadHandlerAudioClip)www.downloadHandler);
                    if (dac != null)
                    {
                        AudioClip ac = dac.audioClip;
                        if (ac != null)
                        {
                            Dbgl("audio clip is not null. samples: " + ac.samples);
                            fishyClip = ac;
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
            
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "HereFishy", "wee.wav");

            filename = "file:///" + path.Replace("\\", "/");

            Dbgl($"filename: {filename}");

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
                            Dbgl("audio clip is not null. samples: " + ac.samples);
                            weeClip = ac;
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


        [HarmonyPatch(typeof(CharacterAnimEvent), "GPower")]
        public static class CharacterAnimEvent_GPower_Patch
        {
            public static bool Prefix()
            {
                return (fishyClip is null || Time.realtimeSinceStartup - lastHereFishy > fishyClip.length);
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