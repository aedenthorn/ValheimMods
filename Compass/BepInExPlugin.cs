using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Compass
{
    [BepInPlugin("aedenthorn.Compass", "Compass", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<string> compassFile;
        public static ConfigEntry<string> maskFile;
        public static ConfigEntry<Color> compassColor;
        public static ConfigEntry<Color> markerColor;
        public static ConfigEntry<float> compassYOffset;
        public static ConfigEntry<float> compassScale;
        public static ConfigEntry<float> maxMarkerDistance;
        

        public static GameObject compassObject;
        public static GameObject pinsObject;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 851, "Nexus mod ID for updates");

            compassScale = Config.Bind<float>("General", "CompassScale", 0.75f, "Compass scale");
            compassYOffset = Config.Bind<float>("General", "CompassYOffset", 0, "Compass offset from top of screen in pixels");
            maxMarkerDistance = Config.Bind<float>("General", "MaxMarkerDistance", 100, "Max marker distance to show on map in metres");

            compassFile = Config.Bind<string>("Files", "CompassFile", "compass.png", "Compass file to use in Compass folder");
            maskFile = Config.Bind<string>("Files", "MaskFile", "mask.png", "Mask file to use in Compass folder");
            compassColor = Config.Bind<Color>("Colors", "CompassColor", Color.white, "Compass color");
            markerColor = Config.Bind<Color>("Colors", "MarkerColor", Color.white, "Marker color");



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

        [HarmonyPatch(typeof(Hud), "Awake")]
        static class Hud_Awake_Patch
        {
            static void Postfix(Hud __instance)
            {
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Compass");

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                byte[] data = File.ReadAllBytes(Path.Combine(path, compassFile.Value));
                texture.LoadImage(data);

                Texture2D maskTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                byte[] maskData = File.ReadAllBytes(Path.Combine(path, maskFile.Value));
                maskTex.LoadImage(maskData);

                float halfWidth = texture.width / 2f;

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                Sprite maskSprite = Sprite.Create(maskTex, new Rect(0, 0, halfWidth, maskTex.height), Vector2.zero);

                float imageScale = Screen.width / halfWidth; // 1 1/3 for 1440p

                // Mask object

                GameObject parent = new GameObject();
                parent.name = "Compass";
                RectTransform prt = parent.AddComponent<RectTransform>();
                prt.SetParent(__instance.m_rootObject.transform);
                prt.anchoredPosition = new Vector2(0f, (Screen.height / imageScale - texture.height) / 2) - Vector2.up * compassYOffset.Value;
                prt.sizeDelta = new Vector2(halfWidth, texture.height);
                prt.localScale = Vector3.one * compassScale.Value;

                Image maskImage = parent.AddComponent<Image>();
                maskImage.sprite = maskSprite;
                maskImage.preserveAspect = true;

                Mask mask = parent.AddComponent<Mask>();
                mask.showMaskGraphic = false;


                // Compass object

                compassObject = new GameObject();
                compassObject.name = "Image";

                RectTransform rt = compassObject.AddComponent<RectTransform>();
                rt.SetParent(parent.transform);
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(texture.width, texture.height);

                Image image = compassObject.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;

                // Pins object

                pinsObject = new GameObject();
                pinsObject.name = "Pins";
                rt = pinsObject.AddComponent<RectTransform>();
                rt.SetParent(parent.transform);
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(halfWidth, texture.height);


                Dbgl("Added compass to hud");
            }
        }



        [HarmonyPatch(typeof(Hud), "Update")]
        static class Hud_Update_Patch
        {
            static void Prefix(Hud __instance)
            {
                if (!modEnabled.Value || !Player.m_localPlayer)
                    return;

                //https://gamedev.stackexchange.com/a/149355

                // Get an arrow pointing to the marked object in the player's local frame of reference
                Vector3 offset = GameCamera.instance.transform.InverseTransformPoint(Vector3.forward);

                // Get an angle that's 0 when the object is directly ahead, 
                // and ranges -pi...pi for objects to the left/right.
                float angle = Mathf.Atan2(offset.x, offset.z);

                Rect rect = compassObject.GetComponent<Image>().sprite.rect;

                compassObject.GetComponent<RectTransform>().localPosition = Vector3.right * (rect.width / 2) * angle / (2f * Mathf.PI) - new Vector3(rect.width * 0.125f, 0, 0);

                compassObject.GetComponent<Image>().color = compassColor.Value;
                compassObject.transform.parent.GetComponent<RectTransform>().localScale = Vector3.one * compassScale.Value;
                compassObject.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, (Screen.height / (Screen.width / (rect.width / 2)) - rect.height) / 2) - Vector2.up * compassYOffset.Value;

                int count = pinsObject.transform.childCount;
                List<string> oldPins = new List<string>();
                for (int i = 0; i < count; i++)
                    oldPins.Add(pinsObject.transform.GetChild(i).name);

                var pinList = new List<Minimap.PinData>(AccessTools.DeclaredField(typeof(Minimap), "m_pins").GetValue(Minimap.instance) as List<Minimap.PinData>);

                pinList.AddRange((AccessTools.DeclaredField(typeof(Minimap), "m_locationPins").GetValue(Minimap.instance) as Dictionary<Vector3, Minimap.PinData>).Values);

                Minimap.PinData deathPin = AccessTools.DeclaredField(typeof(Minimap), "m_deathPin").GetValue(Minimap.instance) as Minimap.PinData;

                if(deathPin != null)
                {
                    pinList.Add(deathPin);
                }

                foreach (Minimap.PinData pin in pinList)
                {
                    string name = pin.m_pos.ToString();
                    oldPins.Remove(name);

                    var t = pinsObject.transform.Find(name);

                    if (Vector3.Distance(Player.m_localPlayer.transform.position, pin.m_pos) > maxMarkerDistance.Value)
                    {
                        if(t)
                            t.gameObject.SetActive(false);
                        continue;
                    }
                    if (t)
                        t.gameObject.SetActive(true);

                    offset = GameCamera.instance.transform.InverseTransformPoint(pin.m_pos);
                    angle = Mathf.Atan2(offset.x, offset.z);

                    GameObject po;
                    RectTransform rt;
                    Image i;

                    if (!t)
                    {
                        po = new GameObject();
                        po.name = pin.m_pos.ToString();
                        rt = po.AddComponent<RectTransform>();
                        rt.SetParent(pinsObject.transform);
                        rt.anchoredPosition = Vector2.zero;
                        i = po.AddComponent<Image>();
                    }
                    else
                    {
                        po = t.gameObject;
                        rt = po.GetComponent<RectTransform>();
                        i = po.GetComponent<Image>();
                    }
                    rt.localScale = Vector3.one * (maxMarkerDistance.Value - Vector3.Distance(Player.m_localPlayer.transform.position, pin.m_pos)) / maxMarkerDistance.Value;
                    i.color = markerColor.Value;
                    i.sprite = pin.m_icon;
                    rt.localPosition = Vector3.right * (rect.width / 2) * angle / (2f * Mathf.PI);
                }
                foreach (string name in oldPins)
                    Destroy(pinsObject.transform.Find(name).gameObject);


                return;
                foreach(var kvp in AccessTools.DeclaredField(typeof(Minimap), "m_locationPins").GetValue(Minimap.instance) as Dictionary<Vector3, Minimap.PinData>)
                {
                    //offset = GameCamera.instance.transform.InverseTransformPoint(Vector3.forward);
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
