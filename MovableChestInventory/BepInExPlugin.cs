using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MovableChestInventory
{
    [BepInPlugin("aedenthorn.MovableChestInventory", "Movable Chest Inventory", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> chestInventoryX;
        public static ConfigEntry<float> chestInventoryY;
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
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            chestInventoryX = Config.Bind<float>("General", "ChestInventoryX", -1, "Current X of chest");
            chestInventoryY = Config.Bind<float>("General", "ChestInventoryY", -1, "Current Y of chest");
            modKeyOne = Config.Bind<string>("General", "ModKeyOne", "mouse 0", "First modifier key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            modKeyTwo = Config.Bind<string>("General", "ModKeyTwo", "left ctrl", "Second modifier key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            nexusID = Config.Bind<int>("General", "NexusID", 130, "Nexus mod ID for updates");

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

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        public static class InventoryGui_Update_Patch
        {
            public static void Postfix(InventoryGui __instance, Container ___m_currentContainer)
            {
                Vector3 mousePos = Input.mousePosition;
                if (!modEnabled.Value || !___m_currentContainer || !___m_currentContainer.IsOwner())
                {
                    lastMousePos = mousePos;
                    return;
                }


                if (chestInventoryX.Value < 0)
                    chestInventoryX.Value = __instance.m_container.anchorMin.x;
                if (chestInventoryY.Value < 0)
                    chestInventoryY.Value = __instance.m_container.anchorMin.y;

                __instance.m_container.anchorMin = new Vector2(chestInventoryX.Value, chestInventoryY.Value); 
                __instance.m_container.anchorMax = new Vector2(chestInventoryX.Value, chestInventoryY.Value); 


                if (lastMousePos == Vector3.zero)
                    lastMousePos = mousePos;


                PointerEventData eventData = new PointerEventData(EventSystem.current)
                {
                    position = lastMousePos
                };

                if (CheckKeyHeld(modKeyOne.Value) && CheckKeyHeld(modKeyTwo.Value))
                {
                    //Dbgl($"position {__instance.m_container.transform.parent.position}");

                    List<RaycastResult> raycastResults = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, raycastResults);

                    foreach (RaycastResult rcr in raycastResults)
                    {

                        if (rcr.gameObject.layer == LayerMask.NameToLayer("UI") && rcr.gameObject.name == "Bkg" && rcr.gameObject.transform.parent.name == "Container")
                        {
                            chestInventoryX.Value += (mousePos.x - lastMousePos.x) / Screen.width;
                            chestInventoryY.Value += (mousePos.y - lastMousePos.y) / Screen.height;
                        }
                    }

                }

                lastMousePos = mousePos;
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
                if (text.ToLower().Equals("movablechestinventory reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "movable chest inventory config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
