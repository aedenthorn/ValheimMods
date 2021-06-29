using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ItemStorageComponent
{
    [BepInPlugin("aedenthorn.ItemStorageComponent", "Item Storage Component", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> requireEquipped;
        public static ConfigEntry<bool> requireExistingTemplate;
        public static ConfigEntry<string> modKey;

        //private static GameObject backpack;
        private static ItemStorage itemStorage;
        private static Container playerContainer;
        private static Dictionary<string, ItemStorage> itemStorageDict = new Dictionary<string, ItemStorage>();
        private static Dictionary<string, ItemStorageMeta> itemStorageMetaDict = new Dictionary<string, ItemStorageMeta>();
        
        public static string assetPath;
        public static string templatesPath;
        public static string itemsPath;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1347, "Nexus mod ID for updates");
            
            requireExistingTemplate = Config.Bind<bool>("Variables", "RequireExistingTemplate", true, "Storage template for item must exist to create inventory. (Otherwise a new template will be created)");
            requireEquipped = Config.Bind<bool>("Variables", "RequireEquipped", true, "Item must be equipped to open inventory");
            modKey = Config.Bind<string>("Variables", "ModKey", "left alt", "Modifier key to selected item's storage. Follow https://docs.unity3d.com/Manual/class-InputManager.html");

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

        private static void OpenItemStorage(ItemDrop.ItemData item)
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
        private static void LoadDataFromDisk()
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

        private static void OnSelectedItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
        {
            if (!modEnabled.Value || item?.m_shared.m_maxStackSize > 1 || mod != InventoryGrid.Modifier.Split)
                return;
            bool same = false;
            if (InventoryGui.instance.IsContainerOpen() && itemStorage != null)
            {
                same = item.m_crafterName.EndsWith("_" + itemStorage.guid);

                CloseContainer();
            }
            if (!same && item != null && item.m_shared.m_maxStackSize <= 1 && (!requireEquipped.Value || item.m_equiped))
            {
                OpenItemStorage(item);
            }

        }

        private static void CloseContainer()
        {
            itemStorage.inventory = playerContainer.GetInventory();
            SaveInventory(itemStorage);
            typeof(InventoryGui).GetMethod("CloseContainer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InventoryGui.instance, new object[] { });
            playerContainer = null;
            itemStorage = null;
        }
        
        private static bool CanBeContainer(ItemDrop.ItemData item)
        {
            return item != null && (!requireEquipped.Value || item.m_equiped) && (!requireExistingTemplate.Value || itemStorageMetaDict.ContainsKey(item.m_dropPrefab.name)) && item.m_shared.m_maxStackSize <= 1 && (item.m_crafterID != 0 || item.m_crafterName == "");
        }

        [HarmonyPatch(typeof(FejdStartup), "Start")]
        static class FejdStartup_Start_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                LoadDataFromDisk();
            }
        }                
        
        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        static class InventoryGui_Awake_Patch
        {
            static void Postfix(InventoryGrid ___m_playerGrid)
            {
                if (!modEnabled.Value)
                    return;

                ___m_playerGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(___m_playerGrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(OnSelectedItem));

            }
        }                
        
        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        static class InventoryGui_OnSelectedItem_Patch
        {
            static bool Prefix(ItemDrop.ItemData item, InventoryGrid.Modifier mod)
            {
                return !modEnabled.Value || !CanBeContainer(item) || mod != InventoryGrid.Modifier.Split;
            }
        }                

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        static class InventoryGui_Update_Patch
        {
            static void Postfix(Animator ___m_animator, ref Container ___m_currentContainer, ItemDrop.ItemData ___m_dragItem)
            {

                if (!modEnabled.Value || ___m_animator.GetBool("visible") || playerContainer == null || itemStorage == null)
                    return;

                CloseContainer();
            }
        }
        private static void SaveInventory(ItemStorage itemStorage)
        {
            Dbgl($"Saving {itemStorage.inventory.GetAllItems().Count} items from inventory for item {itemStorage.guid}, type {itemStorage.meta.itemId}");

            ZPackage zpackage = new ZPackage();
            itemStorage.inventory.Save(zpackage);

            string data = zpackage.GetBase64();
            File.WriteAllText(Path.Combine(itemsPath, itemStorage.meta.itemId + "_" + itemStorage.guid), data);

            string json = JsonUtility.ToJson(itemStorage.meta);
            File.WriteAllText(Path.Combine(templatesPath, itemStorage.meta.itemId + ".json"), json);
        }


        [HarmonyPatch(typeof(Inventory), "GetTotalWeight")]
        [HarmonyPriority(Priority.Last)]
        static class GetTotalWeight_Patch
        {
            static void Postfix(Inventory __instance, ref float __result)
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
                            __result += itemStorageDict[item.m_crafterName.Split('_')[1]].inventory.GetTotalWeight() * itemStorageDict[item.m_crafterName.Split('_')[1]].meta.weightMult;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool) })]
        static class GetTooltip_Patch
        {
            static void Postfix(ItemDrop.ItemData item, ref string __result)
            {
                if (!modEnabled.Value || !item.m_crafterName.Contains("_") || item.m_crafterName.Split('_')[item.m_crafterName.Split('_').Length - 1].Length != 36)
                    return;

                __result = __result.Replace(item.m_crafterName, item.m_crafterName.Split('_')[0]);
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
