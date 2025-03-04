using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MovableInventoryWindows
{
    [BepInPlugin("aedenthorn.MovableInventoryWindows", "Movable Inventory Windows", "0.4.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<Vector2> inventoryPosition;
        public static ConfigEntry<Vector2> chestInventoryPosition;
        public static ConfigEntry<Vector2> craftingPanelPosition;
        public static ConfigEntry<Vector2> infoPanelPosition;
        public static ConfigEntry<float> inventoryScale;
        public static ConfigEntry<float> chestInventoryScale;
        public static ConfigEntry<float> craftingPanelScale;
        public static ConfigEntry<float> infoPanelScale;
        public static ConfigEntry<string> modKeyOne;
        public static ConfigEntry<string> modKeyTwo;
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
            nexusID = Config.Bind<int>("General", "NexusID", 577, "Nexus mod ID for updates");

            inventoryScale = Config.Bind<float>("General", "InventoryScale", 1f, "Scale of inventory");
            chestInventoryScale = Config.Bind<float>("General", "ChestInventoryScale", 1f, "Scale of chest");
            craftingPanelScale = Config.Bind<float>("General", "CraftingPanelScale", 1f, "Scale of crafting panel");
            infoPanelScale = Config.Bind<float>("General", "InfoPanelScale", 1f, "Scale of crafting panel");
            modKeyOne = Config.Bind<string>("General", "ModKeyOne", "mouse 0", "First modifier key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            modKeyTwo = Config.Bind<string>("General", "ModKeyTwo", "left ctrl", "Second modifier key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");

            inventoryPosition = Config.Bind<Vector2>("ZPositions", "InventoryPosition", new Vector2(9999,9999), "Current position of inventory");
            chestInventoryPosition = Config.Bind<Vector2>("ZPositions", "ChestInventoryPosition", new Vector2(9999,9999), "Current position of chest");
            craftingPanelPosition = Config.Bind<Vector2>("ZPositions", "CraftingPanelPosition", new Vector2(9999,9999), "Current position of crafting panel");
            infoPanelPosition = Config.Bind<Vector2>("ZPositions", "InfoPanelPosition", new Vector2(9999,9999), "Current position of crafting panel");

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

        public static bool CheckKeyHeld(string value)
        {
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return false;
            }
        }

        public static Vector3 lastMousePos;
        public static string currentlyDragging;

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        public static class InventoryGui_Update_Patch
        {

            public static void Postfix(InventoryGui __instance)
            {
                Vector3 mousePos = Input.mousePosition;
                if (!modEnabled.Value)
                {
                    lastMousePos = mousePos;
                    return;
                }


                if (inventoryPosition.Value.x == 9999 && inventoryPosition.Value.y == 9999)
                    inventoryPosition.Value = __instance.m_player.anchorMin;

                __instance.m_player.anchorMin = inventoryPosition.Value; 
                __instance.m_player.anchorMax = inventoryPosition.Value;
                __instance.m_player.localScale = new Vector3(inventoryScale.Value, inventoryScale.Value, 1);

                if (chestInventoryPosition.Value.x == 9999 || chestInventoryPosition.Value.y == 9999)
                    chestInventoryPosition.Value = __instance.m_container.anchorMin;

                __instance.m_container.anchorMin = chestInventoryPosition.Value; 
                __instance.m_container.anchorMax = chestInventoryPosition.Value;
                __instance.m_container.localScale = new Vector3(chestInventoryScale.Value, chestInventoryScale.Value, 1);

                if (craftingPanelPosition.Value.x == 9999 || craftingPanelPosition.Value.y == 9999)
                    craftingPanelPosition.Value = __instance.m_player.parent.Find("Crafting").GetComponent<RectTransform>().anchorMin;

                __instance.m_player.parent.Find("Crafting").GetComponent<RectTransform>().anchorMin = craftingPanelPosition.Value; 
                __instance.m_player.parent.Find("Crafting").GetComponent<RectTransform>().anchorMax = craftingPanelPosition.Value;
                __instance.m_player.parent.Find("Crafting").GetComponent<RectTransform>().localScale = new Vector3(craftingPanelScale.Value, craftingPanelScale.Value, 1);

                if (infoPanelPosition.Value.x == 9999 || infoPanelPosition.Value.y == 9999)
                    infoPanelPosition.Value = __instance.m_infoPanel.GetComponent<RectTransform>().anchorMin;

                __instance.m_infoPanel.GetComponent<RectTransform>().anchorMin = infoPanelPosition.Value; 
                __instance.m_infoPanel.GetComponent<RectTransform>().anchorMax = infoPanelPosition.Value;
                __instance.m_infoPanel.GetComponent<RectTransform>().localScale = new Vector3(infoPanelScale.Value, infoPanelScale.Value, 1);

                if (lastMousePos == Vector3.zero)
                    lastMousePos = mousePos;


                PointerEventData eventData = new PointerEventData(EventSystem.current)
                {
                    position = lastMousePos
                };

                if (CheckKeyHeld(modKeyOne.Value) && CheckKeyHeld(modKeyTwo.Value) && lastMousePos != mousePos)
                {
                    List<RaycastResult> raycastResults = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, raycastResults);

                    foreach (RaycastResult rcr in raycastResults)
                    {

                        if (rcr.gameObject.layer == LayerMask.NameToLayer("UI") && rcr.gameObject.name == "Bkg")
                        {
                            if(IsDragging(rcr, "Player"))
                                    inventoryPosition.Value += new Vector2((mousePos.x - lastMousePos.x) / Screen.width,(mousePos.y - lastMousePos.y) / Screen.height);
                            if (IsDragging(rcr, "Container"))
                                    chestInventoryPosition.Value += new Vector2((mousePos.x - lastMousePos.x) / Screen.width, (mousePos.y - lastMousePos.y) / Screen.height);
                            if (IsDragging(rcr, "Crafting"))
                                    craftingPanelPosition.Value += new Vector2((mousePos.x - lastMousePos.x) / Screen.width, (mousePos.y - lastMousePos.y) / Screen.height);
                            if (IsDragging(rcr, "Info"))
                                    infoPanelPosition.Value += new Vector2((mousePos.x - lastMousePos.x) / Screen.width, (mousePos.y - lastMousePos.y) / Screen.height);
                        }
                    }
                }
                else
                {
                    currentlyDragging = "";
                }

                lastMousePos = mousePos;
            }
        }
        public static bool IsDragging(RaycastResult rcr, string name)
        {
            if(rcr.gameObject.transform.parent.name == name)
            {
                if (currentlyDragging == "" || currentlyDragging == name)
                {
                    currentlyDragging = name;
                    return true;
                }
                else
                    return false;
            }
            return false;
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
