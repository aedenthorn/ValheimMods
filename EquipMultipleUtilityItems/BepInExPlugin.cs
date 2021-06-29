using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace EquipMultipleUtilityItems
{
    [BepInPlugin("aedenthorn.EquipMultipleUtilityItems", "Equip Multiple Utility Items", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> maxEquippedItems;


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
            nexusID = Config.Bind<int>("General", "NexusID", 1348, "Nexus mod ID for updates");

            maxEquippedItems = Config.Bind<int>("Variables", "MaxEquippedItems", 5, "Maximum number of utility items equipped at once.");

            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }


        [HarmonyPatch(typeof(Player), "UpdateMovementModifier")]
        static class UpdateMovementModifier_Patch
        {
            static void Postfix(Player __instance, ref float ___m_equipmentMovementModifier, ItemDrop.ItemData ___m_utilityItem)
            {
                if (!modEnabled.Value) 
                    return;


                var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equiped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem);

                foreach (var item in list)
                {
                    ___m_equipmentMovementModifier += item.m_shared.m_movementModifier;
                }
            }
        }                
        

        [HarmonyPatch(typeof(Player), "ApplyArmorDamageMods")]
        static class ApplyArmorDamageMods_Patch
        {
            static void Postfix(Player __instance, ref HitData.DamageModifiers mods, ItemDrop.ItemData ___m_utilityItem)
            {
                if (!modEnabled.Value) 
                    return;


                var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equiped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem);

                foreach (var item in list)
                {
                    mods.Apply(item.m_shared.m_damageModifiers);
                }
            }
        }                
        
        [HarmonyPatch(typeof(Player), "GetBodyArmor")]
        static class GetBodyArmor_Patch
        {
            static void Postfix(Player __instance, ref float __result)
            {
                if (!modEnabled.Value) 
                    return;


                var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equiped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility);

                foreach (var item in list)
                {
                    __result += item.GetArmor();
                }
            }
        }                
        

        [HarmonyPatch(typeof(Player), "QueueEquipItem")]
        static class QueueEquipItem_Patch
        {
            static bool Prefix(Player __instance, ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || item == null || __instance.IsItemQueued(item) || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Utility) 
                    return true;

                var items = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equiped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility);
                if (items.Exists(i => i.m_shared.m_name == item.m_shared.m_name))
                    return false;

                if (items.Count >= maxEquippedItems.Value)
                    return false;

                return true;
            }
        }                
        
        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        static class EquipItem_Patch
        {
            static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, Inventory ___m_inventory, ref bool __result, ref ItemDrop.ItemData ___m_utilityItem)
            {
                if (!modEnabled.Value || item == null || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Utility ||  !__instance.IsPlayer() || __instance.IsItemEquiped(item) || !___m_inventory.ContainsItem(item) || __instance.InAttack() || __instance.InDodge() || (__instance.IsPlayer() && !__instance.IsDead() && __instance.IsSwiming() && !__instance.IsOnGround()) || (item.m_shared.m_useDurability && item.m_durability <= 0f) || (item.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(item.m_shared.m_dlc)))
                    return true;

                int count = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equiped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility).Count;
                if (count >= maxEquippedItems.Value)
                {
                    __result = false;
                    return false;
                }
                if(count == 0)
                    ___m_utilityItem = item;
                item.m_equiped = true;
                typeof(Humanoid).GetMethod("SetupEquipment", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { });
                if (triggerEquipEffects)
                {
                    typeof(Humanoid).GetMethod("TriggerEquipEffect", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { item });
                }
                __result = true;
                return false;
            }
        }                
        

        [HarmonyPatch(typeof(Humanoid), "UpdateEquipmentStatusEffects")]
        static class UpdateEquipmentStatusEffects_Patch
        {
            static void Postfix(Humanoid __instance, ItemDrop.ItemData ___m_utilityItem, ref HashSet<StatusEffect> ___m_eqipmentStatusEffects, SEMan ___m_seman)
            {
                if (!modEnabled.Value || !__instance.IsPlayer())
                    return;

                var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equiped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem);

                foreach(var item in list)
                {
                    if (!item.m_shared.m_equipStatusEffect)
                        continue;
                    ___m_seman.AddStatusEffect(item.m_shared.m_equipStatusEffect, false);
                }
                Dbgl($"added {list.Count} effects");
            }
        }                
        
        [HarmonyPatch(typeof(Humanoid), "IsItemEquiped")]
        static class IsItemEquiped_Patch
        {
            static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
            {
                if (!modEnabled.Value || !__instance.IsPlayer() || __result || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Utility)
                    return;
                __result = item.m_equiped;
            }
        }


        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool) })]
        static class GetTooltip_Patch
        {
            static void Postfix(ref ItemDrop.ItemData item, int qualityLevel, ref string __result)
            {
                if (!modEnabled.Value || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Utility)
                    return;

                __result += string.Format("\n\n$item_armor: <color=orange>{0}</color>", item.GetArmor(qualityLevel));
                if(item.m_shared.m_damageModifiers.Count > 0)
                    __result += SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
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
                   
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
