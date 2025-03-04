using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CustomItems
{
    [BepInPlugin("aedenthorn.CustomItems", "Custom Items", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static Dictionary<string, CustomItem> customItems = new Dictionary<string, CustomItem>();
        public static Dictionary<string, CustomItem> customItemsOutput = new Dictionary<string, CustomItem>();
        public static Dictionary<string, GameObject> objectsToAdd = new Dictionary<string, GameObject>();
        public static ConfigEntry<int> nexusID;
        public static bool creatingObject = false;
        public static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;

        public static void Dbgl(object str, bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
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

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                return;
                List<string> names = new List<string>();
                var dict = Traverse.Create(ZNetScene.instance).Field("m_namedPrefabs").GetValue<Dictionary<int, GameObject>>();
                foreach (var kvp in dict)
                {
                    if(kvp.Value != null)
                        names.Add(kvp.Value.name);
                    else
                        names.Add($"null {kvp.Key} {"NewShield".GetStableHashCode()}");
                }
                Dbgl($"all: {string.Join("\n", names)}");
                Dbgl($"prefabs: {dict["NewShield".GetStableHashCode()]?.name}");
                Dbgl($"db: {ObjectDB.instance.GetItemPrefab("NewShield")?.name}");
            }
        }
        
        public static void LoadItems()
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
                    Dbgl($"Adding custom item {item.id} {item.requirements.Count}");
                    customItems.Add(Path.GetFileNameWithoutExtension(file), item);
                }
                catch (Exception ex)
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


        public static RequirementData MakeReqData(string str)
        {
            return JsonUtility.FromJson<RequirementData>(str);
        }


        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        public static class DoCrafting_Patch
        {
            public static void Prefix(Recipe ___m_craftRecipe)
            {
                string name = ___m_craftRecipe.m_item.gameObject.name;

                if (ObjectDB.instance.GetItemPrefab(name) == null)
                {
                    Dbgl($"Readding item {name} to DB");
                    ObjectDB.instance.m_items.Add(___m_craftRecipe.m_item.gameObject);
                    ((Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ObjectDB.instance)).Add(___m_craftRecipe.m_item.gameObject.name.GetStableHashCode(), ___m_craftRecipe.m_item.gameObject);
                    Dbgl($"in database: {ObjectDB.instance.GetItemPrefab(___m_craftRecipe.m_item.gameObject.name).name}");
                }
            }
        }

        public static GameObject customObject;

        //[HarmonyPatch(typeof(FejdStartup), "LoadMainScene")]
        public static class Game_Patch
        {
            public static void Prefix()
            {
                Dbgl("LoadMainScene");
            }
            public static void Postfix()
            {
                Dbgl("LoadMainScene done");
            }

        }
        //[HarmonyPatch(typeof(ZNetScene), "GetPrefab", new Type[] { typeof(string) })]
        public static class GetPrefab_Patch
        {
            public static bool Prefix(string name, ref GameObject __result)
            {
                if (false && customItems.ContainsKey(name))
                {
                    __result = GetCustomGameObject(name, true);
                    return false;
                }
                return true;
            }

        }
        public static GameObject GetCustomGameObject(string name, bool ready = false)
        {

            CustomItem customItem = customItems[name];
            GameObject baseObject = ObjectDB.instance.GetItemPrefab(customItem.baseItemName);
            GameObject customObject;
            ItemDrop itemDrop;
            creatingObject = true;
            if (ready)
            {
                customObject = Instantiate(baseObject);
                itemDrop = customObject.GetComponent<ItemDrop>();
            }
            else
            {
                customObject = new GameObject(name);
                itemDrop = customObject.AddComponent<ItemDrop>();
                customObject.AddComponent(baseObject.GetComponent<ZNetView>());
                if (baseObject.transform.Find("attach"))
                {
                    Dbgl($"Has attach");
                    Instantiate(baseObject.transform.Find("attach"), customObject.transform);
                }
                else if (baseObject.transform.Find("model"))
                {
                    Dbgl($"Has model");
                    Instantiate(baseObject.transform.Find("model"), customObject.transform);
                }
            }
            creatingObject = false;
            ItemDrop baseItemDrop = baseObject.GetComponent<ItemDrop>();
            customObject.name = name;
            customObject.name = Utils.GetPrefabName(customObject);
            objectsToAdd[customObject.name] = customObject;
            customObject.AddComponent<DontDestroy>();
            customItems[name].gameObject = customObject;
            DontDestroyOnLoad(customItems[name].gameObject);


            Dbgl($"baseitemdrop {baseItemDrop?.name} data {baseItemDrop.m_itemData.m_shared.m_name} customObject {customObject?.name}");

            GameObject prefab = Instantiate(objectsToAdd[customObject.name]);
            DontDestroyOnLoad(prefab);
            itemDrop.m_itemData = baseItemDrop.m_itemData.Clone();
            itemDrop.m_itemData.m_dropPrefab = prefab;
            itemDrop.enabled = true;

            Dbgl($"itemdrop {itemDrop.m_itemData.m_dropPrefab.name} data {itemDrop.m_itemData.m_shared.m_name} gameobject {itemDrop.gameObject.name}");

            itemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData
            {
                m_name = "$" + customItem.name_key,
                m_dlc = customItem.dlc,
                m_itemType = customItem.itemType,
                m_attachOverride = customItem.attachOverride,
                m_description = "$" + customItem.description_key,
                m_maxStackSize = customItem.maxStackSize,
                m_maxQuality = customItem.maxQuality,
                m_weight = customItem.weight,
                m_value = customItem.value,
                m_teleportable = customItem.teleportable,
                m_questItem = customItem.questItem,
                m_equipDuration = customItem.equipDuration,
                m_variants = customItem.variants,
                m_trophyPos = customItem.trophyPos,
                m_buildPieces = customItem.buildPieces,
                m_centerCamera = customItem.centerCamera,
                m_setName = customItem.setName,
                m_setSize = customItem.setSize,
                m_setStatusEffect = customItem.setStatusEffect,
                m_equipStatusEffect = customItem.equipStatusEffect,
                m_movementModifier = customItem.movementModifier,
                m_food = customItem.food,
                m_foodStamina = customItem.foodStamina,
                m_foodBurnTime = customItem.foodBurnTime,
                m_foodRegen = customItem.foodRegen,
                m_armorMaterial = customItem.armorMaterial,
                m_helmetHideHair = customItem.helmetHideHair,
                m_armor = customItem.armor,
                m_armorPerLevel = customItem.armorPerLevel,
                m_damageModifiers = customItem.damageModifiers,
                m_blockPower = customItem.blockPower,
                m_blockPowerPerLevel = customItem.blockPowerPerLevel,
                m_deflectionForce = customItem.deflectionForce,
                m_deflectionForcePerLevel = customItem.deflectionForcePerLevel,
                m_timedBlockBonus = customItem.timedBlockBonus,
                m_animationState = customItem.animationState,
                m_skillType = customItem.skillType,
                m_toolTier = customItem.toolTier,
                m_damages = customItem.damages,
                m_damagesPerLevel = customItem.damagesPerLevel,
                m_attackForce = customItem.attackForce,
                m_backstabBonus = customItem.backstabBonus,
                m_dodgeable = customItem.dodgeable,
                m_blockable = customItem.blockable,
                m_attackStatusEffect = customItem.attackStatusEffect,
                m_spawnOnHit = customItem.spawnOnHit,
                m_spawnOnHitTerrain = customItem.spawnOnHitTerrain,
                m_attack = customItem.attack,
                m_secondaryAttack = customItem.secondaryAttack,
                m_useDurability = customItem.useDurability,
                m_destroyBroken = customItem.destroyBroken,
                m_canBeReparied = customItem.canBeReparied,
                m_maxDurability = customItem.maxDurability,
                m_durabilityPerLevel = customItem.durabilityPerLevel,
                m_useDurabilityDrain = customItem.useDurabilityDrain,
                m_durabilityDrain = customItem.durabilityDrain,
                m_ammoType = customItem.ammoType,
                m_aiAttackRange = customItem.aiAttackRange,
                m_aiAttackRangeMin = customItem.aiAttackRangeMin,
                m_aiAttackInterval = customItem.aiAttackInterval,
                m_aiAttackMaxAngle = customItem.aiAttackMaxAngle,
                m_aiWhenFlying = customItem.aiWhenFlying,
                m_aiWhenWalking = customItem.aiWhenWalking,
                m_aiWhenSwiming = customItem.aiWhenSwiming,
                m_aiPrioritized = customItem.aiPrioritized,
                m_aiTargetType = customItem.aiTargetType,
                m_hitEffect = customItem.hitEffect,
                m_hitTerrainEffect = customItem.hitTerrainEffect,
                m_blockEffect = customItem.blockEffect,
                m_startEffect = customItem.startEffect,
                m_holdStartEffect = customItem.holdStartEffect,
                m_triggerEffect = customItem.triggerEffect,
                m_trailStartEffect = customItem.trailStartEffect,
                m_consumeStatusEffect = customItem.consumeStatusEffect,

                m_icons = baseItemDrop.m_itemData.m_shared.m_icons
            };

            Recipe origRecipe = ObjectDB.instance.GetRecipe(baseItemDrop.m_itemData);

            Recipe recipe = (Recipe)ScriptableObject.CreateInstance("Recipe");
            recipe.m_item = itemDrop;
            recipe.m_amount = customItem.recipe_amount;
            recipe.m_minStationLevel = customItem.minStationLevel;
            recipe.m_craftingStation = origRecipe.m_craftingStation;
            recipe.m_repairStation = origRecipe.m_repairStation;


            //Dbgl($"custom reqs {customItem.requirements.Count}");


            List<Piece.Requirement> reqs = new List<Piece.Requirement>();
            foreach (string str in customItem.requirements)
            {
                RequirementData rd = MakeReqData(str);
                if (rd == null)
                    continue;
                Piece.Requirement req = new Piece.Requirement
                {
                    m_amount = rd.amount,
                    m_amountPerLevel = rd.amountPerLevel,
                    m_recover = rd.recover,
                    m_resItem = ObjectDB.instance.GetItemPrefab(rd.name)?.GetComponent<ItemDrop>()
                };
                // Dbgl($"adding rd {req.m_resItem?.m_itemData?.m_shared?.m_name}");
                reqs.Add(req);
            }

            recipe.m_resources = reqs.ToArray();

            if (ObjectDB.instance.GetItemPrefab(customObject.name) == null)
            {
                Dbgl($"Adding item {customObject.name} to DB");
                ObjectDB.instance.m_items.Add(objectsToAdd[customObject.name]);
                ((Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ObjectDB.instance)).Add(customObject.name.GetStableHashCode(), objectsToAdd[customObject.name]);
                Dbgl($"Added new item {ObjectDB.instance.GetItemPrefab(customObject.name).name} to DB");

            }

            if (ObjectDB.instance.GetRecipe(itemDrop.m_itemData) == null)
            {
                Dbgl($"Adding recipe {customObject.name} to DB");
                ObjectDB.instance.m_recipes.Add(recipe);
            }
            return customObject;

        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {

            public static void Prefix(ZNetScene __instance)
            {
                foreach(GameObject go in objectsToAdd.Values)
                {
                    __instance.m_prefabs.Add(go);
                }
            }
        }
        [HarmonyPatch(typeof(ZNetView), "Awake")]
        public static class ZNetView_Awake_Patch
        {

            public static bool Prefix()
            {
                return !creatingObject;
            }
        }
        //[HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class CopyOtherDB_Patch
        {

            public static void Postfix()
            {
                Dbgl($"ZNetScene Awake");

                foreach (KeyValuePair<string, CustomItem> kvp in customItemsOutput)
                {
                    CustomItem item = kvp.Value;
                    customObject = ObjectDB.instance.GetItemPrefab(item.baseItemName);
                    DontDestroyOnLoad(customObject);
                    ItemDrop.ItemData itemData = customObject.GetComponent<ItemDrop>().m_itemData.Clone();
                    Recipe recipe = ObjectDB.instance.GetRecipe(customObject.GetComponent<ItemDrop>().m_itemData);
                    item.id = customObject.name;
                    item.name_key = itemData.m_shared.m_name.Substring(1);
                    item.dlc = itemData.m_shared.m_dlc;
                    item.itemType = itemData.m_shared.m_itemType;
                    item.attachOverride = itemData.m_shared.m_attachOverride;
                    item.description_key = itemData.m_shared.m_description.Substring(1);
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

                    if (recipe != null)
                    {
                        List<string> reqs = new List<string>();
                        foreach (Piece.Requirement req in recipe.m_resources)
                        {
                            RequirementData rd = new RequirementData();
                            rd.amount = req.m_amount;
                            rd.amountPerLevel = req.m_amountPerLevel;
                            rd.recover = req.m_recover;
                            rd.name = req.m_resItem.name;
                            reqs.Add(JsonUtility.ToJson(rd));
                        }
                        item.requirements = reqs;
                    }
                    string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomItemsOutput", $"{kvp.Key}.json");
                    string json = JsonUtility.ToJson(item);

                    File.WriteAllText(file, json);

                }
                foreach (KeyValuePair<string, CustomItem> kvp in customItems)
                {

                    GetCustomGameObject(kvp.Key);
                    
                    Dbgl($"adding strings {kvp.Value.name_key} = {kvp.Value.name} and  {kvp.Value.description_key} = {kvp.Value.description}");

                    Traverse.Create(Localization.instance).Field("m_translations").GetValue<Dictionary<string,string>>()[kvp.Value.name_key] = kvp.Value.name;
                    Traverse.Create(Localization.instance).Field("m_translations").GetValue<Dictionary<string,string>>()[kvp.Value.description_key] = kvp.Value.description;

                    //Dbgl($"adding {customObject.name} to prefabs; in db? {ObjectDB.instance.GetItemPrefab(kvp.Value.id).name}");
                    //___m_prefabs.Add(customObject);
                    //___m_namedPrefabs[customObject.name.GetStableHashCode()] = customObject;
                }
            }
        }


        public static Transform RecursiveFind(Transform parent, string childName)
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


        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
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
