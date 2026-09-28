using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CustomTextures
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static void ReplaceOneGameObjectTextures(GameObject gameObject, string thingName, string prefix, int? creatureLevel = null, bool force = false)
        {
            if (gameObject == null)
                return;

            int instanceId = gameObject.GetInstanceID();
            if (!force && reloadedObjects.Contains(instanceId))
                return;

            if (!reloadedObjects.Contains(instanceId))
                reloadedObjects.Add(instanceId);

            if (!creatureLevel.HasValue)
                creatureLevel = GetLiveCreatureLevel(gameObject);
            if (thingName.Contains("_frac"))
            {
                if(dumpSceneTextures.Value)
                    outputDump.Add($"skipping _frac {thingName}");
                return;
            }
            //stopwatch.Restart();

            bool dump = dumpSceneTextures.Value;

            if (dump && thingName.EndsWith("(Clone)"))
                dump = false;

            //Dbgl($"loading textures for { gameObject.name}");
            MeshRenderer[] mrs = gameObject.GetComponentsInChildren<MeshRenderer>(true);
            SkinnedMeshRenderer[] smrs = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            InstanceRenderer[] irs = gameObject.GetComponentsInChildren<InstanceRenderer>(true);
            ParticleSystemRenderer[] prs = gameObject.GetComponentsInChildren<ParticleSystemRenderer>(true);
            LineRenderer[] lrs = gameObject.GetComponentsInChildren<LineRenderer>(true);


            if (mrs.Length > 0)
            {
                if(dump)
                    outputDump.Add($"{prefix} {thingName} has {mrs.Length} MeshRenderers:");
                foreach (MeshRenderer r in mrs)
                {
                    if (r == null)
                    {
                        if (dump)
                            outputDump.Add($"\tnull");
                        continue;
                    }

                    if (dump)
                        outputDump.Add($"\tMeshRenderer name: {r.name}");
                    if (r.materials == null || !r.materials.Any())
                    {
                        if (dump)
                            outputDump.Add($"\t\trenderer {r.name} has no materials");
                        continue;
                    }
                    if (dump)
                        outputDump.Add($"\t\trenderer {r.name} has {r.materials.Length} materials");

                    foreach (Material m in r.materials)
                    {
                        if (m == null)
                            continue;

                        try
                        {
                            if (dump)
                                outputDump.Add($"\t\t\t{m.name}:");

                            ReplaceMaterialTextures(gameObject.name, m, thingName, prefix, "MeshRenderer", r.name, dump, creatureLevel);
                        }
                        catch (Exception ex)
                        {
                             logDump.Add($"\t\t\tError loading {r.name}:\r\n\r\n\t\t\t{ex}");
                        }
                    }

                }
            }
            if (smrs.Length > 0)
            {
                if (dump)
                    outputDump.Add($"{prefix} {thingName} has {smrs.Length} SkinnedMeshRenderers:");
                foreach (SkinnedMeshRenderer r in smrs)
                {
                    if (r == null)
                    {
                        if (dump)
                            outputDump.Add($"\tnull");
                        continue;
                    }
                    if (dump)
                        outputDump.Add($"\tSkinnedMeshRenderer name: {r.name}");
                    if (r.materials == null || !r.materials.Any())
                    {
                        if (dump)
                            outputDump.Add($"\t\tsmr {r.name} has no materials");
                        continue;
                    }

                    if (dump)
                        outputDump.Add($"\t\tsmr {r.name} has {r.materials.Length} materials");

                    foreach (Material m in r.materials)
                    {
                        if (m == null)
                            continue;

                        try
                        {
                            if (dump)
                                outputDump.Add($"\t\t\t{m.name}:");

                            ReplaceMaterialTextures(gameObject.name, m, thingName, prefix, "SkinnedMeshRenderer", r.name, dump, creatureLevel);
                        }
                        catch (Exception ex)
                        {
                            logDump.Add($"\t\t\tError loading {r.name}:\r\n\r\n\t\t\t{ex}");
                        }
                    }

                }
            }
            if (irs.Length > 0)
            {
                if (dump)
                    outputDump.Add($"{prefix} {thingName} has {irs.Length} InstanceRenderer:");
                foreach (InstanceRenderer r in irs)
                {
                    if (r == null)
                    {
                        if (dump)
                            outputDump.Add($"\tnull");
                        continue;
                    }

                    if (dump)
                        outputDump.Add($"\tInstanceRenderer name: {r.name}");
                    if (r.m_material == null)
                    {
                        if (dump)
                            outputDump.Add($"\t\tir {r.name} has no material");
                        continue;
                    }

                    try
                    {
                        if (dump)
                            outputDump.Add($"\t\t\t{r.m_material.name}:");

                        ReplaceMaterialTextures(gameObject.name, r.m_material, thingName, prefix, "InstanceRenderer", r.name, dump, creatureLevel);
                    }
                    catch (Exception ex)
                    {
                        logDump.Add($"\t\t\tError loading {r.name}:\r\n\r\n\t\t\t{ex}");
                    }
                }
            }
            if (prs.Length > 0)
            {
                if (dump)
                    outputDump.Add($"{prefix} {thingName} has {prs.Length} ParticleSystemRenderers:");
                foreach (ParticleSystemRenderer r in prs)
                {
                    if (r == null)
                    {
                        if (dump)
                            outputDump.Add($"\tnull");
                        continue;
                    }

                    if (dump)
                        outputDump.Add($"\tParticleSystemRenderer name: {r.name}");
                    foreach (Material m in r.materials)
                    {
                        if (m == null)
                            continue;

                        try
                        {
                            if (dump)
                                outputDump.Add($"\t\t\t{m.name}:");

                            ReplaceMaterialTextures(gameObject.name, m, thingName, prefix, "ParticleSystemRenderer", r.name, dump, creatureLevel);
                        }
                        catch (Exception ex)
                        {
                            logDump.Add($"\t\t\tError loading {r.name}:\r\n\r\n\t\t\t{ex}");
                        }
                    }
                }
            }
            if (lrs.Length > 0)
            {
                if (dump)
                    outputDump.Add($"{prefix} {thingName} has {lrs.Length} LineRenderers:");
                foreach (LineRenderer r in lrs)
                {
                    if (r == null)
                    {
                        if (dump)
                            outputDump.Add($"\tnull");
                        continue;
                    }

                    if (dump)
                        outputDump.Add($"\tLineRenderers name: {r.name}");
                    foreach (Material m in r.materials)
                    {
                        if (m == null)
                            continue;

                        try
                        {
                            if (dump)
                                outputDump.Add($"\t\t\t{m.name}:");

                            ReplaceMaterialTextures(gameObject.name, m, thingName, prefix, "LineRenderer", r.name, dump, creatureLevel);
                        }
                        catch (Exception ex)
                        {
                             logDump.Add($"\t\t\tError loading {r.name}:\r\n\r\n\t\t\t{ex}");
                        }
                    }
                }
            }
            ItemDrop[] items = gameObject.GetComponentsInChildren<ItemDrop>();
            foreach(ItemDrop item in items)
            {
                if (item != null && item.m_itemData.m_shared.m_armorMaterial != null)
                {
                    if (dump)
                        outputDump.Add($"armor {thingName} has Material:");
                    Material m = item.m_itemData.m_shared.m_armorMaterial;

                    if (dump)
                        outputDump.Add($"\tArmor name: {m.name}");

                    ReplaceMaterialTextures(gameObject.name, m, thingName, "armor", "Armor", gameObject.name, dump, creatureLevel);
                }
            }
            //LogStopwatch("OneObject");
        }

        public static void ReplaceMaterialTextures(string goName, Material m, string thingName, string prefix, string rendererType, string rendererName, bool dump, int? creatureLevel = null)
        {
            if (m == null)
                return;

            if (dumpSceneTextures.Value)
                outputDump.Add("\t\t\t\tproperties:");

            if (prefix == "item")
                prefix = "object";

            string mainTexName = null;
            if (m.HasProperty("_MainTex"))
                mainTexName = m.GetTexture("_MainTex")?.name;

            foreach (string property in m.GetTexturePropertyNames())
            {
                if (dumpSceneTextures.Value)
                    outputDump.Add($"\t\t\t\t\t{property} {m.GetTexture(property)?.name}");

                int propHash = Shader.PropertyToID(property);

                string name = m.GetTexture(propHash)?.name;

                if (name == null)
                    name = thingName;

                CheckSetMatTextures(goName, m, prefix, thingName, rendererType, rendererName, name, property, creatureLevel, mainTexName);

            }
        }

        public static void CheckSetMatTextures(string goName, Material m, string prefix, string thingName, string rendererType, string rendererName, string name, string property, int? creatureLevel = null, string mainTexName = null)
        {
            foreach (string str in MakePrefixStrings(prefix, thingName, rendererName, m.name, name, mainTexName))
            {
                string texId = str + property;
                string legacyId = null;
                if (property == "_MainTex")
                    legacyId = str + "_texture";
                else if (property == "_BumpMap")
                    legacyId = str + "_bump";
                else if (property == "_StyleTex")
                    legacyId = str + "_style";

                bool match = ShouldApplyCustomTexture(texId, creatureLevel);
                bool legacy = legacyId != null && ShouldApplyCustomTexture(legacyId, creatureLevel);
                if (!match && !legacy)
                    continue;

                int propHash = Shader.PropertyToID(property);
                if (m.HasProperty(propHash))
                {
                    string usedId = match ? texId : legacyId;
                    Dbgl($"{prefix} {thingName}, {rendererType} {rendererName}, material {m.name}, texture {name}, using {usedId} for {property}.");

                    Texture vanilla = m.GetTexture(propHash);

                    bool isBump = property.Contains("Bump") || property.Contains("Normal");

                    Texture2D result = LoadTexture(usedId, vanilla, isBump, true, true, false, creatureLevel);

                    if (result == null)
                        continue;

                    result.name = name;

                    m.SetTexture(propHash, result);
                    if (property == "_MainTex" && m.HasProperty("_Color"))
                        m.SetColor("_Color", Color.white);
                    if (property == "_EmissionMap" || property == "_EmissiveTex")
                        ApplyEmission(m);
                    break;
                }
            }
        }

        public static void ApplyEmission(Material m)
        {
            if (m == null)
                return;
            if (m.HasProperty("_EmissionColor"))
                m.SetColor("_EmissionColor", Color.white * emissionIntensity.Value);
            m.EnableKeyword("_EMISSION");
        }

        public static string[] MakePrefixStrings(string prefix, string thingName, string rendererName, string matName, string name, string mainTexName = null)
        {
            List<string> names = new List<string>();
            if (!string.IsNullOrEmpty(name))
                names.Add(name);
            if (!string.IsNullOrEmpty(mainTexName) && mainTexName != name)
                names.Add(mainTexName);

            List<string> prefixes = new List<string>();
            foreach (string n in names)
            {
                prefixes.AddRange(MakePrefixStringsForName(prefix, thingName, rendererName, matName, n));
                if (thingName.EndsWith("(Clone)"))
                {
                    string trimmed = thingName.Substring(0, thingName.Length - "(Clone)".Length);
                    prefixes.AddRange(MakePrefixStringsForName(prefix, trimmed, rendererName, matName, n));
                }
            }
            return prefixes.Distinct().ToArray();
        }

        public static string[] MakePrefixStringsForName(string prefix, string thingName, string rendererName, string matName, string name)
        {
            return new string[]
            {
                prefix+"_"+thingName,
                prefix+"mesh_"+thingName+"_"+rendererName,
                prefix+"renderer_"+thingName+"_"+rendererName,
                prefix+"mat_"+thingName+"_"+matName,
                prefix+"renderermat_"+thingName+"_"+rendererName+"_"+matName,
                prefix+"texture_"+thingName+"_"+name,
                "mesh_"+rendererName,
                "renderer_"+rendererName,
                "mat_"+matName,
                "texture_"+name
            };
        }

        public static Action<Texture2D, byte[]> loadPng;

        public static void LoadPng(Texture2D texture, byte[] data)
        {
            if (loadPng == null)
                loadPng = CreateLoadPng();
            loadPng(texture, data);
        }

        public static Action<Texture2D, byte[]> CreateLoadPng()
        {
            MethodInfo byteArrayMethod = null;
            MethodInfo spanMethod = null;
            Type spanParam = null;
            foreach (MethodInfo method in typeof(ImageConversion).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "LoadImage")
                    continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 3 || parameters[0].ParameterType != typeof(Texture2D) || parameters[2].ParameterType != typeof(bool))
                    continue;
                if (parameters[1].ParameterType == typeof(byte[]))
                    byteArrayMethod = method;
                else
                {
                    spanMethod = method;
                    spanParam = parameters[1].ParameterType;
                }
            }
            if (byteArrayMethod != null)
            {
                return (tex, bytes) => byteArrayMethod.Invoke(null, new object[] { tex, bytes, false });
            }
            if (spanMethod == null || spanParam == null)
                throw new MissingMethodException("ImageConversion.LoadImage");

            MethodInfo implicitConv = spanParam.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(byte[]) }, null);
            if (implicitConv == null)
                throw new MissingMethodException("ReadOnlySpan<byte>.op_Implicit");

            DynamicMethod dynamicMethod = new DynamicMethod("LoadPng", typeof(void), new[] { typeof(Texture2D), typeof(byte[]) }, typeof(BepInExPlugin).Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, implicitConv);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, spanMethod);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            return (Action<Texture2D, byte[]>)dynamicMethod.CreateDelegate(typeof(Action<Texture2D, byte[]>));
        }


        public static Texture2D LoadTexture(string id, Texture vanilla, bool isBump, bool point = true, bool needCustom = false, bool isSprite = false, int? creatureLevel = null)
        {
            Texture2D texture;
            id = ResolveTextureId(id, creatureLevel);
            if (cachedTextures.ContainsKey(id))
            {
                logDump.Add($"loading cached texture for {id}");
                texture = cachedTextures[id];
                if (customTextures.ContainsKey(id))
                {
                    if (customTextures[id].Contains("bilinear"))
                    {
                        texture.filterMode = FilterMode.Bilinear;
                    }
                    else if (customTextures[id].Contains("trilinear"))
                    {
                        texture.filterMode = FilterMode.Trilinear;
                    }
                    else if (customTextures[id].Contains($"{Path.DirectorySeparatorChar}point{Path.DirectorySeparatorChar}"))
                    {
                        texture.filterMode = FilterMode.Trilinear;
                    }
                    else if (point)
                        texture.filterMode = FilterMode.Point;
                }
                return texture;
            }

            var layers = customTextures.Where(p => p.Key.StartsWith(id+"_") && !IsCreatureLevelVariantKey(p.Key, id));

            if (!customTextures.ContainsKey(id) && layers.Count() == 0)
            {
                if (needCustom)
                    return null;
                return (Texture2D)vanilla;
            }

            logDump.Add($"loading custom texture for {id} {layers.Count()} layers");


            if (vanilla == null)
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, isBump);
                if (!customTextures.ContainsKey(id))
                {
                    byte[] layerData = File.ReadAllBytes(layers.First().Value);
                    LoadPng(texture, layerData);
                }
            }
            else
                texture = new Texture2D(vanilla.width, vanilla.height, TextureFormat.RGBA32, true, isBump);

            if (customTextures.ContainsKey(id))
            {
                if (customTextures[id].Contains($"{Path.DirectorySeparatorChar}bilinear{Path.DirectorySeparatorChar}"))
                {
                    texture.filterMode = FilterMode.Bilinear;
                }
                else if (customTextures[id].Contains($"{Path.DirectorySeparatorChar}trilinear{Path.DirectorySeparatorChar}"))
                {
                    texture.filterMode = FilterMode.Trilinear;
                }
                else if (customTextures[id].Contains($"{Path.DirectorySeparatorChar}point{Path.DirectorySeparatorChar}"))
                {
                    texture.filterMode = FilterMode.Trilinear;
                }
                else if (point)
                    texture.filterMode = FilterMode.Point;
            }
            else if (point)
                texture.filterMode = FilterMode.Point;

            if (customTextures.ContainsKey(id))
            {
                logDump.Add($"loading custom texture file for {id}");
                byte[] imageData = File.ReadAllBytes(customTextures[id]);
                LoadPng(texture, imageData);
            }
            else if (vanilla != null)
            {
                Dbgl($"texture {id} has no custom texture, using vanilla");

                // https://support.unity.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-

                // Create a temporary RenderTexture of the same size as the texture
                RenderTexture tmp = RenderTexture.GetTemporary(
                                    texture.width,
                                    texture.height,
                                    0,
                                    RenderTextureFormat.Default,
                                    isBump ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);

                // Blit the pixels on texture to the RenderTexture
                Graphics.Blit(vanilla, tmp);

                // Backup the currently set RenderTexture
                RenderTexture previous = RenderTexture.active;

                // Set the current RenderTexture to the temporary one we created
                RenderTexture.active = tmp;

                // Create a new readable Texture2D to copy the pixels to it
                Texture2D myTexture2D = new Texture2D(vanilla.width, vanilla.height, TextureFormat.RGBA32, true, isBump);

                // Copy the pixels from the RenderTexture to the new Texture
                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                myTexture2D.Apply();

                // Reset the active RenderTexture
                RenderTexture.active = previous;

                // Release the temporary RenderTexture
                RenderTexture.ReleaseTemporary(tmp);

                // "myTexture2D" now has the same pixels from "texture" and it's readable.

                texture.SetPixels(myTexture2D.GetPixels());
                texture.Apply();
            }
            if (layers.Count() > 0)
            {
                Dbgl($"texture {id} has {layers.Count()} layers");
                foreach (var layer in layers.Skip(vanilla == null && !customTextures.ContainsKey(id) ? 1 : 0))
                {

                    Texture2D layerTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, isBump);
                    layerTex.filterMode = isSprite ? FilterMode.Bilinear : FilterMode.Point;
                    byte[] layerData = File.ReadAllBytes(layer.Value);
                    LoadPng(layerTex, layerData);

                    int layerx = 0;
                    int layery = 0;
                    int layerw = layerTex.width;
                    int layerh = layerTex.height;

                    if (isSprite)
                    {
                        string[] coords = layer.Key.Substring(id.Length+1).Split('_');
                        if (coords.Length != 4 || !int.TryParse(coords[0], out layerx) || !int.TryParse(coords[1], out layery) || !int.TryParse(coords[2], out layerw) || !int.TryParse(coords[3], out layerh))
                        {
                            //logDump.Add($"Improper sprite layer format {layer.Key}");
                            continue;
                        }
                        else
                        {
                            //logDump.Add($"sprite coords {layerx},{layery}, layer sheet size {layerw},{layerh}");
                        }
                    }

                    //8x5, 2x2

                    float scale = texture.width / (float)layerw; // 8 / 2 = 4 or 2 / 8 = 0.25
                    float scaleY = texture.height / (float)layerh; // 5 / 2 = 2.5 or 2 / 5 = 0.4

                    if(scale != scaleY)
                    {
                        //logDump.Add($"incompatible image ratios {tex.width},{tex.height} {layerw},{layerh}");
                        continue;
                    }


                    logDump.Add($"adding layer {layer.Key} to {id}, scale diff {scale}");

                    int startx = 0;
                    int starty = 0;
                    int endx = layerTex.width;
                    int endy = layerTex.height;


                    if (isSprite)
                    {

                        startx = layerx;
                        starty = layery;
                        endx = layerx + layerTex.width;
                        endy = layery + layerTex.height;
                    }

                    // scale

                    if (scale < 1) // layer is bigger, increase tex size
                    {
                        //logDump.Add($"scaling texture up");

                        TextureScale.Bilinear(texture, (int)(texture.width / scale), (int)(texture.height / scale));
                    }
                    else if (scale > 1) // increase layer size
                    {
                        //logDump.Add($"scaling layer up");

                        TextureScale.Bilinear(layerTex, (int)(layerTex.width * scale), (int)(layerTex.height * scale));

                        startx = (int)(layerx * scale);
                        starty = (int)(layery * scale);
                        endx = (int)((layerx + layerTex.width) * scale);
                        endy = (int)((layery + layerTex.height) * scale);
                    }

                    //logDump.Add($"startx {startx}, endx {endx}, starty {starty}, endy {endy}");

                    List<string> coordsl = new List<string>();

                    for(int x = startx; x < endx; x++)
                    {
                        for (int y = starty; y < endy; y++)
                        {
                            int lx = x - startx;
                            int ly = y - starty;

                            Color layerColor = layerTex.GetPixel(lx, ly);

                            if (isSprite)
                            {
                                layerColor = layerTex.GetPixel(lx, layerTex.height - ly);
                                //coordsl.Add($"{x},{y} {lx},{ly} {layerColor}");
                                texture.SetPixel(x, texture.height - y, layerColor);
                            }
                            else
                            {
                                if (layerColor.a == 0)
                                    continue;
                                //coordsl.Add($"{x},{y} {lx},{ly} {layerColor}");
                                Color texColor = texture.GetPixel(x, y);

                                Color final_color = Color.Lerp(texColor, layerColor, layerColor.a / 1.0f);

                                texture.SetPixel(x, y, final_color);
                            }
                        }
                    }
                    //Dbgl(string.Join("\n", coordsl));
                    texture.Apply();
                }
                if (false)
                {
                    //Dbgl($"tex {tex.width},{tex.height}");
                    //byte[] bytes = ImageConversion.EncodeToPNG(tex);
                    //string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), id + "_test.png");
                    //File.WriteAllBytes(path, bytes);
                }
            }

            cachedTextures[id] = texture;
            return texture;
        }
    }
}
