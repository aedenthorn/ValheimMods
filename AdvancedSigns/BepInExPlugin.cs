using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AdvancedSigns
{
    [BepInPlugin("aedenthorn.AdvancedSigns", "Advanced Signs", "0.1.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<bool> useRichText;
        public static ConfigEntry<string> fontName;
        public static ConfigEntry<Vector2> textPositionOffset;
        public static ConfigEntry<Vector3> signScale;
        public static Font currentFont;
        public static string lastFontName;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 846, "Nexus mod ID for updates");

            signScale = Config.Bind<Vector3>("Signs", "SignScale", new Vector3(1,1,1), "Sign scale (w,h,d)");
            textPositionOffset = Config.Bind<Vector2>("Signs", "TextPositionOffset", new Vector2(0,0), "Default font size");
            useRichText = Config.Bind<bool>("Signs", "UseRichText", true, "Enable rich text");
            fontName = Config.Bind<string>("Signs", "FontName", "AveriaSerifLibre-Bold", "Font name");
            
            if (!modEnabled.Value)
                return;

            currentFont = GetFont(fontName.Value, 20);
            lastFontName = currentFont?.name;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        [HarmonyPatch(typeof(Sign), "Awake")]
        static class Sign_Awake_Patch
        {
            static void Postfix(Sign __instance)
            {
                FixSign(ref __instance);
            }
        }
        [HarmonyPatch(typeof(Sign), "UpdateText")]
        static class Sign_UpdateText_Patch
        {
            static void Postfix(Sign __instance)
            {
                FixSign(ref __instance);
            }
        }
        private static void FixSign(ref Sign sign)
        {
            if (!modEnabled.Value)
                return;

            sign.transform.localScale = signScale.Value;

            sign.m_textWidget.supportRichText = useRichText.Value;
            sign.m_characterLimit = 0;
            sign.m_textWidget.material = null;
            //sign.m_textWidget.fontSize = fontSize.Value;
            sign.m_textWidget.gameObject.GetComponent<RectTransform>().anchoredPosition = textPositionOffset.Value;
            if (lastFontName != fontName.Value) // call when config changes
            {
                lastFontName = fontName.Value;
                Dbgl($"new font {fontName.Value}");
                Font font = GetFont(fontName.Value, 20);
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
        private static Font GetFont(string fontName, int fontSize)
        {
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            foreach (Font font in fonts)
            {
                if (font.name == fontName)
                {
                    return font;
                }
            }
            return Font.CreateDynamicFontFromOSFont(fontName, fontSize);
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
