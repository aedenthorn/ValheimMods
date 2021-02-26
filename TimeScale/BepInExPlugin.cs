using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace TimeMod
{
    [BepInPlugin("aedenthorn.TimeScale", "Time Scale", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<string> m_pauseKey;
        public static ConfigEntry<string> m_speedUpKey;
        public static ConfigEntry<string> m_slowDownKey;
        public static ConfigEntry<string> m_resetKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> pauseOnMenu;
        public static ConfigEntry<bool> showMessages;
        public static ConfigEntry<bool> enableSpeedChangeStepMult;
        public static ConfigEntry<double> speedChangeStep;
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
            m_pauseKey = Config.Bind<string>("General", "PauseKey", "pause", "The hotkey to pause the game");
            m_speedUpKey = Config.Bind<string>("General", "SpeedUpKey", "=", "The hotkey to speed up the game time");
            m_slowDownKey = Config.Bind<string>("General", "SlowDownKey", "-", "The hotkey to slow down the game time");
            m_resetKey = Config.Bind<string>("General", "ResetKey", "\\", "The hotkey to reset the game time");
            pauseOnMenu = Config.Bind<bool>("General", "PauseOnMenu", true, "Pause when opening the menu");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            showMessages = Config.Bind<bool>("General", "ShowMessages", false, "Show hud messages on hotkey press");
            speedChangeStep = Config.Bind<double>("General", "SpeedChangeStep", 0.1, "Amount to change the time scale on each increment");
            enableSpeedChangeStepMult = Config.Bind<bool>("General", "EnableSpeedChangeStepMult", true, "Hold down shift to increment x10");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
            if (ZNetScene.instance == null)
                return;

            string outString = null;
            int mult = enableSpeedChangeStepMult.Value && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? 10 : 1;

            if (m_pauseKey.Value.Length > 0 && Input.GetKeyDown(m_pauseKey.Value))
            {
                Dbgl($"Pressed pause key, timeScale was {Math.Round(Time.timeScale, 1)}.");
                if (Time.timeScale != 0)
                {
                    lastTime = Math.Round(Time.timeScale, 1);
                    Time.timeScale = 0;
                    outString = "You have stopped time.";
                }
                else
                {
                    Time.timeScale = (float)lastTime;
                    outString = "You have allowed the flow of time to resume.";
                }
            }
            else if (m_resetKey.Value.Length > 0 && Input.GetKeyDown(m_resetKey.Value))
            {
                Dbgl($"Pressed reset key, timeScale was {Math.Round(Time.timeScale, 1)}.");
                Time.timeScale = 1;
                outString = "You have reset the speed of time to 1.";
            }
            else if (m_speedUpKey.Value.Length > 0 && Input.GetKeyDown(m_speedUpKey.Value))
            {
                if (Time.timeScale > 0)
                {
                    Time.timeScale = Time.timeScale + (float)speedChangeStep.Value * mult;
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
            else if (m_slowDownKey.Value.Length > 0 && Input.GetKeyDown(m_slowDownKey.Value)) 
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

        [HarmonyPatch(typeof(Menu), "Update")]
        static class Menu_Update_Patch
        {

            static void Prefix(Menu __instance, int ___m_hiddenFrames)
            {
                if (pauseOnMenu.Value && Time.timeScale > 0 && !(Game.instance.IsLoggingOut()) && __instance.m_root.gameObject.activeSelf && !wasActive)
                {
                    Dbgl($"Pausing time on menu open.");
                    pausedMenu = true;
                    lastTime = Math.Round(Time.timeScale, 1);
                    Time.timeScale = 0;
                }
                if (pausedMenu && !__instance.m_root.gameObject.activeSelf)
                {
                    Dbgl($"Unpausing time on menu close.");
                    Time.timeScale = (float)lastTime;
                    pausedMenu = false;
                }
                wasActive = __instance.m_root.gameObject.activeSelf;
            }
        }
        [HarmonyPatch(typeof(Menu), "OnLogout")]
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
    }
}
