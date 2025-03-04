using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomTextures
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {

        public static void ReplaceObjectDBTextures()
        {
            logDump.Clear();
            ObjectDB objectDB = ObjectDB.instance;

            Texture2D vanilla = null;
            foreach(var go in objectDB.m_items)
            {
                if(go?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_icons.Length > 0)
                {
                    vanilla = go.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0].texture;
                    Dbgl($"got atlas at item: {go.name}");
                    break;
                }
            }
            Texture2D tex = LoadTexture("atlas_item_icons", vanilla, false, true, true);
            Dbgl($"Replacing textures for {objectDB.m_items.Count} objects");
            foreach (GameObject go in objectDB.m_items)
            {
                ItemDrop item = go.GetComponent<ItemDrop>();

                // object textures
                ReplaceOneGameObjectTextures(go, go.name, "object");

                if (tex != null)
                {
                    //Dbgl($"sprite format {item.m_itemData.m_shared.m_icons[0].texture.format} {item.m_itemData.m_shared.m_icons[0].texture.graphicsFormat}");
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
            Traverse.Create(objectDB).Method("UpdateRegisters").GetValue();

            if (logDump.Any())
                Dbgl("\n" + string.Join("\n", logDump));
        }

        public static void ReplaceZNetSceneTextures()
        {
            logDump.Clear();

            List<GameObject> gos = new List<GameObject>();

            foreach (ClutterSystem.Clutter clutter in ClutterSystem.instance.m_clutter)
            {
                if (!gos.Contains(clutter.m_prefab))
                    gos.Add(clutter.m_prefab);
            }

            var namedPrefabs = ((Dictionary<int, GameObject>)AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs").GetValue(ZNetScene.instance)).Values;
            foreach (GameObject go in namedPrefabs)
            {
                if (go != null && !gos.Contains(go))
                    gos.Add(go);
            }

            Dbgl($"Checking {gos.Count} prefabs");

            foreach (GameObject gameObject in gos)
            {
                if (gameObject == null || gameObject.name == "_NetSceneRoot")
                    continue;
                }
                ReplaceOneGameObjectTextures(gameObject, gameObject.name, "object");
            }

            ReplaceSkyBoxTexture();

            if (logDump.Any())
                Dbgl("\n" + string.Join("\n", logDump));

        }


        public static void ReplaceSceneObjects()
        {

            SkinnedMeshRenderer[] smrs = FindObjectsOfType<SkinnedMeshRenderer>();
            MeshRenderer[] mrs = FindObjectsOfType<MeshRenderer>();
            ParticleSystemRenderer[] psrs = FindObjectsOfType<ParticleSystemRenderer>();
            InstanceRenderer[] irs = FindObjectsOfType<InstanceRenderer>();
            LineRenderer[] lrs = FindObjectsOfType<LineRenderer>();
            ItemDrop[] ids = FindObjectsOfType<ItemDrop>();
            Dbgl($"smrs {smrs.Length}, mrs {mrs.Length}, psrs {psrs.Length}, lrs {lrs.Length}, ids {ids.Length}");

            List<Component> objects = new List<Component>();
            objects.AddRange(smrs);
            objects.AddRange(mrs);
            objects.AddRange(psrs);
            objects.AddRange(irs);
            objects.AddRange(lrs);
            objects.AddRange(ids);
            foreach (var r in objects)
            {
                var t = r.transform;
                var go = r.gameObject;
                while (t.parent != null)
                {
                    if (t.GetComponent<MeshRenderer>() != null || t.GetComponent<SkinnedMeshRenderer>() != null || t.GetComponent<InstanceRenderer>() != null || t.GetComponent<LineRenderer>() != null || t.GetComponent<ParticleSystemRenderer>() != null || t.GetComponent<ItemDrop>() != null)
                    {
                        go = t.gameObject;
                    }
                    t = t.parent;
                }
                ReplaceOneGameObjectTextures(go, go.name, "object");
            }

        }
        public static void ReplaceSkyBoxTexture()
        {
            if (customTextures.ContainsKey("skybox_StarFieldTex"))
            {
                Cubemap original = RenderSettings.skybox.GetTexture("_StarFieldTex") as Cubemap;
                Dbgl($"original skybox {RenderSettings.skybox.GetTexture("_StarFieldTex").width}x{RenderSettings.skybox.GetTexture("_StarFieldTex").height} {RenderSettings.skybox.GetTexture("_StarFieldTex").graphicsFormat} {RenderSettings.skybox.GetTexture("_StarFieldTex").filterMode}");

                Cubemap cube = new Cubemap(original.width, TextureFormat.RGB24, false);

                //Cubemap cube = new Cubemap(original.width, GraphicsFormat.RGBA_BC7_SRGB, flags);
                //cube.filterMode = FilterMode.Trilinear;


                Texture2D tex = LoadTexture("skybox_StarFieldTex", null, false);
                var color = tex.GetPixels();
                // For each side
                for (int i = 0; i < 6; i++)
                {
                    /*
                    Texture2D temp = new Texture2D(RenderSettings.skybox.GetTexture("_StarFieldTex").width, RenderSettings.skybox.GetTexture("_StarFieldTex").height);
                    temp.SetPixels(cube.GetPixels((CubemapFace)i));
                    temp.Apply();
                    File.WriteAllBytes($"face{i}.png", ImageConversion.EncodeToPNG(temp));
                    */
                    cube.SetPixels(color, (CubemapFace)i);
                    cube.Apply();
                }
                RenderSettings.skybox.SetTexture("_StarFieldTex", cube);
                Dbgl($"set skybox texture");
            }
            if (customTextures.ContainsKey("skybox_MoonTex"))
            {
                Texture2D tex = LoadTexture("skybox_MoonTex", null, false);
                var color = tex.GetPixels();
                // For each side
                RenderSettings.skybox.SetTexture("_MoonTex", tex);
                Dbgl($"set moon texture");
            }
        }
        public static void ReplaceZoneSystemTextures(ZoneSystem __instance)
        {

            Dbgl($"Reloading ZoneSystem textures {__instance.name} {__instance.m_zonePrefab.name}");

            ReplaceOneZoneTextures(__instance.name, __instance.m_zonePrefab);
        }

        public static void ReplaceOneZoneTextures(string zoneSystem, GameObject prefab)
        {
            ReplaceOneGameObjectTextures(prefab, zoneSystem, "zone");

            Heightmap hm = prefab.transform.Find("Terrain")?.GetComponent<Heightmap>();
            Material mat = hm?.m_material;

            if (mat != null && AccessTools.Field(typeof(Heightmap), "m_meshRenderer").GetValue(hm) != null)
            {
                outputDump.Add($"terrain {zoneSystem}, prefab {prefab.name}");
                ReplaceMaterialTextures(prefab.name, mat, zoneSystem, "terrain", "Terrain", prefab.name, dumpSceneTextures.Value);
                hm.Regenerate();
            }
        }
        
        public static void ReplaceLocationTextures()
        {
            if (!dumpSceneTextures.Value)
            {
                try
                {
                    customTextures.First(k => k.Key.StartsWith("location"));
                }
                catch
                {
                    return;
                }
            }
            GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject gameObject in array)
            {
                if (gameObject.name == "_Locations")
                {
                    Location[] locations = gameObject.GetComponentsInChildren<Location>(true);
                    Dbgl($"Checking {locations.Length} locations");
                    foreach (Location location in locations)
                    {
                        if (!dumpSceneTextures.Value)
                        {
                            try
                            {
                                customTextures.First(k => k.Key.StartsWith("location_") && k.Key.Contains(location.gameObject.name));
                            }
                            catch
                            {
                                continue;
                            }
                        }
                        ReplaceOneGameObjectTextures(location.gameObject, location.gameObject.name, "location");
                    }
                    break;
                }
            }
        }

        public static void ReplaceHeightmapTextures()
        {

            Dbgl($"Reloading Heightmap textures for {Heightmap.GetAllHeightmaps().Count} heightmaps");

            ZoneSystem zoneSystem = (ZoneSystem)typeof(ZoneSystem).GetField("m_instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            logDump.Clear();

            Heightmap hm = (Heightmap)typeof(EnvMan).GetField("m_cachedHeightmap", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(EnvMan.instance);
            if(hm != null)
            {
                Material mat = hm.m_material;

                if (mat != null)
                {
                    outputDump.Add($"terrain {zoneSystem.name}, prefab {zoneSystem.m_zonePrefab}");
                    ReplaceMaterialTextures(zoneSystem.m_zonePrefab.name, mat, zoneSystem.name, "terrain", "Terrain", zoneSystem.m_zonePrefab.name, dumpSceneTextures.Value);
                    hm.Regenerate();
                }
            }

            foreach (Heightmap h in Heightmap.GetAllHeightmaps())
            {
                Material mat = h.m_material;

                if (mat != null)
                {
                    outputDump.Add($"terrain {zoneSystem.name}, prefab {zoneSystem.m_zonePrefab}");
                    ReplaceMaterialTextures(zoneSystem.m_zonePrefab.name, mat, zoneSystem.name, "terrain", "Terrain", zoneSystem.m_zonePrefab.name, dumpSceneTextures.Value);
                }
            }
            if (logDump.Any())
                Dbgl("\n" + string.Join("\n", logDump));
        }
        public static void ReplaceEnvironmentTextures()
        {

            Dbgl($"Reloading Environment textures");

            GameObject env = GameObject.Find("_GameMain/_Environment");
            if (env != null)
            {
                int count = env.transform.childCount;
                Dbgl($"Reloading {count} Environment textures");

                logDump.Clear();

                for(int i = 0; i < count; i++)
                {
                    GameObject gameObject = env.transform.GetChild(i).gameObject;
                    ReplaceOneGameObjectTextures(gameObject, gameObject.name, "environment");
                }
                if (logDump.Any())
                    Dbgl("\n" + string.Join("\n", logDump));
            }

        }

        public static void SetEquipmentTexture(string itemName, GameObject item)
        {
            if (item != null && itemName != null && itemName.Length > 0)
            {
                ReplaceOneGameObjectTextures(item.gameObject, itemName, "object");
            }
        }

        public static void SetEquipmentListTexture(string itemName, List<GameObject> items)
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

        public static void SetBodyEquipmentTexture(VisEquipment instance, string itemName, SkinnedMeshRenderer smr, List<GameObject> itemInstances)
        {
            if (smr != null)
                ReplaceOneGameObjectTextures(smr.gameObject, itemName, "object");
            if (itemInstances != null)
                foreach (GameObject go in itemInstances)
                {
                    ReplaceOneGameObjectTextures(go, itemName, "object");
                }
        }
    }
}
