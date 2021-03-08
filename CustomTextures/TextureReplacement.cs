using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomTextures
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {

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

            foreach (GameObject go in itemInstances)
            {
                LoadOneTexture(go, itemName, "object");
            }
        }
    }
}
