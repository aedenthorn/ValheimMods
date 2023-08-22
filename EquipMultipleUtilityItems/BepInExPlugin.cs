using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace EquipMultipleUtilityItems
{
    [BepInPlugin("aedenthorn.EquipMultipleUtilityItems", "Equip Multiple Utility Items", "0.6.0")]
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
            nexusID.Value = 1348;

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
                try
                {
                    var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem);

                    foreach (var item in list)
                    {
                        ___m_equipmentMovementModifier += item.m_shared.m_movementModifier;
                    }
                }
                catch
                {
                    //Dbgl($"Error: {Environment.StackTrace}");

                }
            }
        }                
        

        //[HarmonyPatch(typeof(Player), "ApplyArmorDamageMods")]
        static class ApplyArmorDamageMods_Patch
        {
            static void Postfix(Player __instance, ref HitData.DamageModifiers mods, ItemDrop.ItemData ___m_utilityItem)
            {
                if (!modEnabled.Value) 
                    return;

                try
                {
                    var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem);

                    foreach (var item in list)
                    {
                        mods.Apply(item.m_shared.m_damageModifiers);
                    }

                }
                catch
                {
                    //Dbgl($"Error: {Environment.StackTrace}");

                }
            }
        }                
        
        //[HarmonyPatch(typeof(Player), "GetBodyArmor")]
        static class GetBodyArmor_Patch
        {
            static void Postfix(Player __instance, ref float __result)
            {
                if (!modEnabled.Value) 
                    return;
                try
                {

                    var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility);

                    foreach (var item in list)
                    {
                        __result += item.GetArmor();
                    }
                }
                catch
                {
                    //Dbgl($"Error: {Environment.StackTrace}");

                }
            }
        }                
        
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
        static class Humanoid_IsItemEquiped_Patch
        {
            static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ItemDrop.ItemData ___m_utilityItem, ref bool __result)
            {
                if (!modEnabled.Value || __result) 
                    return;
                try
                {
                    __result = item.m_equipped && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && item != ___m_utilityItem;
                }
                catch
                {
                    //Dbgl($"Error: {Environment.StackTrace}");
                }
            }
        }  
        
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))]
        static class GetEquipmentEitrRegenModifier_Patch
        {
            static void Postfix(Player __instance, ItemDrop.ItemData ___m_utilityItem, ref float __result)
            {
                if (!modEnabled.Value) 
                    return;
                try
                {

                    var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem);

                    foreach (var item in list)
                    {
                        __result += item.m_shared.m_eitrRegenModifier;
                    }
                }
                catch
                {
                    //Dbgl($"Error: {Environment.StackTrace}");

                }
            }
        }                
        

        [HarmonyPatch(typeof(Player), "QueueEquipAction")]
        static class QueueEquipItem_Patch
        {
            static bool Prefix(Player __instance, ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || item == null || __instance.IsEquipActionQueued(item) || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Utility) 
                    return true;
                try
                {
                    var items = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility);
                    if (items.Exists(i => i.m_shared.m_name == item.m_shared.m_name))
                        return false;

                    if (items.Count >= maxEquippedItems.Value)
                        return false;
                }
                catch
                {
                    Dbgl($"Error: {Environment.StackTrace}");

                }

                return true;
            }
        }                
        
        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        static class EquipItem_Patch
        {
            static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, Inventory ___m_inventory, ref bool __result, ref ItemDrop.ItemData ___m_utilityItem)
            {
                try
                {

                    //Dbgl($"trying to equip item {item.m_shared.m_name}");
                    
                    if (!modEnabled.Value || item == null || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Utility || !__instance.IsPlayer() || !___m_inventory.ContainsItem(item) || __instance.InAttack() || __instance.InDodge() || (__instance.IsPlayer() && !__instance.IsDead() && __instance.IsSwimming() && !__instance.IsOnGround()) || (item.m_shared.m_useDurability && item.m_durability <= 0f) || (item.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(item.m_shared.m_dlc)))
                        return true;

                    //Dbgl($"can equip {item.m_shared.m_name}");

                    int count = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility).Count;
                    if (count >= maxEquippedItems.Value)
                    {
                        __result = false;
                        return false;
                    }
                    if (___m_utilityItem == null)
                    {
                        //Dbgl($"setting as utility item {item.m_shared.m_name}");

                        ___m_utilityItem = item;
                    }
                    item.m_equipped = true;
                    typeof(Humanoid).GetMethod("SetupEquipment", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { });
                    if (triggerEquipEffects)
                    {
                        typeof(Humanoid).GetMethod("TriggerEquipEffect", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { item });
                    }
                    __result = true;
                    //Dbgl($"Equipped {item.m_shared.m_name}");
                    return false;
                }
                catch
                {
                    Dbgl($"Error: {Environment.StackTrace}");

                }
                return true;
            }
        }                
        

        [HarmonyPatch(typeof(Humanoid), "UpdateEquipmentStatusEffects")]
        static class UpdateEquipmentStatusEffects_Patch
        {
            static void Prefix(Humanoid __instance, ItemDrop.ItemData ___m_utilityItem, SEMan ___m_seman)
            {
                try
                {
                    if (!modEnabled.Value || !__instance.IsPlayer())
                        return;
                    var list = __instance.GetInventory().GetAllItems().FindAll(i => !i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem && i.m_shared.m_equipStatusEffect);
                    var list2 = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem && i.m_shared.m_equipStatusEffect);

                    foreach (var item in list)
                    {
                        foreach (StatusEffect statusEffect in AccessTools.FieldRefAccess<SEMan, List<StatusEffect>>(___m_seman, "m_statusEffects"))
                        {
                            if (statusEffect.name == item.m_shared.m_equipStatusEffect.name && (___m_utilityItem is null || ___m_utilityItem.m_shared.m_equipStatusEffect.name != statusEffect.name) && !list2.Exists(i => i.m_shared.m_equipStatusEffect.name == statusEffect.name))
                            {
                                ___m_seman.RemoveStatusEffect(statusEffect.NameHash(), false);
                            }
                        }
                    }
                }
                catch
                {
                    //Dbgl($"Error: {Environment.StackTrace}");
                }
            }
            static void Postfix(Humanoid __instance, ItemDrop.ItemData ___m_utilityItem, SEMan ___m_seman)
            {
                try
                {
                    if (!modEnabled.Value || !__instance.IsPlayer())
                        return;
                    var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem && i.m_shared.m_equipStatusEffect);

                    foreach (var item in list)
                    {
                        ___m_seman.AddStatusEffect(item.m_shared.m_equipStatusEffect, false);
                    }
                    //Dbgl($"added {list.Count} effects");
                }
                catch
                {
                    //Dbgl($"Error: {Environment.StackTrace}");
                }
            }
        }                
        
        [HarmonyPatch(typeof(Humanoid), "UnequipAllItems")]
        static class UnequipAllItems_Patch
        {
            static void Postfix(Humanoid __instance, ItemDrop.ItemData ___m_utilityItem)
            {
                try
                {
                    if (!modEnabled.Value || !__instance.IsPlayer())
                        return;

                    var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem);
                    foreach (ItemDrop.ItemData item in list)
                        __instance.UnequipItem(item, false);
                }
                catch
                {
                    Dbgl($"Error: {Environment.StackTrace}");

                }
            }
        }
                    
                    
        [HarmonyPatch(typeof(Player), nameof(Player.UnequipDeathDropItems))]
        static class UnequipDeathDropItems_PatchUnequipItem
        {
            static void Postfix(Player __instance, ItemDrop.ItemData ___m_utilityItem)
            {
                try
                {
                    if (!modEnabled.Value)
                        return;

                    var list = __instance.GetInventory().GetAllItems().FindAll(i => i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && i != ___m_utilityItem);
                    foreach (ItemDrop.ItemData item in list)
                        __instance.UnequipItem(item, false);
                }
                catch
                {
                    Dbgl($"Error: {Environment.StackTrace}");

                }
            }
        }
                    

        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool) , typeof(float) })]
        static class GetTooltip_Patch
        {
            static void Postfix(ref ItemDrop.ItemData item, int qualityLevel, float worldLevel, ref string __result)
            {
                try
                {
                    if (!modEnabled.Value || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Utility)
                        return;

                    __result += string.Format("\n\n$item_armor: <color=orange>{0}</color>", item.GetArmor(qualityLevel, worldLevel));
                    if (item.m_shared.m_damageModifiers.Count > 0)
                        __result += SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
                }
                catch
                {
                    //Dbgl($"Error: {Environment.StackTrace}");

                }
            }
        }


        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                try
                {
                    string text = __instance.m_input.text;
                    if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                    {
                        context.Config.Reload();
                        context.Config.Save();

                        __instance.AddString(text);
                        __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                        return false;
                    }
                }
                catch
                {
                    Dbgl($"Error: {Environment.StackTrace}");
                }
                return true;
            }
        }
    }
}
