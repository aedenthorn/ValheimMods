using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QuickStore
{
    [BepInPlugin("aedenthorn.QuickStore", "Quick Store", "0.5.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

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
        public static ConfigEntry<string> itemDisallowTypesGeneric;
        public static ConfigEntry<string> itemAllowTypesGeneric;

        public static ConfigEntry<string> itemDisallowCategories;
        public static ConfigEntry<string> itemAllowCategories;
        public static ConfigEntry<string> itemDisallowCategoriesChests;
        public static ConfigEntry<string> itemAllowCategoriesChests;
        public static ConfigEntry<string> itemDisallowCategoriesPersonalChests;
        public static ConfigEntry<string> itemAllowCategoriesPersonalChests;
        public static ConfigEntry<string> itemDisallowCategoriesReinforcedChests;
        public static ConfigEntry<string> itemAllowCategoriesReinforcedChests;
        public static ConfigEntry<string> itemDisallowCategoriesBlackMetalChests;
        public static ConfigEntry<string> itemAllowCategoriesBlackMetalChests;
        public static ConfigEntry<string> itemDisallowCategoriesCarts;
        public static ConfigEntry<string> itemAllowCategoriesCarts;
        public static ConfigEntry<string> itemDisallowCategoriesShips;
        public static ConfigEntry<string> itemAllowCategoriesShips;
        public static ConfigEntry<string> itemDisallowCategoriesGeneric;
        public static ConfigEntry<string> itemAllowCategoriesGeneric;
        public static ConfigEntry<string> storedString;
        public static ConfigEntry<string> genericContainerNamePrefixes;

        public static ConfigEntry<string> storeHotkey;
        public static ConfigEntry<bool> mustHaveItemToPull;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static int mask = LayerMask.GetMask(new string[] { "piece", "item", "piece_nonsolid", "vehicle" });

        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
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
            itemDisallowTypesGeneric = Config.Bind<string>("General", "ItemDisallowTypesGeneric", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesGeneric = Config.Bind<string>("General", "ItemAllowTypesGeneric", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesGeneric");
            
            itemDisallowCategories = Config.Bind<string>("General", "ItemDisallowCategories", "", "Categories of item to disallow pulling for, comma-separated.");
            itemAllowCategories = Config.Bind<string>("General", "ItemAllowCategories", "", "Categories of item to only allow pulling for, comma-separated. Overrides ItemDisallowCategories");

            itemDisallowCategoriesChests = Config.Bind<string>("General", "ItemDisallowCategoriesChests", "", "Categories of item to disallow pulling for, comma-separated.");
            itemAllowCategoriesChests = Config.Bind<string>("General", "ItemAllowCategoriesChests", "", "Categories of item to only allow pulling for, comma-separated. Overrides ItemDisallowCategoriesChests");

            itemDisallowCategoriesPersonalChests = Config.Bind<string>("General", "ItemDisallowCategoriesPersonalChests", "", "Categories of item to disallow pulling for, comma-separated.");
            itemAllowCategoriesPersonalChests = Config.Bind<string>("General", "ItemAllowCategoriesPersonalChests", "", "Categories of item to only allow pulling for, comma-separated. Overrides ItemDisallowCategoriesPersonalChests");

            itemDisallowCategoriesBlackMetalChests = Config.Bind<string>("General", "ItemDisallowCategoriesBlackMetalChests", "", "Categories of item to disallow pulling for, comma-separated.");
            itemAllowCategoriesBlackMetalChests = Config.Bind<string>("General", "ItemAllowCategoriesBlackMetalChests", "", "Categories of item to only allow pulling for, comma-separated. Overrides ItemDisallowCategoriesBlackMetalChests");

            itemDisallowCategoriesReinforcedChests = Config.Bind<string>("General", "ItemDisallowCategoriesReinforcedChests", "", "Categories of item to disallow pulling for, comma-separated.");
            itemAllowCategoriesReinforcedChests = Config.Bind<string>("General", "ItemAllowCategoriesReinforcedChests", "", "Categories of item to only allow pulling for, comma-separated. Overrides ItemDisallowCategoriesReinforcedChests");

            itemDisallowCategoriesCarts = Config.Bind<string>("General", "ItemDisallowCategoriesCarts", "", "Categories of item to disallow pulling for, comma-separated.");
            itemAllowCategoriesCarts = Config.Bind<string>("General", "ItemAllowCategoriesCarts", "", "Categories of item to only allow pulling for, comma-separated. Overrides ItemDisallowCategoriesCarts");

            itemDisallowCategoriesShips = Config.Bind<string>("General", "ItemDisallowCategoriesShips", "", "Categories of item to disallow pulling for, comma-separated.");
            itemAllowCategoriesShips = Config.Bind<string>("General", "ItemAllowCategoriesShips", "", "Categories of item to only allow pulling for, comma-separated. Overrides ItemDisallowCategoriesShips");
            
            itemDisallowCategoriesGeneric = Config.Bind<string>("General", "ItemDisallowCategoriesGeneric", "", "Categories of item to disallow pulling for, comma-separated.");
            itemAllowCategoriesGeneric = Config.Bind<string>("General", "ItemAllowCategoriesGeneric", "", "Categories of item to only allow pulling for, comma-separated. Overrides ItemDisallowCategoriesGeneric");
            genericContainerNamePrefixes = Config.Bind<string>("General", "GenericContainerNamePrefixes", "Container_", "Container name prefixes to use for pulling, comma-separated");
            
            mustHaveItemToPull = Config.Bind<bool>("General", "MustHaveItemToPull", true, "If true, a container must already have at least one of the item to pull.");
            storedString = Config.Bind<string>("General", "StoredString", "Stored {0} items in {1} containers", "Text to show after items are stored.");
            
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1595, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses())
                return;
            if (AedenthornUtils.CheckKeyDown(storeHotkey.Value))
            {                        
                Dbgl("Trying to store items from player inventory.");

                int total = 0;
                int containers = 0;

                Vector3 position = Player.m_localPlayer.transform.position + Vector3.up;
                foreach (Collider collider in Physics.OverlapSphere(position, storeRangePlayer.Value, mask))
                {
                    if (!collider)
                        continue;

                    Container c = collider.GetComponent<Container>();
                    if (!c && collider.transform.parent)
                        c = collider.transform.parent.GetComponent<Container>();
                    if(!c && collider.transform.parent?.parent)
                        c = collider.transform.parent.parent.GetComponent<Container>();
                    if(c && !c.IsInUse() && (bool)AccessTools.Method(typeof(Container), "CheckAccess").Invoke(c, new object[] { Player.m_localPlayer.GetPlayerID() } ))
                    {
                        Dbgl($"Storing in {c.name}.");
                        var items = Player.m_localPlayer.GetInventory().GetAllItems();
                        for (int i = items.Count - 1; i >= 0; i--)
                        {
                            var item = items[i];
                            if (item.m_equipped)
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
        public static bool DisallowItem(Container container, ItemDrop.ItemData item)
        {
            string name = item.m_dropPrefab.name;
            string cat = item.m_shared.m_itemType.ToString();
            if (itemAllowTypes.Value != null && itemAllowTypes.Value.Length > 0 && !itemAllowTypes.Value.Split(',').Contains(name))
                return true;
            if (itemDisallowTypes.Value.Split(',').Contains(name))
                return true;

            if (itemAllowCategories.Value != null && itemAllowCategories.Value.Length > 0 && !itemAllowCategories.Value.Split(',').Contains(cat))
                return true;
            if (itemDisallowCategories.Value.Split(',').Contains(cat))
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
                if (itemAllowCategoriesShips.Value != null && itemAllowCategoriesShips.Value.Length > 0 && !itemAllowCategoriesShips.Value.Split(',').Contains(cat))
                    return true;
                if (itemDisallowCategoriesShips.Value.Split(',').Contains(cat))
                    return true;
                return false;
            }
            else if (container.m_wagon)
            {
                if (itemAllowTypesCarts.Value != null && itemAllowTypesCarts.Value.Length > 0 && !itemAllowTypesCarts.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesCarts.Value.Split(',').Contains(name))
                    return true;
                if (itemAllowCategoriesCarts.Value != null && itemAllowCategoriesCarts.Value.Length > 0 && !itemAllowCategoriesCarts.Value.Split(',').Contains(cat))
                    return true;
                if (itemDisallowCategoriesCarts.Value.Split(',').Contains(cat))
                    return true;
                return false;
            }
            else if (container.name.StartsWith("piece_chest_wood"))
            {
                if (itemAllowTypesChests.Value != null && itemAllowTypesChests.Value.Length > 0 && !itemAllowTypesChests.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesChests.Value.Split(',').Contains(name))
                    return true;
                if (itemAllowCategoriesChests.Value != null && itemAllowCategoriesChests.Value.Length > 0 && !itemAllowCategoriesChests.Value.Split(',').Contains(cat))
                    return true;
                if (itemDisallowCategoriesChests.Value.Split(',').Contains(cat))
                    return true;

                return false;
            }
            else if (container.name.StartsWith("piece_chest_private"))
            {
                if (itemAllowTypesPersonalChests.Value != null && itemAllowTypesPersonalChests.Value.Length > 0 && !itemAllowTypesPersonalChests.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesPersonalChests.Value.Split(',').Contains(name))
                    return true;
                if (itemAllowCategoriesPersonalChests.Value != null && itemAllowCategoriesPersonalChests.Value.Length > 0 && !itemAllowCategoriesPersonalChests.Value.Split(',').Contains(cat))
                    return true;
                if (itemDisallowCategoriesPersonalChests.Value.Split(',').Contains(cat))
                    return true;

                return false;
            }
            else if (container.name.StartsWith("piece_chest_blackmetal"))
            {
                if (itemAllowTypesBlackMetalChests.Value != null && itemAllowTypesBlackMetalChests.Value.Length > 0 && !itemAllowTypesBlackMetalChests.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesBlackMetalChests.Value.Split(',').Contains(name))
                    return true;
                if (itemAllowCategoriesBlackMetalChests.Value != null && itemAllowCategoriesBlackMetalChests.Value.Length > 0 && !itemAllowCategoriesBlackMetalChests.Value.Split(',').Contains(cat))
                    return true;
                if (itemDisallowCategoriesBlackMetalChests.Value.Split(',').Contains(cat))
                    return true;

                return false;
            }
            else if (container.name.StartsWith("piece_chest"))
            {
                if (itemAllowTypesReinforcedChests.Value != null && itemAllowTypesReinforcedChests.Value.Length > 0 && !itemAllowTypesReinforcedChests.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesReinforcedChests.Value.Split(',').Contains(name))
                    return true;
                if (itemAllowCategoriesReinforcedChests.Value != null && itemAllowCategoriesReinforcedChests.Value.Length > 0 && !itemAllowCategoriesReinforcedChests.Value.Split(',').Contains(cat))
                    return true;
                if (itemDisallowCategoriesReinforcedChests.Value.Split(',').Contains(cat))
                    return true;

                return false;
            }
            else if (container.name.StartsWith("piece_chest"))
            {
                if (itemAllowTypesReinforcedChests.Value != null && itemAllowTypesReinforcedChests.Value.Length > 0 && !itemAllowTypesReinforcedChests.Value.Split(',').Contains(name))
                    return true;
                if (itemDisallowTypesReinforcedChests.Value.Split(',').Contains(name))
                    return true;
                if (itemAllowCategoriesReinforcedChests.Value != null && itemAllowCategoriesReinforcedChests.Value.Length > 0 && !itemAllowCategoriesReinforcedChests.Value.Split(',').Contains(cat))
                    return true;
                if (itemDisallowCategoriesReinforcedChests.Value.Split(',').Contains(cat))
                    return true;

                return false;
            }
            else if(genericContainerNamePrefixes.Value.Length > 0)
            {
                foreach(var str in genericContainerNamePrefixes.Value.Split(','))
                {
                    if (container.name.StartsWith(str))
                    {
                        if (itemAllowTypesGeneric.Value != null && itemAllowTypesGeneric.Value.Length > 0 && !itemAllowTypesGeneric.Value.Split(',').Contains(name))
                            return true;
                        if (itemDisallowTypesGeneric.Value.Split(',').Contains(name))
                            return true;
                        if (itemAllowCategoriesGeneric.Value != null && itemAllowCategoriesGeneric.Value.Length > 0 && !itemAllowCategoriesGeneric.Value.Split(',').Contains(cat))
                            return true;
                        if (itemDisallowCategoriesGeneric.Value.Split(',').Contains(cat))
                            return true;

                        return false;
                    }
                }
            }

            return true;
        }

        public static bool TryStoreItem(Container __instance, ref ItemDrop.ItemData item)
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
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
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
