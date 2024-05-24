using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace AutoFeed
{
    [BepInPlugin("aedenthorn.AutoFeed", "Auto Feed", "0.8.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<float> containerRange;
        public static ConfigEntry<float> moveProximity;
        public static ConfigEntry<string> feedDisallowTypes;
        public static ConfigEntry<string> animalDisallowTypes;
        public static ConfigEntry<string> containerNameStartsWith;
        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<string> toggleString;
        public static ConfigEntry<bool> isOn;
        public static ConfigEntry<bool> requireMove;
        public static ConfigEntry<bool> requireOnlyFood;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        private static BepInExPlugin context;

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
            containerNameStartsWith = Config.Bind<string>("Config", "ContainerNameFilter", "piece_chest, Container", "Only feed from containers which's name start like this, comma-separated.");

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

        [HarmonyPatch(typeof(MonsterAI), "UpdateConsumeItem")]
        static class UpdateConsumeItem_Patch
        {
            static void Postfix(MonsterAI __instance, ZNetView ___m_nview, Character ___m_character, Tameable ___m_tamable, List<ItemDrop> ___m_consumeItems, float dt, bool __result)
            {
                if (!modEnabled.Value || !isOn.Value 
                    || __result || !___m_character || !___m_nview || !___m_nview.IsOwner() 
                    || ___m_tamable == null || !___m_character.IsTamed() || !___m_tamable.IsHungry() 
                    || ___m_consumeItems == null || ___m_consumeItems.Count == 0)
                    return;

                string name = GetPrefabName(__instance.gameObject.name);
                if (animalDisallowTypes.Value.Split(',').Contains(name))
                {
                    return;
                }

                Vector3 instancePosition = __instance.gameObject.transform.position;
                Traverse traverseAI = Traverse.Create(__instance);
                Container closestContainer = null;
                Vector3 closestContainerPosition = new Vector3(0f, 0f, 0f);
                float closestContainerDistance = containerRange.Value + 1;
                foreach (Collider collider in Physics.OverlapSphere(instancePosition, Mathf.Max(containerRange.Value, 0), LayerMask.GetMask(new string[] { "piece" })))
                {
                    Container container = collider.transform.parent?.parent?.gameObject?.GetComponent<Container>();
                    if (container?.GetComponent<ZNetView>()?.IsValid() == true)
                    {
                        foreach (string containerNameStart in containerNameStartsWith.Value.Split(','))
                        {
                            if (container.name.StartsWith(containerNameStart) && container.GetInventory() != null)
                            {
                                //Dbgl($"{monsterAI.gameObject.name} trough");
                                Vector3 containerPosition = container.transform.position;
                                float distance = Vector3.Distance(containerPosition, instancePosition);
                                if (distance < moveProximity.Value
                                    || traverseAI.Method("HavePath", new object[] { containerPosition }).GetValue<bool>())
                                {
                                    //Dbgl($"{monsterAI.gameObject.name} path");
                                    bool foundInedibleItem = true;
                                    bool foundEdibleItem = false;
                                    foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
                                    {
                                        if (__instance.m_consumeItems.Exists(i => i.m_itemData.m_shared.m_name == item.m_shared.m_name)
                                            && !feedDisallowTypes.Value.Split(',').Contains(item.m_dropPrefab.name))
                                        {
                                            foundEdibleItem = true;
                                            if (!requireOnlyFood.Value)
                                            {
                                                //Dbgl($"{monsterAI.gameObject.name} food found");
                                                break;
                                            }
                                        }
                                        else if (requireOnlyFood.Value)
                                        {
                                            Dbgl($"{__instance.gameObject.name} inedible found");
                                            foundInedibleItem = false;
                                            break;
                                        }
                                    }

                                    if (foundInedibleItem && foundEdibleItem && closestContainerDistance > distance)
                                    {
                                        closestContainer = container;
                                        closestContainerDistance = distance;
                                        closestContainerPosition = containerPosition;
                                    }
                                }
                            }
                        }
                    }
                }

                if (closestContainer != null)
                {
                    Dbgl($"{__instance.gameObject.name} found container: {closestContainerPosition}");
                    if (requireMove.Value && name != "Deer")
                    {
                        Dbgl($"{__instance.gameObject.name} {instancePosition} trying to move to {closestContainerPosition} {Utils.DistanceXZ(instancePosition, closestContainerPosition)}");

                        traverseAI.Field("m_lastFindPathTime").SetValue(0);
                        if (!traverseAI.Method("MoveTo", new object[] { 0.05f, closestContainerPosition, moveProximity.Value, false }).GetValue<bool>())
                            return;

                        traverseAI.Method("LookAt", new object[] { closestContainerPosition }).GetValue();
                        if (!traverseAI.Method("IsLookingAt", new object[] { closestContainerPosition, 20f, false }).GetValue<bool>())
                            return;
                    }

                    using (List<ItemDrop>.Enumerator enumerator = __instance.m_consumeItems.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            ItemDrop.ItemData item = closestContainer.GetInventory().GetItem(enumerator.Current.m_itemData.m_shared.m_name);
                            if (item != null && !feedDisallowTypes.Value.Split(',').Contains(item.m_dropPrefab.name))
                            {
                                Dbgl($"{__instance.gameObject.name} {instancePosition} consuming {item.m_dropPrefab.name} at {closestContainerPosition}, distance {Utils.DistanceXZ(instancePosition, closestContainerPosition)}");
                                ConsumeItem(item, __instance, ___m_character);

                                closestContainer.GetInventory().RemoveItem(item.m_shared.m_name, 1);
                                typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(closestContainer.GetInventory(), new object[] { });
                                typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(closestContainer, new object[] { });
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void ConsumeItem(ItemDrop.ItemData item, MonsterAI monsterAI, Character character)
        {
            monsterAI.m_onConsumedItem?.Invoke(null);

            (character as Humanoid).m_consumeItemEffects.Create(character.transform.position, Quaternion.identity, null, 1f, -1);
            Traverse.Create(monsterAI).Field("m_animator").GetValue<ZSyncAnimation>().SetTrigger("consume");
        }
    }
}
