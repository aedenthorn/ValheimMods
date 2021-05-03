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
    [BepInPlugin("aedenthorn.CustomWeaponStats", "Custom Weapon Stats", "0.3.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> globalDamageMultiplier;
        public static ConfigEntry<float> globalUseDurabilityMultiplier;
        public static ConfigEntry<float> globalAttackForceMultiplier;
        public static ConfigEntry<float> globalBackstabBonusMultiplier;
        public static ConfigEntry<float> globalHoldDurationMinMultiplier;
        private static List<WeaponData> weaponDatas;
        private static string assetPath;

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
            nexusID = Config.Bind<int>("General", "NexusID", 1151, "Nexus mod ID for updates");

            globalDamageMultiplier = Config.Bind<float>("Global", "GlobalDamageMultiplier", 1f, "Global damage multiplier for all weapons");
            globalUseDurabilityMultiplier = Config.Bind<float>("Global", "GlobalUseDurabilityMultiplier", 1f, "Global use durability multiplier for all weapons");
            globalAttackForceMultiplier = Config.Bind<float>("Global", "GlobalAttackForceMultiplier", 1f, "Global attack force multiplier for all weapons");
            globalBackstabBonusMultiplier = Config.Bind<float>("Global", "GlobalBackstabBonusMultiplier", 1f, "Global backstab bonus multiplier for all weapons");
            globalHoldDurationMinMultiplier = Config.Bind<float>("Global", "GlobalHoldDurationMinMultiplier", 1f, "Global hold duration minimum multiplier for all weapons");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix(ZNetScene __instance)
            {
                if (!modEnabled.Value)
                    return;
                LoadAllWeaponData(__instance);
            }

        }

        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        static class ItemDrop_Awake_Patch
        {
            static void Postfix(ItemDrop __instance)
            {
                if (!modEnabled.Value)
                    return;
                CheckWeaponData(ref __instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(ItemDrop), "SlowUpdate")]
        static class ItemDrop_SlowUpdate_Patch
        {
            static void Postfix(ref ItemDrop __instance)
            {
                if (!modEnabled.Value)
                    return;
                CheckWeaponData(ref __instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(Attack), "Start")]
        static class Attack_Start_Patch
        {
            static void Prefix(ref ItemDrop.ItemData weapon, ref ItemDrop.ItemData __state)
            {
                if (!modEnabled.Value)
                    return;

                CheckWeaponData(ref weapon);

                __state = weapon;

                weapon.m_shared.m_useDurabilityDrain *= globalUseDurabilityMultiplier.Value;
                weapon.m_shared.m_holdDurationMin *= globalHoldDurationMinMultiplier.Value;
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

                //Dbgl("damage: "+weapon.m_shared.m_damages.m_damage);
            }
            static void Postfix(ref ItemDrop.ItemData ___m_weapon, ItemDrop.ItemData __state)
            {
                if (!modEnabled.Value)
                    return;

                ___m_weapon = __state;
            }
        }

        private static void LoadAllWeaponData(ZNetScene scene)
        {
            weaponDatas = GetWeaponDataFromFiles();
            foreach (var weapon in weaponDatas)
            {
                GameObject go = scene.GetPrefab(weapon.name);
                if (go == null)
                    continue;
                ItemDrop.ItemData item = go.GetComponent<ItemDrop>().m_itemData;
                SetWeaponData(ref item, weapon);
                go.GetComponent<ItemDrop>().m_itemData = item;
            }
        }

        private static void CheckWeaponData(ref ItemDrop.ItemData instance)
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


        private static List<WeaponData> GetWeaponDataFromFiles()
        {
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomWeaponStats");
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }

            List<WeaponData> weaponDatas = new List<WeaponData>();

            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                WeaponData data = JsonUtility.FromJson<WeaponData>(File.ReadAllText(file));
                weaponDatas.Add(data);
            }
            return weaponDatas;
        }
        private static void SetWeaponData(ref ItemDrop.ItemData item, WeaponData weapon)
        {
            item.m_shared.m_ammoType = weapon.ammoType;
            item.m_shared.m_useDurability = weapon.useDurability;
            item.m_shared.m_useDurabilityDrain = weapon.useDurabilityDrain;
            item.m_shared.m_skillType = weapon.skillType;
            item.m_shared.m_holdDurationMin = weapon.holdDurationMin;
            item.m_shared.m_toolTier = weapon.toolTier;
            item.m_shared.m_blockable = weapon.blockable;
            item.m_shared.m_dodgeable = weapon.dodgeable;
            item.m_shared.m_attackForce = weapon.attackForce;
            item.m_shared.m_backstabBonus = weapon.backStabBonus;
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
            item.m_shared.m_attackStatusEffect = ObjectDB.instance.GetStatusEffect(weapon.statusEffect);
            //Dbgl($"Set weapon data for {weapon.name}");
        }

        private static WeaponData GetWeaponDataByName(string weapon)
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

        private static WeaponData GetWeaponDataFromItem(ItemDrop.ItemData item, string itemName)
        {
            return new WeaponData()
            {
                name = itemName,
                ammoType = item.m_shared.m_ammoType,
                useDurability = item.m_shared.m_useDurability,
                useDurabilityDrain = item.m_shared.m_useDurabilityDrain,
                skillType = item.m_shared.m_skillType,
                holdDurationMin = item.m_shared.m_holdDurationMin,
                toolTier = item.m_shared.m_toolTier,
                blockable = item.m_shared.m_blockable,
                dodgeable = item.m_shared.m_dodgeable,
                attackForce = item.m_shared.m_attackForce,
                backStabBonus = item.m_shared.m_backstabBonus,
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
                statusEffect = item.m_shared.m_attackStatusEffect?.name
            };
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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reload"))
                {
                    weaponDatas = GetWeaponDataFromFiles();
                    if(ZNetScene.instance)
                        LoadAllWeaponData(ZNetScene.instance);
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
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} se"))
                {
                    Dbgl(string.Join("\r\n", ObjectDB.instance.m_StatusEffects.Select(se => se.name)));

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
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
