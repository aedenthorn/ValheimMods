using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace CustomItems
{
    [BepInPlugin("aedenthorn.CustomItems", "Custom Items", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        private static Dictionary<string, CustomItem> customItems = new Dictionary<string, CustomItem>();
        private static Dictionary<string, CustomItem> customItemsOutput = new Dictionary<string, CustomItem>();
        public static ConfigEntry<int> nexusID;
        private static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 229, "Nexus id for update checking");

            if (!modEnabled.Value)
                return;

            LoadItems();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            return;


        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                return;
                if (Console.IsVisible())
                    return;

                Dbgl($"Pressed U.");

                return;
            }
        }

        private static void LoadItems()
        {
            customItems.Clear();

            Dbgl($"Importing items");

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomItems");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            foreach (string file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    CustomItem item = JsonUtility.FromJson<CustomItem>(File.ReadAllText(file));
                    string json = JsonUtility.ToJson(item);
                    //File.WriteAllText(file+".out", json);
                    customItems.Add(Path.GetFileNameWithoutExtension(file), item);
                }
                catch(Exception ex)
                {
                    Dbgl($"Error loading json file {file}: \r\n{ex}");
                }
            }
            if (Directory.Exists(path + "Output"))
            {
                foreach (string file in Directory.GetFiles(path + "Output", "*.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        CustomItem item = JsonUtility.FromJson<CustomItem>(File.ReadAllText(file));
                        string json = JsonUtility.ToJson(item);
                        
                        customItemsOutput.Add(Path.GetFileNameWithoutExtension(file), item);
                    }
                    catch (Exception ex)
                    {
                        Dbgl($"Error loading json file {file}: \r\n{ex}");
                    }
                }
            }
        }
        private static string GetPrefabName(string name)
        {
            char[] anyOf = new char[] { '(', ' ' };
            int num = name.IndexOfAny(anyOf);
            string result;
            if (num >= 0)
                result = name.Substring(0, num);
            else
                result = name;
            return result;
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {
            public static void Postfix()
            {
                return;
                foreach (KeyValuePair<string, CustomItem> kvp in customItems)
                {
                    CustomItem item = kvp.Value;
                    GameObject customObject = Instantiate(ObjectDB.instance.GetItemPrefab(item.baseItemName));
                    ItemDrop itemDrop = customObject.GetComponent<ItemDrop>();
                    Recipe recipe = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);
                    customObject.name = item.id;
                    itemDrop.m_itemData.m_shared.m_name = item.name;
                    itemDrop.m_itemData.m_shared.m_dlc = item.dlc;
                    itemDrop.m_itemData.m_shared.m_itemType = item.itemType;
                    itemDrop.m_itemData.m_shared.m_attachOverride = item.attachOverride;
                    itemDrop.m_itemData.m_shared.m_description = item.description;
                    itemDrop.m_itemData.m_shared.m_maxStackSize = item.maxStackSize;
                    itemDrop.m_itemData.m_shared.m_maxQuality = item.maxQuality;
                    itemDrop.m_itemData.m_shared.m_weight = item.weight;
                    itemDrop.m_itemData.m_shared.m_value = item.value;
                    itemDrop.m_itemData.m_shared.m_teleportable = item.teleportable;
                    itemDrop.m_itemData.m_shared.m_questItem = item.questItem;
                    itemDrop.m_itemData.m_shared.m_equipDuration = item.equipDuration;
                    itemDrop.m_itemData.m_shared.m_variants = item.variants;
                    itemDrop.m_itemData.m_shared.m_trophyPos = item.trophyPos;
                    itemDrop.m_itemData.m_shared.m_buildPieces = item.buildPieces;
                    itemDrop.m_itemData.m_shared.m_centerCamera = item.centerCamera;
                    itemDrop.m_itemData.m_shared.m_setName = item.setName;
                    itemDrop.m_itemData.m_shared.m_setSize = item.setSize;
                    itemDrop.m_itemData.m_shared.m_setStatusEffect = item.setStatusEffect;
                    itemDrop.m_itemData.m_shared.m_equipStatusEffect = item.equipStatusEffect;
                    itemDrop.m_itemData.m_shared.m_movementModifier = item.movementModifier;
                    itemDrop.m_itemData.m_shared.m_food = item.food;
                    itemDrop.m_itemData.m_shared.m_foodStamina = item.foodStamina;
                    itemDrop.m_itemData.m_shared.m_foodBurnTime = item.foodBurnTime;
                    itemDrop.m_itemData.m_shared.m_foodRegen = item.foodRegen;
                    itemDrop.m_itemData.m_shared.m_foodColor = item.foodColor;
                    itemDrop.m_itemData.m_shared.m_armorMaterial = item.armorMaterial;
                    itemDrop.m_itemData.m_shared.m_helmetHideHair = item.helmetHideHair;
                    itemDrop.m_itemData.m_shared.m_armor = item.armor;
                    itemDrop.m_itemData.m_shared.m_armorPerLevel = item.armorPerLevel;
                    itemDrop.m_itemData.m_shared.m_damageModifiers = item.damageModifiers;
                    itemDrop.m_itemData.m_shared.m_blockPower = item.blockPower;
                    itemDrop.m_itemData.m_shared.m_blockPowerPerLevel = item.blockPowerPerLevel;
                    itemDrop.m_itemData.m_shared.m_deflectionForce = item.deflectionForce;
                    itemDrop.m_itemData.m_shared.m_deflectionForcePerLevel = item.deflectionForcePerLevel;
                    itemDrop.m_itemData.m_shared.m_timedBlockBonus = item.timedBlockBonus;
                    itemDrop.m_itemData.m_shared.m_animationState = item.animationState;
                    itemDrop.m_itemData.m_shared.m_skillType = item.skillType;
                    itemDrop.m_itemData.m_shared.m_toolTier = item.toolTier;
                    itemDrop.m_itemData.m_shared.m_damages = item.damages;
                    itemDrop.m_itemData.m_shared.m_damagesPerLevel = item.damagesPerLevel;
                    itemDrop.m_itemData.m_shared.m_attackForce = item.attackForce;
                    itemDrop.m_itemData.m_shared.m_backstabBonus = item.backstabBonus;
                    itemDrop.m_itemData.m_shared.m_dodgeable = item.dodgeable;
                    itemDrop.m_itemData.m_shared.m_blockable = item.blockable;
                    itemDrop.m_itemData.m_shared.m_attackStatusEffect = item.attackStatusEffect;
                    itemDrop.m_itemData.m_shared.m_spawnOnHit = item.spawnOnHit;
                    itemDrop.m_itemData.m_shared.m_spawnOnHitTerrain = item.spawnOnHitTerrain;
                    itemDrop.m_itemData.m_shared.m_attack = item.attack;
                    itemDrop.m_itemData.m_shared.m_secondaryAttack = item.secondaryAttack;
                    itemDrop.m_itemData.m_shared.m_useDurability = item.useDurability;
                    itemDrop.m_itemData.m_shared.m_destroyBroken = item.destroyBroken;
                    itemDrop.m_itemData.m_shared.m_canBeReparied = item.canBeReparied;
                    itemDrop.m_itemData.m_shared.m_maxDurability = item.maxDurability;
                    itemDrop.m_itemData.m_shared.m_durabilityPerLevel = item.durabilityPerLevel;
                    itemDrop.m_itemData.m_shared.m_useDurabilityDrain = item.useDurabilityDrain;
                    itemDrop.m_itemData.m_shared.m_durabilityDrain = item.durabilityDrain;
                    itemDrop.m_itemData.m_shared.m_holdDurationMin = item.holdDurationMin;
                    itemDrop.m_itemData.m_shared.m_holdStaminaDrain = item.holdStaminaDrain;
                    itemDrop.m_itemData.m_shared.m_holdAnimationState = item.holdAnimationState;
                    itemDrop.m_itemData.m_shared.m_ammoType = item.ammoType;
                    itemDrop.m_itemData.m_shared.m_aiAttackRange = item.aiAttackRange;
                    itemDrop.m_itemData.m_shared.m_aiAttackRangeMin = item.aiAttackRangeMin;
                    itemDrop.m_itemData.m_shared.m_aiAttackInterval = item.aiAttackInterval;
                    itemDrop.m_itemData.m_shared.m_aiAttackMaxAngle = item.aiAttackMaxAngle;
                    itemDrop.m_itemData.m_shared.m_aiWhenFlying = item.aiWhenFlying;
                    itemDrop.m_itemData.m_shared.m_aiWhenWalking = item.aiWhenWalking;
                    itemDrop.m_itemData.m_shared.m_aiWhenSwiming = item.aiWhenSwiming;
                    itemDrop.m_itemData.m_shared.m_aiPrioritized = item.aiPrioritized;
                    itemDrop.m_itemData.m_shared.m_aiTargetType = item.aiTargetType;
                    itemDrop.m_itemData.m_shared.m_hitEffect = item.hitEffect;
                    itemDrop.m_itemData.m_shared.m_hitTerrainEffect = item.hitTerrainEffect;
                    itemDrop.m_itemData.m_shared.m_blockEffect = item.blockEffect;
                    itemDrop.m_itemData.m_shared.m_startEffect = item.startEffect;
                    itemDrop.m_itemData.m_shared.m_holdStartEffect = item.holdStartEffect;
                    itemDrop.m_itemData.m_shared.m_triggerEffect = item.triggerEffect;
                    itemDrop.m_itemData.m_shared.m_trailStartEffect = item.trailStartEffect;
                    itemDrop.m_itemData.m_shared.m_consumeStatusEffect = item.consumeStatusEffect;

                    recipe.m_item = itemDrop;
                    recipe.m_amount = item.recipe_amount;
                    recipe.m_minStationLevel = item.minStationLevel;
                    List<Piece.Requirement> reqs = new List<Piece.Requirement>();
                    foreach (RequirementData rd in item.requirements)
                    {
                        Piece.Requirement req = new Piece.Requirement();
                        req.m_amount = rd.amount;
                        req.m_amountPerLevel = rd.amountPerLevel;
                        req.m_recover = rd.recover;
                        req.m_resItem = ObjectDB.instance.GetItemPrefab(rd.name).GetComponent<ItemDrop>();
                        reqs.Add(req);
                    }
                    recipe.m_resources = reqs.ToArray();

                    if (ObjectDB.instance.GetItemPrefab(customObject.name) == null)
                    {
                        Dbgl($"Adding new item {customObject.name} to DB");
                        ObjectDB.instance.m_items.Add(customObject);
                        ((Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ObjectDB.instance)).Add(customObject.name.GetStableHashCode(), customObject);
                    }

                    if (ObjectDB.instance.GetRecipe(itemDrop.m_itemData) == null)
                    {
                        Dbgl($"Adding new recipe {customObject.name} to DB");
                        ObjectDB.instance.m_recipes.Add(recipe);
                    }

                    Traverse.Create(Localization.instance).Method("AddWord", new object[] { item.description_key, item.description });
                }
            }
        }
        [HarmonyPatch(typeof(FejdStartup), "SetupObjectDB")]
        public static class ObjectDB_Awake_Patch
        {
            public static void Postfix()
            {
                Dbgl($"SetupObjectDB finished");

                foreach (KeyValuePair<string, CustomItem> kvp in customItemsOutput)
                {
                    CustomItem item = kvp.Value;
                    GameObject customObject = ObjectDB.instance.GetItemPrefab(item.baseItemName);
                    ItemDrop.ItemData itemData = customObject.GetComponent<ItemDrop>().m_itemData.Clone();
                    Recipe recipe = ObjectDB.instance.GetRecipe(customObject.GetComponent<ItemDrop>().m_itemData);
                    item.id = customObject.name;
                    item.name = itemData.m_shared.m_name;
                    item.dlc = itemData.m_shared.m_dlc;
                    item.itemType = itemData.m_shared.m_itemType;
                    item.attachOverride = itemData.m_shared.m_attachOverride;
                    item.description = itemData.m_shared.m_description;
                    item.maxStackSize = itemData.m_shared.m_maxStackSize;
                    item.maxQuality = itemData.m_shared.m_maxQuality;
                    item.weight = itemData.m_shared.m_weight;
                    item.value = itemData.m_shared.m_value;
                    item.teleportable = itemData.m_shared.m_teleportable;
                    item.questItem = itemData.m_shared.m_questItem;
                    item.equipDuration = itemData.m_shared.m_equipDuration;
                    item.variants = itemData.m_shared.m_variants;
                    item.trophyPos = itemData.m_shared.m_trophyPos;
                    item.buildPieces = itemData.m_shared.m_buildPieces;
                    item.centerCamera = itemData.m_shared.m_centerCamera;
                    item.setName = itemData.m_shared.m_setName;
                    item.setSize = itemData.m_shared.m_setSize;
                    item.setStatusEffect = itemData.m_shared.m_setStatusEffect;
                    item.equipStatusEffect = itemData.m_shared.m_equipStatusEffect;
                    item.movementModifier = itemData.m_shared.m_movementModifier;
                    item.food = itemData.m_shared.m_food;
                    item.foodStamina = itemData.m_shared.m_foodStamina;
                    item.foodBurnTime = itemData.m_shared.m_foodBurnTime;
                    item.foodRegen = itemData.m_shared.m_foodRegen;
                    item.foodColor = itemData.m_shared.m_foodColor;
                    item.armorMaterial = itemData.m_shared.m_armorMaterial;
                    item.helmetHideHair = itemData.m_shared.m_helmetHideHair;
                    item.armor = itemData.m_shared.m_armor;
                    item.armorPerLevel = itemData.m_shared.m_armorPerLevel;
                    item.damageModifiers = itemData.m_shared.m_damageModifiers;
                    item.blockPower = itemData.m_shared.m_blockPower;
                    item.blockPowerPerLevel = itemData.m_shared.m_blockPowerPerLevel;
                    item.deflectionForce = itemData.m_shared.m_deflectionForce;
                    item.deflectionForcePerLevel = itemData.m_shared.m_deflectionForcePerLevel;
                    item.timedBlockBonus = itemData.m_shared.m_timedBlockBonus;
                    item.animationState = itemData.m_shared.m_animationState;
                    item.skillType = itemData.m_shared.m_skillType;
                    item.toolTier = itemData.m_shared.m_toolTier;
                    item.damages = itemData.m_shared.m_damages;
                    item.damagesPerLevel = itemData.m_shared.m_damagesPerLevel;
                    item.attackForce = itemData.m_shared.m_attackForce;
                    item.backstabBonus = itemData.m_shared.m_backstabBonus;
                    item.dodgeable = itemData.m_shared.m_dodgeable;
                    item.blockable = itemData.m_shared.m_blockable;
                    item.attackStatusEffect = itemData.m_shared.m_attackStatusEffect;
                    item.spawnOnHit = itemData.m_shared.m_spawnOnHit;
                    item.spawnOnHitTerrain = itemData.m_shared.m_spawnOnHitTerrain;
                    item.attack = itemData.m_shared.m_attack;
                    item.secondaryAttack = itemData.m_shared.m_secondaryAttack;
                    item.useDurability = itemData.m_shared.m_useDurability;
                    item.destroyBroken = itemData.m_shared.m_destroyBroken;
                    item.canBeReparied = itemData.m_shared.m_canBeReparied;
                    item.maxDurability = itemData.m_shared.m_maxDurability;
                    item.durabilityPerLevel = itemData.m_shared.m_durabilityPerLevel;
                    item.useDurabilityDrain = itemData.m_shared.m_useDurabilityDrain;
                    item.durabilityDrain = itemData.m_shared.m_durabilityDrain;
                    item.holdDurationMin = itemData.m_shared.m_holdDurationMin;
                    item.holdStaminaDrain = itemData.m_shared.m_holdStaminaDrain;
                    item.holdAnimationState = itemData.m_shared.m_holdAnimationState;
                    item.ammoType = itemData.m_shared.m_ammoType;
                    item.aiAttackRange = itemData.m_shared.m_aiAttackRange;
                    item.aiAttackRangeMin = itemData.m_shared.m_aiAttackRangeMin;
                    item.aiAttackInterval = itemData.m_shared.m_aiAttackInterval;
                    item.aiAttackMaxAngle = itemData.m_shared.m_aiAttackMaxAngle;
                    item.aiWhenFlying = itemData.m_shared.m_aiWhenFlying;
                    item.aiWhenWalking = itemData.m_shared.m_aiWhenWalking;
                    item.aiWhenSwiming = itemData.m_shared.m_aiWhenSwiming;
                    item.aiPrioritized = itemData.m_shared.m_aiPrioritized;
                    item.aiTargetType = itemData.m_shared.m_aiTargetType;
                    item.hitEffect = itemData.m_shared.m_hitEffect;
                    item.hitTerrainEffect = itemData.m_shared.m_hitTerrainEffect;
                    item.blockEffect = itemData.m_shared.m_blockEffect;
                    item.startEffect = itemData.m_shared.m_startEffect;
                    item.holdStartEffect = itemData.m_shared.m_holdStartEffect;
                    item.triggerEffect = itemData.m_shared.m_triggerEffect;
                    item.trailStartEffect = itemData.m_shared.m_trailStartEffect;
                    item.consumeStatusEffect = itemData.m_shared.m_consumeStatusEffect;

                    if(recipe != null)
                    {
                        List<RequirementData> reqs = new List<RequirementData>();
                        foreach (Piece.Requirement req in recipe.m_resources)
                        {
                            RequirementData rd = new RequirementData();
                            rd.amount = req.m_amount;
                            rd.amountPerLevel = req.m_amountPerLevel;
                            rd.recover = req.m_recover;
                            rd.name = req.m_resItem.name;
                            reqs.Add(rd);
                        }
                        item.requirements = reqs;
                    }
                    string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomItemsOutput", $"{kvp.Key}.json");
                    string json = JsonUtility.ToJson(item);
                    
                    File.WriteAllText(file, json);

                }
            }
        }


        private static Transform RecursiveFind(Transform parent, string childName)
        {
            Transform child = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                child = parent.GetChild(i);
                if (child.name == childName)
                    break;
                child = RecursiveFind(child, childName);
                if (child != null)
                    break;
            }
            return child;
        }


        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("customitems reload"))
                {
                    LoadItems();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Reloaded custom items" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }


}
