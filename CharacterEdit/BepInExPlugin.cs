using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CharacterEdit
{
    [BepInPlugin("aedenthorn.CharacterEdit", "Character Edit", "0.9.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static bool editingCharacter = false;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<string> titleText;
        public static ConfigEntry<string> buttonText;
        public static ConfigEntry<string> gamePadButton;
        public static ConfigEntry<string> gamePadButtonHint;
        public static ConfigEntry<int> nexusID;
        public static Transform title;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            buttonText = Config.Bind<string>("General", "ButtonText", "Edit", "Button text");
            titleText = Config.Bind<string>("General", "TitleText", "Edit Character", "Title text");
            gamePadButton = Config.Bind<string>("General", "GamePadButton", "JoyLTrigger", "Gamepad button used to press button. Possible values: JoyHide, JoyGPower, JoyRun, JoyCrouch, JoyMap, JoyMenu, JoyBlock, JoyAttack, JoySecondAttack, JoyAltPlace, JoyRotate, JoyPlace, JoyRemove, JoyTabLeft, JoyTabRight, JoyLStickLeft, JoyLStickRight, JoyLStickUp, JoyLStickDown, JoyDPadLeft, JoyDPadRight, JoyDPadUp, JoyDPadDown, JoyLTrigger, JoyRTrigger, JoyLStick, JoyRStick");
            gamePadButtonHint = Config.Bind<string>("General", "GamePadButtonHint", "LT", "Hint to show for gamepad button");
            nexusID = Config.Bind<int>("General", "NexusID", 650, "Nexus mod ID for updates");


            if (!modEnabled.Value)
                return;
            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(FejdStartup), "Awake")]
        public static class FejdStartup_Awake_Patch
        {

            public static void Postfix(FejdStartup __instance)
            {
                if (!modEnabled.Value)
                    return;

                var bw = FejdStartup.instance.m_selectCharacterPanel.transform.Find("BottomWindow");
                var edit = Instantiate(bw.Find("New"));
                edit.name = "Edit";
                edit.transform.SetParent(bw);
                edit.GetComponent<RectTransform>().anchoredPosition = new Vector3(-100, 50, 0);
                edit.transform.Find("Text").GetComponent<TMP_Text>().text = buttonText.Value;
                edit.GetComponent<Button>().onClick.RemoveAllListeners();
                edit.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                edit.GetComponent<Button>().onClick.AddListener(StartCharacterEdit);
                edit.GetComponent<UIGamePad>().m_zinputKey = gamePadButton.Value;
                edit.transform.Find("gamepad_hint").Find("Text").GetComponent<TextMeshProUGUI>().text = gamePadButtonHint.Value;

                title = Instantiate(FejdStartup.instance.m_newCharacterPanel.transform.Find("Topic"));
                title.name = "EditTitle";
                title.SetParent(FejdStartup.instance.m_newCharacterPanel.transform);
                title.GetComponent<TMP_Text>().text = titleText.Value;
                title.GetComponent<RectTransform>().anchoredPosition = FejdStartup.instance.m_newCharacterPanel.transform.Find("Topic").GetComponent<RectTransform>().anchoredPosition;
                title.gameObject.SetActive(false);
            }
        }
        [HarmonyPatch(typeof(FejdStartup), "OnNewCharacterDone")]
        public static class FejdStartup_OnNewCharacterDone_Patch
        {
            public static bool Prefix(FejdStartup __instance, ref List<PlayerProfile> ___m_profiles)
            {
                Dbgl($"New character done, editing {editingCharacter}");
                if (!editingCharacter)
                    return true;

                title.gameObject.SetActive(false);
                FejdStartup.instance.m_newCharacterPanel.transform.Find("Topic").gameObject.SetActive(true);

                editingCharacter = false;

                string text = __instance.m_csNewCharacterName.text;
                string text2 = text.ToLower();

                PlayerProfile playerProfile = Traverse.Create(FejdStartup.instance).Field("m_profiles").GetValue<List<PlayerProfile>>()[Traverse.Create(FejdStartup.instance).Field("m_profileIndex").GetValue<int>()];
                Player currentPlayerInstance = Traverse.Create(FejdStartup.instance).Field("m_playerInstance").GetValue<GameObject>().GetComponent<Player>();
                playerProfile.SavePlayerData(currentPlayerInstance);
                playerProfile.SetName(text);
                playerProfile.Save();

                __instance.m_selectCharacterPanel.SetActive(true);
                __instance.m_newCharacterPanel.SetActive(false);
                ___m_profiles = null;
                Traverse.Create(__instance).Method("SetSelectedProfile", new object[]{ text2 }).GetValue();
                return false;
            }
        }
        [HarmonyPatch(typeof(FejdStartup), "OnNewCharacterCancel")]
        public static class FejdStartup_OnNewCharacterCancel_Patch
        {
            public static void Postfix(FejdStartup __instance)
            {
                Dbgl($"New character cancel, editing {editingCharacter}");

                title.gameObject.SetActive(false);
                FejdStartup.instance.m_newCharacterPanel.transform.Find("Topic").gameObject.SetActive(true);

                editingCharacter = false;
            }
        }
        
        [HarmonyPatch(typeof(PlayerCustomizaton), "OnEnable")]
        public static class PlayerCustomizaton_OnEnable_Patch
        {
            public static void Postfix(PlayerCustomizaton __instance)
            {
                Dbgl($"Player customization enabled");
                if (!editingCharacter)
                    return;

                Dbgl($"is editing");

                Player player = __instance.GetComponentInParent<FejdStartup>().GetPreviewPlayer();
                if (player.GetPlayerModel() == 1)
                {
                    __instance.m_maleToggle.isOn = false;
                    __instance.m_femaleToggle.isOn = true;
                }

                VisEquipment ve = Traverse.Create(player).Field("m_visEquipment").GetValue<VisEquipment>();

                Vector3 skinColor = Traverse.Create(ve).Field("m_skinColor").GetValue<Vector3>();
                float skinValue = Vector3.Distance(skinColor, Utils.ColorToVec3(__instance.m_skinColor0)) / Vector3.Distance(Utils.ColorToVec3(__instance.m_skinColor1), Utils.ColorToVec3(__instance.m_skinColor0)) * (__instance.m_skinHue.maxValue - __instance.m_skinHue.minValue) + __instance.m_skinHue.minValue;
                __instance.m_skinHue.value = skinValue;

                /*
                Vector3 hairColor = Traverse.Create(ve).Field("m_hairColor").GetValue<Vector3>();
                float hairValue = Vector3.Distance(Utils.ColorToVec3(__instance.m_hairColor1), Utils.ColorToVec3(__instance.m_hairColor0)) / Vector3.Distance(hairColor, Utils.ColorToVec3(__instance.m_hairColor1)) * (__instance.m_hairTone.maxValue - __instance.m_hairTone.minValue) + __instance.m_hairTone.minValue;
                __instance.m_hairTone.value = hairValue;
                */
            }
        }

        public static void StartCharacterEdit()
        {
            Dbgl($"Start editing character");

            editingCharacter = true;

            title.gameObject.SetActive(true);
            FejdStartup.instance.m_newCharacterPanel.transform.Find("Topic").gameObject.SetActive(false);

            PlayerProfile playerProfile = Traverse.Create(FejdStartup.instance).Field("m_profiles").GetValue<List<PlayerProfile>>()[Traverse.Create(FejdStartup.instance).Field("m_profileIndex").GetValue<int>()];
            FejdStartup.instance.m_newCharacterPanel.SetActive(true);
            FejdStartup.instance.m_selectCharacterPanel.SetActive(false);
            FejdStartup.instance.m_csNewCharacterName.text = playerProfile.GetName();
            FejdStartup.instance.m_newCharacterError.SetActive(false);
            Traverse.Create(FejdStartup.instance).Method("SetupCharacterPreview", new object[] { playerProfile }).GetValue();
            Gogan.LogEvent("Screen", "Enter", "CreateCharacter", 0L);
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
