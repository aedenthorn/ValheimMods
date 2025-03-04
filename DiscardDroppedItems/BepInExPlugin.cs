using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DiscardDroppedItems
{
    [BepInPlugin("aedenthorn.DiscardDroppedItems", "Discard Dropped Items", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<string> destroyModKey;
        public static ConfigEntry<string> destroyedMessage;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            destroyModKey = Config.Bind<string>("General", "DestroyModKey", "left alt", "Modifier key to destroy ground item");
            destroyedMessage = Config.Bind<string>("General", "DestroyedMessage", "Destroyed {0} {1}", "Message to display after destroying item. {0} is replaced by amount, and {1} is replaced by item name.");
            nexusID = Config.Bind<int>("General", "NexusID", 171, "Mod ID on the Nexus for update checks");
            nexusID.Value = 171;
            Config.Save();
            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ItemDrop), "Pickup")]
        public static class ItemDrop_Pickup_Patch
        {
            public static void Prefix(ItemDrop __instance, Humanoid character, ZNetView ___m_nview)
            {
                if (CheckKeyHeld(destroyModKey.Value) && character.IsPlayer() && (character as Player).GetPlayerID() == Player.m_localPlayer.GetPlayerID())
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format(destroyedMessage.Value, __instance.m_itemData.m_stack, Localization.instance.Localize(__instance.m_itemData.m_shared.m_name)), 0, null);

                    if (___m_nview.GetZDO() == null)
                        Destroy(__instance.gameObject);
                    else
                        ZNetScene.instance.Destroy(__instance.gameObject);
                }
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
    }
}
