using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CraftFromContainers
{
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.5.2")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<bool> modEnabled;
        public static List<Container> containerList = new List<Container>();
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            m_range = Config.Bind<float>("General", "ContainerRange", 10f, "The maximum range from which to pull items from");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 40, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static List<Container> GetNearbyContainers(Vector3 center)
        {
            List<Container> containers = new List<Container>();
            foreach (Container container in containerList)
            {
                if (container != null && container.transform != null && container.GetInventory() != null && Vector3.Distance(center, container.transform.position) < m_range.Value)
                {
                    containers.Add(container);
                }
            }
            return containers;
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        static class Container_Awake_Patch
        {
            static void Postfix(Container __instance, ZNetView ___m_nview)
            {
                containerList.Add(__instance);

            }
        }
        [HarmonyPatch(typeof(Container), "OnDestroyed")]
        static class Container_OnDestroyed_Patch
        {
            static void Prefix(Container __instance)
            {
                containerList.Remove(__instance);

            }
        }

        [HarmonyPatch(typeof(Player), "HaveRequirements", new Type[] { typeof(Piece.Requirement[]), typeof(bool), typeof(int) })]
        static class HaveRequirements_Patch
        {
            static void Postfix(Player __instance, ref bool __result, Piece.Requirement[] resources, bool discover, int qualityLevel, HashSet<string> ___m_knownMaterial)
            {
                if (__result || discover)
                    return;
                List<Container> nearbyContainers = GetNearbyContainers(__instance.transform.position);

                foreach (Piece.Requirement requirement in resources)
                {
                    if (requirement.m_resItem)
                    {
                        int amount = requirement.GetAmount(qualityLevel);
                        int invAmount = __instance.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                        if(invAmount < amount)
                        {
                            foreach(Container c in nearbyContainers)
                                invAmount += c.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                            if (invAmount < amount)
                                return;
                        }
                    }
                }
                __result = true;
            }
        }

        [HarmonyPatch(typeof(Player), "HaveRequirements", new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
        static class HaveRequirements_Patch2
        {
            static void Postfix(Player __instance, ref bool __result, Piece piece, Player.RequirementMode mode, HashSet<string> ___m_knownMaterial, Dictionary<string, int> ___m_knownStations)
            {
                if (__result || __instance?.transform?.position == null)
                    return;

                if (piece.m_craftingStation)
                {
                    if (mode == Player.RequirementMode.IsKnown || mode == Player.RequirementMode.CanAlmostBuild)
                    {
                        if (!___m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
                        {
                            return;
                        }
                    }
                    else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, __instance.transform.position))
                    {
                        return;
                    }
                }
                if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
                {
                    return;
                }

                List<Container> nearbyContainers = GetNearbyContainers(__instance.transform.position);

                foreach (Piece.Requirement requirement in piece.m_resources)
                {
                    if (requirement.m_resItem && requirement.m_amount > 0)
                    {
                        if (mode == Player.RequirementMode.IsKnown)
                        {
                            if (!___m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
                            {
                                return;
                            }
                        }
                        else if (mode == Player.RequirementMode.CanAlmostBuild)
                        {
                            if (!__instance.GetInventory().HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
                            {
                                bool hasItem = false;
                                foreach(Container c in nearbyContainers)
                                {
                                    if (c.GetInventory().HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
                                    {
                                        hasItem = true;
                                        break;
                                    }
                                }
                                if (!hasItem)
                                    return;
                            }
                        }
                        else if (mode == Player.RequirementMode.CanBuild && __instance.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < requirement.m_amount)
                        {
                            int hasItems = __instance.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                            foreach (Container c in nearbyContainers)
                            {
                                try
                                {
                                    hasItems += c.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                                    if (hasItems >= requirement.m_amount)
                                    {
                                        break;
                                    }
                                }
                                catch { }
                            }
                            if (hasItems < requirement.m_amount)
                                return;
                        }
                    }
                }
                __result = true;
            }
        }

        [HarmonyPatch(typeof(Player), "ConsumeResources")]
        static class ConsumeResources_Patch
        {
            static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel)
            {
                Inventory pInventory = __instance.GetInventory();
                List<Container> nearbyContainers = GetNearbyContainers(__instance.transform.position);
                foreach (Piece.Requirement requirement in requirements)
                {
                    if (requirement.m_resItem)
                    {
                        int totalRequirement = requirement.GetAmount(qualityLevel);
                        if (totalRequirement <= 0)
                            continue;

                        string reqName = requirement.m_resItem.m_itemData.m_shared.m_name;
                        int totalAmount = pInventory.CountItems(reqName);
                        Dbgl($"have {totalAmount}/{totalRequirement} {reqName} in player inventory");
                        pInventory.RemoveItem(reqName, Math.Min(totalAmount, totalRequirement));

                        if (totalAmount < totalRequirement)
                        {
                            foreach (Container c in nearbyContainers)
                            {
                                Inventory cInventory = c.GetInventory();
                                int thisAmount = Mathf.Min(cInventory.CountItems(reqName), totalRequirement - totalAmount);

                                Dbgl($"Container at {c.transform.position} has {cInventory.CountItems(reqName)}");

                                if (thisAmount == 0)
                                    continue;


                                for (int i = 0; i < cInventory.GetAllItems().Count; i++)
                                {
                                    ItemDrop.ItemData item = cInventory.GetItem(i);
                                    if(item.m_shared.m_name == reqName)
                                    {
                                        Dbgl($"Got stack of {item.m_stack} {reqName}");
                                        int stackAmount = Mathf.Min(item.m_stack, totalRequirement - totalAmount);
                                        if (stackAmount == item.m_stack)
                                            cInventory.RemoveItem(item);
                                        else
                                            item.m_stack -= stackAmount;
                                        c.GetType().GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });

                                        totalAmount += stackAmount;
                                        Dbgl($"total amount is now {totalAmount}/{totalRequirement} {reqName}");

                                        if (totalAmount >= totalRequirement)
                                            break;
                                    }
                                }
                                cInventory.GetType().GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cInventory, new object[] { });

                                if (totalAmount >= totalRequirement)
                                {
                                    Dbgl($"consumed enough {reqName}");
                                    break;
                                }
                            }
                        }
                    }
                }
                return false;
            }
        }
    }
}
