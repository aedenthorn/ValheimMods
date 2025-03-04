using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace AdvancedSigns
{
    [BepInPlugin("aedenthorn.AdvancedSigns", "Advanced Signs", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<bool> useRichText;
        public static ConfigEntry<string> fontName;
        public static ConfigEntry<string> defaultColor;
        public static ConfigEntry<bool> removeRichText;
        public static ConfigEntry<Vector2> textPositionOffset;
        public static ConfigEntry<Vector3> signScale;
        public static TMP_FontAsset currentFont;
        public static string lastFontName;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 846, "Nexus mod ID for updates");

            signScale = Config.Bind<Vector3>("Signs", "SignScale", new Vector3(1,1,1), "Sign scale (w,h,d)");
            textPositionOffset = Config.Bind<Vector2>("Signs", "TextPositionOffset", new Vector2(0,0), "Default font size");
            useRichText = Config.Bind<bool>("Signs", "UseRichText", true, "Enable rich text");
            fontName = Config.Bind<string>("Signs", "FontName", "AveriaSerifLibre-Bold", "Font name");
            defaultColor = Config.Bind<string>("Signs", "DefaultColor", "#00ffffff", "Default color");
            removeRichText = Config.Bind<bool>("Signs", "RemoveRichText", false, "Remove rich text");
            
            if (!modEnabled.Value)
                return;

            currentFont = GetFont(fontName.Value, 20);
            lastFontName = currentFont?.name;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        [HarmonyPatch(typeof(Sign), "Awake")]
        public static class Sign_Awake_Patch
        {
            public static void Postfix(Sign __instance)
            {
                FixSign(ref __instance);
            }
        }
        [HarmonyPatch(typeof(Sign), "UpdateText")]
        public static class Sign_UpdateText_Patch
        {
            public static bool Prefix(Sign __instance, ZNetView ___m_nview)
            {
                string text = ___m_nview.GetZDO().GetString(ZDOVars.s_text, __instance.m_defaultText);
                if (!string.IsNullOrEmpty(text))
                {
                    if (removeRichText.Value)
                    {
                        if (text.Contains("<"))
                        {
                            text = text.RemoveRichTextTags();
                            ___m_nview.GetZDO().Set(ZDOVars.s_text, text);
                        }

                    } else if (!text.Contains("<") && !string.IsNullOrEmpty(defaultColor.Value)) {
                        text = $"<color={defaultColor.Value}>{text}";
                        ___m_nview.GetZDO().Set(ZDOVars.s_text, text);
                    }
                }

                return true;
            }

            public static void Postfix(Sign __instance)
            {
                FixSign(ref __instance);
            }
        }
        public static void FixSign(ref Sign sign)
        {
            if (!modEnabled.Value)
                return;

            sign.transform.localScale = signScale.Value;

            sign.m_textWidget.richText = useRichText.Value;
            sign.m_characterLimit = 0;
            sign.m_textWidget.material = null;
            //sign.m_textWidget.fontSize = fontSize.Value;
            sign.m_textWidget.gameObject.GetComponent<RectTransform>().anchoredPosition = textPositionOffset.Value;
            if (lastFontName != fontName.Value) // call when config changes
            {
                lastFontName = fontName.Value;
                Dbgl($"new font {fontName.Value}");
                TMP_FontAsset font = GetFont(fontName.Value, 20);
                if (font == null)
                    Dbgl($"new font not found");
                else
                    currentFont = font;
            }
            if (currentFont != null && sign.m_textWidget.font?.name != currentFont.name)
            {
                Dbgl($"setting font {currentFont.name}");
                sign.m_textWidget.font = currentFont;
            }
        }
        public static TMP_FontAsset GetFont(string fontName, int fontSize)
        {
            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (TMP_FontAsset font in fonts)
            {
                if (font.name == fontName)
                {
                    return font;
                }
            }
            return null;
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
