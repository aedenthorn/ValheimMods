using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace HereFishy
{
    [BepInPlugin("aedenthorn.HereFishy", "Here Fishy", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

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

        private static BepInExPlugin context;
        private static AudioClip fishyClip;
        private static AudioClip weeClip;
        private static Fish currentFish;
        private static Vector3 origPos;
        private static Vector3 flatPos;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
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
        private void Update()
        {
            if (CheckKeyDown(hotKey.Value))
            {
                Dbgl($"pressed hotkey");

                float closest = maxFishyDistance.Value;
                Fish closestFish = null;
                foreach (Collider collider in Physics.OverlapSphere(Player.m_localPlayer.transform.position, maxFishyDistance.Value))
                {
                    Fish fish = collider.transform.parent?.gameObject?.GetComponent<Fish>();
                    if (fish?.GetComponent<ZNetView>()?.IsValid() == true)
                    {
                        //Dbgl($"got fishy at {fish.gameObject.transform.position}");

                        float distance = Vector3.Distance(Player.m_localPlayer.transform.position, fish.gameObject.transform.position);
                        if (distance < closest)
                        {
                            //Dbgl($"closest fishy");
                            closest = distance;
                            closestFish = fish;
                        }
                    }
                }
                if (closestFish != null)
                {
                    Dbgl($"got closest fishy at {closestFish.gameObject.transform.position}");

                    currentFish = closestFish;
                    if (playHereFishy.Value && fishyClip != null)
                    {
                        ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Player.m_localPlayer)).SetTrigger("gpower");

                        AudioSource.PlayClipAtPoint(fishyClip, Player.m_localPlayer.transform.position, Mathf.Clamp(hereFishyVolume.Value, 0.1f, 1f));
                        Invoke("StartJump", fishyClip.length);
                    }
                    else
                    {
                        Invoke("StartJump", 1);
                    }
                }
            }
        }
        private void StartJump()
        {
            Dbgl("starting fish jump");
            if (playWeeee.Value)
            {
                AudioSource audioSource = currentFish.gameObject.AddComponent<AudioSource>();
                audioSource.volume = Mathf.Clamp(weeVolume.Value, 0.1f, 1f);
                audioSource.clip = weeClip;
                audioSource.loop = false;
                audioSource.spatialBlend = 1f;
                audioSource.Play();
            }
            origPos = currentFish.gameObject.transform.position;
            flatPos = origPos;
            context.StartCoroutine(FishJump());
        }

        private static IEnumerator FishJump()
        {
            for (; ; )
            {
                
                flatPos = Vector3.MoveTowards(flatPos, Player.m_localPlayer.transform.position, jumpSpeed.Value);

                Vector3 playerPos = Player.m_localPlayer.transform.position;

                float travelled = Vector3.Distance(flatPos, origPos);
                float total = Vector3.Distance(playerPos, origPos);

                float height = (float)Math.Sin(travelled * Math.PI / total) * jumpHeight.Value;

                try
                {
                    currentFish.gameObject.transform.position = new Vector3(flatPos.x, flatPos.y + height, flatPos.z);
                }
                catch
                {
                    break;
                }

                if (Vector3.Distance(playerPos, currentFish.gameObject.transform.position) < jumpSpeed.Value * 20)
                {
                    
                }

                if (Vector3.Distance(playerPos, currentFish.gameObject.transform.position) < jumpSpeed.Value)
                {
                    Dbgl("taking fish");

                    currentFish.Pickup(Player.m_localPlayer);
                    break;
                }
                yield return null;
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

        public static IEnumerator PreloadClipsCoroutine()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "HereFishy", "herefishy.wav");

            string filename = "file:///" + path.Replace("\\", "/");

            Dbgl($"filename: {filename}");

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


        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("herefishy reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Here Fishy config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}