using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CustomServerLoadingScreen
{
    [BepInPlugin("aedenthorn.CustomServerLoadingScreen", "Custom Server Loading Screen", "0.4.0")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<int> maxWaitTime;

        public static ConfigEntry<string> serverLoadingScreen;
        //public static ConfigEntry<bool> differentSpawnScreen;
        public static ConfigEntry<bool> removeVignette;
        public static ConfigEntry<Color> spawnColorMask;
        public static ConfigEntry<Color> tipTextColor;

        public static string loadingTip = "";
        public static Sprite loadingSprite = null;
        public static bool loadedSprite = true;
        //public static Sprite loadingSprite2 = null;
        public static int secondsWaited = 0;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 553, "Nexus mod ID for updates");

            serverLoadingScreen = Config.Bind<string>("General", "ServerLoadingScreen", "https://i.imgur.com/9WlYUlb.png^This is the MOTD!", "Custom loading screen URL and replacement text separated by a caret ^ (server only)");

            maxWaitTime = Config.Bind<int>("General", "MaxWaitTime", 20, "Maximum number of seconds to wait to load the URL (client only)");
            spawnColorMask = Config.Bind<Color>("General", "SpawnColorMask", Color.white, "Change the color mask of the spawn screen  (client only)");
            removeVignette = Config.Bind<bool>("General", "RemoveMask", true, "Remove dark edges for the spawn part (client only)");
            tipTextColor = Config.Bind<Color>("General", "TipTextColor", Color.white, "Custom tip text color (client only)");

            loadedSprite = true;

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }



        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        public static class ZNet_OnNewConnection_Patch
        {
            public static void Postfix(ZNet __instance, ZNetPeer peer)
            {
                Dbgl("New connection");

                peer.m_rpc.Register<string>("ShareLoadingScreen", new Action<ZRpc, string>(RPC_ShareLoadingScreen));
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_ServerHandshake")]
        public static class ZNet_RPC_ServerHandshake_Patch
        {
            public static void Prefix(ZNet __instance, ZRpc rpc)
            {
                ZNetPeer peer = Traverse.Create(__instance).Method("GetPeer", new object[] { rpc }).GetValue<ZNetPeer>();

                Dbgl("Server Handshake");

                if (peer == null)
                {
                    Dbgl("peer is null");
                    return;
                }

                Dbgl($"Sending loading screen {serverLoadingScreen.Value}");

                peer.m_rpc.Invoke("ShareLoadingScreen", new object[]
                {
                    serverLoadingScreen.Value
                });
            }
        }


        public static void RPC_ShareLoadingScreen(ZRpc rpc, string screen)
        {
            Dbgl($"RPC_ShareLoadingScreen received");
            if (!ZNet.instance.IsServer())
            {
                secondsWaited = 0;
                context.StartCoroutine(LoadLoadingScreen(screen));
            }
        }
        public static IEnumerator LoadLoadingScreen(string data)
        {
            LoadingScreenData screenData = new LoadingScreenData(data);
            Dbgl($"data: {data}\n\tpath: {screenData.screen}\n\ttip: {screenData.tip}");

            if (data == null || screenData.screen == null || screenData.screen.Length == 0)
            {
                Dbgl("malformed data");
                loadedSprite = true;
                yield break;
            }
            loadedSprite = false;

            loadingTip = screenData.tip;

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(screenData.screen))
            {
                yield return uwr.SendWebRequest();

                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    Dbgl(uwr.error);
                    loadedSprite = true;
                }
                else
                {
                    Dbgl($"Loaded texture from {screenData.screen}!");

                    var tex = DownloadHandlerTexture.GetContent(uwr);
                    loadingSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1);
                    if (Hud.instance != null)
                    {
                        Dbgl($"setting sprite to menu background screen");

                        Image image = Instantiate(Hud.instance.transform.Find("LoadingBlack").Find("Bkg").GetComponent<Image>(), Hud.instance.transform.Find("LoadingBlack").transform);
                        if (image == null)
                        {
                            Dbgl($"missed bkg");
                            yield break;
                        }
                        Dbgl($"setting sprite to loading screen");

                        image.sprite = loadingSprite;
                        image.color = spawnColorMask.Value;
                        image.type = Image.Type.Simple;
                        image.preserveAspect = true;

                        if (loadingTip.Length > 0)
                        {
                            Instantiate(Hud.instance.m_loadingTip.transform.parent.Find("panel_separator"), Hud.instance.transform.Find("LoadingBlack").transform);
                            TMP_Text text = Instantiate(Hud.instance.m_loadingTip.gameObject, Hud.instance.transform.Find("LoadingBlack").transform).GetComponent<TMP_Text>();
                            if (text != null)
                            {
                                text.text = loadingTip;
                                text.color = tipTextColor.Value;
                            }
                        }
                    }
                    loadedSprite = true;
                }
            }
        }


        //[HarmonyPriority(Priority.First)]
        //[HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake")]
        public static class ZNet_RPC_ClientHandshake_Patch
        {
            public static bool Prefix(ZNet __instance, ZRpc rpc, bool needPassword)
            {
                Dbgl("RPC_ClientHandshake");
                if (!__instance.IsServer() && !loadedSprite)
                {

                    if(secondsWaited > maxWaitTime.Value)
                    {
                        Dbgl($"Timed out waiting for sprite to load!");
                        return true;
                    }

                    Dbgl($"Waiting for sprite to load, waited {secondsWaited++}");

                    WaitForSpriteLoad(__instance, rpc, needPassword);
                    return false;
                }

                if (!__instance.IsServer())
                {

                }

                return true;
            }
        }

        public static async void WaitForSpriteLoad(ZNet __instance, ZRpc rpc, bool needPassword)
        {
            await Task.Delay(1000);
            Traverse.Create(__instance).Method("RPC_ClientHandshake", new object[] { rpc, needPassword }).GetValue();
        }


        [HarmonyPatch(typeof(Hud), "UpdateBlackScreen")]
        public static class UpdateBlackScreen_Patch
        {
            public static bool Prefix(Hud __instance, bool ___m_haveSetupLoadScreen, ref bool __state)
            {
                if (!modEnabled.Value)
                    return true;

                __state = ___m_haveSetupLoadScreen;
                return true;
            }
            public static void Postfix(Hud __instance, bool ___m_haveSetupLoadScreen, ref bool __state)
            {
                if (!modEnabled.Value)
                    return;

                if (!__state && ___m_haveSetupLoadScreen && loadingSprite != null)
                {
                    Dbgl($"UpdateBlackScreen setting sprite to loading screen");

                    __instance.m_loadingImage.sprite = loadingSprite;
                    __instance.m_loadingImage.color = spawnColorMask.Value;

                    if (loadingTip.Length > 0)
                    {
                        __instance.m_loadingTip.text = loadingTip;
                    }
                    __instance.m_loadingTip.color = tipTextColor.Value;
                    
                    if (removeVignette.Value)
                    {
                        __instance.m_loadingProgress.transform.Find("TopFade").gameObject.SetActive(false);
                        __instance.m_loadingProgress.transform.Find("BottomFade").gameObject.SetActive(false);
                        __instance.m_loadingProgress.transform.Find("text_darken").gameObject.SetActive(false);
                    }
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

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
