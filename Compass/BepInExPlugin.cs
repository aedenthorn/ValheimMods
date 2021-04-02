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
    [BepInPlugin("aedenthorn.Compass", "Compass", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<int> compassWidth;
        
        public static ConfigEntry<bool> useRichText;
        public static ConfigEntry<string> fontName;
        public static ConfigEntry<Vector2> textPositionOffset;
        public static ConfigEntry<Vector3> signScale;
        public static GameObject compassObject;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            //nexusID = Config.Bind<int>("General", "NexusID", 827, "Nexus mod ID for updates");

            compassWidth = Config.Bind<int>("General", "CompassWidth", 480, "Compass width");
            /*
            signScale = Config.Bind<Vector3>("Signs", "SignScale", new Vector3(1,1,1), "Sign scale (w,h,d)");
            textPositionOffset = Config.Bind<Vector2>("Signs", "TextPositionOffset", new Vector2(0,0), "Default font size");
            useRichText = Config.Bind<bool>("Signs", "UseRichText", true, "Enable rich text");
            fontName = Config.Bind<string>("Signs", "FontName", "AveriaSerifLibre-Bold", "Font name");
            */

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
                byte[] data = File.ReadAllBytes(Path.Combine(path, "compass.png"));
                texture.LoadImage(data);

                Texture2D maskTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                byte[] maskData = File.ReadAllBytes(Path.Combine(path, "mask.png"));
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
                prt.anchoredPosition = new Vector2(0f, (Screen.height / imageScale - texture.height) / 2);
                prt.sizeDelta = new Vector2(halfWidth, texture.height);
                prt.localScale = Vector3.one;

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
                //rt.anchorMax = new Vector2(2,1);
                rt.sizeDelta = new Vector2(texture.width, texture.height);

                Image image = compassObject.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;


                //go.GetComponent<RectTransform>().localPosition = new Vector2(Screen.width / 2, Screen.height - texture.height);
                /*
                go.GetComponent<RectTransform>().anchorMin = new Vector2(0.25f,0);
                go.GetComponent<RectTransform>().anchorMax = new Vector2(0.75f,1);
                */
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

                compassObject.GetComponent<RectTransform>().localPosition = Vector3.right * (compassObject.GetComponent<Image>().sprite.rect.width / 2) * angle / (2f * Mathf.PI) + new Vector3(compassObject.GetComponent<Image>().sprite.rect.width / 2f, 0, 0);
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
