using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace BuildingHealthDisplay
{
    [BepInPlugin("aedenthorn.BuildingHealthDisplay", "Building Health Display", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> showHealthBar;
        public static ConfigEntry<bool> showHealthText;
        public static ConfigEntry<bool> showIntegrityText;

        public static ConfigEntry<bool> customHealthColors;
        public static ConfigEntry<bool> customIntegrityColors;

        public static ConfigEntry<string> healthText;
        public static ConfigEntry<string> integrityText;

        public static ConfigEntry<Vector2> healthTextPosition;
        public static ConfigEntry<Vector2> integrityTextPosition;

        public static ConfigEntry<Color> lowColor;
        public static ConfigEntry<Color> midColor;
        public static ConfigEntry<Color> highColor;
        public static ConfigEntry<Color> lowIntegrityColor;
        public static ConfigEntry<Color> midIntegrityColor;
        public static ConfigEntry<Color> highIntegrityColor;


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
            nexusID = Config.Bind<int>("General", "NexusID", 793, "Nexus mod ID for updates");

            showHealthBar = Config.Bind<bool>("General", "ShowHealthBar", true, "Show health bar?");
            showHealthText = Config.Bind<bool>("General", "ShowHealthText", true, "Show health text?");
            showIntegrityText = Config.Bind<bool>("General", "ShowIntegrityText", true, "Show integrity text?");
            customHealthColors = Config.Bind<bool>("General", "customHealthColors", true, "Use custom health colors?");
            customIntegrityColors = Config.Bind<bool>("General", "CustomIntegrityColors", true, "Use custom integrity colors?");

            lowColor = Config.Bind<Color>("General", "LowColor", Color.red, "Color used for low health.");
            midColor = Config.Bind<Color>("General", "MidColor", Color.yellow, "Color used for mid health.");
            highColor = Config.Bind<Color>("General", "HighColor", Color.green, "Color used for high health.");
            lowIntegrityColor = Config.Bind<Color>("General", "LowIntegrityColor", Color.red, "Color used for low integrity.");
            midIntegrityColor = Config.Bind<Color>("General", "MidIntegrityColor", Color.yellow, "Color used for mid integrity.");
            highIntegrityColor = Config.Bind<Color>("General", "HighIntegrityColor", Color.green, "Color used for high integrity.");
            healthText = Config.Bind<string>("General", "HealthText", "{0}/{1} ({2}%)", "Health text. {0} is replaced by current health. {1} is replaced by max health. {2} is replaced by percentage health.");
            integrityText = Config.Bind<string>("General", "IntegrityText", "{0}/{1} ({2}%)", "Health text. {0} is replaced by current health. {1} is replaced by max health. {2} is replaced by percentage health.");
            healthTextPosition = Config.Bind<Vector2>("General", "HealthTextPosition", new Vector2(0, 40), "Health text position offset.");
            integrityTextPosition = Config.Bind<Vector2>("General", "IntegrityText", new Vector2(0, -50), "Integrity text position offset.");

            if (!modEnabled.Value)
                return;
            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
        static class UpdateCrosshair_Patch
        {
            static void Postfix(Hud __instance, Player player)
            {
                if (!modEnabled.Value)
                    return;
                Piece hoveringPiece = player.GetHoveringPiece();
                if (hoveringPiece)
                {
                    WearNTear wnt = hoveringPiece.GetComponent<WearNTear>();
                    ZNetView znv = hoveringPiece.GetComponent<ZNetView>();
                    if (wnt && znv?.IsValid() == true)
                    {
                        __instance.m_pieceHealthBar.transform.parent.Find("bkg").gameObject.SetActive(showHealthBar.Value);
                        __instance.m_pieceHealthBar.transform.parent.Find("darken").gameObject.SetActive(showHealthBar.Value);
                        __instance.m_pieceHealthBar.gameObject.SetActive(showHealthBar.Value);
                        float healthPercent = wnt.GetHealthPercentage();
                        if (customHealthColors.Value)
                        {
                            __instance.m_pieceHealthBar.SetValue(wnt.GetHealthPercentage());
                            if(healthPercent < 0.5)
                                __instance.m_pieceHealthBar.SetColor(Color.Lerp(lowColor.Value, midColor.Value, healthPercent));
                            else
                                __instance.m_pieceHealthBar.SetColor(Color.Lerp(midColor.Value, highColor.Value, healthPercent - 0.5f));
                        }
                        if (showHealthText.Value)
                        {
                            Transform t = __instance.m_pieceHealthRoot.Find("_HealthText");
                            if (t == null)
                                t = Instantiate(__instance.m_healthText, __instance.m_pieceHealthRoot.transform).transform;
                            t.name = "_HealthText";

                            t.GetComponent<Text>().text = string.Format(healthText.Value, Mathf.RoundToInt(znv.GetZDO().GetFloat("health", wnt.m_health)), Mathf.RoundToInt(wnt.m_health), Mathf.FloorToInt(healthPercent*100));
                            t.GetComponent<Text>().resizeTextMaxSize = t.GetComponent<Text>().text.Length;
                            t.GetComponent<RectTransform>().anchoredPosition = new Vector2(healthTextPosition.Value.y, healthTextPosition.Value.x);
                        }
                        float support = Traverse.Create(wnt).Method("GetSupport").GetValue<float>();
                        float maxSupport = Traverse.Create(wnt).Method("GetMaxSupport").GetValue<float>();
                        if (showIntegrityText.Value && maxSupport >= support)
                        {
                            Transform t = __instance.m_pieceHealthRoot.Find("_IntegrityText");
                            if (t == null)
                                t = Instantiate(__instance.m_healthText, __instance.m_pieceHealthRoot.transform).transform;
                            t.name = "_IntegrityText";

                            t.GetComponent<Text>().text = string.Format(healthText.Value, Mathf.RoundToInt(support), Mathf.RoundToInt(maxSupport), Mathf.FloorToInt(support/maxSupport*100));
                            t.GetComponent<Text>().resizeTextMaxSize = t.GetComponent<Text>().text.Length;
                            t.GetComponent<RectTransform>().anchoredPosition = new Vector2(integrityTextPosition.Value.y, integrityTextPosition.Value.x);
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(WearNTear), "Highlight")]
        static class WearNTear_Highlight_Patch
        {
            static void Postfix(WearNTear __instance)
            {
                if (!modEnabled.Value || !customIntegrityColors.Value)
                    return;

                float supportColorValue = Traverse.Create(__instance).Method("GetSupportColorValue").GetValue<float>();
                Color color = new Color(0.6f, 0.8f, 1f);
                if (supportColorValue >= 0f)
                {
                    if (supportColorValue >= 0.5f)
                        color = Color.Lerp(midIntegrityColor.Value, highIntegrityColor.Value, supportColorValue - 0.5f);
                    else
                        color = Color.Lerp(lowIntegrityColor.Value, midIntegrityColor.Value, supportColorValue);

                    float h;
                    float s;
                    float v;
                    Color.RGBToHSV(color, out h, out s, out v);
                    s = Mathf.Lerp(1f, 0.5f, supportColorValue);
                    v = Mathf.Lerp(1.2f, 0.9f, supportColorValue);
                    color = Color.HSVToRGB(h, s, v);
                }

                foreach (Renderer renderer in Traverse.Create(__instance).Method("GetHighlightRenderers").GetValue<List<Renderer>>())
                {
                    foreach (Material material in renderer.materials)
                    {
                        material.SetColor("_EmissionColor", color * 0.4f);
                        material.color = color;
                    }
                }
            }
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
