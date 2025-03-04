using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace CartSupport
{
    [BepInPlugin("aedenthorn.CartSupport", "CartSupport", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<bool> includePuller;
        public static ConfigEntry<float> playerRange;
        public static ConfigEntry<float> minMass;
        public static ConfigEntry<float> maxPlayers;
        public static ConfigEntry<float> playerMassReduction;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "isDebug", false, "Enable debug messages");
            nexusID = Config.Bind<int>("General", "NexusID", 947, "Nexus mod ID for updates");
            
            minMass = Config.Bind<float>("Carts", "MinMass", 0.1f, "Minimum mass of carts no matter how many players support it.");
            playerRange = Config.Bind<float>("Carts", "PlayerRange", 5f, "Maximum player distance to support the cart (metres).");
            maxPlayers = Config.Bind<float>("Carts", "MaxPlayers", 4f, "Maximum number of supporting players.");
            playerMassReduction = Config.Bind<float>("Carts", "PlayerMassReduction", 0.2f, "Fractional weight reduction for each supporting player.");
            includePuller = Config.Bind<bool>("Carts", "IncludePuller", false, "Include the puller in weight reduction");

            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        [HarmonyPatch(typeof(Vagon), "SetMass")]
        public static class SetMass_Patch
        {
            public static void Prefix(Vagon __instance, ZNetView ___m_nview,  ref float mass)
            {
                if (!modEnabled.Value || !___m_nview.IsOwner())
                    return;

                float before = mass;

                List<Player> players = new List<Player>();
                Player.GetPlayersInRange(__instance.gameObject.transform.position, playerRange.Value, players);
                if(players.Count > (includePuller.Value ? 0 : 1))
                    mass = Mathf.Max(0.1f, mass - mass * playerMassReduction.Value * Mathf.Min(maxPlayers.Value, players.Count - (includePuller.Value ? 0 : 1)));

                Dbgl($"mass players {playerCount} distance {Vector3.Distance(__instance.gameObject.transform.position, Player.m_localPlayer.transform.position)} before {before} after {mass} is owner {___m_nview.IsOwner()}");
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
