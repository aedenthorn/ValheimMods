using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QuickStore
{
    [BepInPlugin("aedenthorn.QuickStore", "Quick Store", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> storeRangePlayer;
        public static ConfigEntry<string> itemDisallowTypes;
        public static ConfigEntry<string> itemAllowTypes;
        public static ConfigEntry<string> itemDisallowTypesChests;
        public static ConfigEntry<string> itemAllowTypesChests;
        public static ConfigEntry<string> itemDisallowTypesPersonalChests;
        public static ConfigEntry<string> itemAllowTypesPersonalChests;
        public static ConfigEntry<string> itemDisallowTypesReinforcedChests;
        public static ConfigEntry<string> itemAllowTypesReinforcedChests;
        public static ConfigEntry<string> itemDisallowTypesBlackMetalChests;
        public static ConfigEntry<string> itemAllowTypesBlackMetalChests;
        public static ConfigEntry<string> itemDisallowTypesCarts;
        public static ConfigEntry<string> itemAllowTypesCarts;
        public static ConfigEntry<string> itemDisallowTypesShips;
        public static ConfigEntry<string> itemAllowTypesShips;
        public static ConfigEntry<string> storedString;

        public static ConfigEntry<string> storeHotkey;
        public static ConfigEntry<bool> mustHaveItemToPull;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            storeHotkey = Config.Bind<string>("General", "StoreHotkey", ".", "Hotkey to store your inventory into nearby containers. Use https://docs.unity3d.com/Manual/class-InputManager.html syntax.");
            storeRangePlayer = Config.Bind<float>("General", "StoreRange", 5f, "The maximum distance from the player to store items");
            itemDisallowTypes = Config.Bind<string>("General", "ItemDisallowTypes", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypes = Config.Bind<string>("General", "ItemAllowTypes", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypes");
            itemDisallowTypesChests = Config.Bind<string>("General", "ItemDisallowTypesChests", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesChests = Config.Bind<string>("General", "ItemAllowTypesChests", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesChests");
            itemDisallowTypesPersonalChests = Config.Bind<string>("General", "ItemDisallowTypesPersonalChests", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesPersonalChests = Config.Bind<string>("General", "ItemAllowTypesPersonalChests", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesPersonalChests");
            itemDisallowTypesBlackMetalChests = Config.Bind<string>("General", "ItemDisallowTypesBlackMetalChests", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesBlackMetalChests = Config.Bind<string>("General", "ItemAllowTypesBlackMetalChests", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesBlackMetalChests");
            itemDisallowTypesReinforcedChests = Config.Bind<string>("General", "ItemDisallowTypesReinforcedChests", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesReinforcedChests = Config.Bind<string>("General", "ItemAllowTypesReinforcedChests", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesReinforcedChests");
            itemDisallowTypesCarts = Config.Bind<string>("General", "ItemDisallowTypesCarts", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesCarts = Config.Bind<string>("General", "ItemAllowTypesCarts", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesCarts");
            itemDisallowTypesShips = Config.Bind<string>("General", "ItemDisallowTypesShips", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesShips = Config.Bind<string>("General", "ItemAllowTypesShips", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesShips");
            mustHaveItemToPull = Config.Bind<bool>("General", "MustHaveItemToPull", true, "If true, a container must already have at least one of the item to pull.");
            storedString = Config.Bind<string>("General", "StoredString", "Stored {0} items in {1} containers", "Text to show after items are stored.");
            
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1595, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses())
                return;
            if (AedenthornUtils.CheckKeyDown(storeHotkey.Value))
            {                        
                Dbgl("Trying to store items from player inventory.");

                int total = 0;
                int containers = 0;

                Vector3 position = Player.m_localPlayer.transform.position + Vector3.up;
                foreach (Collider collider in Physics.OverlapSphere(position, storeRangePlayer.Value, LayerMask.GetMask(new string[] { "piece" })))
                {
                    if (!collider)
                        continue;
                    Container c = null;
                    if (collider.transform.parent)
                        c = collider.transform.parent.GetComponent<Container>();
                    if(!c && collider.transform.parent?.parent)
                        c = collider.transform.parent.parent.GetComponent<Container>();
                    if(c && !c.IsInUse() && (bool)AccessTools.Method(typeof(Container), "CheckAccess").Invoke(c, new object[] { Player.m_localPlayer.GetPlayerID() } ))
                    {
                        Dbgl($"In {c.name}.");
                        var items = Player.m_localPlayer.GetInventory().GetAllItems();
                        for (int i = items.Count - 1; i >= 0; i--)
                        {
                            var item = items[i];
                            if (item.m_equiped)
                                continue;
                            int originalAmount = item.m_stack;
                            TryStoreItem(c, ref item);
                            if (item.m_stack < originalAmount)
                            {
                                total += originalAmount - item.m_stack;
                                containers++;
                                Player.m_localPlayer.GetInventory().RemoveItem(item, originalAmount - item.m_stack);
                                Dbgl($"stored {originalAmount - item.m_stack} {item.m_shared.m_name} into {c.name}");
                            }
                        }
                        //Dbgl($"nearby item name: {item.m_itemData.m_dropPrefab.name}");
                    }
                }
                if(total > 0)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(storedString.Value, total, containers), 0, null);
            }

        }
        private static bool DisallowItem(Container container, ItemDrop.ItemData item)
        {
            string name = item.m_dropPrefab.name;
            if (itemAllowTypes.Value != null && itemAllowTypes.Value.Length > 0 && !itemAllowTypes.Value.Split(',').Contains(name))
                return true;
            if (itemDisallowTypes.Value.Split(',').Contains(name))
                return true;

            if (mustHaveItemToPull.Value && !container.GetInventory().HaveItem(item.m_shared.m_name))
                return true;

            Ship ship = container.gameObject.transform.parent?.GetComponent<Ship>();
            if (ship != null)
            {
                if (itemAllowTypesShips.Value != null && itemAllowTypesShips.Value.Length > 0 && !itemAllowTypesShips.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesShips.Value.Split(',').Contains(name))
                    return true;
                return false;
            }
            else if (container.m_wagon)
            {
                if (itemAllowTypesCarts.Value != null && itemAllowTypesCarts.Value.Length > 0 && !itemAllowTypesCarts.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesCarts.Value.Split(',').Contains(name))
                    return true;
                return false;
            }
            else if (container.name.StartsWith("piece_chest_wood"))
            {
                if (itemAllowTypesChests.Value != null && itemAllowTypesChests.Value.Length > 0 && !itemAllowTypesChests.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesChests.Value.Split(',').Contains(name))
                    return true;
                return false;
            }
            else if (container.name.StartsWith("piece_chest_private"))
            {
                if (itemAllowTypesPersonalChests.Value != null && itemAllowTypesPersonalChests.Value.Length > 0 && !itemAllowTypesPersonalChests.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesPersonalChests.Value.Split(',').Contains(name))
                    return true;
                return false;
            }
            else if (container.name.StartsWith("piece_chest_blackmetal"))
            {
                if (itemAllowTypesBlackMetalChests.Value != null && itemAllowTypesBlackMetalChests.Value.Length > 0 && !itemAllowTypesBlackMetalChests.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesBlackMetalChests.Value.Split(',').Contains(name))
                    return true;
                return false;
            }
            else if (container.name.StartsWith("piece_chest"))
            {
                if (itemAllowTypesReinforcedChests.Value != null && itemAllowTypesReinforcedChests.Value.Length > 0 && !itemAllowTypesReinforcedChests.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesReinforcedChests.Value.Split(',').Contains(name))
                    return true;
                return false;
            }
            return true;
        }

        private static bool TryStoreItem(Container __instance, ref ItemDrop.ItemData item)
        {

            if (DisallowItem(__instance, item))
                return false;

            while (item.m_stack > 1 && __instance.GetInventory().CanAddItem(item, 1))
            {
                item.m_stack--;
                ItemDrop.ItemData newItem = item.Clone();
                newItem.m_stack = 1;
                __instance.GetInventory().AddItem(newItem);
                typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { });
                typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance.GetInventory(), new object[] { });
            }

            if (item.m_stack == 1 && __instance.GetInventory().CanAddItem(item, 1))
            {
                ItemDrop.ItemData newItem = item.Clone();
                item.m_stack = 0;
                __instance.GetInventory().AddItem(newItem);
                typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { });
                typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance.GetInventory(), new object[] { });
                return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
