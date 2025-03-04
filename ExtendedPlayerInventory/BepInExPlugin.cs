﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace ExtendedPlayerInventory
{
    [BepInPlugin("aedenthorn.ExtendedPlayerInventory", "Extended Player Inventory", "1.0.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> addEquipmentRow;
        public static ConfigEntry<bool> showGearInMenu;
        public static ConfigEntry<bool> displayEquipmentRowSeparate;
        public static ConfigEntry<int> extraRows;
        
        public static ConfigEntry<string> helmetText;
        public static ConfigEntry<string> chestText;
        public static ConfigEntry<string> legsText;
        public static ConfigEntry<string> backText;
        public static ConfigEntry<string> utilityText;
        public static ConfigEntry<float> quickAccessScale;
        
        public static ConfigEntry<string> hotKey1;
        public static ConfigEntry<string> hotKey2;
        public static ConfigEntry<string> hotKey3;
        public static ConfigEntry<string> modKeyOne;
        public static ConfigEntry<string> modKeyTwo;

        public static ConfigEntry<string>[] hotkeys;
        
        public static ConfigEntry<float> quickAccessX;
        public static ConfigEntry<float> quickAccessY;

        public static GameObject elementPrefab;
        
        public static ItemDrop.ItemData.ItemType[] typeEnums = new ItemDrop.ItemData.ItemType[] 
        {
            ItemDrop.ItemData.ItemType.Helmet,
            ItemDrop.ItemData.ItemType.Chest,
            ItemDrop.ItemData.ItemType.Legs,
            ItemDrop.ItemData.ItemType.Shoulder,
            ItemDrop.ItemData.ItemType.Utility,
        };
        
        public static FieldInfo[] fields = new FieldInfo[] 
        {
            AccessTools.Field(typeof(Humanoid), "m_helmetItem"),
            AccessTools.Field(typeof(Humanoid), "m_chestItem"),
            AccessTools.Field(typeof(Humanoid), "m_legItem"),
            AccessTools.Field(typeof(Humanoid), "m_shoulderItem"),
            AccessTools.Field(typeof(Humanoid), "m_utilityItem"),
        };

        public static ItemDrop.ItemData[] equipItems = new ItemDrop.ItemData[5];

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1356, "Nexus mod ID for updates");
            nexusID.Value = 1356;


            extraRows = Config.Bind<int>("Toggles", "ExtraRows", 0, "Number of extra ordinary rows.");
            addEquipmentRow = Config.Bind<bool>("Toggles", "AddEquipmentRow", true, "Add special row for equipped items and quick slots.");
            showGearInMenu = Config.Bind<bool>("Toggles", "ShowGearInMenu", true, "Display equipped items in the main menu.");
            displayEquipmentRowSeparate = Config.Bind<bool>("Toggles", "DisplayEquipmentRowSeparate", true, "Display equipment and quickslots in their own area.");

            helmetText = Config.Bind<string>("Strings", "HelmetText", "Head", "Text to show for helmet slot.");
            chestText = Config.Bind<string>("Strings", "ChestText", "Chest", "Text to show for chest slot.");
            legsText = Config.Bind<string>("Strings", "LegsText", "Legs", "Text to show for legs slot.");
            backText = Config.Bind<string>("Strings", "BackText", "Back", "Text to show for back slot.");
            utilityText = Config.Bind<string>("Strings", "UtilityText", "Utility", "Text to show for utility slot.");
            
            quickAccessScale = Config.Bind<float>("Misc", "QuickAccessScale", 1, "Scale of quick access bar.");
            
            hotKey1 = Config.Bind<string>("Hotkeys", "HotKey1", "v", "Hotkey 1 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey2 = Config.Bind<string>("Hotkeys", "HotKey2", "b", "Hotkey 2 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey3 = Config.Bind<string>("Hotkeys", "HotKey3", "n", "Hotkey 3 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");

            modKeyOne = Config.Bind<string>("Hotkeys", "ModKey1", "mouse 0", "First modifier key to move quick slots. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html format.");
            modKeyTwo = Config.Bind<string>("Hotkeys", "ModKey2", "left ctrl", "Second modifier key to move quick slots. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html format.");

            quickAccessX = Config.Bind<float>("ZCurrentPositions", "quickAccessX", 9999, "Current X of Quick Slots");
            quickAccessY = Config.Bind<float>("ZCurrentPositions", "quickAccessY", 9999, "Current Y of Quick Slots");


            hotkeys = new ConfigEntry<string>[]
            {
                hotKey1,
                hotKey2,
                hotKey3,
            };

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Player), "Awake")]
        public static class Player_Awake_Patch
        {
            public static void Prefix(Player __instance, Inventory ___m_inventory)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl("Player_Awake");

                int height = extraRows.Value + (addEquipmentRow.Value ? 5 : 4);

                AccessTools.FieldRefAccess<Inventory, int>(___m_inventory, "m_height") = height;
                __instance.m_tombstone.GetComponent<Container>().m_height = height;
            }
        }
          
        [HarmonyPatch(typeof(TombStone), "Awake")]
        public static class TombStone_Awake_Patch
        {
            public static void Prefix(TombStone __instance)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl("TombStone_Awake");

                int height = extraRows.Value + (addEquipmentRow.Value ? 5 : 4);

                __instance.GetComponent<Container>().m_height = height;
                //AccessTools.FieldRefAccess<Inventory, int>(AccessTools.FieldRefAccess<Container, Inventory>(__instance.GetComponent<Container>(), "m_inventory"), "m_height") = height;
                //Dbgl($"tombstone Awake {__instance.GetComponent<Container>().GetInventory()?.GetHeight()}");
            }
        }
                     
        [HarmonyPatch(typeof(TombStone), "Interact")]
        public static class TombStone_Interact_Patch
        {
            public static void Prefix(TombStone __instance, Container ___m_container)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl("TombStone_Interact");

                int height = extraRows.Value + (addEquipmentRow.Value ? 5 : 4);

                __instance.GetComponent<Container>().m_height = height;

                Traverse t = Traverse.Create(___m_container);
                string dataString = t.Field("m_nview").GetValue<ZNetView>().GetZDO().GetString("items", "");
                if (string.IsNullOrEmpty(dataString))
                {
                    return;
                }
                ZPackage pkg = new ZPackage(dataString);
                t.Field("m_loading").SetValue(true);
                t.Field("m_inventory").GetValue<Inventory>().Load(pkg);
                t.Field("m_loading").SetValue(false);
                t.Field("m_lastDataString").SetValue(dataString);
            }
        }
                
        [HarmonyPatch(typeof(Inventory), "MoveInventoryToGrave")]
        public static class MoveInventoryToGrave_Patch
        {
            public static void Postfix(Inventory __instance, Inventory original)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl("MoveInventoryToGrave");

                Dbgl($"inv: {__instance.GetHeight()} orig: {original.GetHeight()}");
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Show")]
        public static class InventoryGui_Show_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value || !Player.m_localPlayer)
                    return;

                Dbgl("InventoryGui Show");

                ArrangeEquipment(Player.m_localPlayer);

                if (displayEquipmentRowSeparate.Value && __instance.m_player.Find("EquipmentBkg") == null)
                {
                    Transform bkg = Instantiate(__instance.m_player.Find("Bkg"), __instance.m_player);
                    bkg.SetAsFirstSibling();
                    bkg.name = "EquipmentBkg";
                    bkg.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
                    bkg.GetComponent<RectTransform>().anchorMax = new Vector2(1.5f, 1);
                }
                else if (!displayEquipmentRowSeparate.Value && __instance.m_player.Find("EquipmentBkg"))
                {
                    Destroy(__instance.m_player.Find("EquipmentBkg").gameObject);
                }

            }
        }
        [HarmonyPatch(typeof(Inventory), "Changed")]
        public static class Inventory_Changed_Patch
        {
            public static void Postfix(Inventory __instance)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                    return;

                int height = extraRows.Value + (addEquipmentRow.Value ? 5 : 4);
                AccessTools.FieldRefAccess<Inventory, int>(__instance, "m_height") = height;
                //Dbgl("Changed: "+Environment.StackTrace);
                ArrangeEquipment(Player.m_localPlayer);
            }

        }

        [HarmonyPatch(typeof(Humanoid), "SetupEquipment")]
        public static class Humanoid_SetupEquipment_Patch
        {
            public static void Postfix(Humanoid __instance)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value || !(__instance is Player) || (__instance != Player.m_localPlayer && (!showGearInMenu.Value || Player.m_localPlayer != null)))
                    return;
                //Dbgl("Setup: " + Environment.StackTrace);
                ArrangeEquipment((Player)__instance);
            }

        }
        
        [HarmonyPatch(typeof(Humanoid), "UnequipItem")]
        public static class Humanoid_UnequipItem_Patch
        {
            public static void Postfix(Humanoid __instance, ItemDrop.ItemData item)
            {
                //Dbgl($"Unequip {item?.m_shared.m_name}: " + Environment.StackTrace);
            }

        }

        [HarmonyPatch(typeof(Player), "EquipInventoryItems")]
        public static class Humanoid_EquipItem_Patch
        {
            public static void Postfix(Player __instance)
            {
                //Dbgl("EquipIventoryItems: " + Environment.StackTrace);
                ArrangeEquipment(__instance);
            }
        }

        public static void ArrangeEquipment(Player __instance)
        {
            if (!modEnabled.Value || !addEquipmentRow.Value)
                return;
            Inventory inv = __instance.GetInventory();


            var items = inv.GetAllItems();

            ItemDrop.ItemData[] equipment = new ItemDrop.ItemData[]
            {
                AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(__instance, "m_helmetItem"),
                AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(__instance, "m_chestItem"),
                AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(__instance, "m_legItem"),
                AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(__instance, "m_shoulderItem"),
                AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(__instance, "m_utilityItem")
            };


            int width = inv.GetWidth();
            int offset = width * (inv.GetHeight() - 1);

            if (equipment[0] != null)
                equipment[0].m_gridPos = new Vector2i(offset % width, offset / width);
            offset++;

            if (equipment[1] != null)
                equipment[1].m_gridPos = new Vector2i(offset % width, offset / width);
            offset++;

            if (equipment[2] != null)
                equipment[2].m_gridPos = new Vector2i(offset % width, offset / width);
            offset++;

            if (equipment[3] != null)
                equipment[3].m_gridPos = new Vector2i(offset % width, offset / width);
            offset++;

            if (equipment[4] != null)
                equipment[4].m_gridPos = new Vector2i(offset % width, offset / width);


            for (int i = 0; i < items.Count; i++)
            {
                if (IsAtEquipmentSlot(inv, items[i], out int which))
                {
                    //Dbgl($"{items[i].m_shared.m_itemType} {typeEnums[which]} {items[i].m_shared.m_name} {items[i].m_equiped}");

                    if (items[i] == equipment[which])
                    {
                        //Dbgl($"already equipped: {items[i].m_equiped}");
                        items[i].m_equipped = true;
                        continue;
                    }

                    if (items[i].m_shared.m_itemType == typeEnums[which] && equipItems[which] != items[i]) // in right slot and new
                    {
                        //Dbgl($"equipping: {__instance.EquipItem(items[i])}");
                        __instance.EquipItem(items[i]);
                        equipment[which] = items[i];
                        items[i].m_equipped = true;
                        continue;
                    }

                    // in wrong slot or unequipped in slot or can't equip
                    Vector2i newPos = (Vector2i)typeof(Inventory).GetMethod("FindEmptySlot", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(inv, new object[] { true });
                    if (newPos.x < 0 || newPos.y < 0 || newPos.y == inv.GetHeight() - 1)
                    {
                        __instance.DropItem(inv, items[i], items[i].m_stack);
                    }
                    else
                    {
                        items[i].m_gridPos = newPos;
                    }
                }
            }
            equipItems = equipment;

        }
        [HarmonyPatch(typeof(Player), "Update")]
        public static class Player_Update_Patch
        {
            public static void Postfix(Player __instance, Inventory ___m_inventory)
            {
                if (!modEnabled.Value)
                    return;

                int height = extraRows.Value + (addEquipmentRow.Value ? 5 : 4);

                AccessTools.FieldRefAccess<Inventory, int>(___m_inventory, "m_height") = height;
                __instance.m_tombstone.GetComponent<Container>().m_height = height;


                if (AedenthornUtils.IgnoreKeyPresses(true) || !addEquipmentRow.Value)
                    return;

                //if (AedenthornUtils.CheckKeyDown("9"))
                    //CreateTombStone();

                int which;
                if (AedenthornUtils.CheckKeyDown(hotKey1.Value))
                    which = 1;
                else if (AedenthornUtils.CheckKeyDown(hotKey2.Value))
                    which = 2;
                else if (AedenthornUtils.CheckKeyDown(hotKey3.Value))
                    which = 3;
                else return;

                Dbgl($"Pressed hotkey {which}");

                ItemDrop.ItemData itemAt = ___m_inventory.GetItemAt(which + 4, ___m_inventory.GetHeight() - 1);
                if (itemAt != null)
                {
                    __instance.UseItem(null, itemAt, false);
                }
            }

            public static void CreateTombStone()
            {
                Dbgl($"height {Player.m_localPlayer.m_tombstone.GetComponent<Container>().m_height}");
                GameObject gameObject = Instantiate(Player.m_localPlayer.m_tombstone, Player.m_localPlayer.GetCenterPoint(), Player.m_localPlayer.transform.rotation);
                TombStone component = gameObject.GetComponent<TombStone>();

                Dbgl($"height {gameObject.GetComponent<Container>().m_height}");
                Dbgl($"inv height {gameObject.GetComponent<Container>().GetInventory().GetHeight()}");
                Dbgl($"inv slots {gameObject.GetComponent<Container>().GetInventory().GetEmptySlots()}");

                for (int i = 0; i < gameObject.GetComponent<Container>().GetInventory().GetEmptySlots(); i++)
                {
                    gameObject.GetComponent<Container>().GetInventory().AddItem("SwordBronze", 1, 1, 0, 0, "");
                }
                Dbgl($"no items: {gameObject.GetComponent<Container>().GetInventory().NrOfItems()}");
                PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());

            }
        }
        
        [HarmonyPatch(typeof(InventoryGui), "Update")]
        public static class InventoryGui_Update_Patch
        {
            public static void Postfix(InventoryGui __instance, Animator ___m_animator, InventoryGrid ___m_playerGrid)
            {
                if (!modEnabled.Value || !Player.m_localPlayer)
                    return;


                if (___m_animator.GetBool("visible"))
                {
                    __instance.m_player.Find("Bkg").GetComponent<RectTransform>().anchorMin = new Vector2(0, (extraRows.Value + (addEquipmentRow.Value && !displayEquipmentRowSeparate.Value ? 1 : 0)) * -0.25f);

                    if (addEquipmentRow.Value)
                    {
                        if (displayEquipmentRowSeparate.Value && __instance.m_player.Find("EquipmentBkg") == null)
                        {
                            Transform bkg = Instantiate(__instance.m_player.Find("Bkg"), __instance.m_player);
                            bkg.SetAsFirstSibling();
                            bkg.name = "EquipmentBkg";
                            bkg.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
                            bkg.GetComponent<RectTransform>().anchorMax = new Vector2(1.5f, 1);
                        }
                        else if (!displayEquipmentRowSeparate.Value && __instance.m_player.Find("EquipmentBkg"))
                        {
                            Destroy(__instance.m_player.Find("EquipmentBkg").gameObject);
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(InventoryGui), "UpdateInventory")]
        public static class UpdateInventory_Patch
        {
            public static void Postfix(InventoryGrid ___m_playerGrid)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value)
                    return;

                Inventory inv = Player.m_localPlayer.GetInventory();

                int offset = inv.GetWidth() * (inv.GetHeight() - 1);

                SetSlotText(helmetText.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++));
                SetSlotText(chestText.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++));
                SetSlotText(legsText.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++));
                SetSlotText(backText.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++));
                SetSlotText(utilityText.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++));
                SetSlotText(hotKey1.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++), false);
                SetSlotText(hotKey2.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++), false);
                SetSlotText(hotKey3.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++), false);

                if (displayEquipmentRowSeparate.Value)
                {
                    offset = inv.GetWidth() * (inv.GetHeight() - 1);
                    ___m_playerGrid.m_gridRoot.transform.GetChild(offset++).GetComponent<RectTransform>().anchoredPosition = new Vector2(678, 0);
                    ___m_playerGrid.m_gridRoot.transform.GetChild(offset++).GetComponent<RectTransform>().anchoredPosition = new Vector2(748, -35);
                    ___m_playerGrid.m_gridRoot.transform.GetChild(offset++).GetComponent<RectTransform>().anchoredPosition = new Vector2(678, -70);
                    ___m_playerGrid.m_gridRoot.transform.GetChild(offset++).GetComponent<RectTransform>().anchoredPosition = new Vector2(748, -105);
                    ___m_playerGrid.m_gridRoot.transform.GetChild(offset++).GetComponent<RectTransform>().anchoredPosition = new Vector2(678, -140);
                    ___m_playerGrid.m_gridRoot.transform.GetChild(offset++).GetComponent<RectTransform>().anchoredPosition = new Vector2(643, -210);
                    ___m_playerGrid.m_gridRoot.transform.GetChild(offset++).GetComponent<RectTransform>().anchoredPosition = new Vector2(713, -210);
                    ___m_playerGrid.m_gridRoot.transform.GetChild(offset++).GetComponent<RectTransform>().anchoredPosition = new Vector2(783, -210);
                }

            }
        }

        public static void SetSlotText(string value, Transform transform, bool center = true)
        {
            Transform t = transform.Find("binding");
            if (!t)
            {
                t = Instantiate(elementPrefab.transform.Find("binding"), transform);
            }
            t.GetComponent<TMP_Text>().enabled = true;
            t.GetComponent<TMP_Text>().text = value;
            t.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 17);
            if (center)
            {
                t.GetComponent<RectTransform>().anchoredPosition = new Vector2(32, -10);
            }
        }
        
        [HarmonyPatch(typeof(Inventory), "FindEmptySlot")]
        public static class FindEmptySlot_Patch
        {

            public static void Prefix(Inventory __instance, ref int ___m_height)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                    return;
                Dbgl("FindEmptySlot");

                ___m_height--;
            }
            public static void Postfix(Inventory __instance, ref int ___m_height)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                    return;

                ___m_height++;
            }
        }
        
        [HarmonyPatch(typeof(Inventory), "GetEmptySlots")]
        public static class GetEmptySlots_Patch
        {

            public static bool Prefix(Inventory __instance, ref int __result, List<ItemDrop.ItemData> ___m_inventory, int ___m_width, int ___m_height)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value || __instance != Player.m_localPlayer.GetInventory())
                    return true;
                //Dbgl("GetEmptySlots");
                int count = ___m_inventory.FindAll(i => i.m_gridPos.y < ___m_height - 1).Count;
                __result = (___m_height - 1) * ___m_width - count;
                return false;
            }
        }

        [HarmonyPatch(typeof(Inventory), "HaveEmptySlot")]
        public static class HaveEmptySlot_Patch
        {

            public static bool Prefix(Inventory __instance, ref bool __result, List<ItemDrop.ItemData> ___m_inventory, int ___m_width, int ___m_height)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value || ___m_inventory is null || __instance != Player.m_localPlayer?.GetInventory())
                    return true;

                int count = ___m_inventory.FindAll(i => i?.m_gridPos.y < ___m_height - 1).Count;

                __result = count < ___m_width * (___m_height - 1);
                return false;
            }
        }
        
        [HarmonyPatch(typeof(Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData) })]
        public static class Inventory_AddItem_Patch1
        {
            public static bool Prefix(Inventory __instance, ref bool __result, List<ItemDrop.ItemData> ___m_inventory, ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                    return true;

                Dbgl("AddItem");

                if(IsEquipmentSlotFree(__instance, item, out int which))
                {
                    item.m_gridPos = new Vector2i(which, __instance.GetHeight() - 1);
                }
                else
                    return true;
                ___m_inventory.Add(item);
                Player.m_localPlayer.EquipItem(item, false);
                typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { });
                __result = true;
                return false;
            }
        }


        [HarmonyPatch(typeof(Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
        public static class Inventory_AddItem_Patch2
        {
            public static void Prefix(Inventory __instance, ref int ___m_width, ref int ___m_height, int x, int y)
            {
                if (!modEnabled.Value)
                    return;

                if (x >= ___m_width)
                {
                    ___m_width = x + 1;
                }
                if (y >= ___m_height)
                {
                    ___m_height = y + 1;
                }
            }
        }

        public static bool IsEquipmentSlotFree(Inventory inventory, ItemDrop.ItemData item, out int which)
        {
            which = Array.IndexOf(typeEnums, item.m_shared.m_itemType);
            return which >= 0 && inventory.GetItemAt(which, inventory.GetHeight() - 1) == null;
        }

        public static bool IsAtEquipmentSlot(Inventory inventory, ItemDrop.ItemData item, out int which)
        {
            if (!addEquipmentRow.Value || item.m_gridPos.x > 4 || item.m_gridPos.y < inventory.GetHeight() - 1)
            {
                which = -1;
                return false;
            }
            which = item.m_gridPos.x;
            return true;
        }

        [HarmonyPatch(typeof(Hud), "Awake")]
        public static class Hud_Awake_Patch
        {

            public static void Postfix(Hud __instance)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value)
                    return;

                Transform newBar = Instantiate(__instance.m_rootObject.transform.Find("HotKeyBar"));
                newBar.name = "QuickAccessBar";
                newBar.SetParent(__instance.m_rootObject.transform);
                newBar.GetComponent<RectTransform>().localPosition = Vector3.zero;
                GameObject go = newBar.GetComponent<HotkeyBar>().m_elementPrefab;
                QuickAccessBar qab = newBar.gameObject.AddComponent<QuickAccessBar>();
                qab.m_elementPrefab = go;
                elementPrefab = go;
                Destroy(newBar.GetComponent<HotkeyBar>());
            }
        }

        public static Vector3 lastMousePos;
        public static string currentlyDragging;

        [HarmonyPatch(typeof(Hud), "Update")]
        public static class Hud_Update_Patch
        {
            public static void Postfix(Hud __instance)
            {
                if (!modEnabled.Value || !addEquipmentRow.Value || Player.m_localPlayer == null)
                    return;

                float gameScale = __instance.GetComponent<CanvasScaler>().scaleFactor;

                Vector3 mousePos = Input.mousePosition;

                if (!modEnabled.Value)
                {
                    lastMousePos = mousePos;
                    return;
                }

                SetElementPositions();

                if (lastMousePos == Vector3.zero)
                    lastMousePos = mousePos;


                Transform hudRoot = Hud.instance.transform.Find("hudroot");


                if (AedenthornUtils.CheckKeyHeld(modKeyOne.Value) && AedenthornUtils.CheckKeyHeld(modKeyTwo.Value))
                {

                   
                    Rect quickSlotsRect = Rect.zero;
                    if (hudRoot.Find("QuickAccessBar")?.GetComponent<RectTransform>() != null)
                    {
                        quickSlotsRect = new Rect(
                            hudRoot.Find("QuickAccessBar").GetComponent<RectTransform>().anchoredPosition.x * gameScale,
                            hudRoot.Find("QuickAccessBar").GetComponent<RectTransform>().anchoredPosition.y * gameScale + Screen.height - hudRoot.Find("QuickAccessBar").GetComponent<RectTransform>().sizeDelta.y * gameScale * quickAccessScale.Value,
                            hudRoot.Find("QuickAccessBar").GetComponent<RectTransform>().sizeDelta.x * gameScale * quickAccessScale.Value * (3 / 8f),
                            hudRoot.Find("QuickAccessBar").GetComponent<RectTransform>().sizeDelta.y * gameScale * quickAccessScale.Value
                        );
                    }
                    
                    if (quickSlotsRect.Contains(lastMousePos) && (currentlyDragging == "" || currentlyDragging == "QuickAccessBar"))
                    {
                        quickAccessX.Value += (mousePos.x - lastMousePos.x) / gameScale;
                        quickAccessY.Value += (mousePos.y - lastMousePos.y) / gameScale;
                        currentlyDragging = "QuickAccessBar";
                    }
                    else
                    {
                        currentlyDragging = "";
                    }
                }
                else
                    currentlyDragging = "";

                lastMousePos = mousePos;
            }
        }

        public static void SetElementPositions()
        {
            Transform hudRoot = Hud.instance.transform.Find("hudroot");

            if (hudRoot.Find("QuickAccessBar")?.GetComponent<RectTransform>() != null)
            {
                if (quickAccessX.Value == 9999)
                    quickAccessX.Value = hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x - 32;
                if (quickAccessY.Value == 9999)
                    quickAccessY.Value = hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y - 870;

                hudRoot.Find("QuickAccessBar").GetComponent<RectTransform>().anchoredPosition = new Vector2(quickAccessX.Value, quickAccessY.Value);
                hudRoot.Find("QuickAccessBar").GetComponent<RectTransform>().localScale = new Vector3(quickAccessScale.Value, quickAccessScale.Value, 1);
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
