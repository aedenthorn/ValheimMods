using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomWeaponStats
{
    [BepInPlugin("aedenthorn.CustomWeaponStats", "Custom Weapon Stats", "0.7.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> globalDamageMultiplier;
        public static ConfigEntry<float> globalUseDurabilityMultiplier;
        public static ConfigEntry<float> globalAttackForceMultiplier;
        public static ConfigEntry<float> globalBackstabBonusMultiplier;
        public static ConfigEntry<float> globalHoldDurationMinMultiplier;
        public static ConfigEntry<float> globalHoldStaminaDrainMultiplier;
        public static ConfigEntry<float> globalAttackStaminaUseMultiplier;
        public static List<WeaponData> weaponDatas;
        public static string assetPath;

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
            nexusID = Config.Bind<int>("General", "NexusID", 1151, "Nexus mod ID for updates");

            globalDamageMultiplier = Config.Bind<float>("Global", "GlobalDamageMultiplier", 1f, "Global damage multiplier for all weapons");
            globalUseDurabilityMultiplier = Config.Bind<float>("Global", "GlobalUseDurabilityMultiplier", 1f, "Global use durability multiplier for all weapons");
            globalAttackForceMultiplier = Config.Bind<float>("Global", "GlobalAttackForceMultiplier", 1f, "Global attack force multiplier for all weapons");
            globalBackstabBonusMultiplier = Config.Bind<float>("Global", "GlobalBackstabBonusMultiplier", 1f, "Global backstab bonus multiplier for all weapons");
            globalHoldDurationMinMultiplier = Config.Bind<float>("Global", "GlobalHoldDurationMinMultiplier", 1f, "Global hold duration minimum multiplier for all weapons");
            globalHoldStaminaDrainMultiplier = Config.Bind<float>("Global", "GlobalHoldStaminaDrainMultiplier", 1f, "Global hold stamina drain multiplier for all weapons");
            globalAttackStaminaUseMultiplier = Config.Bind<float>("Global", "GlobalAttackStaminaUseMultiplier", 1f, "Global attack stamina use multiplier for all weapons");

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomWeaponStats");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class CopyOtherDB_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                LoadAllWeaponData(true);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Show")]
        [HarmonyPriority(Priority.Last)]
        public static class InventoryGui_Show_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                LoadAllWeaponData(false);
            }
        }

        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        public static class ItemDrop_Awake_Patch
        {
            public static void Postfix(ItemDrop __instance)
            {
                if (!modEnabled.Value)
                    return;
                CheckWeaponData(ref __instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(ItemDrop), "SlowUpdate")]
        public static class ItemDrop_SlowUpdate_Patch
        {
            public static void Postfix(ref ItemDrop __instance)
            {
                if (!modEnabled.Value)
                    return;
                CheckWeaponData(ref __instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(Attack), "GetAttackStamina")]
        public static class GetAttackStamina_Patch
        {
            public static void Postfix(ref float __result)
            {
                if (!modEnabled.Value)
                    return;

                __result *= globalAttackStaminaUseMultiplier.Value;
            }
        }
 
        [HarmonyPatch(typeof(Attack), "Start")]
        public static class Attack_Start_Patch
        {
            public static void Prefix(ref ItemDrop.ItemData weapon, ref WeaponState __state)
            {
                if (!modEnabled.Value)
                    return;

                CheckWeaponData(ref weapon);

                Dbgl($"pre damage {weapon.m_shared.m_damages.m_slash}");

                __state = new WeaponState(weapon);

                weapon.m_shared.m_useDurabilityDrain *= globalUseDurabilityMultiplier.Value;
                weapon.m_shared.m_attackForce *= globalAttackForceMultiplier.Value;
                weapon.m_shared.m_backstabBonus *= globalBackstabBonusMultiplier.Value;
                weapon.m_shared.m_damages.m_damage *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_blunt *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_slash *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_pierce *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_chop *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_pickaxe *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_fire *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_frost *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_lightning *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_poison *= globalDamageMultiplier.Value;
                weapon.m_shared.m_damages.m_spirit *= globalDamageMultiplier.Value;

                Dbgl($"post damage {weapon.m_shared.m_damages.m_slash}");
            }
            public static void Postfix(ref ItemDrop.ItemData weapon, WeaponState __state)
            {
                if (!modEnabled.Value)
                    return;

                weapon.m_shared.m_useDurabilityDrain = __state.useDurabilityDrain;
                weapon.m_shared.m_attackForce = __state.attackForce;
                weapon.m_shared.m_backstabBonus = __state.backstabBonus;
                weapon.m_shared.m_damages.m_damage = __state.damage;
                weapon.m_shared.m_damages.m_blunt = __state.blunt;
                weapon.m_shared.m_damages.m_slash = __state.slash;
                weapon.m_shared.m_damages.m_pierce = __state.pierce;
                weapon.m_shared.m_damages.m_chop = __state.chop;
                weapon.m_shared.m_damages.m_pickaxe = __state.pickaxe;
                weapon.m_shared.m_damages.m_fire = __state.fire;
                weapon.m_shared.m_damages.m_frost = __state.frost;
                weapon.m_shared.m_damages.m_lightning = __state.lightning;
                weapon.m_shared.m_damages.m_poison = __state.poison;
                weapon.m_shared.m_damages.m_spirit = __state.spirit;
            }
        }

        public static void LoadAllWeaponData(bool reload)
        {
            if(reload)
                weaponDatas = GetWeaponDataFromFiles();
            
            foreach (var weapon in weaponDatas)
            {
                GameObject go = ObjectDB.instance.GetItemPrefab(weapon.name);
                if (go == null)
                    continue;
                var item = go.GetComponent<ItemDrop>()?.m_itemData;

                if (go.GetComponent<ItemDrop>()?.m_itemData == null)
                    continue;
                
                SetWeaponData(ref go.GetComponent<ItemDrop>().m_itemData, weapon);

                for(int i = 0; i < ObjectDB.instance.m_recipes.Count; i++)
                {
                    var recipe = ObjectDB.instance.m_recipes[i];
                    if (!(recipe.m_item == null) && recipe.m_item.m_itemData.m_shared.m_name == item.m_shared.m_name)
                    {
                        SetWeaponData(ref recipe.m_item.m_itemData, weapon);
                        ObjectDB.instance.m_recipes[i] = recipe;
                    }
                }
                if (Player.m_localPlayer)
                {
                    var inv = Player.m_localPlayer.GetInventory().GetAllItems();
                    for (int i = 0; i < inv.Count; i++)
                    {
                        var invItem = inv[i];
                        if (invItem.m_shared.m_name == item.m_shared.m_name)
                            SetWeaponData(ref invItem, weapon);

                        inv[i] = invItem;
                    }
                }
            }
        }

        public static void CheckWeaponData(ref ItemDrop.ItemData instance)
        {
            try
            {
                var name = instance.m_dropPrefab.name;
                var weapon = weaponDatas.First(d => d.name == name);
                SetWeaponData(ref instance, weapon);
                //Dbgl($"Set weapon data for {instance.name}");
            }
            catch
            {

            }
        }


        public static List<WeaponData> GetWeaponDataFromFiles()
        {

            CheckModFolder();

            List<WeaponData> weaponDatas = new List<WeaponData>();

            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                WeaponData data = JsonUtility.FromJson<WeaponData>(File.ReadAllText(file));
                weaponDatas.Add(data);
            }
            return weaponDatas;
        }
        public static void SetWeaponData(ref ItemDrop.ItemData item, WeaponData weapon)
        {
            item.m_shared.m_ammoType = weapon.ammoType;
            item.m_shared.m_useDurability = weapon.useDurability;
            item.m_shared.m_useDurabilityDrain = weapon.useDurabilityDrain;
            item.m_shared.m_durabilityPerLevel = weapon.durabilityPerLevel;
            item.m_shared.m_skillType = weapon.skillType;
            item.m_shared.m_toolTier = weapon.toolTier;
            item.m_shared.m_blockable = weapon.blockable;
            item.m_shared.m_dodgeable = weapon.dodgeable;
            item.m_shared.m_attackForce = weapon.attackForce;
            item.m_shared.m_backstabBonus = weapon.backStabBonus;
            item.m_shared.m_blockPower = weapon.blockPower;
            item.m_shared.m_blockPowerPerLevel = weapon.blockPowerPerLevel;
            item.m_shared.m_deflectionForce = weapon.deflectionForce;
            item.m_shared.m_deflectionForcePerLevel = weapon.deflectionForcePerLevel;

            item.m_shared.m_damages.m_damage = weapon.damage;
            item.m_shared.m_damages.m_blunt = weapon.blunt;
            item.m_shared.m_damages.m_slash = weapon.slash;
            item.m_shared.m_damages.m_pierce = weapon.pierce;
            item.m_shared.m_damages.m_chop = weapon.chop;
            item.m_shared.m_damages.m_pickaxe = weapon.pickaxe;
            item.m_shared.m_damages.m_fire = weapon.fire;
            item.m_shared.m_damages.m_frost = weapon.frost;
            item.m_shared.m_damages.m_lightning = weapon.lightning;
            item.m_shared.m_damages.m_poison = weapon.poison;
            item.m_shared.m_damages.m_spirit = weapon.spirit;

            item.m_shared.m_damagesPerLevel.m_damage = weapon.damagePerLevel;
            item.m_shared.m_damagesPerLevel.m_blunt = weapon.bluntPerLevel;
            item.m_shared.m_damagesPerLevel.m_slash = weapon.slashPerLevel;
            item.m_shared.m_damagesPerLevel.m_pierce = weapon.piercePerLevel;
            item.m_shared.m_damagesPerLevel.m_chop = weapon.chopPerLevel;
            item.m_shared.m_damagesPerLevel.m_pickaxe = weapon.pickaxePerLevel;
            item.m_shared.m_damagesPerLevel.m_fire = weapon.firePerLevel;
            item.m_shared.m_damagesPerLevel.m_frost = weapon.frostPerLevel;
            item.m_shared.m_damagesPerLevel.m_lightning = weapon.lightningPerLevel;
            item.m_shared.m_damagesPerLevel.m_poison = weapon.poisonPerLevel;
            item.m_shared.m_damagesPerLevel.m_spirit = weapon.spiritPerLevel;
            
            item.m_shared.m_attack.m_hitTerrain = weapon.hitTerrain;
            if(item.m_shared.m_secondaryAttack != null)
                item.m_shared.m_secondaryAttack.m_hitTerrain = weapon.hitTerrainSecondary;

            item.m_shared.m_attackStatusEffect = ObjectDB.instance.GetStatusEffect((string.IsNullOrEmpty(weapon.statusEffect) ? 0 : weapon.statusEffect.GetStableHashCode()));



            //Dbgl($"Set weapon data for {weapon.name}");
        }

        public static WeaponData GetWeaponDataByName(string weapon)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(weapon);
            if (!go)
            {
                Dbgl("Weapon not found!");
                return null;
            }

            ItemDrop.ItemData item = go.GetComponent<ItemDrop>().m_itemData;

            return GetWeaponDataFromItem(item, weapon);

        }

        public static WeaponData GetWeaponDataFromItem(ItemDrop.ItemData item, string itemName)
        {
            return new WeaponData()
            {
                name = itemName,
                ammoType = item.m_shared.m_ammoType,
                useDurability = item.m_shared.m_useDurability,
                durabilityPerLevel = item.m_shared.m_durabilityPerLevel,
                useDurabilityDrain = item.m_shared.m_useDurabilityDrain,
                skillType = item.m_shared.m_skillType,
                toolTier = item.m_shared.m_toolTier,
                blockable = item.m_shared.m_blockable,
                dodgeable = item.m_shared.m_dodgeable,
                attackForce = item.m_shared.m_attackForce,
                backStabBonus = item.m_shared.m_backstabBonus,
                blockPower = item.m_shared.m_blockPower,
                blockPowerPerLevel = item.m_shared.m_blockPowerPerLevel,
                deflectionForce = item.m_shared.m_deflectionForce,
                deflectionForcePerLevel = item.m_shared.m_deflectionForcePerLevel,

                damage = item.m_shared.m_damages.m_damage,
                blunt = item.m_shared.m_damages.m_blunt,
                slash = item.m_shared.m_damages.m_slash,
                pierce = item.m_shared.m_damages.m_pierce,
                chop = item.m_shared.m_damages.m_chop,
                pickaxe = item.m_shared.m_damages.m_pickaxe,
                fire = item.m_shared.m_damages.m_fire,
                frost = item.m_shared.m_damages.m_frost,
                lightning = item.m_shared.m_damages.m_lightning,
                poison = item.m_shared.m_damages.m_poison,
                spirit = item.m_shared.m_damages.m_spirit,

                damagePerLevel = item.m_shared.m_damagesPerLevel.m_damage,
                bluntPerLevel = item.m_shared.m_damagesPerLevel.m_blunt,
                slashPerLevel = item.m_shared.m_damagesPerLevel.m_slash,
                piercePerLevel = item.m_shared.m_damagesPerLevel.m_pierce,
                chopPerLevel = item.m_shared.m_damagesPerLevel.m_chop,
                pickaxePerLevel = item.m_shared.m_damagesPerLevel.m_pickaxe,
                firePerLevel = item.m_shared.m_damagesPerLevel.m_fire,
                frostPerLevel = item.m_shared.m_damagesPerLevel.m_frost,
                lightningPerLevel = item.m_shared.m_damagesPerLevel.m_lightning,
                poisonPerLevel = item.m_shared.m_damagesPerLevel.m_poison,
                spiritPerLevel = item.m_shared.m_damagesPerLevel.m_spirit,

                statusEffect = item.m_shared.m_attackStatusEffect?.name,

                hitTerrain = item.m_shared.m_attack?.m_hitTerrain == true,
                hitTerrainSecondary = item.m_shared.m_secondaryAttack?.m_hitTerrain == true
            };
        }
        public static void CheckModFolder()
        {
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }
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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reload"))
                {
                    weaponDatas = GetWeaponDataFromFiles();
                    LoadAllWeaponData(true);
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} reloaded weapon stats from files" }).GetValue();
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} dump "))
                {
                    var t = text.Split(' ');
                    string weapon = t[t.Length - 1];
                    WeaponData weaponData = GetWeaponDataByName(weapon);
                    if (weaponData == null)
                        return false;
                    Dbgl(JsonUtility.ToJson(weaponData));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped {weapon}" }).GetValue();
                    return false;
                }
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} skills"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    
                    List<string> output = new List<string>();
                    foreach(Skills.SkillType type in Enum.GetValues(typeof(Skills.SkillType)))
                    {
                        output.Add(Enum.GetName(typeof(Skills.SkillType), type) + " " + (int)type);
                    }
                    Dbgl(string.Join("\r\n", output));

                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped skill types" }).GetValue();
                    return false;
                }
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} se"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    
                    Dbgl(string.Join("\r\n", ObjectDB.instance.m_StatusEffects.Select(se => se.name)));

                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped status effects" }).GetValue();
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} save "))
                {
                    var t = text.Split(' ');
                    string weapon = t[t.Length - 1];
                    WeaponData weaponData = GetWeaponDataByName(weapon);
                    if (weaponData == null)
                        return false;
                    CheckModFolder();
                    File.WriteAllText(Path.Combine(assetPath, weaponData.name + ".json"), JsonUtility.ToJson(weaponData));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} saved weapon data to {weapon}.json" }).GetValue();
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()}"))
                {
                    string output = $"{context.Info.Metadata.Name} reset\r\n"
                    + $"{context.Info.Metadata.Name} reload\r\n"
                    + $"{context.Info.Metadata.Name} dump <WeaponName>\r\n"
                    + $"{context.Info.Metadata.Name} save <WeaponName>\r\n"
                    + $"{context.Info.Metadata.Name} skills"
                    + $"{context.Info.Metadata.Name} se";
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { output }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
