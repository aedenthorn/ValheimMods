using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraftingFilter
{
    [BepInPlugin("aedenthorn.CraftingFilter", "Crafting Filter", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> useScrollWheel;
        public static ConfigEntry<bool> showMenu;
        public static ConfigEntry<string> scrollModKey;
        public static ConfigEntry<string> prevHotKey;
        public static ConfigEntry<string> nextHotKey;
        
        private static BepInExPlugin context;

        private static int lastItemTypeIndex = 0;
        private static List<ItemDrop.ItemData.ItemType> itemTypes;
        private static List<string> itemTypeNames;
        private static List<GameObject> dropDownList = new List<GameObject>();
        private Vector3 lastMousePos;
        private static bool isShowing = false;
        private static string craftText = "Craft";

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1219, "Nexus mod ID for updates");
            nexusID.Value = 1219;

            useScrollWheel = Config.Bind<bool>("Settings", "ScrollWheel", true, "Use scroll wheel to switch filter");
            showMenu = Config.Bind<bool>("Settings", "ShowMenu", true, "Show filter menu on hover");
            scrollModKey = Config.Bind<string>("Settings", "ScrollModKey", "", "Modifer key to allow scroll wheel change. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            
            prevHotKey = Config.Bind<string>("Settings", "HotKeyPrev", "", "Hotkey to switch to previous filter. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            nextHotKey = Config.Bind<string>("Settings", "HotKeyNext", "", "Hotkey to switch to next filter. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            itemTypes = ((ItemDrop.ItemData.ItemType[])Enum.GetValues(typeof(ItemDrop.ItemData.ItemType))).ToList();
            itemTypeNames = Enum.GetNames(typeof(ItemDrop.ItemData.ItemType)).ToList();

            itemTypes.Sort(delegate (ItemDrop.ItemData.ItemType a, ItemDrop.ItemData.ItemType b)
            {
                if ("" + a == "None")
                    return -1;
                if ("" + b == "None")
                    return 1;
                return ("" + a).CompareTo("" + b);
            });

            itemTypeNames.Sort(delegate (string a, string b)
            {
                if (a == "None")
                    return -1;
                if (b == "None")
                    return 1;
                return (a).CompareTo(b);
            });

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
            if (!modEnabled.Value || !Player.m_localPlayer || !InventoryGui.IsVisible() || (!Player.m_localPlayer.GetCurrentCraftingStation() && !Player.m_localPlayer.NoCostCheat()))
                return;

            bool hover = false;

            Vector3 mousePos = Input.mousePosition;

            if (lastMousePos == Vector3.zero)
                lastMousePos = mousePos;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = lastMousePos
            };

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            foreach (RaycastResult rcr in raycastResults)
            {
                if (rcr.gameObject.layer == LayerMask.NameToLayer("UI") && rcr.gameObject.name == "Craft" && rcr.gameObject.transform.parent.name == "TabsButtons")
                {
                    if (useScrollWheel.Value && AedenthornUtils.CheckKeyHeld(scrollModKey.Value, false) && Input.mouseScrollDelta.y != 0)
                    {
                        SwitchFilter(Input.mouseScrollDelta.y < 0);
                    }
                    else if (showMenu.Value)
                    {
                        UpdateDropDown(true);
                        hover = true;
                    }
                }
                else if (rcr.gameObject.layer == LayerMask.NameToLayer("UI") && rcr.gameObject.transform.parent.name == "TabsButtons" && itemTypeNames.Contains(rcr.gameObject.name))
                {
                    hover = true;
                }
            }

            if(!hover)
                UpdateDropDown(false);

            if (AedenthornUtils.CheckKeyDown(prevHotKey.Value))
            {
                SwitchFilter(false);
            }
            else if(AedenthornUtils.CheckKeyDown(nextHotKey.Value))
            {
                SwitchFilter(true);
            }

            lastMousePos = Input.mousePosition;
        }

        private static void SwitchFilter(int idx)
        {
            Dbgl($"switching to filter {idx}");

            lastItemTypeIndex = idx;
            UpdateDropDown(false);
            SwitchFilter();
        }

        private static void SwitchFilter(bool next)
        {
            Dbgl($"switching to {(next ? "next" : "last")} filter");

            if (next)
            {
                lastItemTypeIndex++;
                lastItemTypeIndex %= itemTypes.Count;
            }
            else
            {
                lastItemTypeIndex--;
                if (lastItemTypeIndex < 0)
                    lastItemTypeIndex = itemTypes.Count - 1;
            }
            List<Recipe> recipes = new List<Recipe>();
            Player.m_localPlayer.GetAvailableRecipes(ref recipes);
            int count = 0;
            while (itemTypeNames[lastItemTypeIndex] != "None" && recipes.FindAll(r => r.m_item.m_itemData.m_shared.m_itemType == itemTypes[lastItemTypeIndex]).Count == 0 && count < itemTypes.Count)
            {
                count++;
                SwitchFilter(next);
            }

            SwitchFilter();
        }

        private static void SwitchFilter()
        {

            List<Recipe> recipes = new List<Recipe>();
            Player.m_localPlayer.GetAvailableRecipes(ref recipes);
            Dbgl($"Switching to filter {itemTypes[lastItemTypeIndex]} {recipes.Count} total recipes ");
            Traverse t = Traverse.Create(InventoryGui.instance);
            t.Method("UpdateRecipeList", new object[] { recipes }).GetValue();
            t.Method("SetRecipe", new object[] { 0, true }).GetValue();
            InventoryGui.instance.m_tabCraft.gameObject.GetComponentInChildren<Text>().text = craftText + (itemTypes[lastItemTypeIndex] == ItemDrop.ItemData.ItemType.None ? "" : "\n" + itemTypeNames[lastItemTypeIndex]);
        }

        private static void GetFilteredRecipes(ref List<Recipe> recipes)
        {
            if(itemTypes[lastItemTypeIndex] != ItemDrop.ItemData.ItemType.None)
            {
                recipes = recipes.FindAll(r => r.m_item.m_itemData.m_shared.m_itemType == itemTypes[lastItemTypeIndex]); 
                Dbgl($"using filter {itemTypes[lastItemTypeIndex]} {recipes.Count} filtered recipes");
            }
        }


        private static void UpdateDropDown(bool show)
        {
            if (show == isShowing)
                return;
            if (show)
            {
                List<Recipe> recipes = new List<Recipe>();
                Player.m_localPlayer.GetAvailableRecipes(ref recipes);

                GameObject buttonObj = InventoryGui.instance.m_tabCraft.gameObject;
                RectTransform rt = buttonObj.GetComponent<RectTransform>();
                Text text = buttonObj.GetComponentInChildren<Text>();

                int showCount = 0;
                for (int i = 0; i < itemTypes.Count; i++)
                {
                    int count = recipes.FindAll(r => r.m_item.m_itemData.m_shared.m_itemType == itemTypes[i]).Count;
                    dropDownList[i].SetActive(count > 0 || itemTypeNames[i] == "None");
                    if (count > 0 || itemTypeNames[i] == "None")
                    {
                        dropDownList[i].GetComponent<RectTransform>().anchoredPosition = rt.anchoredPosition - new Vector2(0, rt.rect.height * (showCount++ + 1));
                        dropDownList[i].GetComponentInChildren<Text>().text = itemTypes[i] + (count == 0 ? "" : $" ({count})");
                    }
                }
            }
            else
            {
                for (int i = 0; i < itemTypes.Count; i++)
                {
                    dropDownList[i].SetActive(false);
                }

            }
            isShowing = show;
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList")]
        static class UpdateRecipeList_Patch
        {

            static void Prefix(ref List<Recipe> recipes)
            {
                if (!modEnabled.Value)
                    return;

                Dbgl($"updating recipes");

                GetFilteredRecipes(ref recipes);
            }
        }
        
        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        static class InventoryGui_Awake_Patch
        {
            static void Postfix(InventoryGui __instance)
            {
                if (!modEnabled.Value)
                    return;

                dropDownList.Clear();

                GameObject buttonObj = __instance.m_tabCraft.gameObject;
                buttonObj.transform.parent.SetAsLastSibling();
                RectTransform rt = buttonObj.GetComponent<RectTransform>();
                craftText = buttonObj.GetComponentInChildren<Text>().text;
                for (int i = 0; i < itemTypes.Count; i++)
                {
                    int idx = i;
                    GameObject go = Instantiate(buttonObj);
                    go.name = "" + itemTypes[i];
                    //go.GetComponentInChildren<Text>().text = go.name;
                    go.transform.SetParent(buttonObj.transform.parent);
                    go.GetComponent<Button>().interactable = true;
                    go.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                    go.GetComponent<Button>().onClick.AddListener(() => SwitchFilter(idx));
                    go.SetActive(false);
                    dropDownList.Add(go);
                }
            }
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
