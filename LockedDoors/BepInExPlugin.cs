using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LockableDoors
{
    [BepInPlugin("aedenthorn.LockableDoors", "Lockable Doors", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<bool> needKeyToClose;
        public static ConfigEntry<string> customKeyIconFile;
        public static ConfigEntry<string> modKey;
        public static ConfigEntry<string> keyName;
        public static ConfigEntry<string> keyDescription;
        public static ConfigEntry<string> lockedMessage;

        private static List<Vector3> newDoors = new List<Vector3>();

        private static Sprite icon = null;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1346, "Nexus mod ID for updates");

            needKeyToClose = Config.Bind<bool>("Variables", "NeedKeyToClose", false, "Require key in order to close a door as well as open it.");
            modKey = Config.Bind<string>("Variables", "LockModKey", "left shift", "Modifier key used to create and lock/unlock a lockable door.");
            keyName = Config.Bind<string>("Variables", "KeyName", "{0} Door Key", "Name of key - replaces {0} with the door coordinates.");
            keyDescription = Config.Bind<string>("Variables", "KeyDescription", "Opens a door at {0}.", "Description of key in tooltip - replaces {0} with the door coordinates.");
            lockedMessage = Config.Bind<string>("Variables", "LockedMessage", "Door locked: {0}.", "Message to show when locking/unlocking the door. Replaces {0} with true or false.");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Player), "PlacePiece")]
        static class Player_PlacePiece_Patch
        {
            static void Postfix(bool __result, Piece piece, GameObject ___m_placementGhost)
            {
                if (!modEnabled.Value || !__result || !AedenthornUtils.CheckKeyHeld(modKey.Value))
                    return;

                Door door = piece.gameObject.GetComponent<Door>();

                if(door)
                {
                    newDoors.Add(___m_placementGhost.transform.position);

                    GameObject keyPrefab = ZNetScene.instance.GetPrefab("CryptKey");
                    GameObject go = Instantiate(keyPrefab, Player.m_localPlayer.transform.position + Vector3.up, Quaternion.identity);
                    go.GetComponent<ItemDrop>().m_itemData.m_crafterName = ___m_placementGhost.transform.position+"";
                    Dbgl($"Spawned door key for door at {___m_placementGhost.transform.position}");
                }
            }
        }
        
        [HarmonyPatch(typeof(Door), "UpdateState")]
        static class Door_UpdateState_Patch
        {
            static void Postfix(Door __instance, ZNetView ___m_nview)
            {
                if (!modEnabled.Value || !___m_nview.IsValid() || !newDoors.Any() || !newDoors.Contains(__instance.transform.position))
                    return;
                ___m_nview.GetZDO().Set("LockedDoor", true);
                ___m_nview.GetZDO().Set("DoorLocked", true);

                newDoors.Remove(__instance.transform.position);
                Dbgl($"Set door as locked at {__instance.transform.position}");
            }
        }
                      
        [HarmonyPatch(typeof(Door), "Interact")]
        static class Door_Interact_Patch
        {
            static bool Prefix(Door __instance, ZNetView ___m_nview, bool __result, Humanoid character)
            {
                if (!modEnabled.Value || ___m_nview.GetZDO() == null || !(character is Player) || !___m_nview.GetZDO().GetBool("LockedDoor"))
                    return true;

                if (AedenthornUtils.CheckKeyHeld(modKey.Value) && (character as Player).GetInventory().GetAllItems().Exists(i => i.m_crafterName == __instance.transform.position + ""))
                {
                    ___m_nview.GetZDO().Set("DoorLocked", !___m_nview.GetZDO().GetBool("DoorLocked"));
                    __result = true;
                    character.Message(MessageHud.MessageType.Center, string.Format(lockedMessage.Value, ___m_nview.GetZDO().GetBool("DoorLocked")), 0, null);

                    Dbgl($"Door locked: {___m_nview.GetZDO().GetBool("DoorLocked")}");
                    return false;
                }
                else if(___m_nview.GetZDO().GetBool("DoorLocked") && (needKeyToClose.Value || ___m_nview.GetZDO().GetInt("state", 0) == 0))
                {
                    if(!(character as Player).GetInventory().GetAllItems().Exists(i => i.m_crafterName == __instance.transform.position + ""))
                    {
                        __instance.m_lockedEffects.Create(__instance.transform.position, __instance.transform.rotation, null, 1f);
                        character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_needkey", new string[]
                        {
                        __instance.transform.position+""
                        }), 0, null);
                        __result = true;
                        Dbgl($"player doesn't have key for {__instance.transform.position}");
                        return false;
                    }
                    else
                    {
                        Dbgl($"player has key for {__instance.transform.position}");
                    }
                }

                return true;
            }
        }
                
        [HarmonyPatch(typeof(Door), "HaveKey")]
        static class HaveKey_Patch
        {
            static void Postfix(Door __instance, ZNetView ___m_nview, bool __result, Humanoid player)
            {
                if (!__result || ___m_nview.GetZDO() == null || !(player is Player))
                    return;

                if(!(player as Player).GetInventory().GetAllItems().Exists(i => i.m_shared.m_name == __instance.m_keyItem.m_itemData.m_shared.m_name && !Regex.IsMatch(i.m_crafterName, @"[-0-9.]+,[-0-9.]+,[-0-9.]+")))
                {
                    __result = false;

                    Dbgl($"Tried to open crypt with door key");
                }

            }
        }
                        
        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool) })]
        static class GetTooltip_Patch
        {
            static void Postfix(ItemDrop.ItemData item, ref string __result)
            {
                if (!modEnabled.Value || item.m_shared.m_name != "$item_cryptkey" || !Regex.IsMatch(item.m_crafterName, @"[-0-9.]+, *[-0-9.]+, *[-0-9.]+"))
                    return;

                __result = __result.Replace(item.m_shared.m_description, string.Format(keyDescription.Value, item.m_crafterName));
            }
        }

        [HarmonyPatch(typeof(Character), "ShowPickupMessage")]
        static class ShowPickupMessage_Patch
        {
            static bool Prefix(Character __instance, ItemDrop.ItemData item, int amount)
            {
                if (!modEnabled.Value || item.m_shared.m_name != "$item_cryptkey" || !Regex.IsMatch(item.m_crafterName, @"[-0-9.]+, *[-0-9.]+, *[-0-9.]+"))
                    return true;

                __instance.Message(MessageHud.MessageType.TopLeft, "$msg_added " + string.Format(keyName.Value, item.m_crafterName), amount, item.GetIcon());
                return false;
            }
        }

        [HarmonyPatch(typeof(ItemDrop), "GetHoverText")]
        static class GetHoverText_Patch
        {
            static void Postfix(ItemDrop __instance, ref string __result)
            {
                if (!modEnabled.Value || __instance.m_itemData.m_shared.m_name != "$item_cryptkey" || !Regex.IsMatch(__instance.m_itemData.m_crafterName, @"[-0-9.]+, *[-0-9.]+, *[-0-9.]+"))
                    return;

                __result = __result.Replace(Localization.instance.Localize(__instance.m_itemData.m_shared.m_name), string.Format(keyName.Value, __instance.m_itemData.m_crafterName));
            }
        }
                                  
        [HarmonyPatch(typeof(InventoryGrid), "CreateItemTooltip")]
        static class CreateItemTooltip_Patch
        {
            static void Postfix(ItemDrop.ItemData item, UITooltip tooltip)
            {
                if (!modEnabled.Value || item.m_shared.m_name != "$item_cryptkey" || !Regex.IsMatch(item.m_crafterName, @"[-0-9.]+, *[-0-9.]+, *[-0-9.]+"))
                    return;

                tooltip.Set(string.Format(keyName.Value, item.m_crafterName), tooltip.m_text);
            }
        }
                       
        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetIcon")]
        static class GetIcon_Patch
        {
            static void Postfix(ItemDrop.ItemData __instance, ref Sprite __result)
            {
                if (!modEnabled.Value || __instance.m_shared.m_name != "$item_cryptkey" || !Regex.IsMatch(__instance.m_crafterName, @"[-0-9.]+, *[-0-9.]+, *[-0-9.]+"))
                    return;

                if(icon == null)
                {
                    var tex = new Texture2D(2, 2);
                    byte[] imageData = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace, "icon.png"));
                    tex.LoadImage(imageData);
                    icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
                }
                __result = icon;
            }
        }

        public static IEnumerator UpdateMap(bool force)
        {
            yield break;
        }

        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;

                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    icon = null;
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
