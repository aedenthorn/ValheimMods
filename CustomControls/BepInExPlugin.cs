using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CustomControls
{
    [BepInPlugin("aedenthorn.CustomControls", "Custom Controls", "0.1.3")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public static Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static Dictionary<string, ButtonInfo> allControls = new Dictionary<string, ButtonInfo>();
        public static Dictionary<string, ButtonInfo> customButtons = new Dictionary<string, ButtonInfo>();
        
        public static void Dbgl(object str, LogLevel level = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(level, str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("Config", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("Config", "Debug", true, "Enable Debug Logs");
            nexusID = Config.Bind<int>("Config", "NexusID", 3161, "Nexus mod ID for updates");

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();

        }

        public static void LoadButtons()
        {
            customButtons.Clear();
            var path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "controls.json");
            if (File.Exists(path))
            {
                try
                {
                    customButtons = JsonConvert.DeserializeObject<Dictionary<string, ButtonInfo>>(File.ReadAllText(path));
                    Dbgl($"Finished loading {customButtons.Count} buttons");
                }
                catch (Exception ex)
                {
                    Dbgl($"Error loading buttons:\n\n {ex}", LogLevel.Error);
                }

                path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "all_controls.json");
                if (!File.Exists(path))
                {
                    SaveButtons();
                }
            }
            else
            {
                SaveButtons();
            }
        }

        public static void SaveButtons()
        {
            var path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "all_controls.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(allControls, Newtonsoft.Json.Formatting.Indented));
            Dbgl($"Finished saving {allControls.Count} buttons");

            path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "all_paths.txt");
            if (!File.Exists(path))
            {
                List<string> list = new List<string>();
                MethodInfo KeyCodeToPath = AccessTools.Method(typeof(ZInput), "KeyCodeToPath");
                foreach (var k in Enum.GetValues(typeof(KeyCode)))
                {
                    var str = (string)KeyCodeToPath.Invoke(null, new object[] { k, false });
                    if(str != "<Keyboard>/None")
                        list.Add(str);
                }
                File.WriteAllLines(path, list);
            }
        }

        public static void SetButtons(ZInput zInput)
        {
            if (!modEnabled.Value)
                return;

            LoadButtons();

            Dictionary<string, ZInput.ButtonDef> m_buttons = AccessTools.FieldRefAccess<ZInput, Dictionary<string, ZInput.ButtonDef>>(zInput, "m_buttons");
            MethodInfo UnsubscribeButton = AccessTools.Method(typeof(ZInput), "UnsubscribeButton");
            MethodInfo AddButton = AccessTools.Method(typeof(ZInput), "AddButton");

            using (var enumerator = customButtons.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    try
                    {
                        var kvp = enumerator.Current;
                        if (m_buttons.TryGetValue(kvp.Key, out var buttonDef))
                        {
                            UnsubscribeButton.Invoke(zInput, new object[] { buttonDef });
                            buttonDef.ButtonAction.Disable();
                            m_buttons.Remove(buttonDef.Name);
                            AddButton.Invoke(zInput, new object[] { kvp.Value.name, kvp.Value.path, kvp.Value.altKey, kvp.Value.showHints, kvp.Value.rebindable, kvp.Value.repeatDelay, kvp.Value.repeatInterval });
                            //Dbgl($"Set button {enumerator.Current.Key}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Dbgl($"Error setting button {enumerator.Current.Key}: {ex}");
                    }
                }
            }
            zInput.Save();
            Dbgl("Finished setting buttons");
        }
        [HarmonyPatch(typeof(ZInput), "AddButton")]
        public static class ZInput_AddButton_Patch
        {
            public static void Postfix(string name, string path, bool altKey, bool showHints, bool rebindable, float repeatDelay, float repeatInterval)
            {
                if (modEnabled.Value)
                {
                    allControls[name] = new ButtonInfo()
                    {
                        name = name,
                        path = path,
                        altKey = altKey,
                        showHints = showHints,
                        rebindable = rebindable,
                        repeatDelay = repeatDelay,
                        repeatInterval = repeatInterval
                    };
                }
            }
        }
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.ChangeLayout))]
        public static class ZInput_ChangeLayout_Patch
        {
            public static void Postfix(ZInput __instance)
            {
                if (!modEnabled.Value)
                    return;

                SetButtons(__instance);
            }
        }
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.Reset))]
        public static class ZInput_Reset_Patch
        {
            public static void Postfix(ZInput __instance)
            {
                if (!modEnabled.Value)
                    return;

                SetButtons(__instance);
            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value || ZInput.instance is null)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    SetButtons(ZInput.instance);
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
    
}
