using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CustomServerLoadingScreen
{
    [BepInPlugin("aedenthorn.CustomServerLoadingScreen", "Custom Server Loading Screen", "0.2.1")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<int> nexusID;
        
        private static ConfigEntry<int> maxWaitTime;

        private static ConfigEntry<string> serverLoadingScreen;
        //private static ConfigEntry<bool> differentSpawnScreen;
        private static ConfigEntry<bool> removeVignette;
        private static ConfigEntry<Color> spawnColorMask;
        private static ConfigEntry<Color> tipTextColor;

        private static string loadingTip = "";
        private static Sprite loadingSprite = null;
        private static bool loadedSprite = true;
        //private static Sprite loadingSprite2 = null;
        private static int secondsWaited = 0;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 553, "Nexus mod ID for updates");
            //differentSpawnScreen = Config.Bind<bool>("General", "DifferentSpawnScreen", true, "Use a different screen for the spawn part");

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

        private static void RPC_ShareLoadingScreen(ZRpc rpc, string screen)
        {
            Dbgl($"RPC_ShareLoadingScreen connected");
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

            if(data == null || screenData.screen == null || screenData.screen.Length == 0)
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

                if (uwr.isNetworkError || uwr.isHttpError )
                {
                    Debug.Log(uwr.error);
                    loadedSprite = true;
                }
                else
                {
                    Dbgl($"Loaded texture from {screenData.screen}!");

                    var tex = DownloadHandlerTexture.GetContent(uwr);
                    loadingSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1);
                    if (false && Hud.instance != null)
                    {
                        Dbgl($"setting sprite to menu background screen");

                        Hud.instance.m_loadingProgress.SetActive(true);
                        Hud.instance.m_loadingScreen.alpha = 1;
                        //Hud.instance.m_loadingImage.sprite = differentSpawnScreen.Value && loadingSprite2 != null ? loadingSprite2 : loadingSprite;
                        Hud.instance.m_loadingImage.sprite = loadingSprite;
                        Hud.instance.m_loadingImage.color = spawnColorMask.Value;
                        if (loadingTip.Any())
                        {
                            Hud.instance.m_loadingTip.text = loadingTip;
                        }
                        Hud.instance.m_loadingTip.color = tipTextColor.Value;

                        if (removeVignette.Value)
                        {
                            Hud.instance.m_loadingProgress.transform.Find("TopFade").gameObject.SetActive(false);
                            Hud.instance.m_loadingProgress.transform.Find("BottomFade").gameObject.SetActive(false);
                            Hud.instance.m_loadingProgress.transform.Find("text_darken").gameObject.SetActive(false);
                        }
                        Traverse.Create(Hud.instance).Field("m_haveSetupLoadScreen").SetValue(true);
                    }
                    loadedSprite = true;
                }
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
        public static class ZNet_RPC_PeerInfo_Patch
        {
            public static bool Prefix(ZNet __instance, ZRpc rpc, ZPackage pkg)
            {
                if (!__instance.IsServer() && !loadedSprite)
                {

                    if(secondsWaited > maxWaitTime.Value)
                    {
                        Dbgl($"Timed out waiting for sprite to load!");
                        return true;
                    }

                    Dbgl($"Waiting for sprite to load, waited {secondsWaited++}");

                    WaitForSpriteLoad(__instance, rpc, pkg);
                    return false;
                }
                return true;
            }
        }

        private static async void WaitForSpriteLoad(ZNet __instance, ZRpc rpc, ZPackage pkg)
        {
            await Task.Delay(1000);
            Traverse.Create(__instance).Method("RPC_PeerInfo", new object[] { rpc, pkg }).GetValue();
        }


        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        public static class ZNet_OnNewConnection_Patch
        {
            public static void Postfix(ZNetPeer peer)
            {
                peer.m_rpc.Register<string>("ShareLoadingScreen", new Action<ZRpc, string>(RPC_ShareLoadingScreen));
            }
        }
        [HarmonyPatch(typeof(ZNet), "RPC_ServerHandshake")]
        public static class ZNet_RPC_ServerHandshake_Patch
        {
            public static void Postfix(ZNet __instance, ZRpc rpc)
            {
                ZNetPeer peer = Traverse.Create(__instance).Method("GetPeer", new object[] { rpc }).GetValue<ZNetPeer>();

                Dbgl($"Sending loading screen {serverLoadingScreen.Value}");


                peer.m_rpc.Invoke("ShareLoadingScreen", new object[]
                {
                    serverLoadingScreen.Value
                });
            }
        }

        [HarmonyPatch(typeof(Hud), "UpdateBlackScreen")]
        public static class UpdateBlackScreen_Patch
        {
            public static void Prefix(Hud __instance, bool ___m_haveSetupLoadScreen, ref bool __state)
            {
                __state = !___m_haveSetupLoadScreen;
            }
            public static void Postfix(Hud __instance, bool ___m_haveSetupLoadScreen, ref bool __state)
            {
                if(__state && ___m_haveSetupLoadScreen && loadingSprite != null)
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
        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("serverloadingscreen reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    //LoadCustomLoadingScreens();
                    //GetRandomLoadingScreen();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Reloaded custom server loading screen" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
