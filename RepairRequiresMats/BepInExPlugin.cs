using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RepairRequiresMats
{
    [BepInPlugin("aedenthorn.RepairRequiresMats", "Repair Requires Mats", "0.4.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> showAllRepairsInToolTip;
        public static ConfigEntry<float> materialRequirementMult;
        public static ConfigEntry<string> titleTooltipColor;
        public static ConfigEntry<string> hasEnoughTooltipColor;
        public static ConfigEntry<string> notEnoughTooltipColor;
        public static ConfigEntry<int> nexusID;
        private static List<ItemDrop.ItemData> orderedWornItems = new List<ItemDrop.ItemData>();

        private static BepInExPlugin context;
        private static Assembly epicLootAssembly;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            showAllRepairsInToolTip = Config.Bind<bool>("General", "ShowAllRepairsInToolTip", true, "Show all repairs in tooltip when hovering over repair button.");
            titleTooltipColor = Config.Bind<string>("General", "TitleTooltipColor", "FFFFFFFF", "Color to use in tooltip title.");
            hasEnoughTooltipColor = Config.Bind<string>("General", "HasEnoughTooltipColor", "FFFFFFFF", "Color to use in tooltip for items with enough resources to repair.");
            notEnoughTooltipColor = Config.Bind<string>("General", "NotEnoughTooltipColor", "FF0000FF", "Color to use in tooltip for items with enough resources to repair.");
            materialRequirementMult = Config.Bind<float>("General", "MaterialRequirementMult", 0.5f, "Multiplier for amount of each material required.");
            nexusID = Config.Bind<int>("General", "NexusID", 215, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }
        private void Start()
        {
            if(Chainloader.PluginInfos.ContainsKey("randyknapp.mods.epicloot"))
                epicLootAssembly = Chainloader.PluginInfos["randyknapp.mods.epicloot"].Instance.GetType().Assembly;

        }


        [HarmonyPatch(typeof(UITooltip), "LateUpdate")]
        static class UITooltip_LateUpdate_Patch
        {
            static void Postfix(UITooltip __instance, UITooltip ___m_current, GameObject ___m_tooltip)
            {

                if (!modEnabled.Value)
                    return;
                if (___m_current == __instance && ___m_tooltip != null && ___m_current.transform.name == "RepairButton")
                {
                    ___m_tooltip.transform.position = Input.mousePosition + new Vector3(-200,-100);
                }
            }
        }
        


        [HarmonyPatch(typeof(InventoryGui), "UpdateRepair")]
        static class InventoryGui_UpdateRepair_Patch
        {
            static void Postfix(InventoryGui __instance, ref List<ItemDrop.ItemData> ___m_tempWornItems)
            {
                if (!modEnabled.Value)
                    return;

                if (!___m_tempWornItems.Any())
                    return;

                List<RepairItemData> freeRepairs = new List<RepairItemData>();
                List<RepairItemData> enoughRepairs = new List<RepairItemData>();
                List<RepairItemData> notEnoughRepairs = new List<RepairItemData>();
                List<RepairItemData> unableRepairs = new List<RepairItemData>();
                List<string> outstring = new List<string>();
                foreach (ItemDrop.ItemData item in ___m_tempWornItems)
                {
                    if (!Traverse.Create(__instance).Method("CanRepair", new object[] { item }).GetValue<bool>())
                    {
                        unableRepairs.Add(new RepairItemData(item));
                        continue;
                    }
                    Recipe recipe = RepairRecipe(item);
                    if (recipe == null)
                    {
                        freeRepairs.Add(new RepairItemData(item));
                        continue;
                    }
                    List<string> reqstring = new List<string>();
                    foreach (Piece.Requirement req in recipe.m_resources)
                    {
                        if (req.GetAmount(item.m_quality) == 0)
                            continue;
                        reqstring.Add($"{req.GetAmount(item.m_quality)}/{Player.m_localPlayer.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name)} {Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name)}");
                    }
                    if (!Traverse.Create(Player.m_localPlayer).Method("HaveRequirements", new object[] { recipe.m_resources, false, 1 }).GetValue<bool>())
                        notEnoughRepairs.Add(new RepairItemData(item, reqstring));
                    else
                        enoughRepairs.Add(new RepairItemData(item, reqstring));
                }
                orderedWornItems = new List<ItemDrop.ItemData>();
                foreach (RepairItemData rid in freeRepairs)
                {
                    outstring.Add($"<color=#{hasEnoughTooltipColor.Value}>{Localization.instance.Localize(rid.item.m_shared.m_name)}: Free</color>");
                    orderedWornItems.Add(rid.item);
                }
                foreach (RepairItemData rid in enoughRepairs)
                {
                    outstring.Add($"<color=#{hasEnoughTooltipColor.Value}>{Localization.instance.Localize(rid.item.m_shared.m_name)}: {string.Join(", ", rid.reqstring)}</color>");
                    orderedWornItems.Add(rid.item);
                }
                foreach (RepairItemData rid in notEnoughRepairs)
                {
                    outstring.Add($"<color=#{notEnoughTooltipColor.Value}>{Localization.instance.Localize(rid.item.m_shared.m_name)}: {string.Join(", ", rid.reqstring)}</color>");
                    orderedWornItems.Add(rid.item);
                }
                foreach (RepairItemData rid in unableRepairs)
                {
                    orderedWornItems.Add(rid.item);
                }
                ___m_tempWornItems = new List<ItemDrop.ItemData>(orderedWornItems);

                if (!showAllRepairsInToolTip.Value)
                    return;

                UITooltip tt = (UITooltip)typeof(UITooltip).GetField("m_current", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                GameObject go = (GameObject)typeof(UITooltip).GetField("m_tooltip", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

                if (go == null || tt.transform.name != "RepairButton")
                    return;

                Utils.FindChild(go.transform, "Text").GetComponent<Text>().supportRichText = true;
                Utils.FindChild(go.transform, "Text").GetComponent<Text>().alignment = TextAnchor.LowerCenter;
                Utils.FindChild(go.transform, "Text").GetComponent<Text>().text = $"<b><color=#{titleTooltipColor.Value}>{Localization.instance.Localize("$inventory_repairbutton")}</color></b>\r\n" + string.Join("\r\n", outstring);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "CanRepair")]
        static class InventoryGui_CanRepair_Patch
        {
            static void Postfix(ItemDrop.ItemData item, ref bool __result)
            {
                if (!modEnabled.Value)
                    return;

                if (modEnabled.Value && Environment.StackTrace.Contains("RepairOneItem") && !Environment.StackTrace.Contains("HaveRepairableItems") && __result == true && item?.m_shared != null && Player.m_localPlayer != null && orderedWornItems.Count > 0)
                {
                    if (orderedWornItems[0] != item)
                    {
                        __result = false;
                        return;
                    }
                    Recipe recipe = RepairRecipe(item, true);
                    if (recipe == null)
                        return;

                    List<string> reqstring = new List<string>();
                    foreach (Piece.Requirement req in recipe.m_resources)
                    {
                        if (req?.m_resItem?.m_itemData?.m_shared == null)
                            continue;
                        reqstring.Add($"{req.GetAmount(item.m_quality)}/{Player.m_localPlayer.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name)} {Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name)}");
                    }
                    string outstring;
                    if (Traverse.Create(Player.m_localPlayer).Method("HaveRequirements", new object[] { recipe.m_resources, false, 1 }).GetValue<bool>())
                    {
                        Player.m_localPlayer.ConsumeResources(recipe.m_resources, item.m_quality);
                        outstring = $"Used {string.Join(", ", reqstring)} to repair {Localization.instance.Localize(item.m_shared.m_name)}";
                        __result = true;
                    }
                    else
                    {
                        outstring = $"Require {string.Join(", ", reqstring)} to repair {item.m_shared.m_name}";
                        __result = false;
                    }

                    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, outstring, 0, null);
                    Dbgl(outstring);
                }
            }
        }

        private static Recipe RepairRecipe(ItemDrop.ItemData item, bool log = false)
        {
            float percent = (item.GetMaxDurability() - item.m_durability) / item.GetMaxDurability();
            Recipe fullRecipe = ObjectDB.instance.GetRecipe(item);
            var fullReqs = fullRecipe.m_resources.ToList();

            bool isMagic = false;
            if (epicLootAssembly != null)
            {
                isMagic = (bool)epicLootAssembly.GetType("EpicLoot.ItemDataExtensions").GetMethod("IsMagic", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(ItemDrop.ItemData) }, null).Invoke(null, new[] { item });
            }
            if (isMagic)
            {
                int rarity = (int)epicLootAssembly.GetType("EpicLoot.ItemDataExtensions").GetMethod("GetRarity", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { item });
                List<KeyValuePair<ItemDrop, int>> magicReqs =  (List<KeyValuePair<ItemDrop, int>>)epicLootAssembly.GetType("EpicLoot.Crafting.EnchantTabController").GetMethod("GetEnchantCosts", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { item, rarity });
                foreach(var kvp in magicReqs)
                {
                    fullReqs.Add(new Piece.Requirement()
                    {
                        m_amount = kvp.Value,
                        m_resItem = kvp.Key
                    });
                }
            }

            
            List<Piece.Requirement> reqs = new List<Piece.Requirement>();
            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
            for (int i = 0; i < fullReqs.Count; i++)
            {

                var req = fullReqs[i];

                int amount = 0;
                for (int j = item.m_quality; j > 0; j--)
                {
                    //Dbgl($"{req.m_resItem.m_itemData.m_shared.m_name} req for level {j} {req.GetAmount(j)}");
                    amount += req.GetAmount(j);
                }

                amount = Mathf.FloorToInt(amount * percent * materialRequirementMult.Value);


                if (amount > 0)
                {
                    //Dbgl($"total {req.m_resItem.m_itemData.m_shared.m_name} reqs for {item.m_shared.m_name}, dur {percent}: {amount}");
                    reqs.Add(req);
                }
            }
            recipe.m_resources = reqs.ToArray();

            if (!reqs.Any())
            {
                return null;
            }
            return recipe;
        }


        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("repairmod reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Repair Items config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
