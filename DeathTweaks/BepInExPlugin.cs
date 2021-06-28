using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using ServerSync;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DeathTweaks
{
    [BepInPlugin("aedenthorn.DeathTweaks", "Death Tweaks", "0.8.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static ConfigSync configSync;
        public static ConfigEntry<bool> modEnabled;
        private ConfigEntry<bool> serverConfigLocked;
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
        public static ConfigEntry<bool> useFixedSpawnCoordinates;
        public static ConfigEntry<bool> spawnAtStart;

        public static ConfigEntry<Vector3> fixedSpawnCoordinates;
        public static ConfigEntry<float> skillReduceFactor;
        
        public static ConfigEntry<string> keepItemTypes;
        public static ConfigEntry<string> dropItemTypes;
        public static ConfigEntry<string> destroyItemTypes;

        private static BepInExPlugin context;
        private static List<string> typeEnums = new List<string>();

        private static Assembly quickSlotsAssembly;
        private Harmony harmony;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        private void Awake()
        {
            configSync = new ConfigSync(Info.Metadata.GUID) { DisplayName = Info.Metadata.Name, CurrentVersion = Info.Metadata.Version.ToString() };

            foreach (int i in Enum.GetValues(typeof(ItemDrop.ItemData.ItemType)))
            {
                typeEnums.Add(Enum.GetName(typeof(ItemDrop.ItemData.ItemType), i));
            }

            context = this;
            modEnabled = config<bool>("General", "Enabled", true, "Enable this mod", true);
            serverConfigLocked = config("General", "Lock Configuration", false, "Lock Configuration", true);
            configSync.AddLockingConfigEntry<bool>(serverConfigLocked);
            isDebug = config<bool>("General", "IsDebug", true, "Enable debug logs", true);
            nexusID = config<int>("General", "NexusID", 1068, "Nexus mod ID for updates", true);
            keepItemTypes = config<string>("ItemLists", "KeepItemTypes", "", $"List of items to keep (comma-separated). Leave empty if using DropItemTypes. Valid types: {string.Join(",", typeEnums)}", true);
            dropItemTypes = config<string>("ItemLists", "DropItemTypes", "", $"List of items to drop (comma-separated). Leave empty if using KeepItemTypes. Valid types: {string.Join(",", typeEnums)}", true);
            destroyItemTypes = config<string>("ItemLists", "DestroyItemTypes", "", $"List of items to destroy (comma-separated). Overrides other lists. Valid types: {string.Join(",", typeEnums)}", true);
            keepAllItems = config<bool>("Toggles", "KeepAllItems", false, "Overrides all other item options if true.", true);
            destroyAllItems = config<bool>("Toggles", "DestroyAllItems", false, "Overrides all other item options except KeepAllItems if true.", true);
            keepEquippedItems = config<bool>("Toggles", "KeepEquippedItems", false, "Overrides item lists if true.", true);
            keepHotbarItems = config<bool>("Toggles", "KeepHotbarItems", false, "Overrides item lists if true.", true);
            useTombStone = config<bool>("Toggles", "UseTombStone", true, "Use tombstone (if false, drops items on ground).", true);
            createDeathEffects = config<bool>("Toggles", "CreateDeathEffects", true, "Create death effects.", true);
            keepFoodLevels = config<bool>("Toggles", "KeepFoodLevels", false, "Keep food levels.", true);
            keepQuickSlotItems = config<bool>("Toggles", "KeepQuickSlotItems", false, "Keep QuickSlot items.", true);
            
            useFixedSpawnCoordinates = config<bool>("Spawn", "UseFixedSpawnCoordinates", false, "Use fixed spawn coordinates.", true);
            spawnAtStart = config<bool>("Spawn", "SpawnAtStart", false, "Respawn at start location.", true);
            fixedSpawnCoordinates = config<Vector3>("Spawn", "FixedSpawnCoordinates", Vector3.zero, "Fixed spawn coordinates.", true);
            
            noSkillProtection = config<bool>("Skills", "NoSkillProtection", false, "Prevents skill protection after death.", true);
            reduceSkills = config<bool>("Skills", "ReduceSkills", true, "Reduce skills.", true);
            skillReduceFactor = config<float>("Skills", "SkillReduceFactor", 0.25f, "Reduce skills by this fraction.", true);

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

        [HarmonyPatch(typeof(Skills), "LowerAllSkills")]
        static class LowerAllSkills_Patch
        {
            static bool Prefix(float factor, Dictionary<Skills.SkillType, Skills.Skill> ___m_skillData, Player ___m_player)
            {
                if (!modEnabled.Value)
                    return true;

                factor = skillReduceFactor.Value;

                foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in ___m_skillData)
                {
                    float level = keyValuePair.Value.m_level;
                    float accum = keyValuePair.Value.m_accumulator;
                    float total = 0;
                    keyValuePair.Value.m_level = 0;
                    var nextLevelReq = typeof(Skills.Skill).GetMethod("GetNextLevelRequirement", BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < level; i++)
                    {
                        total += (float)nextLevelReq.Invoke(keyValuePair.Value, new object[] { });
                        keyValuePair.Value.m_level += 1;
                    }
                    total += accum;

                    //Dbgl($"skill {keyValuePair.Key} total xp {total}, level {level}, next level: {accum} / {nextLevelReq.Invoke(keyValuePair.Value, new object[] { })}");

                    total *= (1 - skillReduceFactor.Value);

                    float newTotal = 0;
                    keyValuePair.Value.m_level = 0;
                    while(true)
                    {
                        float add = (float)nextLevelReq.Invoke(keyValuePair.Value, new object[] { });
                        //Dbgl($"xp at level {keyValuePair.Value.m_level}: {newTotal}, xp to level {keyValuePair.Value.m_level + 1}: add");
                        if(newTotal + add <= total)
                        {
                            newTotal += add;
                            keyValuePair.Value.m_level += 1;
                        }
                        else
                        {
                            keyValuePair.Value.m_accumulator = total - newTotal;
                            break;
                        }
                    }
                    //Dbgl($"skill {keyValuePair.Key} new total xp {total}, level {keyValuePair.Value.m_level}, next level: {keyValuePair.Value.m_accumulator} / {nextLevelReq.Invoke(keyValuePair.Value, new object[] { })}");
                }
                ___m_player.Message(MessageHud.MessageType.TopLeft, "$msg_skills_lowered", 0, null);
                return false;
            }
        }

        [HarmonyPatch(typeof(Game), "FindSpawnPoint")]
        static class FindSpawnPoint_Patch
        {
            static bool Prefix(ref Vector3 point, ref bool usedLogoutPoint, bool ___m_firstSpawn, ref bool __result)
            {
                if (!modEnabled.Value || ___m_firstSpawn)
                    return true;

                if (spawnAtStart.Value)
                {
                    usedLogoutPoint = false;

                    Vector3 a;
                    if (ZoneSystem.instance.GetLocationIcon(Game.instance.m_StartLocation, out a))
                    {
                        point = a + Vector3.up * 2f;
                        ZNet.instance.SetReferencePosition(point);
                        __result = ZNetScene.instance.IsAreaReady(point);
                        if(__result)
                            Dbgl($"respawning at start: {point}");
                    }
                    else
                    {
                        Dbgl("start point not found");
                        ZNet.instance.SetReferencePosition(Vector3.zero);
                        point = Vector3.zero;
                        __result = false;
                    }
                    return false;
                }
                else if (useFixedSpawnCoordinates.Value)
                {
                    usedLogoutPoint = false;

                    point = fixedSpawnCoordinates.Value;
                    ZNet.instance.SetReferencePosition(point);
                    __result = ZNetScene.instance.IsAreaReady(point);
                    if(__result)
                        Dbgl($"respawning at custom point {point}");
                    return false;

                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Skills), "OnDeath")]
        static class Skills_OnDeath_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value)
                    return true;
                return reduceSkills.Value;
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
