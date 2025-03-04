using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DiscardInventoryItem
{
    [BepInPlugin("aedenthorn.DiscardInventoryItem", "Discard or Recycle Inventory Items", "1.0.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> returnUnknownResources;
        public static ConfigEntry<bool> returnEnchantedResources;
        public static ConfigEntry<float> returnResources;
        public static ConfigEntry<int> nexusID;

        public static BepInExPlugin context;
        public static Assembly epicLootAssembly;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            hotKey = Config.Bind<string>("General", "DiscardHotkey", "delete", "The hotkey to discard an item");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            returnUnknownResources = Config.Bind<bool>("General", "ReturnUnknownResources", false, "Return resources if recipe is unknown");
            returnEnchantedResources = Config.Bind<bool>("General", "ReturnEnchantedResources", false, "Return resources for Epic Loot enchantments");
            returnResources = Config.Bind<float>("General", "ReturnResources", 1f, "Fraction of resources to return (0.0 - 1.0)");
            nexusID = Config.Bind<int>("General", "NexusID", 45, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Start()
        {
            if (Chainloader.PluginInfos.ContainsKey("randyknapp.mods.epicloot"))
                epicLootAssembly = Chainloader.PluginInfos["randyknapp.mods.epicloot"].Instance.GetType().Assembly;

        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateItemDrag")]
        public static class UpdateItemDrag_Patch
        {
            public static void Postfix(InventoryGui __instance, ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory, int ___m_dragAmount, ref GameObject ___m_dragGo)
            {
                if (!modEnabled.Value || !Input.GetKeyDown(hotKey.Value) || ___m_dragItem == null || !___m_dragInventory.ContainsItem(___m_dragItem))
                    return;

                Dbgl($"Discarding {___m_dragAmount}/{___m_dragItem.m_stack} {___m_dragItem.m_dropPrefab.name}");

                if (returnResources.Value > 0)
                {
                    Recipe recipe = ObjectDB.instance.GetRecipe(___m_dragItem);

                    if (recipe != null && (returnUnknownResources.Value || Player.m_localPlayer.IsRecipeKnown(___m_dragItem.m_shared.m_name)))
                    {
                        Dbgl($"Recipe stack: {recipe.m_amount} num of stacks: {___m_dragAmount / recipe.m_amount}");


                        var reqs = recipe.m_resources.ToList();

                        bool isMagic = false;
                        bool cancel = false;
                        if (epicLootAssembly != null && returnEnchantedResources.Value)
                        {
                            isMagic = (bool)epicLootAssembly.GetType("EpicLoot.ItemDataExtensions").GetMethod("IsMagic", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(ItemDrop.ItemData) }, null).Invoke(null, new[] { ___m_dragItem });
                        }
                        if (isMagic)
                        {
                            int rarity = (int)epicLootAssembly.GetType("EpicLoot.ItemDataExtensions").GetMethod("GetRarity", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { ___m_dragItem });
                            List<KeyValuePair<ItemDrop, int>> magicReqs = (List<KeyValuePair<ItemDrop, int>>)epicLootAssembly.GetType("EpicLoot.Crafting.EnchantHelper").GetMethod("GetEnchantCosts", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { ___m_dragItem, rarity });
                            foreach (var kvp in magicReqs)
                            {
                                if (!returnUnknownResources.Value && ((ObjectDB.instance.GetRecipe(kvp.Key.m_itemData) && !Player.m_localPlayer.IsRecipeKnown(kvp.Key.m_itemData.m_shared.m_name)) || !Traverse.Create(Player.m_localPlayer).Field("m_knownMaterial").GetValue<HashSet<string>>().Contains(kvp.Key.m_itemData.m_shared.m_name)))
                                {
                                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You don't know all the recipes for this item's materials.");
                                    return;
                                }
                                reqs.Add(new Piece.Requirement()
                                {
                                    m_amount = kvp.Value,
                                    m_resItem = kvp.Key
                                });
                            }
                        }

                        if (!cancel && ___m_dragAmount / recipe.m_amount > 0)
                        {
                            for (int i = 0; i < ___m_dragAmount / recipe.m_amount; i++)
                            {
                                foreach (Piece.Requirement req in reqs)
                                {
                                    int quality = ___m_dragItem.m_quality;
                                    for (int j = quality; j > 0; j--)
                                    {
                                        GameObject prefab = ObjectDB.instance.m_items.FirstOrDefault(item => item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == req.m_resItem.m_itemData.m_shared.m_name);
                                        ItemDrop.ItemData newItem = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
                                        int numToAdd = Mathf.RoundToInt(req.GetAmount(j) * returnResources.Value);
                                        Dbgl($"Returning {numToAdd}/{req.GetAmount(j)} {prefab.name}");
                                        while (numToAdd > 0)
                                        {
                                            int stack = Mathf.Min(req.m_resItem.m_itemData.m_shared.m_maxStackSize, numToAdd);
                                            numToAdd -= stack;

                                            if (Player.m_localPlayer.GetInventory().AddItem(prefab.name, stack, req.m_resItem.m_itemData.m_quality, req.m_resItem.m_itemData.m_variant, 0, "") == null)
                                            {
                                                ItemDrop component = Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward + Player.m_localPlayer.transform.up, Player.m_localPlayer.transform.rotation).GetComponent<ItemDrop>();
                                                component.m_itemData = newItem;
                                                component.m_itemData.m_dropPrefab = prefab;
                                                component.m_itemData.m_stack = stack;
                                                Traverse.Create(component).Method("Save").GetValue();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (___m_dragAmount == ___m_dragItem.m_stack)
                {
                    Player.m_localPlayer.RemoveEquipAction(___m_dragItem);
                    Player.m_localPlayer.UnequipItem(___m_dragItem, false);
                    ___m_dragInventory.RemoveItem(___m_dragItem);
                }
                else
                    ___m_dragInventory.RemoveItem(___m_dragItem, ___m_dragAmount);
                Destroy(___m_dragGo);
                ___m_dragGo = null;
                __instance.GetType().GetMethod("UpdateCraftingPanel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { false });
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

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
