using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CraftFromContainers
{
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<bool> modEnabled;
        public static List<Container> containerList = new List<Container>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            m_range = Config.Bind<float>("General", "ContainerRange", 10f, "The maximum range from which to pull items from");
            modEnabled = Config.Bind<bool>("General", "enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static List<Container> GetNearbyContainers(Vector3 center)
        {
            List<Container> containers = new List<Container>();
            foreach (Container container in containerList)
                if (container != null && container.transform != null && container.IsOwner()  && Vector3.Distance(center, container.transform.position) < m_range.Value)
                    containers.Add(container);
            return containers;
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        static class Container_Awake_Patch
        {
            static void Prefix(Container __instance)
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

                foreach (Piece.Requirement requirement in resources)
                {
                    if (requirement.m_resItem)
                    {
                        if (discover)
                        {
                            if (requirement.m_amount > 0 && !___m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
                                return;
                        }
                        else
                        {
                            int amount = requirement.GetAmount(qualityLevel);
                            int invAmount = __instance.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                            if(invAmount < amount)
                            {
                                foreach(Container c in GetNearbyContainers(__instance.transform.position))
                                    invAmount += c.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                                if (invAmount < amount)
                                    return;
                            }
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
                        pInventory.RemoveItem(reqName, totalAmount);

                        if (totalAmount < totalRequirement)
                        {
                            foreach (Container c in GetNearbyContainers(__instance.transform.position))
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
