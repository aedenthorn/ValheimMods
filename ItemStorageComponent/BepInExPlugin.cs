using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ItemStorageComponent
{
    [BepInPlugin("aedenthorn.ItemStorageComponent", "Item Storage Component", "0.5.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> requireEquipped;
        public static ConfigEntry<bool> requireExistingTemplate;

        //public static GameObject backpack;
        public static ItemStorage itemStorage;
        public static Container playerContainer;
        public static Dictionary<string, ItemStorage> itemStorageDict = new Dictionary<string, ItemStorage>();
        public static Dictionary<string, ItemStorageMeta> itemStorageMetaDict = new Dictionary<string, ItemStorageMeta>();
        
        public static string assetPath;
        public static string templatesPath;
        public static string itemsPath;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1347, "Nexus mod ID for updates");
            nexusID.Value = 1347;

            requireExistingTemplate = Config.Bind<bool>("Variables", "RequireExistingTemplate", true, "Storage template for item must exist to create inventory. (Otherwise a new template will be created)");
            requireEquipped = Config.Bind<bool>("Variables", "RequireEquipped", true, "Item must be equipped to open inventory");

            if (!modEnabled.Value)
                return;
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            templatesPath = Path.Combine(assetPath, "templates");
            itemsPath = Path.Combine(assetPath, "items");
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
                Directory.CreateDirectory(templatesPath);
                Directory.CreateDirectory(itemsPath);
            }

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public static void OpenItemStorage(ItemDrop.ItemData item)
        {
            if(requireExistingTemplate.Value && !itemStorageMetaDict.ContainsKey(item.m_dropPrefab.name))
            {
                Dbgl($"Template file required for {item.m_dropPrefab.name}");
                return;
            }
            
            Dbgl($"Opening storage for item {item.m_shared.m_name}");

            string guid;
            if (!item.m_crafterName.Contains("_") || item.m_crafterName.Split('_')[item.m_crafterName.Split('_').Length - 1].Length != 36)
            {
                Dbgl("Item has no storage, creating new storage");
                guid = Guid.NewGuid().ToString();
                item.m_crafterName += "_" + guid;
            }
            else
            {
                guid = item.m_crafterName.Split('_')[1];
                Dbgl($"Item has storage, loading storage {guid}");
            }

            if (!itemStorageDict.ContainsKey(guid))
            {
                Dbgl("Storage not found, creating new storage");
                itemStorageDict[guid] = new ItemStorage(item, guid);
                SaveInventory(itemStorageDict[guid]);
            }

            itemStorage = itemStorageDict[guid];
            playerContainer = Player.m_localPlayer.gameObject.GetComponent<Container>();
            if (!playerContainer)
            {
                playerContainer = Player.m_localPlayer.gameObject.AddComponent<Container>();
            }
            playerContainer.m_name = itemStorage.meta.itemName;
            AccessTools.FieldRefAccess<Container, Inventory>(playerContainer, "m_inventory") = itemStorage.inventory;
            InventoryGui.instance.Show(playerContainer);
        }
        public static void LoadDataFromDisk()
        {
            Dbgl("Loading item inventories");

            if (!Directory.Exists(templatesPath))
            {
                Directory.CreateDirectory(templatesPath);
            }
            if (!Directory.Exists(Path.Combine(itemsPath)))
            {
                Directory.CreateDirectory(itemsPath);
            }
            foreach(string itemFile in Directory.GetFiles(itemsPath))
            {
                try
                {
                    ItemStorage itemStorage = new ItemStorage(itemFile);
                    itemStorageDict[itemStorage.guid] = itemStorage;
                }
                catch (Exception ex)
                {
                    Dbgl($"Item file {itemFile} corrupt!\n{ex}");
                }
            }
            foreach(string templateFile in Directory.GetFiles(templatesPath))
            {
                try
                {
                    ItemStorageMeta meta = JsonUtility.FromJson<ItemStorageMeta>(File.ReadAllText(templateFile));
                    itemStorageMetaDict[Path.GetFileNameWithoutExtension(templateFile)] = meta;
                }
                catch (Exception ex)
                {
                    Dbgl($"Template file {templateFile} corrupt!\n{ex}");
                }
            }
        }

        public static void OnSelectedItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
        {
            if (!modEnabled.Value || !CanBeContainer(item) || mod != InventoryGrid.Modifier.Split)
                return;
            bool same = false;
            if (InventoryGui.instance.IsContainerOpen() && itemStorage != null)
            {
                same = item.m_crafterName.EndsWith("_" + itemStorage.guid);

                CloseContainer();
            }
            if (!same && item != null && item.m_shared.m_maxStackSize <= 1 && (!requireEquipped.Value || item.m_equipped))
            {
                OpenItemStorage(item);
            }

        }

        public static void CloseContainer()
        {
            itemStorage.inventory = playerContainer.GetInventory();
            SaveInventory(itemStorage);
            typeof(InventoryGui).GetMethod("CloseContainer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InventoryGui.instance, new object[] { });
            playerContainer = null;
            itemStorage = null;
        }
        
        public static bool CanBeContainer(ItemDrop.ItemData item)
        {
            return item != null && (!requireEquipped.Value || item.m_equipped) && (!requireExistingTemplate.Value || itemStorageMetaDict.ContainsKey(item.m_dropPrefab.name)) && item.m_shared.m_maxStackSize <= 1;
        }

        [HarmonyPatch(typeof(FejdStartup), "Start")]
        public static class FejdStartup_Start_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                LoadDataFromDisk();
            }
        }                
        
        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        public static class InventoryGui_Awake_Patch
        {
            public static void Postfix(InventoryGrid ___m_playerGrid)
            {
                if (!modEnabled.Value)
                    return;

                ___m_playerGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(___m_playerGrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(OnSelectedItem));

            }
        }                
        
        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        public static class InventoryGui_OnSelectedItem_Patch
        {
            public static bool Prefix(ItemDrop.ItemData item, InventoryGrid.Modifier mod)
            {
                var result = !modEnabled.Value || !CanBeContainer(item) || mod != InventoryGrid.Modifier.Split;
                //Dbgl("result " + result + " " + !modEnabled.Value + " " + !CanBeContainer(item) + " "+(mod != InventoryGrid.Modifier.Split));
                return result;
            }
        }                

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        public static class InventoryGui_Update_Patch
        {
            public static void Postfix(Animator ___m_animator, ref Container ___m_currentContainer, ItemDrop.ItemData ___m_dragItem)
            {

                if (!modEnabled.Value || ___m_animator.GetBool("visible") || playerContainer == null || itemStorage == null)
                    return;

                CloseContainer();
            }
        }
        public static void SaveInventory(ItemStorage itemStorage)
        {
            string itemFile = Path.Combine(itemsPath, itemStorage.meta.itemId + "_" + itemStorage.guid);
            if (!File.Exists(itemFile) && itemStorage.inventory.NrOfItems() == 0)
                return;

            Dbgl($"Saving {itemStorage.inventory.GetAllItems().Count} items from inventory for item {itemStorage.guid}, type {itemStorage.meta.itemId}");

            ZPackage zpackage = new ZPackage();
            itemStorage.inventory.Save(zpackage);

            string data = zpackage.GetBase64();
            File.WriteAllText(itemFile, data);

            string templateFile = Path.Combine(templatesPath, itemStorage.meta.itemId + ".json");
            if (!File.Exists(templateFile))
            {
                string json = JsonUtility.ToJson(itemStorage.meta);
                File.WriteAllText(templateFile, json);
            }
        }


        [HarmonyPatch(typeof(Inventory), "GetTotalWeight")]
        [HarmonyPriority(Priority.Last)]
        public static class GetTotalWeight_Patch
        {
            public static void Postfix(Inventory __instance, ref float __result)
            {
                if (!modEnabled.Value || !playerContainer || !Player.m_localPlayer)
                    return;
                if(__instance == Player.m_localPlayer.GetInventory())
                {
                    if (new StackFrame(2).ToString().IndexOf("OverrideGetTotalWeight") > -1)
                    {
                        return;
                    }
                    foreach(ItemDrop.ItemData item in __instance.GetAllItems())
                    {
                        if(item.m_crafterName.Contains("_") && itemStorageDict.ContainsKey(item.m_crafterName.Split('_')[1]))
                        {

                            __result += itemStorageDict[item.m_crafterName.Split('_')[1]].inventory.GetTotalWeight() * itemStorageDict[item.m_crafterName.Split('_')[1]].meta.weightMult;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float) })]
        public static class GetTooltip_Patch
        {
            public static void Postfix(ItemDrop.ItemData item, ref string __result)
            {
                if (!modEnabled.Value || !item.m_crafterName.Contains("_") || item.m_crafterName.Split('_')[item.m_crafterName.Split('_').Length - 1].Length != 36)
                    return;

                __result = __result.Replace(item.m_crafterName, item.m_crafterName.Split('_')[0]);
            }
        }
        
        [HarmonyPatch(typeof(Inventory), "CanAddItem", new Type[] { typeof(GameObject), typeof(int) })]
        public static class CanAddItem_Patch1
        {
            public static bool Prefix(ref bool __result, Inventory __instance, GameObject prefab)
            {
                if (!modEnabled.Value)
                    return true;

                if (!ItemIsAllowed(__instance.GetName(), prefab.name))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Inventory), "CanAddItem", new Type[] { typeof(ItemDrop.ItemData), typeof(int) })]
        public static class CanAddItem_Patch2
        {
            public static bool Prefix(ref bool __result, Inventory __instance, ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || item.m_dropPrefab == null)
                    return true;

                if (!ItemIsAllowed(__instance.GetName(), item.m_dropPrefab.name))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
                
        [HarmonyPatch(typeof(Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData) })]
        public static class AddItem_Patch1
        {
            public static bool Prefix(ref bool __result, Inventory __instance, ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || item.m_dropPrefab == null)
                    return true;

                if (!ItemIsAllowed(__instance.GetName(), item.m_dropPrefab.name))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }                
        [HarmonyPatch(typeof(Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
        public static class AddItem_Patch2
        {
            public static bool Prefix(ref bool __result, Inventory __instance, ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || item.m_dropPrefab == null)
                    return true;

                if (!ItemIsAllowed(__instance.GetName(), item.m_dropPrefab.name))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Inventory), "AddItem", new Type[] { typeof(string), typeof(int), typeof(float), typeof(Vector2i), typeof(bool), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Dictionary<string, string>), typeof(int), typeof(bool) })]
        public static class AddItem_Patch3
        {
            public static bool Prefix(ref bool __result, Inventory __instance, string name)
            {
                if (!modEnabled.Value)
                    return true;

                if (!ItemIsAllowed(__instance.GetName(), name))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        public static bool ItemIsAllowed(string inventoryName, string itemName)
        {
            if (!itemStorageDict.Values.ToList().Exists(i => i.meta.itemName == inventoryName))
                return true;
            //var mis = itemStorageDict.Values.ToList().First(i => i.meta.itemName == inventoryName);
            //Dbgl($"{mis.meta.itemName} inventory found, allowed items: {string.Join(", ", mis.meta.allowedItems)} - contains item {itemName}? {mis.meta.allowedItems.Contains(itemName)}, disallowed items: {string.Join(", ", mis.meta.disallowedItems)} - contains item {itemName}? {mis.meta.disallowedItems.Contains(itemName)}" );

            return !itemStorageDict.Values.ToList().Exists(i => i.meta.itemName == inventoryName && ((i.meta.allowedItems.Length > 0 && !i.meta.allowedItems.Contains(itemName)) || (i.meta.allowedItems.Length == 0 && i.meta.disallowedItems.Length > 0 && i.meta.disallowedItems.Contains(itemName))));
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
                    
                    LoadDataFromDisk();

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
