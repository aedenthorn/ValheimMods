using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ShowContainerContents
{
    [BepInPlugin("aedenthorn.ShowContainerContents", "Show Container Contents", "0.1.2")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> maxEntries;
        public static ConfigEntry<SortType> sortType;
        public static ConfigEntry<bool> sortAsc;
        public static ConfigEntry<string> entryString;
        public static ConfigEntry<string> overFlowText;

        public enum SortType
        {
            Name,
            Weight,
            Amount,
            Value
        }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 829, "Nexus mod ID for updates");
            maxEntries = Config.Bind<int>("General", "MaxEntries", -1, "Max number of entries to show");
            sortType = Config.Bind<SortType>("General", "SortType", SortType.Value, "Type by which to sort entries.");
            sortAsc = Config.Bind<bool>("General", "SortAsc", false, "Sort ascending?");
            entryString = Config.Bind<string>("General", "EntryText", "<color=#AAAAAAFF>{0} {1}</color>", "Entry text. {0} is replaced by the total amount, {1} is replaced by the item name.");
            overFlowText = Config.Bind<string>("General", "OverFlowText", "<color=#AAAAAAFF>...</color>", "Overflow text if more items than max entries.");


            //lastRegenDate = Config.Bind<int>("ZAuto", "LastRegenDate", 0, "Last regen date (auto updated)");
            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }



        [HarmonyPatch(typeof(Container), "GetHoverText")]
        static class GetHoverText_Patch
        {
            static void Postfix(Container __instance, ref string __result)
            {
                if (!modEnabled.Value || (__instance.m_checkGuardStone && !PrivateArea.CheckAccess(__instance.transform.position, 0f, false, false)) || __instance.GetInventory().NrOfItems() == 0)
                    return;

                var items = new List<ItemDrop.ItemData>(__instance.GetInventory().GetAllItems());
                SortUtils.SortByType(SortType.Value, items, sortAsc.Value);
                int entries = 0;
                int amount = 0;
                string name = "";
                for(int i = 0; i < items.Count; i++)
                {
                    if (maxEntries.Value >= 0 && entries >= maxEntries.Value)
                    {
                        if(overFlowText.Value.Length > 0)
                            __result += "\n"+overFlowText.Value;
                        break;
                    }
                    ItemDrop.ItemData item = items[i];

                    if (item.m_shared.m_name == name || name == "")
                    {
                        amount += item.m_stack;
                    }
                    else
                    {
                        __result += "\n" + string.Format(entryString.Value, amount, Localization.instance.Localize(name));
                        entries++;

                        amount = item.m_stack;
                    }
                    name = item.m_shared.m_name;
                    if (i == items.Count - 1)
                    {
                        __result += "\n" + string.Format(entryString.Value, amount, Localization.instance.Localize(name));
                    }
                }
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
