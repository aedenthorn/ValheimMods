using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TimeMod
{
    [BepInPlugin("aedenthorn.TimeMod", "Time Mod", "0.7.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        public static ConfigEntry<string> m_pauseKey;
        public static ConfigEntry<string> m_speedUpKey;
        public static ConfigEntry<string> m_slowDownKey;
        public static ConfigEntry<string> m_resetKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> pauseOnMenu;
        public static ConfigEntry<bool> stopRenderingOnMenuPause;
        public static ConfigEntry<bool> stopRenderingOnKeyPause;
        public static ConfigEntry<bool> showMessages;
        public static ConfigEntry<bool> enableSpeedChangeStepMult;
        public static ConfigEntry<double> speedChangeStep;
        public static ConfigEntry<int> nexusID;

        public static double lastTime = 1;
        public static bool pausedMenu = false; 
        public static bool wasActive = false; 

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        } 
        private void Awake()
        {
            context = this;
            m_pauseKey = Config.Bind<string>("General", "PauseKey", "pause", "The hotkey to pause the game");
            m_speedUpKey = Config.Bind<string>("General", "SpeedUpKey", "=", "The hotkey to speed up the game time");
            m_slowDownKey = Config.Bind<string>("General", "SlowDownKey", "-", "The hotkey to slow down the game time");
            m_resetKey = Config.Bind<string>("General", "ResetKey", "\\", "The hotkey to reset the game time");
            pauseOnMenu = Config.Bind<bool>("General", "PauseOnMenu", true, "Pause when opening the menu");
            stopRenderingOnMenuPause = Config.Bind<bool>("General", "StopRenderingOnMenuPause", true, "Stop rendering the game scene when you pause via the menu and show a snapshot instead (saves a lot of GPU usage)");
            stopRenderingOnKeyPause = Config.Bind<bool>("General", "StopRenderingOnKeyPause", false, "Stop rendering the game scene when you pause via the pause hotkey and show a snapshot instead (saves a lot of GPU usage)");
            showMessages = Config.Bind<bool>("General", "ShowMessages", false, "Show hud messages on hotkey press");
            speedChangeStep = Config.Bind<double>("General", "SpeedChangeStep", 0.1, "Amount to change the time scale on each increment");
            enableSpeedChangeStepMult = Config.Bind<bool>("General", "EnableSpeedChangeStepMult", true, "Hold down shift to increment x10");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 68, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
            if (ZNetScene.instance == null || Console.IsVisible() || Chat.instance?.HasFocus() == true)
                return;

            string outString = null;
            int mult = enableSpeedChangeStepMult.Value && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? 10 : 1;

            if (m_pauseKey.Value.Length > 0 && CheckKeyDown(m_pauseKey.Value))
            {
                Dbgl($"Pressed pause key, timeScale was {Math.Round(Time.timeScale, 1)}.");
                if (Time.timeScale != 0)
                {
                    if (stopRenderingOnKeyPause.Value)
                    {
                        Traverse.Create(GameCamera.instance).Field("m_mouseCapture").SetValue(false);
                        context.StartCoroutine(SnapPhoto(false));
                    }
                    else
                    {
                        lastTime = Math.Round(Time.timeScale, 1);
                        Time.timeScale = 0;
                    }
                    outString = "You have stopped time.";
                }
                else
                {
                    Traverse.Create(GameCamera.instance).Field("m_mouseCapture").SetValue(true);
                    Destroy(image);
                    image = null;
                    GameCamera.instance.gameObject.GetComponent<Camera>().enabled = true;

                    Time.timeScale = (float)lastTime;
                    outString = "You have allowed the flow of time to resume.";
                }
            }
            else if (m_resetKey.Value.Length > 0 && CheckKeyDown(m_resetKey.Value))
            {
                if (Time.timeScale == 0)
                {
                    Dbgl($"Pressed reset key while paused, lastTime was {Math.Round(lastTime, 1)}.");
                    lastTime = 1;
                    outString = "You have reset the normal speed of time. Time is still paused.";
                }
                else
                {
                    Dbgl($"Pressed reset key, timeScale was {Math.Round(Time.timeScale, 1)}.");
                    Time.timeScale = 1;
                    outString = "You have reset the speed of time to 1.";
                }
            }
            else if (m_speedUpKey.Value.Length > 0 && CheckKeyDown(m_speedUpKey.Value))
            {
                if (Time.timeScale > 0)
                {
                    Time.timeScale += (float)speedChangeStep.Value * mult;
                    Dbgl($"Pressed speedup key, timeScale is now  {Math.Round(Time.timeScale, 1)}.");
                    outString = $"You have increased the speed of time to {Math.Round(Time.timeScale, 1)}.";
                }
                else
                {
                    lastTime += speedChangeStep.Value * mult;
                    Dbgl($"Pressed speedup key, time is paused, unpaused timeScale will be {lastTime}.");
                    //outString = $"Time is stopped, but you have increased the normal speed of time to {lastTime}.";
                }
            }
            else if (m_slowDownKey.Value.Length > 0 && CheckKeyDown(m_slowDownKey.Value)) 
            {
                if (Time.timeScale > 0)
                {
                    if (Time.timeScale > 0.1)
                    {
                        Time.timeScale = Mathf.Max(0.1f, Time.timeScale - (float)speedChangeStep.Value * mult);
                        Dbgl($"Pressed slowdown key, timeScale is now  {Math.Round(Time.timeScale, 1)}.");
                        outString = $"You have decreased the speed of time to {Math.Round(Time.timeScale, 1)}.";
                    }
                    else
                    {
                        Dbgl("Pressed slowdown key, time speed is already as slow as it can go without stopping.");
                        outString = $"Time speed is already as slow as it can go.";
                    }
                }
                else
                {
                    if (lastTime > 0.1)
                    {
                        lastTime = Math.Max(0.1, lastTime - speedChangeStep.Value * mult);
                        Dbgl($"Pressed slowdown key, time is paused, unpaused timeScale will be {lastTime}.");
                        //outString = $"Time is stopped, but you have decreased the normal speed of time to {lastTime}.";
                    }
                    else
                    {
                        Dbgl("Pressed slowdown key, time is paused, ordinary time speed is already as slow as it can go without stopping.");
                        //outString = $"Time speed is already as slow as it can go.";
                    }

                }
            }
            if(outString != null && showMessages.Value)
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, outString, 0, null);

        }

        private bool CheckKeyDown(string value)
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

        private static Image image;

        [HarmonyPatch(typeof(Menu), "Update")]
        static class Menu_Update_Patch
        {

            static void Prefix(Menu __instance)
            {

                if (pauseOnMenu.Value && Time.timeScale > 0 && !(bool)typeof(Game).GetField("m_shuttingDown", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Game.instance) && __instance.m_root.gameObject.activeSelf && !wasActive)
                {
                    Dbgl($"Pausing time on menu open.");

                    if (stopRenderingOnMenuPause.Value)
                    {
                        context.StartCoroutine(SnapPhoto(true));
                    }
                    else
                    {
                        lastTime = Math.Round(Time.timeScale, 1);
                        Time.timeScale = 0;
                        pausedMenu = true;
                    }
                }
                if (pausedMenu && !__instance.m_root.gameObject.activeSelf)
                {
                    Dbgl($"Unpausing time on menu close.");

                    Destroy(image);
                    image = null;
                    GameCamera.instance.gameObject.GetComponent<Camera>().enabled = true;
                    Time.timeScale = (float)lastTime;
                    pausedMenu = false;
                }
                wasActive = __instance.m_root.gameObject.activeSelf;
            }
        }

        private static IEnumerator SnapPhoto(bool menuPause)
        {
            yield return new WaitForEndOfFrame();

            RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            Texture2D background = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);

            foreach (Camera cam in Camera.allCameras)
            {
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
            }

            RenderTexture.active = rt;
            background.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            background.Apply();
            Camera.main.targetTexture = null;
            RenderTexture.active = null;

            yield return 0;
            image = Menu.instance.gameObject.AddComponent<Image>();
            image.sprite = Sprite.Create(background, new Rect(0.0f, 0.0f, background.width, background.height), Vector2.zero, 100.0f);
            GameCamera.instance.gameObject.GetComponent<Camera>().enabled = false;
            Dbgl($"Created fake scene texture.");
            lastTime = Math.Round(Time.timeScale, 1);
            Time.timeScale = 0;
            
            if(menuPause)
                pausedMenu = true;
        }

        
        [HarmonyPatch(typeof(PlayerController), "LateUpdate")]
        static class PlayerController_LateUpdate_Patch
        {
            static bool Prefix()
            {
                return (!modEnabled.Value || Time.timeScale != 0 || !stopRenderingOnKeyPause.Value || Utils.GetMainCamera() != null);
            }
        }
        [HarmonyPatch(typeof(DamageText), "LateUpdate")]
        static class DamageText_LateUpdate_Patch
        {
            static bool Prefix()
            {
                return (!modEnabled.Value || Utils.GetMainCamera() != null);
            }
        }
        [HarmonyPatch(typeof(Chat), "LateUpdate")]
        static class Chat_LateUpdate_Patch
        {
            static bool Prefix()
            {
                return (!modEnabled.Value || Utils.GetMainCamera() != null);
            }
        }

        [HarmonyPatch(typeof(Menu), "OnLogoutYes")]
        static class Menu_OnLogout_Patch
        {
            static void Prefix()
            {
                Time.timeScale = 1;
            }
        }
        [HarmonyPatch(typeof(Menu), "OnQuitYes")]
        static class Menu_OnQuitYes_Patch
        {
            static void Prefix()
            {
                Time.timeScale = 1;
            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
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
                return true;
            }
        }
    }
}
