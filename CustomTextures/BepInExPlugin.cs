using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomTextures
{
    [BepInPlugin("aedenthorn.CustomTextures", "Custom Textures", "1.5.2")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> dumpSceneTextures;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<int> nexusID;
        public static Dictionary<string, string> customTextures = new Dictionary<string, string>();
        public static Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();
        public static List<string> outputDump = new List<string>();
        public static List<string> logDump = new List<string>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            hotKey = Config.Bind<string>("General", "HotKey", "page down", "Key to reload textures");
            dumpSceneTextures = Config.Bind<bool>("General", "DumpSceneTextures", false, "Dump scene textures to BepInEx/plugins/CustomTextures/scene_dump.txt");
            nexusID = Config.Bind<int>("General", "NexusID", 48, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            LoadCustomTextures();

            //SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (ZNetScene.instance != null && CheckKeyDown(hotKey.Value))
            {
                Dbgl($"Pressed reload key.");

                logDump.Clear();

                LoadCustomTextures();
                ReplaceObjectDBTextures();

                GameObject root = (GameObject)typeof(ZNetScene).GetField("m_netSceneRoot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ZNetScene.instance);

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

                List<GameObject> gos = new List<GameObject>();
                foreach (Transform t in transforms)
                {
                    if(t.parent == root.transform)
                        gos.Add(t.gameObject);
                }

                foreach (ClutterSystem.Clutter clutter in ClutterSystem.instance.m_clutter)
                {
                    gos.Add(clutter.m_prefab);
                }
                LoadSceneTextures(gos.ToArray());
                LoadSceneTextures(((Dictionary<int, GameObject>)typeof(ZNetScene).GetField("m_namedPrefabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ZNetScene.instance )).Values.ToArray());

                foreach (Player player in Player.GetAllPlayers())
                {
                    VisEquipment ve = (VisEquipment)typeof(Humanoid).GetField("m_visEquipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);
                    if(ve != null)
                    {
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_leftItem").GetValue<string>(), Traverse.Create(ve).Field("m_leftItemInstance").GetValue<GameObject>());
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_rightItem").GetValue<string>(), Traverse.Create(ve).Field("m_rightItemInstance").GetValue<GameObject>());
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_helmetItem").GetValue<string>(), Traverse.Create(ve).Field("m_helmetItemInstance").GetValue<GameObject>());
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_leftBackItem").GetValue<string>(), Traverse.Create(ve).Field("m_leftBackItemInstance").GetValue<GameObject>());
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_rightBackItem").GetValue<string>(), Traverse.Create(ve).Field("m_rightBackItemInstance").GetValue<GameObject>());
                        SetEquipmentListTexture(Traverse.Create(ve).Field("m_shoulderItem").GetValue<string>(), Traverse.Create(ve).Field("m_shoulderItemInstances").GetValue<List<GameObject>>());
                        SetEquipmentListTexture(Traverse.Create(ve).Field("m_utilityItem").GetValue<string>(), Traverse.Create(ve).Field("m_utilityItemInstances").GetValue<List<GameObject>>());
                        SetBodyEquipmentTexture(ve, Traverse.Create(ve).Field("m_legItem").GetValue<string>(), ve.m_bodyModel, Traverse.Create(ve).Field("m_legItemInstances").GetValue<List<GameObject>>(), "_Legs", Traverse.Create(ve).Field("m_currentLegItemHash").GetValue<int>());
                        SetBodyEquipmentTexture(ve, Traverse.Create(ve).Field("m_chestItem").GetValue<string>(), ve.m_bodyModel, Traverse.Create(ve).Field("m_chestItemInstances").GetValue<List<GameObject>>(), "_Chest", Traverse.Create(ve).Field("m_currentChestItemHash").GetValue<int>());
                    }
                }

                if (logDump.Any())
                    Dbgl("\n" + string.Join("\n", logDump));
            }

        }
        private static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }

        private static void LoadCustomTextures()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"CustomTextures");

            if (!Directory.Exists(path))
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
                return;
            }

            customTextures.Clear();
            cachedTextures.Clear();

            foreach (string file in Directory.GetFiles(path, "*.png", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                Dbgl($"adding {fileName} custom texture.");

                string id = Path.GetFileNameWithoutExtension(fileName);
                
                customTextures[id] = file;
            }
        }

        private static bool HasCustomTexture(string id)
        {
            return (customTextures.ContainsKey(id) || customTextures.Any(p => p.Key.StartsWith(id)));
        }

        private static Texture2D LoadTexture(string id, Texture vanilla, bool point = true, bool needLayers = false)
        {
            if (cachedTextures.ContainsKey(id))
            {
                //Dbgl($"loading cached texture for {id}");
                return cachedTextures[id];
            }

            Texture2D tex;
            var layers = customTextures.Where(p => p.Key.StartsWith($"{id}_"));

            if (!customTextures.ContainsKey(id) && !layers.Any())
            {
                if (needLayers)
                    return null;
                return (Texture2D)vanilla;
            }

            if (vanilla == null)
            {


                tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, id.EndsWith("_bump"));
                if (point)
                    tex.filterMode = FilterMode.Point;
                if (!customTextures.ContainsKey(id))
                {
                    byte[] layerData = File.ReadAllBytes(layers.First().Value);
                    tex.LoadImage(layerData);
                }
            }
            else
                tex = new Texture2D(vanilla.width, vanilla.height, TextureFormat.RGBA32, true, id.EndsWith("_bump"));

            if(point)
                tex.filterMode = FilterMode.Point;

            if (customTextures.ContainsKey(id))
            {
                //Dbgl($"loading custom texture file for {id}");
                byte[] imageData = File.ReadAllBytes(customTextures[id]);
                tex.LoadImage(imageData);
            }
            else if(vanilla != null)
            {
                //Dbgl($"texture {id} has no custom texture, using vanilla");

                // https://support.unity.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-

                // Create a temporary RenderTexture of the same size as the texture
                RenderTexture tmp = RenderTexture.GetTemporary(
                                    tex.width,
                                    tex.height,
                                    0,
                                    RenderTextureFormat.Default,
                                    RenderTextureReadWrite.Linear);

                // Blit the pixels on texture to the RenderTexture
                Graphics.Blit(vanilla, tmp);

                // Backup the currently set RenderTexture
                RenderTexture previous = RenderTexture.active;

                // Set the current RenderTexture to the temporary one we created
                RenderTexture.active = tmp;

                // Create a new readable Texture2D to copy the pixels to it
                Texture2D myTexture2D = new Texture2D(vanilla.width, vanilla.height);

                // Copy the pixels from the RenderTexture to the new Texture
                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                myTexture2D.Apply();

                // Reset the active RenderTexture
                RenderTexture.active = previous;

                // Release the temporary RenderTexture
                RenderTexture.ReleaseTemporary(tmp);

                // "myTexture2D" now has the same pixels from "texture" and it's readable.

                tex.SetPixels(myTexture2D.GetPixels());
            }
            if (layers.Any())
            {
                //Dbgl($"texture {id} has {layers.Count()} layers");
                foreach(var layer in layers.Skip(vanilla == null? 1 : 0))
                {

                    Texture2D layerTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, id.EndsWith("_bump"));
                    layerTex.filterMode = FilterMode.Point;
                    byte[] layerData = File.ReadAllBytes(layer.Value);
                    layerTex.LoadImage(layerData);

                    //8x5, 2x2

                    float scaleX = tex.width / (float)layerTex.width; // 8 / 2 = 4 or 2 / 8 = 0.25
                    float scaleY = tex.height / (float)layerTex.height; // 5 / 2 = 2.5 or 2 / 5 = 0.4

                    int width = layerTex.width;
                    int height = layerTex.width;

                    if (scaleX * scaleY < 1) // layer is bigger
                    {
                        width = tex.width;
                        height = tex.height;
                    }

                    Dbgl($"adding layer {layer.Key} to {id}, scale diff {scaleX},{scaleY}");


                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if(scaleX == 1 && scaleY == 1)
                            {
                                Color texColor = tex.GetPixel(x, y);
                                Color layerColor = layerTex.GetPixel(x, y);

                                Color final_color = Color.Lerp(texColor, layerColor, layerColor.a / 1.0f);

                                tex.SetPixel(x, y, final_color);

                            }
                            else if (scaleX * scaleY < 1) // layer is bigger
                            {

                                for (int i = 0; i < (int)(1 / scaleX); i++) // < 4, so 0, 1, 2, 3 become layer x = 0
                                {
                                    for (int j = 0; j < (int)(1 / scaleY); j++) // < 2, so 0, 1 become layer y = 0
                                    {
                                        Color texColor = tex.GetPixel(x, y);
                                        Color layerColor = layerTex.GetPixel((x * (int)(1 / scaleX)) + i, (y * (int)(1 / scaleY)) + j);
                                        
                                        if (layerColor == Color.clear)
                                            continue;

                                        Color final_color = Color.Lerp(texColor, layerColor, layerColor.a / 1.0f);
                                        final_color.a = 1f;
                                        
                                        tex.SetPixel(x, y, final_color);
                                    }
                                }
                            }
                            else // tex is bigger, multiply layer
                            {
                                for(int i = 0; i < (int)scaleX; i++) // < 4, so 0, 1, 2, 3 become layer x = 0    2 so 0,1
                                {
                                    for (int j = 0; j < (int)scaleY; j++) // < 2, so 0, 1 become layer y = 0    2 so 0,1
                                    {
                                        Color texColor = tex.GetPixel((x * (int)scaleX) + i, (y * (int)scaleY) + j);
                                        Color layerColor = layerTex.GetPixel(x, y);
                                        if (layerColor == Color.clear)
                                            continue;

                                        Color final_color = Color.Lerp(texColor, layerColor, layerColor.a / 1.0f);
                                        final_color.a = 1f;

                                        tex.SetPixel((x * (int)scaleX) + i, (y * (int)scaleY) + j, final_color);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            tex.Apply();

            cachedTextures[id] = tex;
            return tex;
        }

        private static void LoadSceneTextures(GameObject[] gos)
        {

            Dbgl($"loading {gos.Length} scene textures");

            foreach (GameObject gameObject in gos)
            {
                
                if (gameObject.name == "_NetSceneRoot")
                    continue;

                LoadOneTexture(gameObject, gameObject.name, "object");

            }

        }
        private static void ReplaceObjectDBTextures()
        {
            logDump.Clear();
            ObjectDB objectDB = ObjectDB.instance;

            Texture2D tex = LoadTexture("atlas_item_icons", objectDB.m_items[0]?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_icons[0]?.texture, false, true);
            Dbgl($"Replacing textures for {objectDB.m_items.Count} objects...");
            foreach (GameObject go in objectDB.m_items)
            {
                ItemDrop item = go.GetComponent<ItemDrop>();

                // object textures

                LoadOneTexture(go, go.name, "object");

                if (tex != null)
                {
                    //Dbgl($"Loading inventory icons for {go.name}, {item.m_itemData.m_shared.m_icons.Length} icons...");
                    for (int i = 0; i < item.m_itemData.m_shared.m_icons.Length; i++)
                    {
                        Sprite sprite = item.m_itemData.m_shared.m_icons[i];
                        float scaleX = tex.width / (float)sprite.texture.width;
                        float scaleY = tex.height / (float)sprite.texture.height;
                        float scale = (scaleX + scaleY) / 2;

                        sprite = Sprite.Create(tex, new Rect(sprite.textureRect.x * scaleX, sprite.textureRect.y * scaleY, sprite.textureRect.width * scaleX, sprite.textureRect.height * scaleY), Vector2.zero, sprite.pixelsPerUnit * scale);
                        item.m_itemData.m_shared.m_icons[i] = sprite;
                    }
                }
            }
            Traverse.Create(objectDB).Method("UpdateItemHashes").GetValue();

            if (logDump.Any())
                Dbgl("\n" + string.Join("\n", logDump));
        }

        private static void LoadOneTexture(GameObject gameObject, string thingName, string prefix)
        {
            if (thingName.Contains("_frac"))
            {
                outputDump.Add($"skipping _frac {thingName}");
                return;
            }

            //Dbgl($"loading textures for { gameObject.name}");
            MeshRenderer[] mrs = gameObject.GetComponentsInChildren<MeshRenderer>(true);
            SkinnedMeshRenderer[] smrs = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            InstanceRenderer[] irs = gameObject.GetComponentsInChildren<InstanceRenderer>(true);


            if (mrs?.Any() == true)
            {
                outputDump.Add($"{prefix} {thingName} has {mrs.Length} MeshRenderers:");
                foreach(MeshRenderer r in mrs)
                {
                    if (r == null)
                    {
                        outputDump.Add($"\tnull");
                        continue;
                    }

                    outputDump.Add($"\tMeshRenderer name: {r.name}");
                    if (r.materials == null || !r.materials.Any())
                    {
                        outputDump.Add($"\t\tmr {r.name} has no materials");
                        continue;
                    }
                    outputDump.Add($"\t\tmr {r.name} has {r.materials.Length} materials");

                    foreach (Material m in r.materials)
                    {
                        try
                        {
                            outputDump.Add($"\t\t\t{m.name}:");

                            ReplaceMaterialTextures(m, thingName, prefix, "MeshRenderer", r.name, logDump);
                        }
                        catch (Exception ex)
                        {
                            logDump.Add($"\t\t\tError loading {r.name}:\r\n{ex}");
                        }
                    }

                }
            }
            if (smrs?.Any() == true)
            {
                outputDump.Add($"{prefix} {thingName} has {smrs.Length} SkinnedMeshRenderers:");
                foreach (SkinnedMeshRenderer r in smrs)
                {
                    if (r == null)
                    {
                        outputDump.Add($"\tnull");
                        continue;
                    }

                    outputDump.Add($"\tSkinnedMeshRenderer name: {r.name}");
                    if (r.materials == null || !r.materials.Any())
                    {
                        outputDump.Add($"\t\tsmr {r.name} has no materials");
                        continue;
                    }

                    outputDump.Add($"\t\tsmr {r.name} has {r.materials.Length} materials");

                    foreach (Material m in r.materials)
                    {
                        try
                        {
                            outputDump.Add($"\t\t\t{m.name}:");

                            ReplaceMaterialTextures(m, thingName, prefix, "SkinnedMeshRenderer", r.name, logDump);
                        }
                        catch (Exception ex)
                        {
                            logDump.Add($"\t\t\tError loading {r.name}:\r\n{ex}");
                        }
                    }

                }
            }
            if (irs?.Any() == true)
            {
                outputDump.Add($"{prefix} {thingName} has {irs.Length} InstanceRenderer:");
                foreach (InstanceRenderer r in irs)
                {
                    if (r == null)
                    {
                        outputDump.Add($"\tnull");
                        continue;
                    }

                    outputDump.Add($"\tInstanceRenderer name: {r.name}");
                    if (r.m_material == null)
                    {
                        outputDump.Add($"\t\tir {r.name} has no material");
                        continue;
                    }

                    try
                    {
                        outputDump.Add($"\t\t\t{r.m_material.name}:");

                        ReplaceMaterialTextures(r.m_material, thingName, prefix, "InstanceRenderer", r.name, logDump);
                    }
                    catch (Exception ex)
                    {
                        logDump.Add($"\t\t\tError loading {r.name}:\r\n{ex}");
                    }
                }
            }

            ItemDrop item = gameObject.GetComponent<ItemDrop>();
            if (item != null && item.m_itemData.m_shared.m_armorMaterial != null)
            {
                outputDump.Add($"armor {thingName} has Material:");
                Material m = item.m_itemData.m_shared.m_armorMaterial;
                
                outputDump.Add($"\tArmor name: {m.name}");

                ReplaceMaterialTextures(m, thingName, "armor", "Armor", gameObject.name, logDump);
            }

        }

        private static void ReplaceMaterialTextures(Material m, string thingName, string prefix, string rendererType, string rendererName, List<string> logDump)
        {
            outputDump.Add("\t\t\t\tproperties:");

            if (prefix == "item")
                prefix = "object";

            foreach (string property in m.GetTexturePropertyNames())
            {
                outputDump.Add($"\t\t\t\t\t{property} {m.GetTexture(property)?.name}");

                string name = m.GetTexture(property)?.name;

                if (name == null)
                    name = thingName;

                List<string> strings = MakePrefixStrings(prefix, thingName, rendererName, name);
                CheckSetMatTextures(m, prefix, thingName, rendererType, rendererName, name, property, strings, logDump);

            }
        }

        private static void CheckSetMatTextures(Material m, string prefix, string thingName, string rendererType, string rendererName, string name, string property, List<string> strings, List<string> logDump)
        {
            foreach (string str in strings)
            {
                if (HasCustomTexture($"{str}{property}") || (property == "_MainTex" && HasCustomTexture($"{str}_texture")) || (property == "_BumpMap" && HasCustomTexture($"{str}_bump")))
                {
                    logDump.Add($"{prefix} {thingName}, {rendererType} {rendererName}, material {m.name}, texture {name}, using {str}{property}.png for {property}.");
                    if (m.HasProperty(property))
                    {
                        logDump.Add($"replacing {property}");

                        Texture2D result = null;
                        if (HasCustomTexture($"{str}{property}"))
                            result = LoadTexture($"{str}{property}", m.GetTexture(property));
                        else if(property == "_MainTex" && HasCustomTexture($"{str}_texture"))
                            result = LoadTexture($"{str}_texture", m.GetTexture(property));
                        else if(property == "_BumpMap" && HasCustomTexture($"{str}_bump"))
                            result = LoadTexture($"{str}_bump", m.GetTexture(property));

                        if (result == null)
                            continue;

                        m.SetTexture(property, result);
                        if (m.GetTexture(property) != null)
                        {
                            m.GetTexture(property).name = name;
                            m.color = Color.white;
                        }
                    }
                    break;
                }
            }
        }

        private static List<string> MakePrefixStrings(string prefix, string thingName, string rendererName, string name)
        {
            List<string> strings = new List<string>();
            strings.Add($"{prefix}_{thingName}");
            strings.Add($"{prefix}mesh_{thingName}_{rendererName}");
            strings.Add($"{prefix}texture_{thingName}_{name}");
            strings.Add($"mesh_{rendererName}");
            strings.Add($"texture_{name}");
            return strings;
        }

        private static void SetEquipmentTexture(string itemName, GameObject item)
        {
            if (item != null && itemName != null && itemName.Length > 0)
            {
                LoadOneTexture(item.gameObject, itemName, "object");
            }
        }

        private static void SetEquipmentListTexture(string itemName, List<GameObject> items)
        {
            if (items != null && items.Any() && itemName != null && itemName.Length > 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] == null)
                        continue;
                    SetEquipmentTexture(itemName, items[i]);

                }
            }
        }

        private static void SetBodyEquipmentTexture(VisEquipment instance, string itemName, SkinnedMeshRenderer smr, List<GameObject> itemInstances, string which, int hash)
        {
            LoadOneTexture(smr.gameObject, itemName, "object");

            foreach(GameObject go in itemInstances)
            {
                LoadOneTexture(go, itemName, "object");
            }
        }
    }
}
