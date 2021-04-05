// Based on code made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using HarmonyLib;
using System.Linq;
using UnityEngine.UI;

namespace ConfigurationManager
{
    static class Patches
    {
        const string OpenLogString = "Show Player.log";
        private static Text OpenLogButton { get; set; }
        internal static void ApplyPatches()
        {
            Harmony harmony = new Harmony(BepInExPlugin.GUID);

            harmony.Patch(
                original: AccessTools.Method(typeof(FejdStartup), "OnButtonShowLog"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.OnButtonShowLog)))
                );

            harmony.Patch(
                original: AccessTools.Method(typeof(FejdStartup), "Start"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.Start)))
                );

            harmony.Patch(
                original: AccessTools.Method(typeof(Console), "InputText"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.InputText)))
                );

            BepInExPlugin._openMenuText.SettingChanged += (s,e) => ResetButtonText();
            BepInExPlugin._replaceLogButton.SettingChanged += (s,e) => ResetButtonText();
        }

        internal static void ResetButtonText()
        {
            if (OpenLogButton == null)
                OpenLogButton = FejdStartup.instance?.m_mainMenu.GetComponentsInChildren<Text>().FirstOrDefault(t => t.text == OpenLogString || t.text == Localization.instance.Localize(OpenLogString));

            if (OpenLogButton?.text is string)
                OpenLogButton.text = BepInExPlugin._replaceLogButton.Value ? BepInExPlugin._openMenuText.Value : Localization.instance.Localize(OpenLogString);
        }

        private static bool InputText(Console __instance)
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

        private static void Start()
        {
            ResetButtonText();
        }

        private static bool OnButtonShowLog(FejdStartup __instance)
        {
            if (!BepInExPlugin._replaceLogButton.Value || BepInExPlugin.context.DisplayingWindow)
                return true;

            BepInExPlugin.context.DisplayingWindow = true;
            return false;
        }
    }
}
