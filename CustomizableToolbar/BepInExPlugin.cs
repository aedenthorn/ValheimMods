using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CustomizableToolbar
{
    [BepInPlugin("aedenthorn.CustomizableToolbar", "Customizable Toolbar", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> toolbarX;
        public static ConfigEntry<float> toolbarY;
        public static ConfigEntry<string> modKeyOne;
        public static ConfigEntry<string> modKeyTwo;
        public static ConfigEntry<int> itemsPerRow;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            itemsPerRow = Config.Bind<int>("General", "ItemsPerRow", 4, "Number of items per row in the toolbar");
            toolbarX = Config.Bind<float>("General", "ToolbarX", -1, "Current X of chest");
            toolbarY = Config.Bind<float>("General", "ToolbarY", -1, "Current Y of chest");
            modKeyOne = Config.Bind<string>("General", "ModKeyOne", "mouse 0", "First modifier key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            modKeyTwo = Config.Bind<string>("General", "ModKeyTwo", "left ctrl", "Second modifier key. Use https://docs.unity3d.com/Manual/class-InputManager.html format.");
            nexusID = Config.Bind<int>("General", "NexusID", 0, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;
            harmony = new Harmony("aedenthorn.RealClockMod");
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony.UnpatchAll();
        }

        private static bool CheckKeyHeld(string value)
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

        private static bool started = false;
        private static Vector3 lastMousePos;
        private static Vector2 defaultPosition;

        [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
        static class HotkeyBar_Update_Patch
        {
            static void Postfix(HotkeyBar __instance)
            {
                if (!started)
                {
                    defaultPosition = __instance.gameObject.GetComponent<RectTransform>().anchoredPosition;
                    int count = __instance.transform.childCount;

                    if(count > 0)
                        started = true;

                    for (int i = 0; i < count; i++)
                    {
                        int x = i % itemsPerRow.Value;
                        int y = i / itemsPerRow.Value;

                        Transform t = __instance.transform.GetChild(i);
                        t.GetComponent<RectTransform>().anchoredPosition = new Vector2(70 * x, -70 * y);
                        Dbgl($"element {i}, position {t.GetComponent<RectTransform>().anchoredPosition}");
                    }
                }
                Vector3 mousePos = Input.mousePosition;

                if (!modEnabled.Value)
                {
                    lastMousePos = mousePos;
                    return;
                }

                Vector2 position = __instance.gameObject.GetComponent<RectTransform>().anchoredPosition;

                if (toolbarX.Value < 0)
                    toolbarX.Value = __instance.gameObject.GetComponent<RectTransform>().anchorMin.x * Screen.width;
                if (toolbarY.Value < 0)
                    toolbarY.Value = __instance.gameObject.GetComponent<RectTransform>().anchorMax.y * Screen.height;

                __instance.gameObject.GetComponent<RectTransform>().anchorMax = new Vector2(__instance.gameObject.GetComponent<RectTransform>().anchorMax.x, toolbarY.Value / Screen.height);
                __instance.gameObject.GetComponent<RectTransform>().anchorMin = new Vector2(toolbarX.Value / Screen.width, __instance.gameObject.GetComponent<RectTransform>().anchorMin.y);

                if (lastMousePos == Vector3.zero)
                    lastMousePos = mousePos;


                PointerEventData eventData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                if (CheckKeyHeld(modKeyOne.Value) && CheckKeyHeld(modKeyTwo.Value))
                {
                    Rect rect = new Rect(__instance.gameObject.GetComponent<RectTransform>().anchorMin.x * Screen.width + __instance.gameObject.GetComponent<RectTransform>().anchoredPosition.x + 5, 
                        __instance.gameObject.GetComponent<RectTransform>().anchorMax.y * Screen.height - Mathf.CeilToInt(8f / itemsPerRow.Value) * 75 + __instance.gameObject.GetComponent<RectTransform>().anchoredPosition.y,
                        itemsPerRow.Value * 75, 
                        Mathf.CeilToInt(8f / itemsPerRow.Value) * 75 + __instance.gameObject.GetComponent<RectTransform>().anchoredPosition.y);

                    Dbgl($"Rect {rect}, mouse {mousePos}");

                    if (rect.Contains(mousePos))
                    {
                        Dbgl($"dragging");
                        toolbarX.Value += mousePos.x - lastMousePos.x;
                        toolbarY.Value += mousePos.y - lastMousePos.y;
                    }

                }

                lastMousePos = mousePos;
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
                if (text.ToLower().Equals("customizabletoolbar reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "customizable toolbar config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
