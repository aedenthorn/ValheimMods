using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MapDetails
{
    [BepInPlugin("aedenthorn.MapDetails", "Map Details", "0.4.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> showRange;
        public static ConfigEntry<float> updateDelta;
        public static ConfigEntry<bool> showBuildings;
        
        public static ConfigEntry<Color> personalBuildingColor;
        public static ConfigEntry<Color> otherBuildingColor;
        public static ConfigEntry<Color> unownedBuildingColor;
        public static ConfigEntry<string> customPlayerColors;

        public static Vector2 lastPos = Vector2.zero;
        public static List<int> lastPixels = new List<int>();
        public static Texture2D mapTexture;
        public static Texture2D tempTexture;
        public static Dictionary<string, Color> playerColorDict = new Dictionary<string, Color>();


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1332, "Nexus mod ID for updates");

            showRange = Config.Bind<float>("Variables", "ShowRange", 50f, "Range in metres around player to show details");
            updateDelta = Config.Bind<float>("Variables", "UpdateDelta", 5f, "Distance in metres to move before automatically updating the map details");
            showBuildings = Config.Bind<bool>("Variables", "ShowBuildings", true, "Show building pieces");
            personalBuildingColor = Config.Bind<Color>("Variables", "PersonalBuildingColor", Color.green, "Color of one's own build pieces");
            otherBuildingColor = Config.Bind<Color>("Variables", "OtherBuildingColor", Color.red, "Color of other players' build pieces");
            unownedBuildingColor = Config.Bind<Color>("Variables", "UnownedBuildingColor", Color.yellow, "Color of npc build pieces");
            customPlayerColors = Config.Bind<string>("Variables", "CustomPlayerColors", "", "Custom color list, comma-separated. Use either <name>:<colorCode> pair entries or just <colorCode> entries. E.g. Erinthe:FF0000 or just FF0000. The latter will assign a color randomly to each connected peer.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            customPlayerColors.SettingChanged += CustomPlayerColors_SettingChanged;
            ReloadNames();
        }

        public void CustomPlayerColors_SettingChanged(object sender, EventArgs e)
        {
            ReloadNames();
        }

        public void ReloadNames()
        {
            playerColorDict.Clear();
            if (customPlayerColors.Value.Length > 0)
            {
                var customColors = customPlayerColors.Value.Split(',');
                if (customPlayerColors.Value.Contains(":"))
                {
                    foreach(var c in customColors)
                    {
                        string[] pair = c.Split(':');
                        if(ColorUtility.TryParseHtmlString(pair[1], out Color color))
                        {
                            playerColorDict.Add(pair[0], color);

                        }
                    }

                }
                else if(ColorUtility.TryParseHtmlString(customColors[0], out Color color))
                {
                    playerColorDict.Add("?", color);
                }

            }
            Dictionary<long, Color> assignedColors = new Dictionary<long, Color>();
        }

        public void Update()
        {
            if(Minimap.instance && Player.m_localPlayer)
                StartCoroutine(UpdateMap(false));
        }

        [HarmonyPatch(typeof(Player), "PlacePiece")]
        public static class Player_PlacePiece_Patch
        {
            public static void Postfix(bool __result)
            {
                if (!modEnabled.Value || !__result)
                    return;
                context.StartCoroutine(UpdateMap(true));
            }
        }
        [HarmonyPatch(typeof(Player), "RemovePiece")]
        public static class Player_RemovePiece_Patch
        {
            public static void Postfix(bool __result)
            {
                if (!modEnabled.Value || !__result)
                    return;
                context.StartCoroutine(UpdateMap(true));
            }
        }

        [HarmonyPatch(typeof(Minimap), "GenerateWorldMap")]
        public static class GenerateWorldMap_Patch
        {
            public static void Postfix(Texture2D ___m_mapTexture)
            {
                if (!modEnabled.Value)
                    return;
                Color32[] data = ___m_mapTexture.GetPixels32();

                mapTexture = new Texture2D(___m_mapTexture.width, ___m_mapTexture.height, TextureFormat.RGBA32, false);
                mapTexture.wrapMode = TextureWrapMode.Clamp;
                mapTexture.SetPixels32(data);
                mapTexture.Apply();
                tempTexture = new Texture2D(mapTexture.width, mapTexture.height, TextureFormat.RGBA32, false);
                tempTexture.wrapMode = TextureWrapMode.Clamp;
            }
        }

        public static IEnumerator UpdateMap(bool force)
        {
            if(force)
                yield return null;

            Vector2 coords = new Vector2(Player.m_localPlayer.transform.position.x, Player.m_localPlayer.transform.position.z);

            if (!force && Vector2.Distance(lastPos, coords) < updateDelta.Value)
                yield break;
            
            lastPos = coords;

            Dictionary<int, long> pixels = new Dictionary<int, long>();
            bool newPix = false;

            foreach (Collider collider in Physics.OverlapSphere(Player.m_localPlayer.transform.position, Mathf.Max(showRange.Value, 0), LayerMask.GetMask(new string[] { "piece" })))
            {
                Piece piece = collider.GetComponentInParent<Piece>();
                if (piece != null && piece.GetComponent<ZNetView>().IsValid())
                {
                    Vector3 pos = piece.transform.position;
                    float mx;
                    float my;
                    int num = Minimap.instance.m_textureSize / 2;
                    mx = pos.x / Minimap.instance.m_pixelSize + num;
                    my = pos.z / Minimap.instance.m_pixelSize + num;

                    int x = Mathf.RoundToInt(mx / Minimap.instance.m_textureSize * mapTexture.width);
                    int y = Mathf.RoundToInt(my / Minimap.instance.m_textureSize * mapTexture.height);

                    int idx = x + y * mapTexture.width;
                    if (!pixels.ContainsKey(idx))
                    {
                        if (!lastPixels.Contains(idx))
                            newPix = true;
                        pixels[idx] = piece.GetCreator();
                    }
                }
            }

            if (!newPix)
            {
                foreach (int i in lastPixels)
                {
                    if (!pixels.ContainsKey(i))
                        goto newpixels;
                }
                //Dbgl("No new pixels");
                yield break;
            }
            newpixels:

            lastPixels = new List<int>(pixels.Keys);

            if (pixels.Count == 0)
            {
                //Dbgl("No pixels to add");
                SetMaps(mapTexture);
                yield break;
            }

            Color32[] data = mapTexture.GetPixels32();
            foreach (var kvp in pixels)
            {
                var player = Player.GetPlayer(kvp.Value)?.GetPlayerName();
                Color color = Color.clear;
                if (player != null && !playerColorDict.TryGetValue(player, out color) && kvp.Value != 0)
                {
                    playerColorDict.TryGetValue("?", out color);
                }

                if(color == Color.clear)
                    GetUserColor(kvp.Value, out color);
                data[kvp.Key] = color;
            }

            tempTexture.SetPixels32(data);
            tempTexture.Apply();

            SetMaps(tempTexture);

            yield break;
        }

        public static void GetUserColor(long id, out Color color)
        {
            color = id == 0 ? unownedBuildingColor.Value : (id == Player.m_localPlayer.GetPlayerID() ? personalBuildingColor.Value : otherBuildingColor.Value);
        }

        public static void SetMaps(Texture2D texture)
        {
            Minimap.instance.m_mapImageSmall.material.SetTexture("_MainTex", texture);
            Minimap.instance.m_mapImageLarge.material.SetTexture("_MainTex", texture);
            if (Minimap.instance.m_smallRoot.activeSelf)
            {
                Minimap.instance.m_smallRoot.SetActive(false);
                Minimap.instance.m_smallRoot.SetActive(true);
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
