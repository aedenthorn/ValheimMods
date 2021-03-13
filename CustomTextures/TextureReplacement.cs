using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomTextures
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {

        private static void ReplaceObjectDBTextures()
        {
            logDump.Clear();
            ObjectDB objectDB = ObjectDB.instance;

            Texture2D tex = LoadTexture("atlas_item_icons", objectDB.m_items[0]?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_icons[0]?.texture, false, true, true);
            Dbgl($"Replacing textures for {objectDB.m_items.Count} objects...");
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
            Traverse.Create(objectDB).Method("UpdateItemHashes").GetValue();

            if (logDump.Any())
                Dbgl("\n" + string.Join("\n", logDump));
        }

        private static void ReplaceSceneTextures(GameObject[] gos)
        {

            Dbgl($"loading {gos.Length} scene textures");

            foreach (GameObject gameObject in gos)
            {

                if (gameObject.name == "_NetSceneRoot")
                    continue;

                ReplaceOneGameObjectTextures(gameObject, gameObject.name, "object");

            }

        }
        private static void ReplaceZoneSystemTextures(ZoneSystem __instance)
        {

            Dbgl($"Reloading ZoneSystem textures {__instance.name} {__instance.m_zonePrefab.name}");

            ReplaceOneZoneTextures(__instance.name, __instance.m_zonePrefab);

        }

        private static void ReplaceOneZoneTextures(string zoneSystem, GameObject prefab)
        {
            ReplaceOneGameObjectTextures(prefab, zoneSystem, "zone");

            Heightmap hm = prefab.transform.Find("Terrain")?.GetComponent<Heightmap>();
            Material mat = hm?.m_material;

            if (mat != null)
            {
                outputDump.Add($"terrain {zoneSystem}, prefab {prefab.name}");
                ReplaceMaterialTextures(mat, zoneSystem, "terrain", "Terrain", prefab.name, logDump);
                hm.Regenerate();
            }
        }

        private static void ReplaceHeightmapTextures()
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
                    ReplaceMaterialTextures(mat, zoneSystem.name, "terrain", "Terrain", zoneSystem.m_zonePrefab.name, logDump);
                    hm.Regenerate();
                }
            }

            foreach (Heightmap h in Heightmap.GetAllHeightmaps())
            {
                Material mat = h.m_material;

                if (mat != null)
                {
                    outputDump.Add($"terrain {zoneSystem.name}, prefab {zoneSystem.m_zonePrefab}");
                    ReplaceMaterialTextures(mat, zoneSystem.name, "terrain", "Terrain", zoneSystem.m_zonePrefab.name, logDump);
                }
            }
            if (logDump.Any())
                Dbgl("\n" + string.Join("\n", logDump));
        }
        private static void ReplaceEnvironmentTextures()
        {

            Dbgl($"Reloading Environment textures");

            GameObject env = GameObject.Find("_GameMain/environment");
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

        private static void SetEquipmentTexture(string itemName, GameObject item)
        {
            if (item != null && itemName != null && itemName.Length > 0)
            {
                ReplaceOneGameObjectTextures(item.gameObject, itemName, "object");
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

        private static void SetBodyEquipmentTexture(VisEquipment instance, string itemName, SkinnedMeshRenderer smr, List<GameObject> itemInstances)
        {
            if (smr != null)
                ReplaceOneGameObjectTextures(smr.gameObject, itemName, "object");
            if (itemInstances != null)
                foreach (GameObject go in itemInstances)
                   ReplaceOneGameObjectTextures(go, itemName, "object");
        }
    }
}
