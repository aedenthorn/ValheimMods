using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Debug = UnityEngine.Debug;
using AzuExtendedPlayerInventory;

namespace EquipMultipleUtilityItems
{
    [BepInDependency("Azumatt.AzuExtendedPlayerInventory", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("aedenthorn.EquipMultipleUtilityItems", "Equip Multiple Utility Items", "0.7.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> maxEquippedItems;

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
            nexusID = Config.Bind<int>("General", "NexusID", 1348, "Nexus mod ID for updates");
            nexusID.Value = 1348;

            maxEquippedItems = Config.Bind<int>("Variables", "MaxEquippedItems", 5, "Maximum number of utility items equipped at once.");

            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();

            if (AzuExtendedPlayerInventory.API.IsLoaded())
            {
                Dbgl("AzuExtendedPlayerInventory API is loaded.", true);
                for (int i = 1; i <= maxEquippedItems.Value; i++)
                {
                    AzuExtendedPlayerInventory.API.AddSlot($"Utility{i}", GetUtilityItem, IsValidUtilityItem);
                }
            }
            else
            {
                Dbgl("AzuExtendedPlayerInventory API is not loaded.", true);
            }
        }

#nullable enable

        private ItemDrop.ItemData? GetUtilityItem(Player player)
        {
            // Logic to get the utility item for the player
            return player.GetInventory().GetAllItems().Find(item => item.m_equipped && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility);
        }

#nullable disable

        private bool IsValidUtilityItem(ItemDrop.ItemData item)
        {
            // Logic to validate if the item is a utility item
            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility;
        }

        [HarmonyPatch(typeof(Player), "UpdateModifiers")]
        public static class Player_UpdateModifiers_Patch
        {
            public static void Postfix(Player __instance, FieldInfo[] ___s_equipmentModifierSourceFields, float[] ___m_equipmentModifierValues, ItemDrop.ItemData ___m_utilityItem)
            {
                if (!modEnabled.Value)
                    return;

                if (___s_equipmentModifierSourceFields == null)
                {
                    return;
                }
                for (int i = 0; i < ___m_equipmentModifierValues.Length; i++)
                {
                    float num = 0f;
                    try
                    {
                        var list = __instance.GetInventory().GetAllItems().FindAll(item => item.m_equipped && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && item != ___m_utilityItem);

                        foreach (var item in list)
                        {
                            num += (float)___s_equipmentModifierSourceFields[i].GetValue(item.m_shared);
                        }
                    }
                    catch
                    {
                        //Dbgl($"Error: {Environment.StackTrace}");

                    }

                    ___m_equipmentModifierValues[i] += num;
                }
            }
        }

        // Other patches remain unchanged...

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
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
