using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmmoCount
{
    [BepInPlugin("aedenthorn.AmmoCount", "Ammo Count", "0.4.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<bool> showIcon;
        public static ConfigEntry<Vector2> iconPosition;
        public static ConfigEntry<Vector2> iconSize;
        public static ConfigEntry<string> ammoStringFormat;
        public static ConfigEntry<int> ammoStringSize;
        public static ConfigEntry<string> ammoStringFont;
        public static ConfigEntry<FontStyles> ammoStringFontStyle;
        public static ConfigEntry<TextAlignmentOptions> ammoStringAlignment;
        public static ConfigEntry<Color> ammoStringColor;
        public static ConfigEntry<Vector2> ammoStringPosition;

        public static FieldInfo elementsAmmo;
        public static TMP_FontAsset currentFont;
        public static bool reset = true;

        public static void Dbgl(object obj, bool pref = true)
        {
            if (isDebug)
                context.Logger.Log(LogLevel.Info, (pref ? typeof(BepInExPlugin).Namespace + " " : "") + obj?.ToString());
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 2273, "Nexus mod ID for updates");

            showIcon = Config.Bind<bool>("Options", "ShowIcon", true, "Show Ammo Icon");
            iconPosition = Config.Bind<Vector2>("Options", "IconPosition", new Vector2(0,32), "Icon position");
            iconSize = Config.Bind<Vector2>("Options", "IconSize", new Vector2(20,20), "Icon Size");
            ammoStringFormat = Config.Bind<string>("Options", "AmmoTextFormat", "{amount}", "Ammo string. Use {amount} and/or {name}");
            ammoStringSize = Config.Bind<int>("Options", "AmmoTextSize", 16, "Ammo count size.");
            ammoStringFont = Config.Bind<string>("Options", "AmmoTextFont", "AveriaSansLibre-Bold", "Ammo count font");
            ammoStringFontStyle = Config.Bind<FontStyles>("Options", "AmmoTextFontStyle", FontStyles.Bold, "Ammo count font style");
            ammoStringAlignment = Config.Bind<TextAlignmentOptions>("Options", "AmmoTextAlignment", TextAlignmentOptions.TopRight, "Ammo count alignment");
            ammoStringColor = Config.Bind<Color>("Options", "AmmoTextColor", new Color(1, 1, 1, 1), "Ammo count color");
            ammoStringPosition = Config.Bind<Vector2>("Options", "AmmoTextPosition", new Vector2(-6, -4), "Ammo count position offset");

            elementsAmmo = AccessTools.Field(typeof(HotkeyBar), "m_elements");

            ammoStringFont.SettingChanged += SettingChanged;

            showIcon.SettingChanged += SettingChanged;
            iconPosition.SettingChanged += SettingChanged;
            iconSize.SettingChanged += SettingChanged;
            
            ammoStringFormat.SettingChanged += SettingChanged;
            ammoStringSize.SettingChanged += SettingChanged;
            ammoStringFontStyle.SettingChanged += SettingChanged;
            ammoStringAlignment.SettingChanged += SettingChanged;
            ammoStringColor.SettingChanged += SettingChanged;
            ammoStringPosition.SettingChanged += SettingChanged;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }
        
        public void SettingChanged(object sender, EventArgs e)
        {
            reset = true;
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }
        public static TMP_FontAsset GetFont(string fontName, int fontSize)
        {
            try
            {
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (TMP_FontAsset font in fonts)
                {
                    if (font.name == fontName)
                    {
                        return font;
                    }
                }
            }
            catch
            {
            }
            return currentFont; 
            
        }
        [HarmonyPatch(typeof(HotkeyBar), "Update")]
        public static class HotkeyBar_Update_Patch
        {
            public static void Postfix(HotkeyBar __instance, List<ItemDrop.ItemData> ___m_items)
            {
                if (!modEnabled.Value || !Player.m_localPlayer)
                    return;

                var list = elementsAmmo.GetValue(__instance) as IEnumerable<object>;
                if (reset)
                {
                    currentFont = GetFont(ammoStringFont.Value, ammoStringSize.Value);
                }

                using (var e = list.GetEnumerator())
                {
                    while (e.MoveNext())
                    {
                        if (e.Current is null)
                            continue;
                        GameObject slotObject = (GameObject)AccessTools.Field(e.Current.GetType(), "m_go").GetValue(e.Current);
                        Transform t = slotObject.transform.Find("AmmoCount");
                        if (t != null && (reset || !(bool)AccessTools.Field(e.Current.GetType(), "m_used").GetValue(e.Current)))
                        {
                            Destroy(t.gameObject);
                        }
                    }
                }
                reset = false;
                for (int i = 0; i < ___m_items.Count(); i++)
                {
                    int k = ___m_items[i].m_gridPos.x;
                    if (k < 0 || k >= list.Count() || list.ElementAt(k) is null)
                        continue;
                    GameObject slotObject = (GameObject)AccessTools.Field(list.ElementAt(k).GetType(), "m_go").GetValue(list.ElementAt(k));

                    Transform t = slotObject.transform.Find("AmmoCount");
                    if (t is null)
                    {
                        GameObject go = new GameObject("AmmoCount");
                        go.transform.SetParent(slotObject.transform);
                        go.AddComponent<RectTransform>().anchoredPosition = Vector2.zero;
                        t = go.transform;

                        GameObject textObj = new GameObject("Text");
                        textObj.transform.SetParent(go.transform);
                        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                        textObj.GetComponent<RectTransform>().anchoredPosition = ammoStringPosition.Value;
                        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 90);
                        text.font = currentFont;
                        text.fontSize = ammoStringSize.Value;
                        text.fontStyle = ammoStringFontStyle.Value;
                        text.alignment = ammoStringAlignment.Value;
                        text.color = ammoStringColor.Value;
                        if (showIcon.Value)
                        {
                            GameObject image = new GameObject("Icon");
                            image.transform.SetParent(go.transform);
                            image.AddComponent<Image>();
                            image.GetComponent<RectTransform>().anchoredPosition = iconPosition.Value;
                            image.GetComponent<RectTransform>().sizeDelta = iconSize.Value;

                        }
                    }
                    CheckAmmo(___m_items[i], t.gameObject);
                }
            }
            public static void CheckAmmo(ItemDrop.ItemData itemData, GameObject go)
            {
                if (string.IsNullOrEmpty(itemData.m_shared.m_ammoType) || itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo)
                {
                    go.SetActive(false);
                    return;
                }
                var ammo = FindAmmo(itemData);
                if(ammo is null)
                {
                    go.SetActive(false); 
                    return;
                }
                go.SetActive(true);
                if (!string.IsNullOrEmpty(ammoStringFormat.Value))
                {
                    go.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = ammoStringFormat.Value.Replace("{amount}", ammo.m_stack.ToString()).Replace("{name}", Localization.instance.Localize(ammo.m_shared.m_name));
                }
                if (showIcon.Value)
                {
                    go.transform.Find("Icon").GetComponent<Image>().sprite = ammo.GetIcon();

                }
            }
            public static ItemDrop.ItemData FindAmmo(ItemDrop.ItemData weapon)
            {
                if (string.IsNullOrEmpty(weapon.m_shared.m_ammoType))
                {
                    return null;
                }
                ItemDrop.ItemData itemData = Player.m_localPlayer.GetAmmoItem();
                if (itemData != null && (!Player.m_localPlayer.GetInventory().ContainsItem(itemData) || itemData.m_shared.m_ammoType != weapon.m_shared.m_ammoType))
                {
                    itemData = null;
                }
                if (itemData == null)
                {
                    itemData = Player.m_localPlayer.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType, null);
                }
                return itemData;
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

                    currentFont = GetFont(ammoStringFont.Value, ammoStringSize.Value);
                    reset = true;

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
