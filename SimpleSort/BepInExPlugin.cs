using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSort
{
    [BepInPlugin("aedenthorn.SimpleSort", "Simple Sort", "0.9.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> useGamePad;
        public static ConfigEntry<bool> allowPlayerInventorySort;
        public static ConfigEntry<bool> playerStickySort;
        public static ConfigEntry<bool> containerStickySort;
        public static ConfigEntry<SortType> playerSortType;
        public static ConfigEntry<SortType> containerSortType;
        public static ConfigEntry<bool> playerSortAsc;
        public static ConfigEntry<bool> containerSortAsc;
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
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            useGamePad = Config.Bind<bool>("General", "useGamePad", false, "Allow sorting with gamepad?");
            allowPlayerInventorySort = Config.Bind<bool>("General", "AllowPlayerInventorySort", true, "Allow player inventory sorting?");
            playerStickySort = Config.Bind<bool>("General", "PlayerStickySort", true, "Automatically apply last sort type to player inventory on open");
            containerStickySort = Config.Bind<bool>("General", "ContainerStickySort", true, "Automatically apply last sort type to container inventory on open");
            playerSortAsc = Config.Bind<bool>("General", "playerSortAsc", true, "Current player sort method (changes automatically for sticky sorting)");
            containerSortAsc = Config.Bind<bool>("General", "containerSortAsc", true, "Current container sort method (changes automatically for sticky sorting)");
            playerSortType = Config.Bind<SortType>("General", "PlayerSortType", SortType.Name, "Current player sort type (changes automatically for sticky sorting)");
            containerSortType = Config.Bind<SortType>("General", "ContainerSortType", SortType.Name, "Current container sort type (changes automatically for sticky sorting)");
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

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony.UnpatchAll();
        }

        [HarmonyPatch(typeof(InventoryGui), "Show")]
        public static class InventoryGui_Show_Patch
        {
            public static void Postfix(Container ___m_currentContainer)
            {
                if (!modEnabled.Value)
                    return;
                if(Player.m_localPlayer && playerStickySort.Value && allowPlayerInventorySort.Value)
                {
                    SortByType(playerSortType.Value, Player.m_localPlayer.GetInventory(), playerSortAsc.Value, true);
                }
                if(containerStickySort.Value && ___m_currentContainer?.GetInventory() != null)
                {
                    SortByType(containerSortType.Value, ___m_currentContainer.GetInventory(), containerSortAsc.Value, false);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        public static class InventoryGui_Update_Patch
        {
            public static void Postfix(InventoryGui __instance, Container ___m_currentContainer, int ___m_activeGroup)
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
                                type = SortType.Value;
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
                                type = SortType.Value;
                            }
                        }
                    }

                    if (name == "" || (name == "Player" && !allowPlayerInventorySort.Value))
                        return;

                    Dbgl($"Sorting {name} inventory by {type} {(asc ? "asc" : "desc")}");
                    if (name == "Player")
                    {
                        playerSortType.Value = type;
                        playerSortAsc.Value = asc;
                        context.Config.Save();
                        SortByType(type, Player.m_localPlayer.GetInventory(), asc, true);
                    }
                    else if (name == "Container")
                    {
                        containerSortType.Value = type;
                        containerSortAsc.Value = asc;
                        context.Config.Save();
                        SortByType(type, ___m_currentContainer.GetInventory(), asc, false);
                    }
                }
            }
        }

        public static void SortByType(SortType type, Inventory inventory, bool asc, bool player)
        {
            // combine
            var items = inventory.GetAllItems();
            SortUtils.SortByName(items, true, player);

            for (int i = 0; i < items.Count; i++)
            {
                if (player && ((playerSortStartRow.Value > 1 && items[i].m_gridPos.y < playerSortStartRow.Value - 1) || (playerSortEndRow.Value > 0 && items[i].m_gridPos.y >= playerSortEndRow.Value)))
                    continue;

                while (i < items.Count - 1 && items[i].m_stack < items[i].m_shared.m_maxStackSize && items[i + 1].m_shared.m_name == items[i].m_shared.m_name)
                {
                    int amount = Mathf.Min(items[i].m_shared.m_maxStackSize - items[i].m_stack, items[i + 1].m_stack);
                    items[i].m_stack += amount;
                    if (amount == items[i + 1].m_stack)
                    {
                        items.RemoveAt(i + 1);
                    }
                    else
                        items[i + 1].m_stack -= amount;
                }
            }
            switch (type)
            {
                case SortType.Name:
                    SortUtils.SortByName(items, asc, player);
                    break;
                case SortType.Weight:
                    SortUtils.SortByWeight(items, asc, player);
                    break;
                case SortType.Value:
                    SortUtils.SortByValue(items, asc, player);
                    break;
            }
            SortToGrid(inventory, player);
        }
        public static void SortToGrid(Inventory inventory, bool player)
        {
            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            int width = Traverse.Create(inventory).Field("m_width").GetValue<int>();

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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
