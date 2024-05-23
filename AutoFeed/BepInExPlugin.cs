﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace AutoFeed
{
    [BepInPlugin("aedenthorn.AutoFeed", "Auto Feed", "0.8.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<float> containerRange;
        public static ConfigEntry<float> moveProximity;
        public static ConfigEntry<string> feedDisallowTypes;
        public static ConfigEntry<string> animalDisallowTypes;
        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<string> toggleString;
        public static ConfigEntry<bool> isOn;
        public static ConfigEntry<bool> requireMove;
        public static ConfigEntry<bool> requireOnlyFood;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        private static BepInExPlugin context;
        private static float lastFeed;
        private static int feedCount;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            containerRange = Config.Bind<float>("Config", "ContainerRange", 10f, "Container range in metres.");
            feedDisallowTypes = Config.Bind<string>("Config", "FeedDisallowTypes", "", "Types of item to disallow as feed, comma-separated.");
            animalDisallowTypes = Config.Bind<string>("Config", "AnimalDisallowTypes", "", "Types of creature to disallow to feed, comma-separated.");
            requireMove = Config.Bind<bool>("Config", "RequireMove", true, "Require animals to move to container to feed.");
            requireOnlyFood = Config.Bind<bool>("Config", "RequireOnlyFood", false, "Don't allow feeding from containers that have items that the animal will not eat as well.");
            moveProximity = Config.Bind<float>("Config", "MoveProximity", 2f, "How close to move towards the container if RequireMove is true.");
            
            toggleKey = Config.Bind<string>("General", "ToggleKey", "", "Key to toggle behaviour. Leave blank to disable the toggle key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            toggleString = Config.Bind<string>("General", "ToggleString", "Auto Feed: {0}", "Text to show on toggle. {0} is replaced with true/false");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Show debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 985, "Nexus mod ID for updates");
            
            isOn = Config.Bind<bool>("ZAuto", "IsOn", true, "Behaviour is currently on or not");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (AedenthornUtils.CheckKeyDown(toggleKey.Value) && !AedenthornUtils.IgnoreKeyPresses(true))
            {
                isOn.Value = !isOn.Value;
                Config.Save();
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(toggleString.Value, isOn.Value), 0, null);
            }

        }
        private static string GetPrefabName(string name)
        {
            char[] anyOf = new char[]{'(',' '};
            int num = name.IndexOfAny(anyOf);
            string result;
            if (num >= 0)
                result = name.Substring(0, num);
            else
                result = name;
            return result;
        }

        private static Container FindClosestContainer(Vector3 center, float range, MonsterAI monsterAI)
        {
            Traverse traverseAI = Traverse.Create(monsterAI);
            Container closestContainer = null;
            float closestDistance = 999999f; ;
            foreach (Collider collider in Physics.OverlapSphere(center, Mathf.Max(range, 0), LayerMask.GetMask(new string[] { "piece" })))
            {
                Container container = collider.transform.parent?.parent?.gameObject?.GetComponent<Container>();
                if (container?.GetComponent<ZNetView>()?.IsValid() == true)
                {
                    //Dbgl($"valid {Vector3.Distance(center, container.transform.position)}");
                    if (container.name.StartsWith("piece_chest_trough") && container.GetInventory() != null)
                    {
                        //Dbgl($"trough {Vector3.Distance(center, container.transform.position)}");

                        float distance = Vector3.Distance(container.transform.position, center);
                        if (distance < moveProximity.Value
                            || traverseAI.Method("HavePath", new object[] { container.transform.position }).GetValue<bool>())
                        {
                            //Dbgl($"path {Vector3.Distance(center, container.transform.position)}");
                            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
                            {
                                if (monsterAI.m_consumeItems.Exists(i => i.m_itemData.m_shared.m_name == item.m_shared.m_name))
                                {
                                    //Dbgl($"{monsterAI.gameObject.name} found suitable container at ({container.transform.position},  {Vector3.Distance(center, container.transform.position)})");
                                    if (closestDistance > distance)
                                    {
                                        closestContainer = container;
                                        closestDistance = distance;
                                    }
                                    break;
                                }
                                //Dbgl($"no item");
                            }
                        }
                    }
                }
            }

            return closestContainer;
        }

        [HarmonyPatch(typeof(MonsterAI), "UpdateConsumeItem")]
        static class UpdateConsumeItem_Patch
        {
            static void Postfix(MonsterAI __instance, ZNetView ___m_nview, Character ___m_character, Tameable ___m_tamable, List<ItemDrop> ___m_consumeItems, float dt, bool __result)
            {
                if (!modEnabled.Value || !isOn.Value || __result || !___m_character || !___m_nview || !___m_nview.IsOwner() || ___m_tamable == null || !___m_character.IsTamed() || !___m_tamable.IsHungry() || ___m_consumeItems == null || ___m_consumeItems.Count == 0)
                    return;

                string name = GetPrefabName(__instance.gameObject.name);

                if (animalDisallowTypes.Value.Split(',').Contains(name))
                {
                    return;
                }

                var nearbyContainer = FindClosestContainer(___m_character.gameObject.transform.position, containerRange.Value, __instance);

                using (List<ItemDrop>.Enumerator enumerator = __instance.m_consumeItems.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (nearbyContainer != null)
                        {
                            if (Utils.DistanceXZ(nearbyContainer.transform.position, __instance.transform.position) < moveProximity.Value && Mathf.Abs(nearbyContainer.transform.position.y - __instance.transform.position.y) > moveProximity.Value)
                                continue;

                            ItemDrop.ItemData item = nearbyContainer.GetInventory().GetItem(enumerator.Current.m_itemData.m_shared.m_name);
                            if (item != null)
                            {
                                if (feedDisallowTypes.Value.Split(',').Contains(item.m_dropPrefab.name))
                                {
                                    continue;
                                }

                                if (Time.time - lastFeed < 0.1)
                                {
                                    feedCount++;
                                    FeedAnimal(__instance, ___m_tamable, ___m_character, nearbyContainer, item, feedCount * 33);
                                }
                                else
                                {
                                    feedCount = 0;
                                    lastFeed = Time.time;
                                    FeedAnimal(__instance, ___m_tamable, ___m_character, nearbyContainer, item, 0);
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }
        public static async void FeedAnimal(MonsterAI monsterAI, Tameable tamable, Character character, Container c, ItemDrop.ItemData item, int delay)
        {
            await Task.Delay(delay);

            if (tamable is null || monsterAI is null || !tamable.IsHungry())
                return;

            if (requireOnlyFood.Value)
            {
                foreach (ItemDrop.ItemData temp in c.GetInventory().GetAllItems())
                {
                    if (!monsterAI.m_consumeItems.Exists(i => i.m_itemData.m_shared.m_name == temp.m_shared.m_name))
                        return;
                }
            }

            string name = GetPrefabName(monsterAI.gameObject.name);
            if (requireMove.Value && name != "Deer")
            {
                try
                {
                    //Dbgl($"{monsterAI.gameObject.name} {monsterAI.transform.position} trying to move to {c.transform.position} {Utils.DistanceXZ(monsterAI.transform.position, c.transform.position)}");

                    ZoneSystem.instance.GetGroundHeight(c.transform.position, out float ground);

                    Vector3 groundTarget = new Vector3(c.transform.position.x, ground, c.transform.position.z);

                    Traverse traverseAI = Traverse.Create(monsterAI);
                    traverseAI.Field("m_lastFindPathTime").SetValue(0);

                    if (!traverseAI.Method("MoveTo", new object[] { 0.05f, groundTarget, moveProximity.Value, false }).GetValue<bool>())
                        return;

                    if (Mathf.Abs(c.transform.position.y - monsterAI.transform.position.y) > moveProximity.Value)
                        return;

                    traverseAI.Method("LookAt", new object[] { c.transform.position }).GetValue();

                    if (!traverseAI.Method("IsLookingAt", new object[] { c.transform.position, 90f, false}).GetValue<bool>())
                        return;
                }
                catch
                {

                }

                //Dbgl($"{monsterAI.gameObject.name} looking at");
            }

            Dbgl($"{monsterAI.gameObject.name} {monsterAI.transform.position} consuming {item.m_dropPrefab.name} at {c.transform.position}, distance {Utils.DistanceXZ(monsterAI.transform.position, c.transform.position)}");
            ConsumeItem(item, monsterAI, character);

            c.GetInventory().RemoveItem(item.m_shared.m_name, 1);
            typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
            typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
        }

        private static void ConsumeItem(ItemDrop.ItemData item, MonsterAI monsterAI, Character character)
        {
            monsterAI.m_onConsumedItem?.Invoke(null);

            (character as Humanoid).m_consumeItemEffects.Create(character.transform.position, Quaternion.identity, null, 1f, -1);
            Traverse.Create(monsterAI).Field("m_animator").GetValue<ZSyncAnimation>().SetTrigger("consume");
        }
    }
}
