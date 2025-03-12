using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Compass
{
    [BepInPlugin("aedenthorn.Compass", "Compass", "1.4.2")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<bool> enableMarkers;
        public static ConfigEntry<bool> showPlayerMarkers;
        public static ConfigEntry<bool> usePlayerDirection;
        public static ConfigEntry<bool> showCenterMarker;
        public static ConfigEntry<string> compassFile;
        public static ConfigEntry<string> overlayFile;
        public static ConfigEntry<string> underlayFile;
        public static ConfigEntry<string> maskFile;
        public static ConfigEntry<string> centerFile;
        public static ConfigEntry<Color> compassColor;
        public static ConfigEntry<Color> centerColor;
        public static ConfigEntry<Color> markerColor;
        public static ConfigEntry<float> compassYOffset;
        public static ConfigEntry<float> compassScale;
        public static ConfigEntry<float> markerScale;
        public static ConfigEntry<float> minMarkerDistance;
        public static ConfigEntry<float> maxMarkerDistance;
        public static ConfigEntry<float> minMarkerScale;
        public static ConfigEntry<string> ignoredMarkerNames;
        public static ConfigEntry<string> ignoredMarkerTypes;
        //public static ConfigEntry<string> unlimitedRangeMarkerNames;
        

        public static GameObject compassObject;
        public static GameObject pinsObject;
        public static GameObject centerObject;
        public static GameObject parentObject;

        public static float lastAngle;
        public static bool dbgl;
        public static string[] ignoredTypes;
        public static string[] ignoredNames;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 851, "Nexus mod ID for updates");

            usePlayerDirection = Config.Bind<bool>("Compass", "UsePlayerDirection", false, "Orient the compass based on the player's facing direction, rather than the middle of the screen");
            showCenterMarker = Config.Bind<bool>("Compass", "ShowCenterMarker", true, "Show center marker");
            compassScale = Config.Bind<float>("Compass", "CompassScale", 0.75f, "Compass scale");
            compassYOffset = Config.Bind<float>("Compass", "CompassYOffset", 0, "Compass offset from top of screen in pixels");

            enableMarkers = Config.Bind<bool>("Markers", "EnableMarkers", true, "Show markers on compass.");
            showPlayerMarkers = Config.Bind<bool>("Markers", "ShowPlayerMarkers", true, "Show player markers on compass.");
            markerScale = Config.Bind<float>("Markers", "MarkerScale", 1f, "Marker scale");
            minMarkerDistance = Config.Bind<float>("Markers", "MinMarkerDistance", 1, "Minimum marker distance to show on map in metres");
            maxMarkerDistance = Config.Bind<float>("Markers", "MaxMarkerDistance", 100, "Max marker distance to show on map in metres");
            minMarkerScale = Config.Bind<float>("Markers", "MinMarkerScale", 0.25f, "Marker scale at max marker distance (before applying MarkerScale)");
            ignoredMarkerNames = Config.Bind<string>("Markers", "IgnoredMarkerNames", "Silver,Obsidian,Copper,Tin", "Ignore markers with these names (comma-separated). End a string with * to denote a prefix. Default list is pins added by AutoMapPins");
            ignoredMarkerTypes = Config.Bind<string>("Markers", "IgnoredMarkerTypes", "", "Ignore markers with these types (comma-separated). Possible types include: Icon0,Icon1,Icon2,Icon3,Death,Bed,Icon4,Shout,None,Boss,Player,RandomEvent,Ping,EventArea");
            //unlimitedRangeMarkerNames = Config.Bind<string>("Markers", "UnlimitedRangeMarkerNames", "", "Ignore max range limits for markers with these names (comma-separated).");

            compassFile = Config.Bind<string>("Files", "CompassFile", "compass.png", "Compass file to use in Compass folder");
            overlayFile = Config.Bind<string>("Files", "OverlayFile", "", "Overlay file to use in Compass folder. This file is just an arbitrary graphic to show on top of the compass, e.g. a frame.");
            underlayFile = Config.Bind<string>("Files", "UnderlayFile", "", "Underlay file to use in Compass folder. This file is just an arbitrary graphic to show below the compass, e.g. a frame.");
            maskFile = Config.Bind<string>("Files", "MaskFile", "mask.png", "Mask file to use in Compass folder");
            centerFile = Config.Bind<string>("Files", "CenterFile", "center.png", "Center file to use in Compass folder");

            compassColor = Config.Bind<Color>("Colors", "CompassColor", Color.white, "Compass color");
            centerColor = Config.Bind<Color>("Colors", "CenterColor", new Color(1,1,0,0.5f), "Center marker color");
            markerColor = Config.Bind<Color>("Colors", "MarkerColor", Color.white, "Marker color");

            ignoredMarkerNames.SettingChanged += IgnoredMarkerNames_SettingChanged;
            ignoredMarkerTypes.SettingChanged += IgnoredMarkerTypes_SettingChanged;

            ignoredTypes = ignoredMarkerTypes.Value.Split(',');
            ignoredNames = ignoredMarkerNames.Value.Split(',');


            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void IgnoredMarkerTypes_SettingChanged(object sender, EventArgs e)
        {
            ignoredTypes = ignoredMarkerTypes.Value.Split(',');
        }

        public void IgnoredMarkerNames_SettingChanged(object sender, EventArgs e)
        {
            ignoredNames = ignoredMarkerNames.Value.Split(',');
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        [HarmonyPatch(typeof(Hud), "Awake")]
        public static class Hud_Awake_Patch
        {

            public static void Postfix(Hud __instance)
            {
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Compass");

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
                try
                {
                    byte[] data = File.ReadAllBytes(Path.Combine(path, compassFile.Value));
                    texture.LoadImage(data);

                }
                catch(Exception ex)
                {
                    Dbgl($"Invalid compass file {compassFile.Value}: {ex}");

                    return;
                }

                Texture2D maskTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
                try
                {
                    byte[] maskData = File.ReadAllBytes(Path.Combine(path, maskFile.Value));
                    maskTex.LoadImage(maskData);
                }
                catch(Exception ex)
                {
                    Dbgl($"Invalid mask file {maskFile.Value}: {ex}");

                    return;
                }

                Texture2D centerTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
                try {

                    byte[] centerData = File.ReadAllBytes(Path.Combine(path, centerFile.Value));
                    centerTex.LoadImage(centerData);
                }
                catch (Exception ex)
                {
                    Dbgl($"Invalid center file {centerFile.Value}: {ex}");

                    return;
                }
                Dbgl($"Loaded image files");


                float halfWidth = texture.width / 2f;

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                Sprite maskSprite = Sprite.Create(maskTex, new Rect(0, 0, halfWidth, maskTex.height), Vector2.zero);
                Sprite centerSprite = Sprite.Create(centerTex, new Rect(0, 0, centerTex.width, centerTex.height), Vector2.zero);


                Sprite overlaySprite = null;
                Sprite underlaySprite = null;

                Texture2D overlayTex = null;
                if(overlayFile.Value.Length > 0)
                {
                    try
                    {
                        overlayTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
                        byte[] overlayData = File.ReadAllBytes(Path.Combine(path, overlayFile.Value));
                        overlayTex.LoadImage(overlayData);
                        overlaySprite = Sprite.Create(overlayTex, new Rect(0, 0, overlayTex.width, overlayTex.height), Vector2.zero);
                    }
                    catch
                    {
                        Dbgl($"Invalid overlay file");
                    }
                }
                
                Texture2D underlayTex = null;
                if(underlayFile.Value.Length > 0)
                {
                    try
                    {
                        underlayTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
                        byte[] underlayData = File.ReadAllBytes(Path.Combine(path, underlayFile.Value));
                        underlayTex.LoadImage(underlayData);
                        underlaySprite = Sprite.Create(underlayTex, new Rect(0, 0, underlayTex.width, underlayTex.height), Vector2.zero);
                    }
                    catch
                    {
                        Dbgl($"Invalid underlay file");
                    }
                }


                
                // Parent Object

                parentObject = new GameObject();
                parentObject.name = "Compass";
                RectTransform prt = parentObject.AddComponent<RectTransform>();
                prt.SetParent(__instance.m_rootObject.transform);


                // Overlay object

                if(overlayTex != null)
                {
                    GameObject overlayObject = new GameObject();
                    overlayObject.name = "Overlay";
                    RectTransform ort = overlayObject.AddComponent<RectTransform>();
                    ort.SetParent(parentObject.transform);
                    ort.localScale = Vector3.one * compassScale.Value;
                    ort.sizeDelta = new Vector2(overlayTex.width, overlayTex.height);
                    ort.anchoredPosition = Vector2.zero;
                    Image overlayImage = overlayObject.AddComponent<Image>();
                    overlayImage.sprite = overlaySprite;
                    overlayImage.preserveAspect = true;
                }
                

                // Underlay object

                if(underlayTex != null)
                {
                    GameObject underlayObject = new GameObject();
                    underlayObject.name = "Underlay";
                    RectTransform ort = underlayObject.AddComponent<RectTransform>();
                    ort.SetParent(parentObject.transform);
                    ort.localScale = Vector3.one * compassScale.Value;
                    ort.sizeDelta = new Vector2(underlayTex.width, underlayTex.height);
                    ort.anchoredPosition = Vector2.zero;
                    Image underlayImage = underlayObject.AddComponent<Image>();
                    underlayImage.sprite = underlaySprite;
                    underlayImage.preserveAspect = true;
                }

                // Mask object

                GameObject maskObject = new GameObject();
                maskObject.name = "Mask";
                RectTransform mrt = maskObject.AddComponent<RectTransform>();
                mrt.SetParent(parentObject.transform);
                mrt.sizeDelta = new Vector2(halfWidth, texture.height);
                mrt.localScale = Vector3.one * compassScale.Value;
                mrt.anchoredPosition = Vector2.zero;

                Image maskImage = maskObject.AddComponent<Image>();
                maskImage.sprite = maskSprite;
                maskImage.preserveAspect = true;

                Mask mask = maskObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                // Compass object

                compassObject = new GameObject();
                compassObject.name = "Image";

                RectTransform rt = compassObject.AddComponent<RectTransform>();
                rt.SetParent(maskObject.transform);
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(texture.width, texture.height);

                Image image = compassObject.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;

                // Center object

                centerObject = new GameObject();
                centerObject.name = "CenterImage";

                RectTransform crt = centerObject.AddComponent<RectTransform>();
                crt.SetParent(maskObject.transform);
                crt.localScale = Vector3.one;
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(centerTex.width, centerTex.height);

                Image cimage = centerObject.AddComponent<Image>();
                cimage.sprite = centerSprite;
                cimage.preserveAspect = true;

                // Pins object

                pinsObject = new GameObject();
                pinsObject.name = "Pins";
                rt = pinsObject.AddComponent<RectTransform>();
                rt.SetParent(maskObject.transform);
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(halfWidth, texture.height);


                Dbgl("Added compass to hud");
            }
        }


        [HarmonyPatch(typeof(Hud), "Update")]
        public static class Hud_Update_Patch
        {
            public static void Prefix(Hud __instance)
            {
                if (!modEnabled.Value || Player.m_localPlayer is null || compassObject is null)
                    return;

                float angle;
                
                if(usePlayerDirection.Value)
                    angle = Player.m_localPlayer.transform.eulerAngles.y;
                else
                    angle = GameCamera.instance.transform.eulerAngles.y;

                if (angle > 180)
                    angle -= 360;

                angle *= -Mathf.Deg2Rad;

                Rect rect = compassObject.GetComponent<Image>().sprite.rect;
                if(GameObject.Find("LoadingGUI") is null)
                {
                    return;
                }
                float imageScale = __instance.GetComponent<CanvasScaler>().scaleFactor;

                compassObject.GetComponent<RectTransform>().localPosition = Vector3.right * (rect.width / 2) * angle / (2f * Mathf.PI) - new Vector3(rect.width * 0.125f, 0, 0);

                compassObject.GetComponent<Image>().color = compassColor.Value;
                parentObject.GetComponent<RectTransform>().localScale = Vector3.one * compassScale.Value;
                parentObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, (Screen.height / imageScale - compassObject.GetComponent<Image>().sprite.texture.height * compassScale.Value) / 2) - Vector2.up * compassYOffset.Value;

                centerObject.GetComponent<Image>().color = centerColor.Value;
                centerObject.SetActive(showCenterMarker.Value);

                int count = pinsObject.transform.childCount;
                List<string> oldPins = new List<string>();
                foreach (Transform t in pinsObject.transform)
                {
                    if (!enableMarkers.Value)
                    {
                        Destroy(t.gameObject); 
                        continue;
                    }
                    oldPins.Add(t.name);
                }
                if (!enableMarkers.Value)
                    return;

                var pinList = new List<Minimap.PinData>();
                pinList.AddRange(AccessTools.DeclaredField(typeof(Minimap), "m_pins").GetValue(Minimap.instance) as List<Minimap.PinData>);

                pinList.AddRange((AccessTools.DeclaredField(typeof(Minimap), "m_locationPins").GetValue(Minimap.instance) as Dictionary<Vector3, Minimap.PinData>).Values);
                
                if(showPlayerMarkers.Value)
                    pinList.AddRange((AccessTools.DeclaredField(typeof(Minimap), "m_playerPins").GetValue(Minimap.instance) as List<Minimap.PinData>));

                Minimap.PinData deathPin = AccessTools.DeclaredField(typeof(Minimap), "m_deathPin").GetValue(Minimap.instance) as Minimap.PinData;

                if(deathPin != null)
                {
                    pinList.Add(deathPin);
                }

                //string[] unlimitedRangeMarkerNamesList = unlimitedRangeMarkerNames.Value.Split(',');
                Transform pt = Player.m_localPlayer.transform;
                float zeroScaleDistance = maxMarkerDistance.Value / (1 - minMarkerScale.Value);

                foreach (Minimap.PinData pin in pinList)
                {
                    string name = pin.m_pos.ToString();
                    oldPins.Remove(name);

                    var t = pinsObject.transform.Find(name);

                    if (ignoredNames.Contains(pin.m_name) || ignoredTypes.Contains(pin.m_type.ToString()) || (ignoredMarkerNames.Value.Contains("*") && Array.Exists(ignoredNames, s => s.EndsWith("*") && name.StartsWith(s.Substring(0,s.Length-1)))) || Vector3.Distance(pt.position, pin.m_pos) > maxMarkerDistance.Value || Vector3.Distance(pt.position, pin.m_pos) < minMarkerDistance.Value)
                    {
                        if(t)
                            t.gameObject.SetActive(false);
                        continue;
                    }
                    if (t)
                        t.gameObject.SetActive(true);

                    Vector3 offset;
                    if (usePlayerDirection.Value)
                        offset = pt.InverseTransformPoint(pin.m_pos);
                    else
                        offset = GameCamera.instance.transform.InverseTransformPoint(pin.m_pos);

                    angle = Mathf.Atan2(offset.x, offset.z);

                    GameObject po;
                    RectTransform rt;
                    Image img;

                    if (!t)
                    {
                        po = new GameObject();
                        po.name = pin.m_pos.ToString();
                        rt = po.AddComponent<RectTransform>();
                        rt.SetParent(pinsObject.transform);
                        rt.anchoredPosition = Vector2.zero;
                        img = po.AddComponent<Image>();
                    }
                    else
                    {
                        po = t.gameObject;
                        rt = t.GetComponent<RectTransform>();
                        img = t.GetComponent<Image>();
                    }

                    float distanceScale = minMarkerScale.Value < 1 ? (zeroScaleDistance - Vector3.Distance(pt.position, pin.m_pos)) / zeroScaleDistance : 1;
                    rt.localScale = Vector3.one * distanceScale * 0.5f * markerScale.Value;
                    img.color = markerColor.Value;
                    img.sprite = pin.m_icon;
                    rt.localPosition = Vector3.right * (rect.width / 2) * angle / (2f * Mathf.PI);
                }
                foreach (string name in oldPins)
                    Destroy(pinsObject.transform.Find(name).gameObject);

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

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
