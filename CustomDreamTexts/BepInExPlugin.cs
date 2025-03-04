using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace CustomDreamTexts
{
    [BepInPlugin("aedenthorn.CustomDreamTexts", "Custom Dream Texts", "0.2.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> currentDreamFolder;
        public static ConfigEntry<string> quoteAuthorSeparator;
        public static ConfigEntry<string> fontName;
        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<float> defaultChance;
        public static ConfigEntry<Color> textColor;

        public static BepInExPlugin context;
        public static TMP_FontAsset currentFont;
        public static string lastFontName;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            quoteAuthorSeparator = Config.Bind<string>("Conversion", "QuoteAuthorSeparator", "\r\n\r\n-- ", "String to separate quote and author.");
            defaultChance = Config.Bind<float>("Conversion", "DefaultChance", 1.0f, "Default dream chance when converting from text file (use values between 0 and 1).");
            
            fontName = Config.Bind<string>("Text", "DreamFont", "AveriaSansLibre-Bold", "Font to use for dream texts.");
            fontSize = Config.Bind<int>("Text", "FontSize", 32, "Font size for dream texts.");
            textColor = Config.Bind<Color>("Text", "TextColor", new Color(0.1470588f, 0.7529414f, 1, 1), "Color to use for dream texts.");
            
            currentDreamFolder = Config.Bind<string>("General", "CurrentDreamFolder", "Default", "Current folder to use for dream texts.");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Show debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 989, "Nexus mod ID for updates");
            
            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }



        [HarmonyPatch(typeof(Hud), "Awake")]
        public static class Hud_Awake_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                LoadDreams();
            }
        }

        public static void LoadDreams()
        {
            if (!Hud.instance)
                return;

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomDreamTexts");

            if (!Directory.Exists(path))
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
            }
            string defaultFolder = Path.Combine(path, "Default");

            SleepText sleepText = Hud.instance.m_sleepingProgress.GetComponent<SleepText>();
            DreamTexts dreamTexts = sleepText.m_dreamTexts;

            if (!Directory.Exists(defaultFolder))
            {
                Dbgl($"Directory {defaultFolder} does not exist! Creating.");
                Directory.CreateDirectory(defaultFolder);

                List<string> localizedDreams = new List<string>();
                for (int i = 0; i < dreamTexts.m_texts.Count; i++)
                {
                    DreamTexts.DreamText dt = dreamTexts.m_texts[i];
                    localizedDreams.Add(dt.m_text + ": " + Localization.instance.Localize(dt.m_text));
                    string json = JsonUtility.ToJson(dt);
                    File.WriteAllText(Path.Combine(defaultFolder, (i + 1) + ".json"), json);
                }
                File.WriteAllLines(Path.Combine(defaultFolder, "localized.txt"), localizedDreams);
            }

            var files = Directory.GetFiles(path, "*.txt");

            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string folder = Path.Combine(path, name);
                if (Directory.Exists(folder))
                    continue;
                Directory.CreateDirectory(folder);
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split('|');
                    string text;
                    if (parts[0].Contains("^"))
                    {
                        string[] quoteAuthor = parts[0].Split('^');
                        text = quoteAuthor[0] + quoteAuthorSeparator.Value + quoteAuthor[1];

                    }
                    else
                        text = parts[0];

                    float chance = parts.Length > 1 && parts[1].Length > 0 ? float.Parse(parts[1]) : defaultChance.Value;

                    DreamTexts.DreamText dt = new DreamTexts.DreamText()
                    {
                        m_chanceToDream = chance,
                        m_text = text
                    };
                    string json = JsonUtility.ToJson(dt);
                    File.WriteAllText(Path.Combine(folder, (i + 1) + ".json"), json);
                }
            }

            if (currentDreamFolder.Value != "Default")
            {
                string dreamFolder = Path.Combine(path, currentDreamFolder.Value);
                if (!Directory.Exists(dreamFolder))
                {
                    Dbgl("Dream folder does not exist!");
                    return;
                }
                sleepText.m_dreamTexts.m_texts.Clear();
                int count = 1;
                string thisDream = Path.Combine(dreamFolder, (count++) + ".json");
                while (File.Exists(thisDream))
                {
                    string dream = File.ReadAllText(thisDream);
                    sleepText.m_dreamTexts.m_texts.Add(JsonUtility.FromJson<DreamTexts.DreamText>(dream));
                    thisDream = Path.Combine(dreamFolder, (count++) + ".json");
                }
                Dbgl($"Loaded {sleepText.m_dreamTexts.m_texts.Count} dreams");
            }
        }

        [HarmonyPatch(typeof(SleepText), "ShowDreamText")]
        public static class ShowDreamText_Patch
        {

            public static void Prefix(SleepText __instance)
            {
                if (!modEnabled.Value)
                    return;

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
                if(currentFont != null)
                    __instance.m_dreamField.font = currentFont;
                    
                __instance.m_dreamField.fontSize = fontSize.Value;
                __instance.m_dreamField.color = textColor.Value;

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