using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DiscardInventoryItem
{
    [BepInPlugin("aedenthorn.DiscardInventoryItem", "Discard Inventory Items", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<string> m_hotkey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> returnResources;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            m_hotkey = Config.Bind<string>("General", "DiscardHotkey", "delete", "The hotkey to discard an item");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            returnResources = Config.Bind<float>("General", "ReturnResources", 1f, "Fraction of resources to return");
            nexusID = Config.Bind<int>("General", "NexusID", 45, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateItemDrag")]
        static class UpdateItemDrag_Patch
        {
            static void Postfix(InventoryGui __instance, ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory, int ___m_dragAmount, ref GameObject ___m_dragGo)
            {
                if(Input.GetKeyDown(m_hotkey.Value) && ___m_dragItem != null && ___m_dragInventory.ContainsItem(___m_dragItem))
                {
                    Dbgl($"Discarding {___m_dragAmount}/{___m_dragItem.m_stack} {___m_dragItem.m_dropPrefab.name}");

                    if (returnResources.Value > 0)
                    {
                        Recipe recipe = ObjectDB.instance.GetRecipe(___m_dragItem);

                        if (recipe != null)
                        {
                            for (int i = 0; i < ___m_dragAmount; i++)
                            {
                                foreach (Piece.Requirement req in recipe.m_resources)
                                {
                                    int quality = ___m_dragItem.m_quality;
                                    for(int j = quality; j > 0; j--)
                                    {
                                        GameObject prefab = ObjectDB.instance.m_items.FirstOrDefault(item => item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == req.m_resItem.m_itemData.m_shared.m_name);
                                        ItemDrop.ItemData newItem = prefab.GetComponent<ItemDrop>().m_itemData;
                                        int numToAdd = Mathf.RoundToInt(req.GetAmount(j) * returnResources.Value);
                                        Dbgl($"Returning {numToAdd}/{req.GetAmount(j)} {prefab.name}");
                                        while (numToAdd > 0)
                                        {
                                            int stack = Mathf.Min(req.m_resItem.m_itemData.m_shared.m_maxStackSize, numToAdd);
                                            numToAdd -= stack;

                                            if (Player.m_localPlayer.GetInventory().AddItem(prefab.name, stack, req.m_resItem.m_itemData.m_quality, req.m_resItem.m_itemData.m_variant, Player.m_localPlayer.GetPlayerID(), Player.m_localPlayer.GetPlayerName()) == null)
                                            {
                                                ItemDrop component = Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward + Player.m_localPlayer.transform.up, Player.m_localPlayer.transform.rotation).GetComponent<ItemDrop>();
                                                component.m_itemData = newItem.Clone();
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

                    if (___m_dragAmount == ___m_dragItem.m_stack)
                    {
                        ___m_dragInventory.RemoveItem(___m_dragItem);

                    }
                    else
                        ___m_dragInventory.RemoveItem(___m_dragItem, ___m_dragAmount);
                    Destroy(___m_dragGo);
                    ___m_dragGo = null; 
                    __instance.GetType().GetMethod("UpdateCraftingPanel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { false });
                }
            }
        }
    }
}
