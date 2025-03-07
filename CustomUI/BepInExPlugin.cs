using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomUI
{
    [BepInPlugin("aedenthorn.CustomUI", "Custom UI", "0.8.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<float> toolbarX;
        public static ConfigEntry<float> toolbarY;
        public static ConfigEntry<float> toolbarItemScale;
        public static ConfigEntry<int> toolbarItemsPerRow;
        public static ConfigEntry<int> healthbarRotation;

        public static ConfigEntry<float> healthbarX;
        public static ConfigEntry<float> healthbarY;
        public static ConfigEntry<float> healthbarScale;

        public static ConfigEntry<float> guardianX;
        public static ConfigEntry<float> guardianY;
        public static ConfigEntry<float> guardianScale;

        public static ConfigEntry<float> mapX;
        public static ConfigEntry<float> mapY;
        public static ConfigEntry<float> mapScale;

        public static ConfigEntry<float> statusX;
        public static ConfigEntry<float> statusY;
        public static ConfigEntry<float> statusScale;

        public static ConfigEntry<float> quickSlotsX;
        public static ConfigEntry<float> quickSlotsY;
        public static ConfigEntry<float> quickSlotsScale;

        public static ConfigEntry<float> buildingX;
        public static ConfigEntry<float> buildingY;
        public static ConfigEntry<float> buildingScale;

        public static ConfigEntry<float> chatX;
        public static ConfigEntry<float> chatY;
        public static ConfigEntry<float> chatScale;

        public static ConfigEntry<string> modKeyOne;
        public static ConfigEntry<string> modKeyTwo;
        public static ConfigEntry<int> nexusID;

        public static int itemSize = 48;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            nexusID = Config.Bind<int>("General", "NexusID", 625, "Nexus mod ID for updates");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            modKeyOne = Config.Bind<string>("General", "ModKeyOne", "mouse 0", "First modifier key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html format.");
            modKeyTwo = Config.Bind<string>("General", "ModKeyTwo", "left ctrl", "Second modifier key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html format.");
            toolbarItemsPerRow = Config.Bind<int>("General", "ToolbarItemsPerRow", 8, "Number of items per row in the toolbar");
            toolbarItemsPerRow = Config.Bind<int>("General", "ToolbarItemsPerRow", 8, "Number of items per row in the toolbar");
            healthbarRotation = Config.Bind<int>("General", "HealthbarRotation", 0, "Rotation of healthbar. Must be a multiple of 90.");

            toolbarItemScale = Config.Bind<float>("Scale", "ToolbarItemScale", 1f, "Toolbar item scale");
            healthbarScale = Config.Bind<float>("Scale", "HealthbarScale", 1f, "Healthbar scale");
            guardianScale = Config.Bind<float>("Scale", "GuardianScale", 1f, "Guardian power scale");
            mapScale = Config.Bind<float>("Scale", "MapScale", 1f, "map scale");
            statusScale = Config.Bind<float>("Scale", "StatusScale", 1f, "status scale");
            quickSlotsScale = Config.Bind<float>("Scale", "QuickSlotsScale", 1f, "Quick slots scale");
            buildingScale = Config.Bind<float>("Scale", "BuildingScale", 1f, "Building scale");
            chatScale = Config.Bind<float>("Scale", "ChatScale", 1f, "Chat scale");
            
            toolbarX = Config.Bind<float>("ZCurrentPositions", "ToolbarX", 9999, "Current X of toolbar");
            toolbarY = Config.Bind<float>("ZCurrentPositions", "ToolbarY", 9999, "Current Y of toolbar");

            healthbarX = Config.Bind<float>("ZCurrentPositions", "HealthbarX", 9999, "Current X of healthbar");
            healthbarY = Config.Bind<float>("ZCurrentPositions", "HealthbarY", 9999, "Current Y of healthbar");

            guardianX = Config.Bind<float>("ZCurrentPositions", "GuardianX", 9999, "Current X of guardian power");
            guardianY = Config.Bind<float>("ZCurrentPositions", "GuardianY", 9999, "Current Y of guardian power");

            mapX = Config.Bind<float>("ZCurrentPositions", "MapX", 9999, "Current X of map");
            mapY = Config.Bind<float>("ZCurrentPositions", "MapY", 9999, "Current Y of map");

            quickSlotsX = Config.Bind<float>("ZCurrentPositions", "QuickSlotsX", 9999, "Current X of Quick Slots");
            quickSlotsY = Config.Bind<float>("ZCurrentPositions", "QuickSlotsY", 9999, "Current Y of Quick Slots");

            statusX = Config.Bind<float>("ZCurrentPositions", "StatusX", 9999, "Current X of status");
            statusY = Config.Bind<float>("ZCurrentPositions", "StatusY", 9999, "Current Y of status");

            chatX = Config.Bind<float>("ZCurrentPositions", "ChatX", 9999, "Current X of chat");
            chatY = Config.Bind<float>("ZCurrentPositions", "ChatY", 9999, "Current Y of chat");

            buildingX = Config.Bind<float>("ZCurrentPositions", "BuildingX", 9999, "Current X of building");
            buildingY = Config.Bind<float>("ZCurrentPositions", "BuildingY", 9999, "Current Y of building");

            toolbarItemsPerRow.Value = Mathf.Clamp(toolbarItemsPerRow.Value, 1, 8);

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

        [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
        public static class HotkeyBar_UpdateIcons_Patch
        {
            public static void Postfix(HotkeyBar __instance)
            {
                if (!modEnabled.Value || Player.m_localPlayer == null || __instance.name != "HotKeyBar")
                    return;

                int count = __instance.transform.childCount;

                float scaledSize = __instance.m_elementSpace * toolbarItemScale.Value;

                for (int i = 0; i < count; i++)
                {
                    int x = i % toolbarItemsPerRow.Value;
                    int y = i / toolbarItemsPerRow.Value;

                    Transform t = __instance.transform.GetChild(i);
                    t.GetComponent<RectTransform>().anchoredPosition = new Vector2(scaledSize * x, -scaledSize * y);
                    t.GetComponent<RectTransform>().localScale = new Vector3(toolbarItemScale.Value, toolbarItemScale.Value, 1);
                    //Dbgl($"element {i}, position {t.GetComponent<RectTransform>().anchoredPosition}");
                }
            }
        }

        [HarmonyPatch(typeof(Hud), "Update")]
        public static class Hud_Update_Patch
        {
            public static void Postfix(Hud __instance)
            {
                if (!modEnabled.Value || Player.m_localPlayer == null || InventoryGui.IsVisible() == true)
                    return;
                float gameScale = __instance.GetComponent<CanvasScaler>().scaleFactor;

                int healthRot = healthbarRotation.Value / 90 % 4 * 90;

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

                Rect healthRect = Rect.zero;
                switch (healthRot)
                {
                    case 0:
                        healthRect = new Rect(
                            (hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x) * gameScale,
                            (hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y) * gameScale - hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.y * gameScale * healthbarScale.Value,
                            hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.x * gameScale * healthbarScale.Value,
                            hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.y * gameScale * healthbarScale.Value
                        );
                        break;
                    case 90:
                        healthRect = new Rect(
                            (hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x) * gameScale,
                            (hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y) * gameScale,
                            hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.y * gameScale * healthbarScale.Value,
                            hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.x * gameScale * healthbarScale.Value
                        );
                        break;
                    case 180:
                        healthRect = new Rect(
                            (hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x) * gameScale - hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.x * gameScale * healthbarScale.Value,
                            (hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y) * gameScale,
                            hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.x * gameScale * healthbarScale.Value,
                            hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.y * gameScale * healthbarScale.Value
                        );
                        break;
                    case 270:
                        healthRect = new Rect(
                            (hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x) * gameScale - hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.y * gameScale * healthbarScale.Value,
                            (hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y) * gameScale - hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.x * gameScale * healthbarScale.Value,
                            hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.y * gameScale * healthbarScale.Value,
                            hudRoot.Find("healthpanel").GetComponent<RectTransform>().sizeDelta.x * gameScale * healthbarScale.Value
                        );
                        break;
                }
                if (hudRoot.Find("healthpanel").Find("Health").Find("QuickSlotsHotkeyBar"))
                {
                    hudRoot.Find("healthpanel").Find("Health").Find("QuickSlotsHotkeyBar").SetParent(hudRoot);
                    hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().anchoredPosition += new Vector2(healthRect.x, healthRect.y);
                    hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, 0);
                }


                if (CheckKeyHeld(modKeyOne.Value) && CheckKeyHeld(modKeyTwo.Value))
                {

                    Rect hotkeyRect = new Rect(
                        hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchorMin.x * Screen.width + hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchoredPosition.x * gameScale,
                        hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchorMax.y * Screen.height + hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchoredPosition.y * gameScale - hudRoot.Find("HotKeyBar").GetComponent<HotkeyBar>().m_elementSpace * gameScale * toolbarItemScale.Value * (7 / toolbarItemsPerRow.Value + 1),
                        hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().sizeDelta.x * gameScale * toolbarItemScale.Value * (toolbarItemsPerRow.Value / 8f),
                        hudRoot.Find("HotKeyBar").GetComponent<HotkeyBar>().m_elementSpace * gameScale * toolbarItemScale.Value * (7 / toolbarItemsPerRow.Value + 1)
                    );


                    Rect guardianRect = new Rect(
                        (hudRoot.Find("GuardianPower").GetComponent<RectTransform>().anchoredPosition.x + hudRoot.Find("GuardianPower").GetComponent<RectTransform>().rect.x * guardianScale.Value)* gameScale,
                        (hudRoot.Find("GuardianPower").GetComponent<RectTransform>().anchoredPosition.y + hudRoot.Find("GuardianPower").GetComponent<RectTransform>().rect.y * guardianScale.Value) * gameScale,
                        hudRoot.Find("GuardianPower").GetComponent<RectTransform>().sizeDelta.x * gameScale * guardianScale.Value,
                        hudRoot.Find("GuardianPower").GetComponent<RectTransform>().sizeDelta.y * gameScale * guardianScale.Value
                    );
                    Rect statusRect = new Rect(
                        Screen.width + (hudRoot.Find("StatusEffects").GetComponent<RectTransform>().anchoredPosition.x + hudRoot.Find("StatusEffects").GetComponent<RectTransform>().rect.x * statusScale.Value) * gameScale,
                        Screen.height + (hudRoot.Find("StatusEffects").GetComponent<RectTransform>().anchoredPosition.y + hudRoot.Find("StatusEffects").GetComponent<RectTransform>().rect.y * statusScale.Value) * gameScale,
                        hudRoot.Find("StatusEffects").GetComponent<RectTransform>().sizeDelta.x * gameScale * statusScale.Value,
                        hudRoot.Find("StatusEffects").GetComponent<RectTransform>().sizeDelta.y * gameScale * statusScale.Value
                    );
                    Rect mapRect = new Rect(
                        (hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().anchoredPosition.x - hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().sizeDelta.x * mapScale.Value) * gameScale + Screen.width,
                        (hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().anchoredPosition.y - hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().sizeDelta.y * mapScale.Value) * gameScale + Screen.height,
                        hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().sizeDelta.x * gameScale * mapScale.Value,
                        hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().sizeDelta.y * gameScale * mapScale.Value
                    );

                    Rect quickSlotsRect = Rect.zero;
                    if (hudRoot.Find("QuickSlotsHotkeyBar")?.GetComponent<RectTransform>() != null)
                    {
                        quickSlotsRect = new Rect(
                            hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().anchoredPosition.x * gameScale,
                            hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().anchoredPosition.y * gameScale + Screen.height - hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().sizeDelta.y * gameScale * quickSlotsScale.Value,
                            hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().sizeDelta.x * gameScale * quickSlotsScale.Value * (3/8f),
                            hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().sizeDelta.y * gameScale * quickSlotsScale.Value
                        );
                    }

                    Rect chatRect = new Rect(
                        Screen.width + (Chat.instance.m_chatWindow.anchoredPosition.x + Chat.instance.m_chatWindow.rect.x * chatScale.Value) * gameScale,
                        Chat.instance.m_chatWindow.anchoredPosition.y * gameScale,
                        Chat.instance.m_chatWindow.sizeDelta.x * gameScale * chatScale.Value,
                        Chat.instance.m_chatWindow.sizeDelta.y * gameScale * chatScale.Value
                    );

                    Rect buildRect = new Rect(
                        Screen.width / 2 + (Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().anchoredPosition.x + Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().rect.x * buildingScale.Value) * gameScale,
                        Screen.height / 2 + (Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().anchoredPosition.y + Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().rect.y * buildingScale.Value) * gameScale,
                        Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().rect.width * gameScale * buildingScale.Value,
                        Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().rect.height * gameScale * buildingScale.Value
                    );

                    if (hotkeyRect.Contains(lastMousePos) && (currentlyDragging == "" || currentlyDragging == "HotKeyBar"))
                    {
                        //Dbgl("in hotkeybar");
                        toolbarX.Value += mousePos.x - lastMousePos.x;
                        toolbarY.Value += mousePos.y - lastMousePos.y;
                        currentlyDragging = "HotKeyBar";
                    }
                    else if (healthRect.Contains(lastMousePos) && (currentlyDragging == "" || currentlyDragging == "healthpanel"))
                    {
                        
                        //Dbgl("in healthpanel");
                        healthbarX.Value += (mousePos.x - lastMousePos.x) / gameScale;
                        healthbarY.Value += (mousePos.y - lastMousePos.y) / gameScale;
                        currentlyDragging = "healthpanel";
                    }
                    else if (guardianRect.Contains(lastMousePos) && (currentlyDragging == "" || currentlyDragging == "GuardianPower"))
                    {
                        //Dbgl("in guardianPower");
                        guardianX.Value += (mousePos.x - lastMousePos.x) / gameScale;
                        guardianY.Value += (mousePos.y - lastMousePos.y) / gameScale;
                        currentlyDragging = "GuardianPower";
                    }
                    else if (mapRect.Contains(lastMousePos) && (currentlyDragging == "" || currentlyDragging == "MiniMap"))
                    {
                        //Dbgl("in MiniMap");
                        mapX.Value += (mousePos.x - lastMousePos.x) / gameScale;
                        mapY.Value += (mousePos.y - lastMousePos.y) / gameScale;
                        currentlyDragging = "MiniMap";
                    }
                    else if (statusRect.Contains(lastMousePos) && (currentlyDragging == "" || currentlyDragging == "StatusEffects"))
                    {
                        //Dbgl("in StatusEffects");
                        statusX.Value += (mousePos.x - lastMousePos.x) / gameScale;
                        statusY.Value += (mousePos.y - lastMousePos.y) / gameScale;
                        currentlyDragging = "StatusEffects";
                    }
                    else if (quickSlotsRect.Contains(lastMousePos) && (currentlyDragging == "" || currentlyDragging == "QuickSlots"))
                    {
                        //Dbgl("in QuickSlots");
                        quickSlotsX.Value += (mousePos.x - lastMousePos.x) / gameScale;
                        quickSlotsY.Value += (mousePos.y - lastMousePos.y) / gameScale;
                        currentlyDragging = "QuickSlots";
                    }
                    else if (Chat.instance.m_chatWindow.gameObject.activeInHierarchy && chatRect.Contains(lastMousePos) && (currentlyDragging == "" || currentlyDragging == "Chat"))
                    {
                        //Dbgl("in QuickSlots");
                        chatX.Value += (mousePos.x - lastMousePos.x) / gameScale;
                        chatY.Value += (mousePos.y - lastMousePos.y) / gameScale;
                        currentlyDragging = "Chat";
                    }
                    else if (Hud.instance.m_pieceSelectionWindow.activeSelf && buildRect.Contains(lastMousePos) && (currentlyDragging == "" || currentlyDragging == "Building"))
                    {
                        //Dbgl("in QuickSlots");
                        buildingX.Value += (mousePos.x - lastMousePos.x) / gameScale;
                        buildingY.Value += (mousePos.y - lastMousePos.y) / gameScale;
                        currentlyDragging = "Building";
                    }
                    else
                    {
                        //Dbgl($"mouse {mousePos}, hotkey rect {hotkeyRect}, health rect {healthRect}, guardian rect {guardianRect}, map rect {mapRect}, chat rect {chatRect}");
                        Dbgl($"mouse {mousePos} build rect {buildRect} game scale {gameScale}");
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
            int healthRot = healthbarRotation.Value / 90 % 4 * 90;

            if (toolbarX.Value == 9999)
                toolbarX.Value = hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchorMin.x * Screen.width;
            if (toolbarY.Value == 9999)
                toolbarY.Value = hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchorMax.y * Screen.height;

            if (healthbarX.Value == 9999)
                healthbarX.Value = hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x;
            if (healthbarY.Value == 9999)
                healthbarY.Value = hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y;

            if (guardianX.Value == 9999)
                guardianX.Value = hudRoot.Find("GuardianPower").GetComponent<RectTransform>().anchoredPosition.x;
            if (guardianY.Value == 9999)
                guardianY.Value = hudRoot.Find("GuardianPower").GetComponent<RectTransform>().anchoredPosition.y;

            if (statusX.Value == 9999)
                statusX.Value = hudRoot.Find("StatusEffects").GetComponent<RectTransform>().anchoredPosition.x;
            if (statusY.Value == 9999)
                statusY.Value = hudRoot.Find("StatusEffects").GetComponent<RectTransform>().anchoredPosition.y;

            if (mapX.Value == 9999)
                mapX.Value = hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().anchoredPosition.x;
            if (mapY.Value == 9999)
                mapY.Value = hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().anchoredPosition.y;

            if (chatX.Value == 9999)
                chatX.Value = Chat.instance.m_chatWindow.anchoredPosition.x;
            if (chatY.Value == 9999)
                chatY.Value = Chat.instance.m_chatWindow.anchoredPosition.y;

            if (buildingX.Value == 9999)
                buildingX.Value = Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().anchoredPosition.x;
            if (buildingY.Value == 9999)
                buildingY.Value = Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().anchoredPosition.y;

            hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchorMax = new Vector2(hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchorMax.x, toolbarY.Value / Screen.height);
            hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchorMin = new Vector2(toolbarX.Value / Screen.width, hudRoot.Find("HotKeyBar").GetComponent<RectTransform>().anchorMin.y);

            hudRoot.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition = new Vector2(healthbarX.Value, healthbarY.Value);
            hudRoot.Find("healthpanel").GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, healthRot);
            hudRoot.Find("healthpanel").Find("Health").Find("fast").Find("bar").Find("HealthText").GetComponent<RectTransform>().eulerAngles = Vector3.zero;
            hudRoot.Find("healthpanel").Find("food0").GetComponent<RectTransform>().eulerAngles = Vector3.zero;
            hudRoot.Find("healthpanel").Find("food1").GetComponent<RectTransform>().eulerAngles = Vector3.zero;
            hudRoot.Find("healthpanel").Find("food2").GetComponent<RectTransform>().eulerAngles = Vector3.zero;

            hudRoot.Find("GuardianPower").GetComponent<RectTransform>().anchoredPosition = new Vector2(guardianX.Value, guardianY.Value);

            hudRoot.Find("StatusEffects").GetComponent<RectTransform>().anchoredPosition = new Vector2(statusX.Value, statusY.Value);

            hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().anchoredPosition = new Vector2(mapX.Value, mapY.Value);

            hudRoot.Find("healthpanel").GetComponent<RectTransform>().localScale = new Vector3(healthbarScale.Value, healthbarScale.Value, 1);
            hudRoot.Find("GuardianPower").GetComponent<RectTransform>().localScale = new Vector3(guardianScale.Value, guardianScale.Value, 1);
            hudRoot.Find("StatusEffects").GetComponent<RectTransform>().localScale = new Vector3(statusScale.Value, statusScale.Value, 1);
            hudRoot.Find("MiniMap").Find("small").GetComponent<RectTransform>().localScale = new Vector3(mapScale.Value, mapScale.Value, 1);

            Chat.instance.m_chatWindow.anchoredPosition = new Vector2(chatX.Value, chatY.Value);
            Chat.instance.m_chatWindow.localScale = new Vector3(chatScale.Value, chatScale.Value, 1);

            Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(buildingX.Value, buildingY.Value);
            Hud.instance.m_pieceSelectionWindow.GetComponent<RectTransform>().localScale = new Vector3(buildingScale.Value, buildingScale.Value, 1);


            if (hudRoot.Find("QuickSlotsHotkeyBar")?.GetComponent<RectTransform>() != null)
            {
                if (quickSlotsX.Value == 9999)
                    quickSlotsX.Value = hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().anchoredPosition.x;
                if (quickSlotsY.Value == 9999)
                    quickSlotsY.Value = hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().anchoredPosition.y;

                hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().anchoredPosition = new Vector2(quickSlotsX.Value, quickSlotsY.Value);

                hudRoot.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().localScale = new Vector3(quickSlotsScale.Value, quickSlotsScale.Value, 1);
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
