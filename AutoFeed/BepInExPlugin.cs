using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace AutoFeed
{
    [BepInPlugin("aedenthorn.AutoFeed", "Auto Feed", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<float> containerRange;
        public static ConfigEntry<string> feedDisallowTypes;
        public static ConfigEntry<string> animalDisallowTypes;
        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<string> toggleString;
        public static ConfigEntry<bool> isOn;
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
            toggleKey = Config.Bind<string>("General", "ToggleKey", "", "Key to toggle behaviour. Leave blank to disable the toggle key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            toggleString = Config.Bind<string>("General", "ToggleString", "Auto Feed: {0}", "Text to show on toggle. {0} is replaced with true/false");
            isOn = Config.Bind<bool>("ZAuto", "IsOn", true, "Behaviour is currently on or not");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Show debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 985, "Nexus mod ID for updates");

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

        public static List<Container> GetNearbyContainers(Vector3 center, float range)
        {
            try { 
                List<Container> containers = new List<Container>();

                foreach (Collider collider in Physics.OverlapSphere(center, Mathf.Max(range, 0), LayerMask.GetMask(new string[] { "piece" })))
                {
                    Container container = collider.transform.parent?.parent?.gameObject?.GetComponent<Container>();
                    if (container?.GetComponent<ZNetView>()?.IsValid() != true)
                        continue;
                    if ((container.name.StartsWith("piece_chest") || container.name.StartsWith("Container")) && container.GetInventory() != null)
                    {
                        containers.Add(container);
                    }
                }
                return containers;
            }
            catch
            {
                return new List<Container>();
            }
        }

        [HarmonyPatch(typeof(Tameable), "TamingUpdate")]
        static class TamingUpdate_Patch
        {
            static void Postfix(Tameable __instance, ZNetView ___m_nview, MonsterAI ___m_monsterAI, Character ___m_character)
            {
                if (!modEnabled.Value || !isOn.Value || !___m_nview.IsOwner() || !__instance.IsHungry())
                    return;

                string name = GetPrefabName(__instance.gameObject.name);

                if (animalDisallowTypes.Value.Split(',').Contains(name))
                {
                    return;
                }

                if (Time.time - lastFeed < 0.1)
                {
                    feedCount++;
                    FeedAnimal(__instance, ___m_monsterAI, ___m_character, feedCount * 33);
                }
                else
                {
                    feedCount = 0;
                    lastFeed = Time.time;
                    FeedAnimal(__instance, ___m_monsterAI, ___m_character, 0);
                }
            }
        }
        public static async void FeedAnimal(Tameable tameable, MonsterAI monsterAI, Character character, int delay)
        {

            await Task.Delay(delay);

            var nearbyContainers = GetNearbyContainers(tameable.gameObject.transform.position, containerRange.Value);

            using (List<ItemDrop>.Enumerator enumerator = monsterAI.m_consumeItems.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    foreach (Container c in nearbyContainers)
                    {
                        ItemDrop.ItemData item = c.GetInventory().GetItem(enumerator.Current.m_itemData.m_shared.m_name);
                        if (item != null)
                        {
                            if (feedDisallowTypes.Value.Split(',').Contains(item.m_dropPrefab.name))
                            {
                                continue;
                            }
                            Dbgl($"{tameable.gameObject.name} consuming {item.m_dropPrefab.name}");

                            if (monsterAI.m_onConsumedItem != null)
                            {
                                Dbgl($"on consumed");
                                monsterAI.m_onConsumedItem(null);
                            }
                            (character as Humanoid).m_consumeItemEffects.Create(tameable.transform.position, Quaternion.identity, null, 1f);
                            Traverse.Create(monsterAI).Field("m_animator").GetValue<ZSyncAnimation>().SetTrigger("consume");
                            if (monsterAI.m_consumeHeal > 0f)
                            {
                                Dbgl($"healing");
                                character.Heal(monsterAI.m_consumeHeal, true);
                            }

                            c.GetInventory().RemoveItem(item.m_shared.m_name, 1);
                            typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
                            typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
                            return;
                        }
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
