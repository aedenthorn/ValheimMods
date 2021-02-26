using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace AutoFuel
{
    [BepInPlugin("aedenthorn.AutoFuel", "Auto Fuel", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> dropRange;
        public static ConfigEntry<float> containerRange;
        public static ConfigEntry<string> fuelDisallowTypes;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static List<Container> containerList = new List<Container>();
        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            dropRange = Config.Bind<float>("General", "DropRange", 10f, "The maximum range to pull dropped fuel");
            containerRange = Config.Bind<float>("General", "ContainerRange", 10f, "The maximum range to pull fuel from containers");
            fuelDisallowTypes = Config.Bind<string>("General", "FuelDisallowTypes", "RoundLog,FineWood", "Types of item to disallow as fuel.");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 146, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public static List<Container> GetNearbyContainers(Vector3 center)
        {
            List<Container> containers = new List<Container>();
            foreach (Container container in containerList)
            {
                if (container != null && container.transform != null && container.GetInventory() != null && (containerRange.Value <= 0 || Vector3.Distance(center, container.transform.position) < containerRange.Value) && Traverse.Create(container).Method("CheckAccess", new object[] { Player.m_localPlayer.GetPlayerID() }).GetValue<bool>())
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
                if(__instance.name.StartsWith("piece_chest") && __instance.GetInventory() != null)
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



        [HarmonyPatch(typeof(Smelter), "FixedUpdate")]
        static class Smelter_FixedUpdate_Patch
        {
            static void Postfix(Smelter __instance, ZNetView ___m_nview)
            {
                if (((int)typeof(Smelter).GetMethod("GetQueueSize", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { })) >= __instance.m_maxOre)
                    return;

                Vector3 vector = base.transform.position + Vector3.up;
                foreach (Collider collider in Physics.OverlapSphere(vector, this.m_autoPickupRange, this.m_autoPickupMask))
                {
                    if (collider.attachedRigidbody)
                    {
                        ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
                        if (!(component == null) && component.m_autoPickup && !this.HaveUniqueKey(component.m_itemData.m_shared.m_name) && component.GetComponent<ZNetView>().IsValid())
                        {
                            if (!component.CanPickup())
                            {
                                component.RequestOwn();
                            }
                            else if (this.m_inventory.CanAddItem(component.m_itemData, -1) && component.m_itemData.GetWeight() + this.m_inventory.GetTotalWeight() <= this.GetMaxCarryWeight())
                            {
                                float num = Vector3.Distance(component.transform.position, vector);
                                if (num <= this.m_autoPickupRange)
                                {
                                    if (num < 0.3f)
                                    {
                                        base.Pickup(component.gameObject);
                                    }
                                    else
                                    {
                                        Vector3 a = Vector3.Normalize(vector - component.transform.position);
                                        float d = 15f;
                                        component.transform.position = component.transform.position + a * d * dt;
                                    }
                                }
                            }
                        }
                    }
                }

                List<Container> nearbyContainers = GetNearbyContainers(__instance.transform.position);

                foreach (Smelter.ItemConversion itemConversion in __instance.m_conversion)
                {
                    foreach (Container c in nearbyContainers)
                    {
                        ItemDrop.ItemData item = c.GetInventory().GetItem(itemConversion.m_from.m_itemData.m_shared.m_name);
                        if (item != null)
                        {
                            if (fuelDisallowTypes.Value.Split(',').Contains(item.m_dropPrefab.name))
                            {
                                Dbgl($"container at {c.transform.position} has {item.m_stack} {item.m_dropPrefab.name} but it's forbidden by config");
                                continue;
                            }

                            Dbgl($"container at {c.transform.position} has {item.m_stack} {item.m_dropPrefab.name}, taking one");
                            ___m_nview.InvokeRPC("AddFuel", new object[] { });

                            c.GetInventory().RemoveItem(itemConversion.m_from.m_itemData.m_shared.m_name, 1);
                            typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
                            typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
                            return;
                        }
                    }
                }
            }
        }
    }
}
