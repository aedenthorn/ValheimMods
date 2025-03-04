using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DespawnTrader
{
    [BepInPlugin("aedenthorn.DespawnTrader", "Despawn Trader", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<string> modKey;
        public static ConfigEntry<string> despawnedMessage;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            modKey = Config.Bind<string>("General", "ModKey", "left alt", "Modifier key to despawn trader");
            despawnedMessage = Config.Bind<string>("General", "DespawnedMessage", "Despawned trader", "Message to display after despawning trader.");
            nexusID = Config.Bind<int>("General", "NexusID", 557, "Mod ID on the Nexus for update checks");

            Config.Save();

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Trader), "Interact")]
        public static class Trader_Interact_Patch
        {
            public static bool Prefix(Trader __instance, Humanoid character, bool hold)
            {
                if (!hold && CheckKeyHeld(modKey.Value) && character.IsPlayer() && (character as Player).GetPlayerID() == Player.m_localPlayer.GetPlayerID())
                {
 
                    if (__instance.gameObject.GetComponent<ZNetView>() == null)
                        Destroy(__instance.gameObject);
                    else
                        ZNetScene.instance.Destroy(__instance.gameObject);
                    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format(despawnedMessage.Value), 0, null);
                    return false;
                }
                return true;
            }
        }

        public static bool CheckKeyHeld(string value)
        {
            if (value == null || value.Length == 0)
                return true;
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return false;
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
