using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CustomizableToolbar
{
    [BepInPlugin("aedenthorn.CustomizableToolbar", "Customizable Toolbar", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> toolbarX;
        public static ConfigEntry<float> toolbarY;
        public static ConfigEntry<float> itemScale;
        public static ConfigEntry<string> modKeyOne;
        public static ConfigEntry<string> modKeyTwo;
        public static ConfigEntry<int> itemsPerRow;
        public static ConfigEntry<int> nexusID;

        public static int itemSize = 70;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            itemsPerRow = Config.Bind<int>("General", "ItemsPerRow", 8, "Number of items per row in the toolbar");
            toolbarX = Config.Bind<float>("General", "ToolbarX", -9999, "Current X of toolbar");
            toolbarY = Config.Bind<float>("General", "ToolbarY", -9999, "Current Y of toolbar");
            itemScale = Config.Bind<float>("General", "ItemScale", 1f, "Item scale");
            modKeyOne = Config.Bind<string>("General", "ModKeyOne", "mouse 0", "First modifier key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html format.");
            modKeyTwo = Config.Bind<string>("General", "ModKeyTwo", "left ctrl", "Second modifier key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html format.");
            nexusID = Config.Bind<int>("General", "NexusID", 569, "Nexus mod ID for updates");

            itemsPerRow.Value = Mathf.Clamp(itemsPerRow.Value, 1, 8);

            if (!modEnabled.Value)
                return;
            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            if (!modEnabled.Value)
                return;

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

        [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
        public static class HotkeyBar_Update_Patch
        {
            public static void Postfix(HotkeyBar __instance)
            {
                if (!Player.m_localPlayer)
                    return;

                int count = __instance.transform.childCount;

                if (count != Player.m_localPlayer.GetInventory().GetWidth())
                    return;

                Vector3 mousePos = Input.mousePosition;

                if (!modEnabled.Value)
                {
                    lastMousePos = mousePos;
                    return;
                }

                float scaledSize = itemSize * itemScale.Value;

                for (int i = 0; i < count; i++)
                {
                    int x = i % itemsPerRow.Value;
                    int y = i / itemsPerRow.Value;

                    Transform t = __instance.transform.GetChild(i);
                    t.GetComponent<RectTransform>().anchoredPosition = new Vector2(scaledSize * x, -scaledSize * y);
                    t.GetComponent<RectTransform>().localScale = new Vector3(itemScale.Value, itemScale.Value, 1);
                    //Dbgl($"element {i}, position {t.GetComponent<RectTransform>().anchoredPosition}");
                }

                if (toolbarX.Value == -9999)
                    toolbarX.Value = __instance.gameObject.GetComponent<RectTransform>().anchorMin.x * Screen.width;
                if (toolbarY.Value == -9999)
                    toolbarY.Value = __instance.gameObject.GetComponent<RectTransform>().anchorMax.y * Screen.height;

                __instance.gameObject.GetComponent<RectTransform>().anchorMax = new Vector2(__instance.gameObject.GetComponent<RectTransform>().anchorMax.x, toolbarY.Value / Screen.height);
                __instance.gameObject.GetComponent<RectTransform>().anchorMin = new Vector2(toolbarX.Value / Screen.width, __instance.gameObject.GetComponent<RectTransform>().anchorMin.y);

                if (lastMousePos == Vector3.zero)
                    lastMousePos = mousePos;


                if (CheckKeyHeld(modKeyOne.Value) && CheckKeyHeld(modKeyTwo.Value))
                {
                    Rect rect = new Rect(__instance.gameObject.GetComponent<RectTransform>().anchorMin.x * Screen.width + 47,
                        __instance.gameObject.GetComponent<RectTransform>().anchorMax.y * Screen.height - Mathf.CeilToInt(8f / itemsPerRow.Value) * scaledSize * 1.5f - 44,
                        itemsPerRow.Value * scaledSize * 1.5f,
                        Mathf.CeilToInt(8f / itemsPerRow.Value) * scaledSize * 1.5f);

                    if (rect.Contains(lastMousePos))
                    {
                        toolbarX.Value += mousePos.x - lastMousePos.x;
                        toolbarY.Value += mousePos.y - lastMousePos.y;
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
