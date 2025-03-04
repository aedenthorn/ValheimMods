using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace EmoteKeys
{
    [BepInPlugin("aedenthorn.EmoteKeys", "Emote Keys", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> modKey1;
        public static ConfigEntry<string> modKey2;
        public static ConfigEntry<string> sitKey;
        public static ConfigEntry<string> waveKey;
        public static ConfigEntry<string> challengeKey;
        public static ConfigEntry<string> cheerKey;
        public static ConfigEntry<string> noKey;
        public static ConfigEntry<string> thumbsUpKey;
        public static ConfigEntry<string> pointKey;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 318, "Nexus mod ID for updates");
            
            modKey1 = Config.Bind<string>("Config", "ModKey1", "", "Mod key one (leave blank to disable)");
            modKey2 = Config.Bind<string>("Config", "ModKey2", "", "Mod key two (set blank to disable)");
            sitKey = Config.Bind<string>("Config", "SitKey", "[1]", "Key to sit");
            waveKey = Config.Bind<string>("Config", "WaveKey", "[2]", "Key to wave");
            challengeKey = Config.Bind<string>("Config", "ChallengeKey", "[3]", "Key to wave");
            cheerKey = Config.Bind<string>("Config", "CheerKey", "[4]", "Key to cheer");
            noKey = Config.Bind<string>("Config", "NoKey", "[5]", "Key to nonono");
            thumbsUpKey = Config.Bind<string>("Config", "ThumbsUpKey", "[6]", "Key to thumbs up");
            pointKey = Config.Bind<string>("Config", "PointKey", "[7]", "Key to point");
            
            if (!modEnabled.Value)
                return;
            
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

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
        public static bool CheckKeyHeld(string value, bool req)
        {
            if (value == "")
                return !req;
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return !req;
            }
        }
        public void Update()
        {
            if (!modEnabled.Value || Player.m_localPlayer == null || !Traverse.Create(Player.m_localPlayer).Method("TakeInput").GetValue<bool>())
                return;

            if (CheckKeyHeld(modKey1.Value, false) && CheckKeyHeld(modKey2.Value, false))
            {
                if (CheckKeyDown(sitKey.Value))
                {
                    Dbgl("Trying to sit");
                    Player.m_localPlayer.StartEmote("sit", false);

                }
                else if (CheckKeyDown(waveKey.Value))
                {
                    Dbgl("Trying to wave");
                    Player.m_localPlayer.StartEmote("wave", true);

                }
                else if (CheckKeyDown(challengeKey.Value))
                {
                    Dbgl("Trying to challenge");
                    Player.m_localPlayer.StartEmote("challenge", true);

                }
                else if (CheckKeyDown(cheerKey.Value))
                {
                    Dbgl("Trying to cheer");
                    Player.m_localPlayer.StartEmote("cheer", true);

                }
                else if (CheckKeyDown(noKey.Value))
                {
                    Dbgl("Trying to nonono");
                    Player.m_localPlayer.StartEmote("nonono", true);
                }
                else if (CheckKeyDown(thumbsUpKey.Value))
                {
                    Dbgl("Trying to thumbs up");
                    Player.m_localPlayer.StartEmote("thumbsup", true);
                }
                else if (CheckKeyDown(pointKey.Value))
                {
                    Dbgl("Trying to point");
                    Player.m_localPlayer.FaceLookDirection();
                    Player.m_localPlayer.StartEmote("point", true);
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