using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomArmorStats
{
    [BepInPlugin("aedenthorn.CustomArmorStats", "Custom Armor Stats", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> globalArmorDurabilityLossMult;
        public static ConfigEntry<float> globalArmorMovementModMult;

        private static List<ArmorData> armorDatas;
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
            
            globalArmorDurabilityLossMult = Config.Bind<float>("Stats", "GlobalArmorDurabilityLossMult", 1f, "Global armor durability loss multiplier");
            globalArmorMovementModMult = Config.Bind<float>("Stats", "GlobalArmorMovementModMult", 1f, "Global armor movement modifier multiplier");

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomArmorStats");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix(ZNetScene __instance)
            {
                if (!modEnabled.Value)
                    return;
                LoadAllArmorData(__instance);
            }

        }

        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        static class ItemDrop_Awake_Patch
        {
            static void Postfix(ItemDrop __instance)
            {
                if (!modEnabled.Value)
                    return;
                CheckArmorData(ref __instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(ItemDrop), "SlowUpdate")]
        static class ItemDrop_SlowUpdate_Patch
        {
            static void Postfix(ref ItemDrop __instance)
            {
                if (!modEnabled.Value)
                    return;
                CheckArmorData(ref __instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(Humanoid), "DamageArmorDurability")]
        static class DamageArmorDurability_Patch
        {
            static void Prefix(ref HitData hit)
            {
                if (!modEnabled.Value)
                    return;
                hit.ApplyModifier(globalArmorDurabilityLossMult.Value);
            }
        }

        [HarmonyPatch(typeof(Player), "GetEquipmentMovementModifier")]
        static class GetEquipmentMovementModifier_Patch
        {
            static void Postfix(ref float __result)
            {
                if (!modEnabled.Value)
                    return;
                __result *= globalArmorMovementModMult.Value;
            }
        }

        private static void LoadAllArmorData(ZNetScene scene)
        {
            armorDatas = GetArmorDataFromFiles();
            foreach (var armor in armorDatas)
            {
                GameObject go = scene.GetPrefab(armor.name);
                if (go == null)
                    continue;
                ItemDrop.ItemData item = go.GetComponent<ItemDrop>().m_itemData;
                SetArmorData(ref item, armor);
                go.GetComponent<ItemDrop>().m_itemData = item;
            }
        }

        private static void CheckArmorData(ref ItemDrop.ItemData instance)
        {
            try
            {
                var name = instance.m_dropPrefab.name;
                var armor = armorDatas.First(d => d.name == name);
                SetArmorData(ref instance, armor);
                //Dbgl($"Set armor data for {instance.name}");
            }
            catch
            {

            }
        }

        private static List<ArmorData> GetArmorDataFromFiles()
        {
            CheckModFolder();

            List<ArmorData> armorDatas = new List<ArmorData>();

            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                ArmorData data = JsonUtility.FromJson<ArmorData>(File.ReadAllText(file));
                armorDatas.Add(data);
            }
            return armorDatas;
        }

        private static void CheckModFolder()
        {
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }
        }

        private static void SetArmorData(ref ItemDrop.ItemData item, ArmorData armor)
        {
            item.m_shared.m_armor = armor.armor;
            item.m_shared.m_armorPerLevel = armor.armorPerLevel;
            item.m_shared.m_movementModifier = armor.movementModifier;

            item.m_shared.m_damageModifiers.Clear();
            foreach(string modString in armor.damageModifiers)
            {
                string[] mod = modString.Split(':');
                item.m_shared.m_damageModifiers.Add(new HitData.DamageModPair() { m_type = (HitData.DamageType)Enum.Parse(typeof(HitData.DamageType), mod[0]), m_modifier = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), mod[1]) });
            }
        }

        private static ArmorData GetArmorDataByName(string armor)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(armor);
            if (!go)
            {
                Dbgl("Armor not found!");
                return null;
            }

            ItemDrop.ItemData item = go.GetComponent<ItemDrop>().m_itemData;

            return GetArmorDataFromItem(item, armor);
        }

        private static ArmorData GetArmorDataFromItem(ItemDrop.ItemData item, string itemName)
        {
            var armor = new ArmorData()
            {
                name = itemName,

                armor = item.m_shared.m_armor,
                armorPerLevel = item.m_shared.m_armorPerLevel,
                movementModifier = item.m_shared.m_movementModifier,
                damageModifiers = item.m_shared.m_damageModifiers.Select(m => m.m_type + ":" + m.m_modifier).ToList()
            };

            List<string> mods = new List<string>();
            foreach(var mod in item.m_shared.m_damageModifiers)
            {
                mods.Add(mod.m_type + "," + mod.m_modifier);
            }

            return armor;
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
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reload"))
                {
                    armorDatas = GetArmorDataFromFiles();
                    if(ZNetScene.instance)
                        LoadAllArmorData(ZNetScene.instance);
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} reloaded armor stats from files" }).GetValue();
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} damagetypes"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    
                    Dbgl("\r\n" + string.Join("\r\n", Enum.GetNames(typeof(HitData.DamageType))));

                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped damage types" }).GetValue();
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} damagemods"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();

                    Dbgl("\r\n"+string.Join("\r\n", Enum.GetNames(typeof(HitData.DamageModifier))));

                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped damage modifiers" }).GetValue();
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} save "))
                {
                    var t = text.Split(' ');
                    string armor = t[t.Length - 1];
                    ArmorData armorData = GetArmorDataByName(armor);
                    if (armorData == null)
                        return false;
                    CheckModFolder();
                    File.WriteAllText(Path.Combine(assetPath, armorData.name + ".json"), JsonUtility.ToJson(armorData));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} saved armor data to {armor}.json" }).GetValue();
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} dump "))
                {
                    var t = text.Split(' ');
                    string armor = t[t.Length - 1];
                    ArmorData armorData = GetArmorDataByName(armor);
                    if (armorData == null)
                        return false;
                    Dbgl(JsonUtility.ToJson(armorData));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped {armor}" }).GetValue();
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()}"))
                {
                    string output = $"{context.Info.Metadata.Name} reset\r\n"
                    + $"{context.Info.Metadata.Name} reload\r\n"
                    + $"{context.Info.Metadata.Name} dump <ArmorName>\r\n"
                    + $"{context.Info.Metadata.Name} save <ArmorName>\r\n"
                    + $"{context.Info.Metadata.Name} damagetypes\r\n"
                    + $"{context.Info.Metadata.Name} damagemods";

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { output }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
