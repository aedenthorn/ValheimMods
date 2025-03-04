using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CustomArmorStats
{
    [BepInPlugin("aedenthorn.CustomArmorStats", "Custom Armor Stats", "0.6.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> globalArmorDurabilityLossMult;
        public static ConfigEntry<float> globalArmorMovementModMult;
        
        public static ConfigEntry<string> waterModifierName;

        public static List<ArmorData> armorDatas;
        public static string assetPath;

        public enum NewDamageTypes 
        {
            Water = 1024
        }

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
            nexusID = Config.Bind<int>("General", "NexusID", 1162, "Nexus mod ID for updates");
            nexusID.Value = 1162;

            globalArmorDurabilityLossMult = Config.Bind<float>("Stats", "GlobalArmorDurabilityLossMult", 1f, "Global armor durability loss multiplier");
            globalArmorMovementModMult = Config.Bind<float>("Stats", "GlobalArmorMovementModMult", 1f, "Global armor movement modifier multiplier");

            waterModifierName = Config.Bind<string>("Strings", "WaterModifierName", "Water", "Name of water damage modifier to show in tooltip");

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomArmorStats");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {
            public static void Postfix(ZNetScene __instance)
            {
                if (!modEnabled.Value)
                    return;
                LoadAllArmorData(__instance);
            }

        }

        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        public static class ItemDrop_Awake_Patch
        {
            public static void Postfix(ItemDrop __instance)
            {
                if (!modEnabled.Value)
                    return;
                CheckArmorData(ref __instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(ItemDrop), "SlowUpdate")]
        public static class ItemDrop_SlowUpdate_Patch
        {
            public static void Postfix(ref ItemDrop __instance)
            {
                if (!modEnabled.Value)
                    return;
                CheckArmorData(ref __instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(Player), "DamageArmorDurability")]
        public static class Player_DamageArmorDurability_Patch
        {
            public static void Prefix(ref HitData hit)
            {
                if (!modEnabled.Value)
                    return;
                hit.ApplyModifier(globalArmorDurabilityLossMult.Value);
            }
        }

        [HarmonyPatch(typeof(Player), "GetEquipmentMovementModifier")]
        public static class GetEquipmentMovementModifier_Patch
        {
            public static void Postfix(ref float __result)
            {
                if (!modEnabled.Value)
                    return;
                __result *= globalArmorMovementModMult.Value;
            }
        }
        
        [HarmonyPatch(typeof(SEMan), "AddStatusEffect", new Type[] { typeof(StatusEffect), typeof(bool), typeof(int), typeof(float) })]
        public static class SEMan_AddStatusEffect_Patch
        {
            public static bool Prefix(SEMan __instance, StatusEffect statusEffect, Character ___m_character, ref StatusEffect __result)
            {
                if (!modEnabled.Value || !___m_character.IsPlayer())
                    return true;

                if(statusEffect.m_name == "$se_wet_name")
                {
                    var mod = GetNewDamageTypeMod(NewDamageTypes.Water, ___m_character);
                    if(mod == HitData.DamageModifier.Ignore || mod == HitData.DamageModifier.Immune)
                    {
                        __result = null;
                        return false;
                    }
                }

                return true;
            }

        }
        
        [HarmonyPatch(typeof(SE_Stats), "GetDamageModifiersTooltipString")]
        public static class GetDamageModifiersTooltipString_Patch
        {
            public static void Postfix(ref string __result, List<HitData.DamageModPair> mods)
            {
                if (!modEnabled.Value)
                    return;

                __result = Regex.Replace(__result, @"\n.*<color=orange></color>", "");
                foreach (HitData.DamageModPair damageModPair in mods)
                {
                    if (Enum.IsDefined(typeof(HitData.DamageType), damageModPair.m_type))
                        continue;

                    if (damageModPair.m_modifier != HitData.DamageModifier.Ignore && damageModPair.m_modifier != HitData.DamageModifier.Normal)
                    {
                        switch (damageModPair.m_modifier)
                        {
                            case HitData.DamageModifier.Resistant:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_resistant</color> VS ";
                                break;
                            case HitData.DamageModifier.Weak:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_weak</color> VS ";
                                break;
                            case HitData.DamageModifier.Immune:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_immune</color> VS ";
                                break;
                            case HitData.DamageModifier.VeryResistant:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_veryresistant</color> VS ";
                                break;
                            case HitData.DamageModifier.VeryWeak:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_veryweak</color> VS ";
                                break;
                        }
                        if ((int)damageModPair.m_type == (int)NewDamageTypes.Water)
                        {
                            __result += "<color=orange>"+ waterModifierName.Value +"</color>";
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), "UpdateEnvStatusEffects")]
        public static class UpdateEnvStatusEffects_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling UpdateEnvStatusEffects");

                var codes = new List<CodeInstruction>(instructions);
                var outCodes = new List<CodeInstruction>();
                bool notFound = true;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (notFound && codes[i].opcode == OpCodes.Ldloc_S && codes[i + 1].opcode == OpCodes.Ldc_I4_1 && codes[i + 2].opcode == OpCodes.Beq && codes[i + 3].opcode == OpCodes.Ldloc_S && codes[i + 3].operand == codes[i].operand && codes[i + 4].opcode == OpCodes.Ldc_I4_5)
                    {
                        Dbgl($"Adding frost immune and ignore");

                        outCodes.Add(new CodeInstruction(codes[i]));
                        outCodes.Add(new CodeInstruction(OpCodes.Ldc_I4_3));
                        outCodes.Add(new CodeInstruction(codes[i + 2]));
                        outCodes.Add(new CodeInstruction(codes[i]));
                        outCodes.Add(new CodeInstruction(OpCodes.Ldc_I4_4));
                        outCodes.Add(new CodeInstruction(codes[i + 2]));
                        notFound = false;
                    }
                    outCodes.Add(codes[i]);
                }

                return outCodes.AsEnumerable();
            }
            public static void Postfix(float dt, Player __instance, ItemDrop.ItemData ___m_chestItem, ItemDrop.ItemData ___m_legItem, ItemDrop.ItemData ___m_helmetItem, ItemDrop.ItemData ___m_shoulderItem, SEMan ___m_seman)
            {
                if (!modEnabled.Value)
                    return;
                var hash = "Wet".GetStableHashCode();
                if (___m_seman.HaveStatusEffect(hash))
                {
                    HitData.DamageModifier water = GetNewDamageTypeMod(NewDamageTypes.Water, ___m_chestItem, ___m_legItem, ___m_helmetItem, ___m_shoulderItem);
                    var wet = ___m_seman.GetStatusEffect(hash);
                    var t = Traverse.Create(wet);

                    if (water == HitData.DamageModifier.Ignore || water == HitData.DamageModifier.Immune)
                    {
                        ___m_seman.RemoveStatusEffect(hash, true);
                    }
                    else if (water == HitData.DamageModifier.VeryResistant && !(bool)AccessTools.Method(typeof(Character), "InLiquidSwimDepth").Invoke(__instance, new object[] { }))
                    {
                        ___m_seman.RemoveStatusEffect(hash, true);
                    }
                    else if (water == HitData.DamageModifier.Resistant)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() + dt);
                        ___m_seman.RemoveStatusEffect(hash, true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                    else if (water == HitData.DamageModifier.Weak)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() - dt / 3);
                        ___m_seman.RemoveStatusEffect(hash, true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                    else if (water == HitData.DamageModifier.VeryWeak)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() - dt * 2 / 3);
                        ___m_seman.RemoveStatusEffect(hash, true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                }
            }
        }
        public static HitData.DamageModifier GetNewDamageTypeMod(NewDamageTypes type, Character character)
        {
            Traverse t = Traverse.Create(character);
            return GetNewDamageTypeMod(type, t.Field("m_chestItem").GetValue<ItemDrop.ItemData>(), t.Field("m_legItem").GetValue<ItemDrop.ItemData>(), t.Field("m_helmetItem").GetValue<ItemDrop.ItemData>(), t.Field("m_shoulderItem").GetValue<ItemDrop.ItemData>());
        }

        public static HitData.DamageModifier GetNewDamageTypeMod(NewDamageTypes type, ItemDrop.ItemData chestItem, ItemDrop.ItemData legItem, ItemDrop.ItemData helmetItem, ItemDrop.ItemData shoulderItem)
        {
            HitData.DamageModPair modPair = new HitData.DamageModPair();
            
            if(chestItem != null)
                modPair = chestItem.m_shared.m_damageModifiers.FirstOrDefault(s => (int)s.m_type == (int)type);

            if (legItem != null)
            {
                var leg = legItem.m_shared.m_damageModifiers.FirstOrDefault(s => (int)s.m_type == (int)type);
                if (ShouldOverride(modPair.m_modifier, leg.m_modifier))
                    modPair = leg;
            }
            if (helmetItem != null)
            {
                var helm = helmetItem.m_shared.m_damageModifiers.FirstOrDefault(s => (int)s.m_type == (int)type);
                if (ShouldOverride(modPair.m_modifier, helm.m_modifier))
                    modPair = helm;
            }
            if (shoulderItem != null)
            {
                var shoulder = shoulderItem.m_shared.m_damageModifiers.FirstOrDefault(s => (int)s.m_type == (int)type);
                if (ShouldOverride(modPair.m_modifier, shoulder.m_modifier))
                    modPair = shoulder;
            }
            return modPair.m_modifier;
        }
        public static bool ShouldOverride(HitData.DamageModifier a, HitData.DamageModifier b)
        {
            return a != HitData.DamageModifier.Ignore && (b == HitData.DamageModifier.Immune || ((a != HitData.DamageModifier.VeryResistant || b != HitData.DamageModifier.Resistant) && (a != HitData.DamageModifier.VeryWeak || b != HitData.DamageModifier.Weak)));
        }

        public static void LoadAllArmorData(ZNetScene scene)
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

        public static void CheckArmorData(ref ItemDrop.ItemData instance)
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

        public static List<ArmorData> GetArmorDataFromFiles()
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

        public static void CheckModFolder()
        {
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }
        }

        public static void SetArmorData(ref ItemDrop.ItemData item, ArmorData armor)
        {
            item.m_shared.m_armor = armor.armor;
            item.m_shared.m_armorPerLevel = armor.armorPerLevel;
            item.m_shared.m_movementModifier = armor.movementModifier;

            item.m_shared.m_damageModifiers.Clear();
            foreach(string modString in armor.damageModifiers)
            {
                string[] mod = modString.Split(':');
                int modType = Enum.TryParse<NewDamageTypes>(mod[0], out NewDamageTypes result) ? (int)result : (int)Enum.Parse(typeof(HitData.DamageType), mod[0]);
                item.m_shared.m_damageModifiers.Add(new HitData.DamageModPair() { m_type = (HitData.DamageType)modType, m_modifier = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), mod[1]) });
            }
        }

        public static ArmorData GetArmorDataByName(string armor)
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

        public static ArmorData GetArmorDataFromItem(ItemDrop.ItemData item, string itemName)
        {
            var armor = new ArmorData()
            {
                name = itemName,

                armor = item.m_shared.m_armor,
                armorPerLevel = item.m_shared.m_armorPerLevel,
                movementModifier = item.m_shared.m_movementModifier,
                damageModifiers = item.m_shared.m_damageModifiers.Select(m => m.m_type + ":" + m.m_modifier).ToList()
            };

            return armor;
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
                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reload"))
                {
                    armorDatas = GetArmorDataFromFiles();
                    if(ZNetScene.instance)
                        LoadAllArmorData(ZNetScene.instance);
                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} reloaded armor stats from files");
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} damagetypes"))
                {
                    __instance.AddString(text);
                    
                    Dbgl("\r\n" + string.Join("\r\n", Enum.GetNames(typeof(HitData.DamageType))));

                    __instance.AddString($"{context.Info.Metadata.Name} dumped damage types");
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} damagemods"))
                {
                    __instance.AddString(text);

                    Dbgl("\r\n"+string.Join("\r\n", Enum.GetNames(typeof(HitData.DamageModifier))));

                    __instance.AddString($"{context.Info.Metadata.Name} dumped damage modifiers");
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
                    File.WriteAllText(Path.Combine(assetPath, armorData.name + ".json"), JsonUtility.ToJson(armorData, true));
                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} saved armor data to {armor}.json");
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
                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} dumped {armor}");
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

                    __instance.AddString(text);
                    __instance.AddString(output);
                    return false;
                }
                return true;
            }
        }
    }
}
