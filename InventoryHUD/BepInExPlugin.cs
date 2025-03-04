using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryHUD
{
    [BepInPlugin("aedenthorn.InventoryHUD", "InventoryHUD", "0.4.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<string> modKey;
        public static ConfigEntry<Vector2> hudPosition;
        public static ConfigEntry<Vector2> infoStringOffset;
        public static ConfigEntry<Vector2> weightOffset;
        public static ConfigEntry<int> extraSlots;

        public static ConfigEntry<string> infoString;
        public static ConfigEntry<int> infoStringSize;
        public static ConfigEntry<string> infoStringFont;
        public static ConfigEntry<TextAlignmentOptions> infoStringAlignment;
        public static ConfigEntry<Color> infoStringColor;

        public static ConfigEntry<string> weightFile;
        public static ConfigEntry<Color> weightColor;
        public static ConfigEntry<Color> fillColor;
        public static ConfigEntry<float> weightScale;
        

        public static GameObject partialObject;
        public static GameObject fullObject;
        public static GameObject maskObject;
        public static GameObject infoObject;

        public static Texture2D weightTexture;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1089, "Nexus mod ID for updates");

            modKey = Config.Bind<string>("General", "ModKey", "left alt", "Modifier key to allow moving with the mouse.");
            hudPosition = Config.Bind<Vector2>("General", "HudPosition", new Vector2(Screen.width - 40, Screen.height / 2), "Weight icon file to use in InventoryHUD folder");
            
            extraSlots = Config.Bind<int>("General", "ExtraSlots", 0, "Extra slots added by mods outside of the actual player inventory. Use this for mods that add extra inventories or other hacky things. E.g. Equipment and Quick Slots mods adds eight extra slots outside of the actual player inventory. Set this to 8 if you use that mod.");


            infoStringOffset = Config.Bind<Vector2>("Info", "InfoStringOffset", new Vector2(-64,0), "Inventory info string offset");
            infoString = Config.Bind<string>("Info", "InfoString", "{0}/{1}\r\n{2}/{3}", "Inventory info string to show. {0} is replaced by current number of items. {1} is replaced by number of slots total. {2} is replaced by current weight. {3} is replaced by total weight. See string.Format API for advanced usage.");
            infoStringSize = Config.Bind<int>("Info", "InfoStringSize", 12, "Inventory info string size.");
            infoStringFont = Config.Bind<string>("Info", "InfoStringFont", "AveriaSerifLibre-Bold", "Inventory info string font.");
            infoStringAlignment = Config.Bind<TextAlignmentOptions>("Info", "InfoStringAlignment", TextAlignmentOptions.Center, "Info string alignment");
            infoStringColor = Config.Bind<Color>("Info", "InfoStringColor", new Color(1, 1, 1, 0.5f), "Info string color");

            weightOffset = Config.Bind<Vector2>("Weight", "WeightOffset", new Vector2(0,0), "Weight icon offset");
            weightFile = Config.Bind<string>("Weight", "WeightFile", "bag.png", "Weight icon file to use in InventoryHUD folder");
            weightScale = Config.Bind<float>("Weight", "WeightScale", 1f, "Weight icon scale");
            weightColor = Config.Bind<Color>("Weight", "WeightColor", new Color(1,1,1,0.5f), "Weight icon color");
            fillColor = Config.Bind<Color>("Weight", "WeightFillColor", new Color(1, 1, 0.5f, 1f), "Weight icon fill color");

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
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

        public static void AddWeightObject(Hud hud)
        {

            if (!modEnabled.Value)
                return;

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "InventoryHUD");

            weightTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            byte[] data = File.ReadAllBytes(Path.Combine(path, weightFile.Value));
            weightTexture.LoadImage(data);
            weightTexture.filterMode = FilterMode.Point;

            Texture2D maskTexture = new Texture2D(weightTexture.width, weightTexture.height, TextureFormat.RGBA32, false);
            for (int i = 0; i < weightTexture.width; i++)
            {
                for (int j = 0; j < weightTexture.height; j++)
                {
                    maskTexture.SetPixel(i, j, Color.white);
                }
            }
            maskTexture.Apply();

            Sprite sprite = Sprite.Create(weightTexture, new Rect(0, 0, weightTexture.width, weightTexture.height), Vector2.zero);
            Sprite fsprite = Sprite.Create(weightTexture, new Rect(0, 0, weightTexture.width, weightTexture.height), Vector2.zero);
            Sprite maskSprite = Sprite.Create(maskTexture, new Rect(0, 0, weightTexture.width, weightTexture.height), Vector2.zero);

            // Full object

            fullObject = new GameObject
            {
                name = "InventoryHUDFullImage"
            };

            RectTransform frt = fullObject.AddComponent<RectTransform>();
            frt.SetParent(hud.m_rootObject.transform);
            frt.localScale = Vector3.one * weightScale.Value;
            frt.anchoredPosition = Vector2.zero;
            frt.sizeDelta = new Vector2(weightTexture.width, weightTexture.height);

            Image fimage = fullObject.AddComponent<Image>();
            fimage.sprite = fsprite;
            fimage.color = weightColor.Value;
            fimage.preserveAspect = true;

            // Mask object

            maskObject = new GameObject();
            maskObject.name = "InventoryHUDMaskImage";
            RectTransform prt = maskObject.AddComponent<RectTransform>();
            prt.SetParent(hud.m_rootObject.transform);
            prt.localScale = Vector3.one * weightScale.Value;
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(weightTexture.width, weightTexture.height);


            Image maskImage = maskObject.AddComponent<Image>();
            maskImage.sprite = maskSprite;
            maskImage.preserveAspect = true;

            Mask mask = maskObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;


            // Partial object

            partialObject = new GameObject
            {
                name = "Image"
            };

            RectTransform rt = partialObject.AddComponent<RectTransform>();
            rt.SetParent(maskObject.transform);
            rt.localScale = Vector3.one * weightScale.Value;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(weightTexture.width, weightTexture.height);

            Image image = partialObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = fillColor.Value;
            image.preserveAspect = true;


            Dbgl("Added weight object to hud");
        }

        public static void AddInfoString(Hud hud)
        {
            infoObject = new GameObject
            {
                name = "InventoryHUDInfo"
            };

            RectTransform rt = infoObject.AddComponent<RectTransform>();
            rt.SetParent(hud.m_rootObject.transform);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = infoObject.AddComponent<TextMeshProUGUI>();
            Dbgl($"text: {text?.GetType()}");
            Dbgl($"{text.text}");
            var font = GetFont(infoStringFont.Value, infoStringSize.Value);
            if (font != null)
                text.font = font;
        }


        [HarmonyPatch(typeof(Hud), "Awake")]
        public static class Hud_Awake_Patch
        {
            public static void Postfix(Hud __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (weightFile.Value.Length > 0)
                    AddWeightObject(__instance);
                if (infoString.Value.Length > 0)
                    AddInfoString(__instance);
            }

        }


        [HarmonyPatch(typeof(Hud), "Update")]
        public static class Hud_Update_Patch
        {
            public static Vector3 lastPosition = Vector3.zero;
            public static void Prefix(Hud __instance)
            {
                if (!modEnabled.Value || Player.m_localPlayer is null)
                    return;

                Vector3 hudPos = new Vector3(hudPosition.Value.x, hudPosition.Value.y, 0);
                if (__instance.m_rootObject?.transform.localPosition.x > 1000f)
                {
                    maskObject?.SetActive(false);
                    partialObject?.SetActive(false);
                    fullObject?.SetActive(false);
                    infoObject?.SetActive(false);
                    return;
                }
                maskObject?.SetActive(true);
                partialObject?.SetActive(true);
                fullObject?.SetActive(true);
                infoObject?.SetActive(true);

                Inventory inv = Player.m_localPlayer.GetInventory();
                Vector3 weightPos = hudPos + new Vector3(weightOffset.Value.x, weightOffset.Value.y, 0);

                float weight = inv.GetTotalWeight();
                float totalWeight = Player.m_localPlayer.GetMaxCarryWeight();
                if (fullObject != null)
                {
                    float hudScale = GameObject.Find("LoadingGUI").GetComponent<CanvasScaler>().scaleFactor;

                    float maskOffset = (1 - weight / totalWeight ) * weightTexture.height * weightScale.Value * hudScale;

                    if (AedenthornUtils.CheckKeyHeld(modKey.Value, true) && Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        Dbgl($"{lastPosition} {hudPos} {Vector3.Distance(lastPosition, hudPos)} {(weightTexture.height + weightTexture.width) / 4f} {maskOffset}");

                        if (Vector3.Distance(Input.mousePosition, hudPos) < (weightTexture.height + weightTexture.width) / 4f * weightScale.Value * hudScale)
                        {
                            Dbgl("dragging start");
                            lastPosition = Input.mousePosition;
                        }
                    }
                    else if (AedenthornUtils.CheckKeyHeld(modKey.Value, true) && Input.GetKey(KeyCode.Mouse0) && Vector3.Distance(lastPosition, weightPos) < (weightTexture.height + weightTexture.width) / 4f * weightScale.Value * hudScale)
                    {
                        hudPos += Input.mousePosition - lastPosition;
                        hudPosition.Value = new Vector2(hudPos.x, hudPos.y);
                    }
                    lastPosition = Input.mousePosition;

                    partialObject.GetComponent<Image>().color = fillColor.Value;
                    fullObject.GetComponent<Image>().color = weightColor.Value;

                    maskObject.transform.position = weightPos - new Vector3(0, maskOffset, 0);
                    partialObject.transform.position = weightPos;
                    fullObject.transform.position = weightPos;
                }
                if(infoObject != null)
                {
                    infoObject.transform.position = hudPos + new Vector3(infoStringOffset.Value.x, infoStringOffset.Value.y, 0);

                    int items = inv.GetAllItems().Count;
                    int slots = inv.GetWidth() * inv.GetHeight() + extraSlots.Value;
                    TextMeshProUGUI text = infoObject.GetComponent<TextMeshProUGUI>();
                    text.text = string.Format(infoString.Value, new object[] { items, slots, Math.Round(weight), Math.Round(totalWeight) });
                    text.color = infoStringColor.Value;
                    text.alignment = infoStringAlignment.Value;
                    text.fontSize = infoStringSize.Value;
                }

                /*
                Rect rect = parentObject.GetComponent<Image>().sprite.rect;
                partialObject.transform.parent.GetComponent<RectTransform>().localScale = Vector3.one * weightScale.Value;
                */

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

                    Destroy(partialObject);
                    Destroy(fullObject);
                    Destroy(maskObject);
                    Destroy(infoObject);

                    if (modEnabled.Value && Hud.instance)
                    {
                        if (weightFile.Value.Length > 0)
                            AddWeightObject(Hud.instance);
                        if (infoString.Value.Length > 0)
                            AddInfoString(Hud.instance);
                    }

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
