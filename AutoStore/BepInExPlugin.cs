using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AutoStore
{
    [BepInPlugin("aedenthorn.AutoStore", "Auto Store", "0.7.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<float> dropRangeChests;
        public static ConfigEntry<float> dropRangePersonalChests;
        public static ConfigEntry<float> dropRangeReinforcedChests;
        public static ConfigEntry<float> dropRangeCarts;
        public static ConfigEntry<float> dropRangeShips;
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

        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<string> toggleString;

        public static ConfigEntry<bool> mustHaveItemToPull;
        public static ConfigEntry<bool> pullWhileBuilding;
        public static ConfigEntry<bool> isOn;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            dropRangeChests = Config.Bind<float>("General", "DropRangeChests", 5f, "The maximum range to pull dropped items");
            dropRangePersonalChests = Config.Bind<float>("General", "DropRangePersonalChests", 5f, "The maximum range to pull dropped items");
            dropRangeReinforcedChests = Config.Bind<float>("General", "DropRangeReinforcedChests", 5f, "The maximum range to pull dropped items");
            dropRangeCarts = Config.Bind<float>("General", "DropRangeCarts", 5f, "The maximum range to pull dropped items");
            dropRangeShips = Config.Bind<float>("General", "DropRangeShips", 5f, "The maximum range to pull dropped items");
            itemDisallowTypes = Config.Bind<string>("General", "ItemDisallowTypes", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypes = Config.Bind<string>("General", "ItemAllowTypes", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypes");
            itemDisallowTypesChests = Config.Bind<string>("General", "ItemDisallowTypesChests", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesChests = Config.Bind<string>("General", "ItemAllowTypesChests", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesChests");
            itemDisallowTypesPersonalChests = Config.Bind<string>("General", "ItemDisallowTypesPersonalChests", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesPersonalChests = Config.Bind<string>("General", "ItemAllowTypesPersonalChests", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesPersonalChests");
            itemDisallowTypesReinforcedChests = Config.Bind<string>("General", "ItemDisallowTypesReinforcedChests", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesReinforcedChests = Config.Bind<string>("General", "ItemAllowTypesReinforcedChests", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesReinforcedChests");
            itemDisallowTypesBlackMetalChests = Config.Bind<string>("General", "ItemDisallowTypesBlackMetalChests", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesBlackMetalChests = Config.Bind<string>("General", "ItemAllowTypesBlackMetalChests", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesBlackMetalChests");
            itemDisallowTypesCarts = Config.Bind<string>("General", "ItemDisallowTypesCarts", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesCarts = Config.Bind<string>("General", "ItemAllowTypesCarts", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesCarts");
            itemDisallowTypesShips = Config.Bind<string>("General", "ItemDisallowTypesShips", "", "Types of item to disallow pulling for, comma-separated.");
            itemAllowTypesShips = Config.Bind<string>("General", "ItemAllowTypesShips", "", "Types of item to only allow pulling for, comma-separated. Overrides ItemDisallowTypesShips");
            toggleString = Config.Bind<string>("General", "ToggleString", "Auto Pull: {0}", "Text to show on toggle. {0} is replaced with true/false");
            toggleKey = Config.Bind<string>("General", "ToggleKey", "", "Key to toggle behaviour. Leave blank to disable the toggle key. Use https://docs.unity3d.com/Manual/class-InputManager.html syntax.");
            mustHaveItemToPull = Config.Bind<bool>("General", "MustHaveItemToPull", false, "If true, a container must already have at least one of the item to pull.");
            pullWhileBuilding = Config.Bind<bool>("General", "PullWhileBuilding", true, "If false, containers won't pull while the player is holding the hammer.");
            isOn = Config.Bind<bool>("General", "IsOn", true, "Behaviour is currently on or not");
            
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 174, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses())
                return;
            if (AedenthornUtils.CheckKeyDown(toggleKey.Value))
            {
                isOn.Value = !isOn.Value;
                Config.Save();
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(toggleString.Value, isOn.Value), 0, null);
            }

        }
        public static bool DisallowItem(Container container, ItemDrop.ItemData item)
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

        public static float ContainerRange(Container container)
        {
            if (container.GetInventory() == null)
                return -1f;

            Ship ship = container.gameObject.transform.parent?.GetComponent<Ship>();
            if (ship != null)
            {

                return dropRangeShips.Value;
            }
            else if (container.m_wagon)
            {
                return dropRangeCarts.Value;
            }
            else if (container.name.StartsWith("piece_chest_wood"))
            {
                return dropRangeChests.Value;
            }
            else if (container.name.StartsWith("piece_chest_private"))
            {
                return dropRangePersonalChests.Value;
            }
            else if (container.name.StartsWith("piece_chest"))
            {
                return dropRangeReinforcedChests.Value;
            }
            return -1f;
        }

        [HarmonyPatch(typeof(Container), "CheckForChanges")]
        public static class Container_CheckForChanges_Patch
        {
            public static void Postfix(Container __instance, ZNetView ___m_nview)
            {
                if (!isOn.Value || ___m_nview == null || ___m_nview.GetZDO() == null || (!pullWhileBuilding.Value && (((ItemDrop.ItemData)AccessTools.Method(typeof(Player), "GetLeftItem").Invoke(Player.m_localPlayer, new object[0]))?.m_shared?.m_buildPieces.m_pieces.Count > 0 || ((ItemDrop.ItemData)AccessTools.Method(typeof(Player), "GetRightItem").Invoke(Player.m_localPlayer, new object[0]))?.m_shared?.m_buildPieces.m_pieces.Count > 0)))
                    return;

                Vector3 position = __instance.transform.position + Vector3.up;
                foreach (Collider collider in Physics.OverlapSphere(position, ContainerRange(__instance), LayerMask.GetMask(new string[] { "item" })))
                {
                    if (collider?.attachedRigidbody)
                    {
                        ItemDrop item = collider.attachedRigidbody.GetComponent<ItemDrop>();

                        if (item?.GetComponent<ZNetView>()?.IsValid() != true || !item.GetComponent<ZNetView>().IsOwner())
                            continue;
                        //Dbgl($"nearby item name: {item.m_itemData.m_dropPrefab.name}");
                        if (TryStoreItem(__instance, ref item.m_itemData))
                        {
                            AccessTools.Method(typeof(ItemDrop), "Save").Invoke(item, new object[] { });
                            if (item.GetComponent<ZNetView>() == null)
                                DestroyImmediate(item.gameObject);
                            else
                                ZNetScene.instance.Destroy(item.gameObject);
                        }
                    }
                }
            }
        }

        public static bool TryStoreItem(Container __instance, ref ItemDrop.ItemData item)
        {

            if (DisallowItem(__instance, item))
                return false;

            //Dbgl($"auto storing {item.m_dropPrefab.name} from ground");
            bool changed = false;
            while (item.m_stack > 1 && __instance.GetInventory().CanAddItem(item, 1))
            {
                changed = true;
                item.m_stack--;
                ItemDrop.ItemData newItem = item.Clone();
                newItem.m_stack = 1;
                __instance.GetInventory().AddItem(newItem);
            }

            if (item.m_stack == 1 && __instance.GetInventory().CanAddItem(item, 1))
            {
                ItemDrop.ItemData newItem = item.Clone();
                item.m_stack = 0;
                __instance.GetInventory().AddItem(newItem);
                changed = true;
            }
            if(changed)
                AccessTools.Method(typeof(Container), "Save").Invoke(__instance, new object[] { });

            return changed;
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
