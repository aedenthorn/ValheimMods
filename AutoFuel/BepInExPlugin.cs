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
    [BepInPlugin("aedenthorn.AutoFuel", "Auto Fuel", "0.2.3")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> dropRange;
        public static ConfigEntry<float> containerRange;
        public static ConfigEntry<string> fuelDisallowTypes;
        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<string> toggleString;
        public static ConfigEntry<bool> isOn;
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
            toggleString = Config.Bind<string>("General", "ToggleString", "Auto Fuel: {0}", "Text to show on toggle. {0} is replaced with true/false");
            toggleKey = Config.Bind<string>("General", "ToggleKey", "", "Key to toggle behaviour. Leave blank to disable the toggle key.");
            isOn = Config.Bind<bool>("General", "IsOn", true, "Behaviour is currently on or not");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 159, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (Console.IsVisible())
                return;
            if (CheckKeyDown(toggleKey.Value))
            {
                isOn.Value = !isOn.Value;
                Config.Save();
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(toggleString.Value, toggleKey.Value), 0, null);
            }

        }
        private static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }

        public static List<Container> GetNearbyContainers(Vector3 center)
        {
            if (Player.m_localPlayer == null)
                return new List<Container>();
            List<Container> containers = new List<Container>();
            foreach (Container container in containerList)
            {
                if (container?.transform != null && container.GetInventory() != null && (containerRange.Value <= 0 || Vector3.Distance(center, container.transform.position) < containerRange.Value) && Traverse.Create(container).Method("CheckAccess", new object[] { Player.m_localPlayer.GetPlayerID() }).GetValue<bool>())
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
                if (!Player.m_localPlayer || !isOn.Value)
                    return;

                if (Mathf.CeilToInt(___m_nview.GetZDO().GetFloat("fuel", 0f)) >= __instance.m_maxFuel)
                    return;

                Vector3 position = __instance.transform.position + Vector3.up;
                foreach (Collider collider in Physics.OverlapSphere(position, containerRange.Value, LayerMask.GetMask(new string[] { "item" })))
                {
                    if (collider.attachedRigidbody)
                    {
                        ItemDrop item = collider.attachedRigidbody.GetComponent<ItemDrop>();
                        //Dbgl($"nearby item name: {item.m_itemData.m_dropPrefab.name}");
                        if (item?.GetComponent<ZNetView>()?.IsValid() == true && item.m_itemData.m_shared.m_name == __instance.m_fuelItem.m_itemData.m_shared.m_name && Mathf.CeilToInt(___m_nview.GetZDO().GetFloat("fuel", 0f)) < __instance.m_maxFuel)
                        {
                            if (fuelDisallowTypes.Value.Split(',').Contains(item.m_itemData.m_dropPrefab.name))
                            {
                                //Dbgl($"ground has {item.m_itemData.m_dropPrefab.name} but it's forbidden by config");
                                continue; 
                            }

                            Dbgl($"auto adding fuel from ground");
                            
                            while(item.m_itemData.m_stack > 1 && Mathf.CeilToInt(___m_nview.GetZDO().GetFloat("fuel", 0f)) < __instance.m_maxFuel)
                            {
                                item.m_itemData.m_stack--;
                                ___m_nview.InvokeRPC("AddFuel", new object[] { });
                                Traverse.Create(item).Method("Save").GetValue();
                            }

                            if(item.m_itemData.m_stack == 1 && Mathf.CeilToInt(___m_nview.GetZDO().GetFloat("fuel", 0f)) < __instance.m_maxFuel)
                            {
                                if (___m_nview.GetZDO() == null)
                                    Destroy(item.gameObject);
                                else
                                    ZNetScene.instance.Destroy(item.gameObject);
                                ___m_nview.InvokeRPC("AddFuel", new object[] { });
                            }

                            if (Mathf.CeilToInt(___m_nview.GetZDO().GetFloat("fuel", 0f)) >= __instance.m_maxFuel)
                                return;
                        }
                    }
                }

                List<Container> nearbyContainers = GetNearbyContainers(__instance.transform.position);

                foreach (Container c in nearbyContainers)
                {
                    ItemDrop.ItemData item = c.GetInventory().GetItem(__instance.m_fuelItem.m_itemData.m_shared.m_name);
                    if (item != null)
                    {
                        if (fuelDisallowTypes.Value.Split(',').Contains(item.m_dropPrefab.name))
                        {
                            //Dbgl($"container at {c.transform.position} has {item.m_stack} {item.m_dropPrefab.name} but it's forbidden by config");
                            continue;
                        }

                        Dbgl($"container at {c.transform.position} has {item.m_stack} {item.m_dropPrefab.name}, taking one");
                        ___m_nview.InvokeRPC("AddFuel", new object[] { });

                        c.GetInventory().RemoveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name, 1);
                        typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
                        typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
                        if (Mathf.CeilToInt(___m_nview.GetZDO().GetFloat("fuel", 0f)) >= __instance.m_maxFuel)
                            return;
                    }
                }
            }
        }
    }
}
