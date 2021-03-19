using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSort
{
    [BepInPlugin("aedenthorn.SimpleSort", "Simple Sort", "0.4.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> useGamePad;
        public static ConfigEntry<bool> allowPlayerInventorySort;
        public static ConfigEntry<string> ascModKey;
        public static ConfigEntry<string> descModKey;
        public static ConfigEntry<string> sortByNameKey;
        public static ConfigEntry<string> sortByWeightKey;
        public static ConfigEntry<string> sortByValueKey;
        public static ConfigEntry<int> playerSortStartRow;
        public static ConfigEntry<int> playerSortEndRow;
        public static ConfigEntry<int> nexusID;

        public enum SortType
        {
            Name,
            Weight,
            Value
        }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            useGamePad = Config.Bind<bool>("General", "useGamePad", false, "Allow sorting with gamepad?");
            allowPlayerInventorySort = Config.Bind<bool>("General", "AllowPlayerInventorySort", true, "Allow player inventory sorting?");
            sortByValueKey = Config.Bind<string>("General", "SortByValueKey", "v", "Sort by value key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            sortByWeightKey = Config.Bind<string>("General", "SortByWeightKey", "h", "Sort by weight key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            sortByNameKey = Config.Bind<string>("General", "SortByNameKey", "n", "Sort by name key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            ascModKey = Config.Bind<string>("General", "AscModKey", "left alt", "Sort ascending mod key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            descModKey = Config.Bind<string>("General", "DescModKey", "left ctrl", "Sort descending mod key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            playerSortStartRow = Config.Bind<int>("General", "PlayerSortStartRow", 2, "Player sort start row");
            playerSortEndRow = Config.Bind<int>("General", "PlayerSortEndRow", -1, "Player sort end row (use -1 to sort to the end)");
            nexusID = Config.Bind<int>("General", "NexusID", 584, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony.UnpatchAll();
        }

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        static class InventoryGui_Update_Patch
        {
            static void Postfix(InventoryGui __instance, Container ___m_currentContainer, int ___m_activeGroup)
            {
                Vector3 mousePos = Input.mousePosition;
                if (!modEnabled.Value)
                    return;



                if (AedenthornUtils.CheckKeyHeld(ascModKey.Value, false) || AedenthornUtils.CheckKeyHeld(descModKey.Value, false))
                {
                    bool asc = AedenthornUtils.CheckKeyHeld(ascModKey.Value, false);
                    if (AedenthornUtils.CheckKeyHeld(descModKey.Value, true))
                        asc = false;

                    string name = "";
                    SortType type = SortType.Name;

                    PointerEventData eventData = new PointerEventData(EventSystem.current)
                    {
                        position = mousePos
                    };
                    List<RaycastResult> raycastResults = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, raycastResults);
                    foreach (RaycastResult rcr in raycastResults)
                    {

                        if (rcr.gameObject.layer == LayerMask.NameToLayer("UI") && rcr.gameObject.name == "Bkg" && (rcr.gameObject.transform.parent.name == "Player" || rcr.gameObject.transform.parent.name == "Container"))
                        {
                            if (AedenthornUtils.CheckKeyDown(sortByNameKey.Value))
                            {
                                name = rcr.gameObject.transform.parent.name;
                                type = SortType.Name;
                                break;
                            }
                            else if (AedenthornUtils.CheckKeyDown(sortByWeightKey.Value))
                            {
                                name = rcr.gameObject.transform.parent.name;
                                type = SortType.Weight;
                                break;
                            }
                            else if (AedenthornUtils.CheckKeyDown(sortByValueKey.Value))
                            {
                                name = rcr.gameObject.transform.parent.name;
                                type = SortType.Weight;
                                break;
                            }
                            return;
                        }
                    }
                    if (name == "" && useGamePad.Value)
                    {
                        if (___m_activeGroup == 0 && !__instance.IsContainerOpen())
                            return;

                        if (___m_activeGroup == 0 || ___m_activeGroup == 1)
                        {
                            if (AedenthornUtils.CheckKeyDown(sortByNameKey.Value))
                            {
                                name = ___m_activeGroup == 0 ? "Container" : "Player";
                                type = SortType.Name;
                            }
                            else if (AedenthornUtils.CheckKeyDown(sortByWeightKey.Value))
                            {
                                name = ___m_activeGroup == 0 ? "Container" : "Player";
                                type = SortType.Weight;
                            }
                            else if (AedenthornUtils.CheckKeyDown(sortByValueKey.Value))
                            {
                                name = ___m_activeGroup == 0 ? "Container" : "Player";
                                type = SortType.Weight;
                            }
                        }
                    }

                    if (name == "" || (name == "Player" && !allowPlayerInventorySort.Value))
                        return;

                    Dbgl($"Sorting {name} inventory by name {(asc ? "asc" : "desc")}");
                    if (name == "Player")
                    {
                        switch (type)
                        {
                            case SortType.Name:
                                SortByName(Player.m_localPlayer.GetInventory(), asc, true);
                                break;
                            case SortType.Weight:
                                SortByWeight(Player.m_localPlayer.GetInventory(), asc, true);
                                break;
                            case SortType.Value:
                                SortByValue(Player.m_localPlayer.GetInventory(), asc, true);
                                break;

                        }
                    }
                    else if (name == "Container")
                    {
                        switch (type)
                        {
                            case SortType.Name:
                                SortByName(___m_currentContainer.GetInventory(), asc, false);
                                break;
                            case SortType.Weight:
                                SortByWeight(___m_currentContainer.GetInventory(), asc, false);
                                break;
                            case SortType.Value:
                                SortByValue(___m_currentContainer.GetInventory(), asc, false);
                                break;

                        }
                    }
                }
            }
        }

        private static void SortByName(Inventory inventory, bool asc, bool player)
        {
            var items = inventory.GetAllItems();
            int width = Traverse.Create(inventory).Field("m_width").GetValue<int>();
            int height = Traverse.Create(inventory).Field("m_width").GetValue<int>();
            items.Sort(delegate(ItemDrop.ItemData a, ItemDrop.ItemData b) { return CompareStrings(Localization.instance.Localize(a.m_shared.m_name), Localization.instance.Localize(b.m_shared.m_name), asc); });
            int idx = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (player && ((playerSortStartRow.Value > 1 && items[i].m_gridPos.y < playerSortStartRow.Value - 1) || (playerSortEndRow.Value > 0 && items[i].m_gridPos.y >= playerSortEndRow.Value)))
                    continue;

                int x = idx % width;
                int y = idx / width + (player ? playerSortStartRow.Value - 1 : 0);
                items[i].m_gridPos = new Vector2i(x, y);
                idx++;
            }
            Traverse.Create(inventory).Method("Changed").GetValue();
        }
        private static void SortByWeight(Inventory inventory, bool asc, bool player)
        {
            var items = inventory.GetAllItems();
            int width = Traverse.Create(inventory).Field("m_width").GetValue<int>();
            items.Sort(delegate (ItemDrop.ItemData a, ItemDrop.ItemData b) {
                if(a.m_shared.m_weight == b.m_shared.m_weight)
                {
                    if (a.m_shared.m_name == b.m_shared.m_name)
                        return CompareInts(b.m_stack, a.m_stack, asc);
                    return CompareStrings(Localization.instance.Localize(a.m_shared.m_name), Localization.instance.Localize(b.m_shared.m_name), asc);
                }
                return CompareFloats(a.m_shared.m_weight, b.m_shared.m_weight, asc); 
            });
            int idx = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (player && ((playerSortStartRow.Value > 1 && items[i].m_gridPos.y < playerSortStartRow.Value - 1) || (playerSortEndRow.Value > 0 && items[i].m_gridPos.y >= playerSortEndRow.Value)))
                    continue;


                int x = idx % width;
                int y = idx / width + (player ? playerSortStartRow.Value - 1 : 0);
                items[i].m_gridPos = new Vector2i(x, y);
                idx++;
            }
            Traverse.Create(inventory).Method("Changed").GetValue();
        }
        private static void SortByValue(Inventory inventory, bool asc, bool player)
        {
            var items = inventory.GetAllItems();
            int width = Traverse.Create(inventory).Field("m_width").GetValue<int>();
            items.Sort(delegate (ItemDrop.ItemData a, ItemDrop.ItemData b) {
                if (a.m_shared.m_value == b.m_shared.m_value)
                {
                    if (a.m_shared.m_name == b.m_shared.m_name)
                        return CompareInts(b.m_stack, a.m_stack, asc);
                    return CompareStrings(Localization.instance.Localize(a.m_shared.m_name), Localization.instance.Localize(b.m_shared.m_name), asc);
                }
                return CompareInts(a.m_shared.m_value, b.m_shared.m_value, asc); 
            });
            int idx = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (player && ((playerSortStartRow.Value > 1 && items[i].m_gridPos.y < playerSortStartRow.Value - 1) || (playerSortEndRow.Value > 0 && items[i].m_gridPos.y >= playerSortEndRow.Value)))
                    continue;

                int x = idx % width;
                int y = idx / width + (player ? playerSortStartRow.Value - 1 : 0);
                items[i].m_gridPos = new Vector2i(x, y);
                idx++;
            }
            Traverse.Create(inventory).Method("Changed").GetValue();
        }

        public static int CompareStrings(string a, string b, bool asc)
        {
            if(asc)
                return a.CompareTo(b);
            else
                return b.CompareTo(a);
        }

        public static int CompareFloats(float a, float b, bool asc)
        {
            if (asc)
                return a.CompareTo(b);
            else
                return b.CompareTo(a);
        }

        public static int CompareInts(float a, float b, bool asc)
        {
            if (asc)
                return a.CompareTo(b);
            else
                return b.CompareTo(a);
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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
