using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NotificationTweaks
{
    [BepInPlugin("aedenthorn.NotificationTweaks", "Notification Tweaks", "0.5.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> ignoreKeywords;
        public static ConfigEntry<int> maxNotifications;
        public static ConfigEntry<float> smallSpeedMultiplier;
        public static ConfigEntry<float> largeSpeedMultiplier;
        public static ConfigEntry<int> smallNotificationSize;
        public static ConfigEntry<int> largeNotificationSize;
        public static ConfigEntry<Color> smallColor;
        public static ConfigEntry<Color> largeColor;
        public static ConfigEntry<Vector2> smallNotificationPosition;

        public enum NotificationType
        {
            TopRight,
            TopLeft,
            Center,
            BottomRight,
            BottomLeft
        }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 951, "Nexus mod ID for updates");
            ignoreKeywords = Config.Bind<string>("Notifications", "IgnoreKeywords", "", "Don't display notifications with any of these keywords or phrases (comma-separated)");
            smallNotificationPosition = Config.Bind<Vector2>("Notifications", "SmallNotificationPosition", new Vector2(40,170), "Small notification screen position");
            maxNotifications = Config.Bind<int>("Notifications", "MaxNotifications", 4, "Max number of notifications to show. Use -1 for unlimited");
            smallSpeedMultiplier = Config.Bind<float>("Notifications", "SmallSpeedMultiplier", 1f, "Make small notifications disappear faster by this multiplier (requires game restart)");
            largeSpeedMultiplier = Config.Bind<float>("Notifications", "LargeSpeedMultiplier", 1f, "Make large notifications disappear faster by this multiplier (requires game restart)");
            smallNotificationSize = Config.Bind<int>("Notifications", "SmallNotificationSize", 20, "Small notification font size");
            largeNotificationSize = Config.Bind<int>("Notifications", "LargeNotificationSize", 40, "Large notification font size");
            smallColor = Config.Bind<Color>("Notifications", "SmallNotificationColor", new Color(0.86f,0.86f,0.86f,1), "Small notification color");
            largeColor = Config.Bind<Color>("Notifications", "LargeNotificationColor", new Color(1f, 0.807f, 0, 1), "Large notification color");

            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }



        [HarmonyPatch(typeof(MessageHud), "Awake")]
        public static class Awake_Patch
        {
            public static void Prefix(MessageHud __instance)
            {
                if (!modEnabled.Value)
                    return;

                __instance.m_messageText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
            }
        }

        

        [HarmonyPatch(typeof(MessageHud), "ShowMessage")]
        public static class ShowMessage_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                if (!modEnabled.Value)
                    return instructions;

                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if(i > 2 && codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand  == 4 && codes[i - 3].opcode == OpCodes.Ldc_R4)
                    {
                        codes[i].operand = (float)codes[i].operand / largeSpeedMultiplier.Value;
                    }
                }
                return codes;
            }
        }
        

        [HarmonyPatch(typeof(MessageHud), "UpdateMessage")]
        public static class UpdateMessage_Patch
        {
            public static void Prefix(MessageHud __instance, ref float dt)
            {
                if (!modEnabled.Value)
                    return;
                __instance.m_messageText.fontSize = smallNotificationSize.Value;
                __instance.m_messageCenterText.fontSize = largeNotificationSize.Value;
                __instance.m_messageText.color = smallColor.Value;
                __instance.m_messageCenterText.color = largeColor.Value;
                __instance.m_messageText.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(smallNotificationPosition.Value.x, -smallNotificationPosition.Value.y);
            }
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                if (!modEnabled.Value)
                    return instructions;

                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if(i > 2 && codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand  == 4 && codes[i - 3].opcode == OpCodes.Ldc_R4)
                    {
                        codes[i].operand = (float)codes[i].operand / smallSpeedMultiplier.Value;
                    }
                }
                return codes;
            }

            public static void Postfix(MessageHud __instance)
            {
                if (!modEnabled.Value)
                    return;
                
                object obj = typeof(MessageHud).GetField("m_msgQeue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);

                IEnumerable<object> msgQueue = obj as IEnumerable<object>;

                if (msgQueue.Count() == 0)
                    return;

                __instance.m_messageText.CrossFadeAlpha(1, 0, true);

                object currentObj = typeof(MessageHud).GetField("currentMsg", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);

                FieldInfo amountfi = currentObj.GetType().GetField("m_amount", BindingFlags.Public | BindingFlags.Instance);
                FieldInfo textfi = currentObj.GetType().GetField("m_text", BindingFlags.Public | BindingFlags.Instance);

                string ctext = (string)textfi.GetValue(currentObj);

                Dictionary<string, object> amounts = new Dictionary<string, object>();
                string[] ignore = ignoreKeywords.Value.Split(',');
                foreach (object msg in msgQueue)
                {
                    int amount = (int)amountfi.GetValue(msg);
                    string text = (string)textfi.GetValue(msg);
                    foreach (string str in ignore)
                        if (text.Contains(str))
                            continue;
                    //if (amount > 1)
                    //    text = text.Replace(" x" + amount, "");
                    if(ctext == text)
                        amountfi.SetValue(currentObj, (int)amountfi.GetValue(currentObj) + amount);
                    else if (amounts.ContainsKey(text))
                        amountfi.SetValue(amounts[text], (int)amountfi.GetValue(amounts[text]) + amount);
                    else
                        amounts[text] = msg;
                }

                int camount = (int)amountfi.GetValue(currentObj);
                __instance.m_messageText.text = ctext + (camount > 1 ? " x" + camount : "");
                typeof(MessageHud).GetField("currentMsg", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, currentObj);

                obj.GetType().GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance).Invoke(obj, null);
                int count = 0;
                foreach (var kvp in amounts)
                {
                    if(count < (maxNotifications.Value < 0 ? msgQueue.Count() : maxNotifications.Value))
                    {
                        int amount = (int)amountfi.GetValue(kvp.Value);
                        string text = (string)textfi.GetValue(kvp.Value);
                        __instance.m_messageText.text = text + (amount > 1 ? " x" + amount : "") + "\n" + __instance.m_messageText.text;
                    }
                    count++;
                    obj.GetType().GetMethod("Enqueue", BindingFlags.Public | BindingFlags.Instance).Invoke(obj, new object[] { kvp.Value });
                }
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

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
