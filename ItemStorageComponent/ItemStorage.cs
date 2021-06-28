using System;
using System.IO;
using UnityEngine;

namespace ItemStorageComponent
{
    public class ItemStorage
    {
        public Inventory inventory;
        public ItemStorageMeta meta;
        public string guid = Guid.NewGuid().ToString();

        public ItemStorage(ItemDrop.ItemData item, string oldGuid)
        {
            guid = oldGuid;
            meta = new ItemStorageMeta()
            {
                itemName = item.m_shared.m_name.Replace("$", "")
            };
            inventory = new Inventory(item.m_shared.m_name.Replace("$", ""), null, meta.width, meta.height);
            BepInExPlugin.Dbgl($"Created new item storage {meta.itemName} {oldGuid}");
        }
        public ItemStorage(string itemFile)
        {
            string[] parts = Path.GetFileNameWithoutExtension(itemFile).Split('_');
            string templateFile = Path.Combine(BepInExPlugin.templatesPath, parts[0] + ".json");

            BepInExPlugin.Dbgl($"Loading item storage {parts[0]} {parts[1]}");

            guid = parts[1];
            if (File.Exists(templateFile))
            {
                BepInExPlugin.Dbgl("Loading template data");
                meta = JsonUtility.FromJson<ItemStorageMeta>(File.ReadAllText(templateFile));
            }
            else
            {
                BepInExPlugin.Dbgl("Creating new template data");
                meta = new ItemStorageMeta()
                {
                    itemName = parts[0]
                };
            }

            inventory = new Inventory(meta.itemName, null, meta.width, meta.height);

            if (File.Exists(itemFile))
            {
                string input = File.ReadAllText(itemFile);
                ZPackage pkg = new ZPackage(input);
                inventory.Load(pkg);
                BepInExPlugin.Dbgl($"Loaded existing inventory with {inventory.NrOfItems()} items");
            }
        }
    }

    public class ItemStorageMeta
    {
        public string itemName;
        public int width = 4;
        public int height = 2;
        public float weightMult = 1f;
    }
}