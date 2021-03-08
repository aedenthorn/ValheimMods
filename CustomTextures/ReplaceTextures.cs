using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomTextures
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
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
                foreach (MeshRenderer r in mrs)
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
                if (ShouldLoadCustomTexture($"{str}{property}") || (property == "_MainTex" && ShouldLoadCustomTexture($"{str}_texture")) || (property == "_BumpMap" && ShouldLoadCustomTexture($"{str}_bump")))
                {
                    logDump.Add($"{prefix} {thingName}, {rendererType} {rendererName}, material {m.name}, texture {name}, using {str}{property}.png for {property}.");
                    if (m.HasProperty(property))
                    {
                        logDump.Add($"replacing {property}");

                        Texture2D result = null;
                        if (ShouldLoadCustomTexture($"{str}{property}"))
                            result = LoadTexture($"{str}{property}", m.GetTexture(property));
                        else if (property == "_MainTex" && ShouldLoadCustomTexture($"{str}_texture"))
                            result = LoadTexture($"{str}_texture", m.GetTexture(property));
                        else if (property == "_BumpMap" && ShouldLoadCustomTexture($"{str}_bump"))
                            result = LoadTexture($"{str}_bump", m.GetTexture(property));

                        if (result == null)
                            continue;

                        m.SetTexture(property, result);
                        if (m.GetTexture(property) != null)
                        {
                            m.GetTexture(property).name = "xyz"+name;
                            if(property == "_MainTex")
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


        private static Texture2D LoadTexture(string id, Texture vanilla, bool point = true, bool needLayers = false)
        {
            if (cachedTextures.ContainsKey(id))
            {
                //Dbgl($"loading cached texture for {id}");
                return cachedTextures[id];
            }

            bool isBump = id.EndsWith("_bump") || id.EndsWith("BumpMap");

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
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, isBump);
                if (point)
                    tex.filterMode = FilterMode.Point;
                if (!customTextures.ContainsKey(id))
                {
                    byte[] layerData = File.ReadAllBytes(layers.First().Value);
                    tex.LoadImage(layerData);
                }
            }
            else
                tex = new Texture2D(vanilla.width, vanilla.height, TextureFormat.RGBA32, true, isBump);

            if (point)
                tex.filterMode = FilterMode.Point;

            if (customTextures.ContainsKey(id))
            {
                //Dbgl($"loading custom texture file for {id}");
                byte[] imageData = File.ReadAllBytes(customTextures[id]);
                tex.LoadImage(imageData);
            }
            else if (vanilla != null)
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
                foreach (var layer in layers.Skip(vanilla == null ? 1 : 0))
                {

                    Texture2D layerTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, isBump);
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
                            if (scaleX == 1 && scaleY == 1)
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
                                for (int i = 0; i < (int)scaleX; i++) // < 4, so 0, 1, 2, 3 become layer x = 0    2 so 0,1
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
    }
}
