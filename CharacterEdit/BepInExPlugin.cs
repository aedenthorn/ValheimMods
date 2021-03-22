using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CharacterEdit
{
    [BepInPlugin("aedenthorn.CharacterEdit", "Character Edit", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static bool editingCharacter = false;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<string> buttonText;
        public static ConfigEntry<int> nexusID;

        public static int itemSize = 70;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            buttonText = Config.Bind<string>("General", "ButtonText", "Edit", "Button text");
            nexusID = Config.Bind<int>("General", "NexusID", 650, "Nexus mod ID for updates");


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

        [HarmonyPatch(typeof(FejdStartup), "Awake")]
        static class FejdStartup_Awake_Patch
        {
            static void Postfix(FejdStartup __instance)
            {
                if (!modEnabled.Value)
                    return;

                var edit = Instantiate(FejdStartup.instance.m_selectCharacterPanel.transform.Find("BottomWindow").Find("New"));
                edit.name = "Edit";
                edit.transform.parent = FejdStartup.instance.m_selectCharacterPanel.transform.Find("BottomWindow");
                edit.GetComponent<RectTransform>().anchoredPosition = new Vector3(-751, -50, 0);
                edit.transform.Find("Text").GetComponent<Text>().text = buttonText.Value;
                edit.GetComponent<Button>().onClick.RemoveAllListeners();
                edit.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                edit.GetComponent<Button>().onClick.AddListener(StartCharacterEdit);
            }
        }
        [HarmonyPatch(typeof(FejdStartup), "OnNewCharacterDone")]
        static class FejdStartup_OnNewCharacterDone_Patch
        {
            static bool Prefix(FejdStartup __instance)
            {
                Dbgl($"New character done, editing {editingCharacter}");
                if (!editingCharacter)
                    return true;

                editingCharacter = false;

                string text = __instance.m_csNewCharacterName.text;

                PlayerProfile playerProfile = Traverse.Create(FejdStartup.instance).Field("m_profiles").GetValue<List<PlayerProfile>>()[Traverse.Create(FejdStartup.instance).Field("m_profileIndex").GetValue<int>()];

                Player component = Traverse.Create(FejdStartup.instance).Field("m_playerInstance").GetValue<GameObject>().GetComponent<Player>();

                playerProfile.SetName(text);
                playerProfile.SavePlayerData(component);
                playerProfile.Save();
                __instance.m_selectCharacterPanel.SetActive(true);
                __instance.m_newCharacterPanel.SetActive(false);

                return false;
            }
        }
        [HarmonyPatch(typeof(FejdStartup), "OnNewCharacterCancel")]
        static class FejdStartup_OnNewCharacterCancel_Patch
        {
            static void Postfix(FejdStartup __instance)
            {
                Dbgl($"New character cancel, editing {editingCharacter}");

                editingCharacter = false;
            }
        }
        
        [HarmonyPatch(typeof(PlayerCustomizaton), "OnEnable")]
        static class PlayerCustomizaton_OnEnable_Patch
        {
            static void Postfix(PlayerCustomizaton __instance)
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

        private static void StartCharacterEdit()
        {
            Dbgl($"Start editing character");

            editingCharacter = true;
            PlayerProfile playerProfile = Traverse.Create(FejdStartup.instance).Field("m_profiles").GetValue<List<PlayerProfile>>()[Traverse.Create(FejdStartup.instance).Field("m_profileIndex").GetValue<int>()];
            FejdStartup.instance.m_newCharacterPanel.SetActive(true);
            FejdStartup.instance.m_selectCharacterPanel.SetActive(false);
            FejdStartup.instance.m_csNewCharacterName.text = playerProfile.GetName();
            FejdStartup.instance.m_newCharacterError.SetActive(false);
            Traverse.Create(FejdStartup.instance).Method("SetupCharacterPreview", new object[] { playerProfile }).GetValue();
            Gogan.LogEvent("Screen", "Enter", "CreateCharacter", 0L);
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
