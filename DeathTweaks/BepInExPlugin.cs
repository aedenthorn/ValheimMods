using BepInEx;
using BepInEx.Bootstrap;
using AuthoritativeConfig;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DeathTweaks
{
    [BepInPlugin("aedenthorn.DeathTweaks", "Death Tweaks", "0.6.2")]
    public class BepInExPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> keepEquippedItems;
        public static ConfigEntry<bool> keepHotbarItems;
        public static ConfigEntry<bool> keepAllItems;
        public static ConfigEntry<bool> destroyAllItems;
        
        public static ConfigEntry<bool> useTombStone;
        public static ConfigEntry<bool> keepFoodLevels;
        public static ConfigEntry<bool> keepQuickSlotItems;
        public static ConfigEntry<bool> createDeathEffects;
        public static ConfigEntry<bool> reduceSkills;
        public static ConfigEntry<bool> noSkillProtection;

        public static ConfigEntry<string> keepItemTypes;
        public static ConfigEntry<string> dropItemTypes;
        public static ConfigEntry<string> destroyItemTypes;

        private static BepInExPlugin context;
        private static List<string> typeEnums = new List<string>();

        private static Assembly quickSlotsAssembly;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            
            foreach (int i in Enum.GetValues(typeof(ItemDrop.ItemData.ItemType)))
            {
                typeEnums.Add(Enum.GetName(typeof(ItemDrop.ItemData.ItemType), i));
            }

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1068, "Nexus mod ID for updates");
            keepItemTypes = Config.Bind<string>("ItemLists", "KeepItemTypes", "", $"List of items to keep (comma-separated). Leave empty if using DropItemTypes. Valid types: {string.Join(",", typeEnums)}");
            dropItemTypes = Config.Bind<string>("ItemLists", "DropItemTypes", "", $"List of items to drop (comma-separated). Leave empty if using KeepItemTypes. Valid types: {string.Join(",", typeEnums)}");
            destroyItemTypes = Config.Bind<string>("ItemLists", "DestroyItemTypes", "", $"List of items to destroy (comma-separated). Overrides other lists. Valid types: {string.Join(",", typeEnums)}");
            keepAllItems = Config.Bind<bool>("Toggles", "KeepAllItems", false, "Overrides all other item options if true.");
            destroyAllItems = Config.Bind<bool>("Toggles", "DestroyAllItems", false, "Overrides all other item options except KeepAllItems if true.");
            keepEquippedItems = Config.Bind<bool>("Toggles", "KeepEquippedItems", false, "Overrides item lists if true.");
            keepHotbarItems = Config.Bind<bool>("Toggles", "KeepHotbarItems", false, "Overrides item lists if true.");
            useTombStone = Config.Bind<bool>("Toggles", "UseTombStone", true, "Use tombstone (if false, drops items on ground).");
            createDeathEffects = Config.Bind<bool>("Toggles", "CreateDeathEffects", true, "Create death effects.");
            keepFoodLevels = Config.Bind<bool>("Toggles", "KeepFoodLevels", false, "Keep food levels.");
            keepQuickSlotItems = Config.Bind<bool>("Toggles", "KeepQuickSlotItems", false, "Keep QuickSlot items.");
            reduceSkills = Config.Bind<bool>("Toggles", "ReduceSkills", true, "Reduce skills.");
            noSkillProtection = Config.Bind<bool>("Toggles", "NoSkillProtection", false, "Prevents skill protection after death.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }
        private void Start()
        {
            if (Chainloader.PluginInfos.ContainsKey("randyknapp.mods.equipmentandquickslots"))
                quickSlotsAssembly = Chainloader.PluginInfos["randyknapp.mods.equipmentandquickslots"].Instance.GetType().Assembly;

        }

        [HarmonyPatch(typeof(Player), "OnDeath")]
        [HarmonyPriority(Priority.First)]
        static class OnDeath_Patch
        {
            static bool Prefix(Player __instance, Inventory ___m_inventory, ref float ___m_timeSinceDeath, float ___m_hardDeathCooldown, ZNetView ___m_nview, List<Player.Food> ___m_foods, Skills ___m_skills)
            {
                if (!modEnabled.Value)
                    return true;

                ___m_nview.GetZDO().Set("dead", true);
                ___m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath", new object[] { });
                Game.instance.GetPlayerProfile().m_playerStats.m_deaths++;

                Game.instance.GetPlayerProfile().SetDeathPoint(__instance.transform.position);

                if (createDeathEffects.Value)
                    Traverse.Create(__instance).Method("CreateDeathEffects").GetValue();

                List<ItemDrop.ItemData> dropItems = new List<ItemDrop.ItemData>();

                if (!keepAllItems.Value)
                {

                    List<Inventory> inventories = new List<Inventory>();

                    if (quickSlotsAssembly != null)
                    {
                        var extendedInventory = quickSlotsAssembly.GetType("EquipmentAndQuickSlots.InventoryExtensions").GetMethod("Extended", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { ___m_inventory });
                        inventories = (List<Inventory>)quickSlotsAssembly.GetType("EquipmentAndQuickSlots.ExtendedInventory").GetField("_inventories", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(extendedInventory);
                    }
                    else
                    {
                        inventories.Add(___m_inventory);
                    }

                    for (int i = 0; i < inventories.Count; i++)
                    {
                        Inventory inv = inventories[i];

                        if (quickSlotsAssembly != null && keepQuickSlotItems.Value && inv == (Inventory)quickSlotsAssembly.GetType("EquipmentAndQuickSlots.PlayerExtensions").GetMethod("GetQuickSlotInventory", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { __instance }))
                        {
                            Dbgl("Skipping quick slot inventory");
                            continue;
                        }

                        List<ItemDrop.ItemData> keepItems = Traverse.Create(inv).Field("m_inventory").GetValue<List<ItemDrop.ItemData>>();

                        if (destroyAllItems.Value)
                            keepItems.Clear();
                        else
                        {

                            for(int j = keepItems.Count - 1; j >= 0; j--)
                            {
                                ItemDrop.ItemData item = keepItems[j];

                                if (keepEquippedItems.Value && item.m_equiped)
                                    continue;

                                if (keepHotbarItems.Value && item.m_gridPos.y == 0)
                                    continue;

                                if (item.m_shared.m_questItem)
                                    continue;

                                if (destroyItemTypes.Value.Length > 0)
                                {
                                    string[] destroyTypes = destroyItemTypes.Value.Split(',');
                                    if (destroyTypes.Contains(Enum.GetName(typeof(ItemDrop.ItemData.ItemType), item.m_shared.m_itemType)))
                                    {
                                        keepItems.RemoveAt(j);
                                        continue;
                                    }
                                }

                                if (keepItemTypes.Value.Length > 0)
                                {
                                    string[] keepTypes = keepItemTypes.Value.Split(',');
                                    if (keepTypes.Contains(Enum.GetName(typeof(ItemDrop.ItemData.ItemType), item.m_shared.m_itemType)))
                                        continue;
                                }
                                else if (dropItemTypes.Value.Length > 0)
                                {
                                    string[] dropTypes = dropItemTypes.Value.Split(',');
                                    if (dropTypes.Contains(Enum.GetName(typeof(ItemDrop.ItemData.ItemType), item.m_shared.m_itemType)))
                                    {
                                        dropItems.Add(item);
                                        keepItems.RemoveAt(j);
                                    }
                                    continue;
                                }

                                dropItems.Add(item);
                                keepItems.RemoveAt(j);
                            }
                        }
                        Traverse.Create(inv).Method("Changed").GetValue();
                    }
                }

                if (useTombStone.Value && dropItems.Any())
                {
                    GameObject gameObject = Instantiate(__instance.m_tombstone, __instance.GetCenterPoint(), __instance.transform.rotation);
                    gameObject.GetComponent<Container>().GetInventory().RemoveAll();


                    int width = Traverse.Create(___m_inventory).Field("m_width").GetValue<int>();
                    int height = Traverse.Create(___m_inventory).Field("m_height").GetValue<int>();
                    Traverse.Create(gameObject.GetComponent<Container>().GetInventory()).Field("m_width").SetValue(width);
                    Traverse.Create(gameObject.GetComponent<Container>().GetInventory()).Field("m_height").SetValue(height);

                   
                    Traverse.Create(gameObject.GetComponent<Container>().GetInventory()).Field("m_inventory").SetValue(dropItems);
                    Traverse.Create(gameObject.GetComponent<Container>().GetInventory()).Method("Changed").GetValue();

                    TombStone component = gameObject.GetComponent<TombStone>();
                    PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                    component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
                }
                else
                {
                    foreach(ItemDrop.ItemData item in dropItems)
                    {
                        Vector3 position = __instance.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                        Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
                        ItemDrop.DropItem(item, 0, position, rotation);
                    }
                }

                if (!keepFoodLevels.Value)
                    ___m_foods.Clear();

                bool hardDeath = noSkillProtection.Value || ___m_timeSinceDeath > ___m_hardDeathCooldown;

                if (hardDeath && reduceSkills.Value)
                {
                    ___m_skills.OnDeath();
                }
                Game.instance.RequestRespawn(10f);
                
                ___m_timeSinceDeath = 0;

                if (!hardDeath)
                {
                    __instance.Message(MessageHud.MessageType.TopLeft, "$msg_softdeath", 0, null);
                }
                __instance.Message(MessageHud.MessageType.Center, "$msg_youdied", 0, null);
                __instance.ShowTutorial("death", false);
                string eventLabel = "biome:" + __instance.GetCurrentBiome().ToString();
                Gogan.LogEvent("Game", "Death", eventLabel, 0L);

                return false;
            }

        }

        [HarmonyPatch(typeof(Player), "HardDeath")]
        static class HardDeath_Patch
        {
            static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value || !noSkillProtection.Value)
                    return true;
                __result = true;
                return false;
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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
