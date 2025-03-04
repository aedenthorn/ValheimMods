// Based on code made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using HarmonyLib;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ConfigurationManager
{
    public static class Patches
    {
        const string OpenLogString = "Show Player.log";
        public static GameObject OpenMenuButton { get; set; }

        internal static void ApplyPatches()
        {
            Harmony harmony = new Harmony(BepInExPlugin.GUID);

            harmony.Patch(
                original: AccessTools.Method(typeof(FejdStartup), "Start"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.Start)))
                );

            harmony.Patch(
                original: AccessTools.Method(typeof(Console), "InputText"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.InputText)))
                );

            BepInExPlugin._openMenuText.SettingChanged += (s,e) => SetupMenuButton();
            BepInExPlugin._showMenuButton.SettingChanged += (s,e) => SetupMenuButton();
        }

        internal static void SetupMenuButton()
        {
            if (OpenMenuButton == null && FejdStartup.instance?.m_mainMenu.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t => t.text == OpenLogString || t.text == Localization.instance.Localize(OpenLogString)) is Text openLogButton)
            {
                OpenMenuButton = Object.Instantiate(openLogButton.transform.parent.gameObject, openLogButton.transform.parent.parent);
                OpenMenuButton.name = "OpenConfigMenu";
                OpenMenuButton.transform.localPosition += new Vector3(0, 25, 0);
                OpenMenuButton.GetComponentInChildren<TMP_Text>().text = BepInExPlugin._openMenuText.Value;
                var button = OpenMenuButton.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => BepInExPlugin.context.DisplayingWindow = true);
            }

            OpenMenuButton.GetComponentInChildren<TMP_Text>().text = BepInExPlugin._openMenuText.Value;
            OpenMenuButton.SetActive(BepInExPlugin._showMenuButton.Value);
        }

        public static bool InputText(Console __instance)
        {
            string text = __instance.m_input.text;
            if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
            {
                BepInExPlugin.context.Config.Reload();
                BepInExPlugin.context.Config.Save();
                Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                Traverse.Create(__instance).Method("AddString", new object[] { $"{BepInExPlugin.context.Info.Metadata.Name} config reloaded" }).GetValue();
                return false;
            }
            return true;
        }

        public static void Start()
        {
            SetupMenuButton();
        }
    }
}