using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CraftFromContainers
{
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.11.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        private static BepInExPlugin modInstance = null;
        private static List<ConnectionParams> containerConnections = new List<ConnectionParams>();
        private static GameObject connectionVfxPrefab = null;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<bool> showGhostConnections;
        public static ConfigEntry<float> ghostConnectionStartOffset;
        public static ConfigEntry<float> ghostConnectionRemovalDelay;
        public static ConfigEntry<Color> flashColor;
        public static ConfigEntry<Color> unFlashColor;
        public static ConfigEntry<string> resourceString;
        public static ConfigEntry<string> pullItemsKey;
        public static ConfigEntry<string> pulledMessage;
        public static ConfigEntry<string> preventModKey;
        public static ConfigEntry<bool> switchAddAll;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static List<Container> containerList = new List<Container>();
        private static BepInExPlugin context;

        public class ConnectionParams
        {
            public GameObject connection = null;
            public Vector3 stationPos;
        }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modInstance = this;

            m_range = Config.Bind<float>("General", "ContainerRange", 10f, "The maximum range from which to pull items from. Set to -1 to allow pulling from all active containers in the world");
            resourceString = Config.Bind<string>("General", "ResourceCostString", "{0}/{1}", "String used to show required and available resources. {0} is replaced by how much is available, and {1} is replaced by how much is required");
            flashColor = Config.Bind<Color>("General", "FlashColor", Color.yellow, "Resource amounts will flash to this colour when coming from containers");
            unFlashColor = Config.Bind<Color>("General", "UnFlashColor", Color.white, "Resource amounts will flash from this colour when coming from containers (set both colors to the same color for no flashing)");
            pullItemsKey = Config.Bind<string>("General", "PullItemsKey", "left ctrl", "Holding down this key while crafting or building will pull resources into your inventory instead of building");
            pulledMessage = Config.Bind<string>("General", "PulledMessage", "Pulled items to inventory", "Message to show after pulling items to player inventory");
            preventModKey = Config.Bind<string>("General", "PreventModKey", "left shift", "Modifier key to toggle fuel and ore filling behaviour when down");
            switchAddAll = Config.Bind<bool>("General", "SwitchAddAll", true, "if true, holding down the modifier key will prevent this mod's behaviour; if false, holding down the key will allow it");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 40, "Nexus mod ID for updates");
            showGhostConnections = Config.Bind<bool>("Station Connections", "ShowConnections", false, "If true, will display connections to nearby workstations within range when building containers");
            ghostConnectionStartOffset = Config.Bind<float>("Station Connections", "ConnectionStartOffset", 1.25f, "Height offset for the connection VFX start position");
            ghostConnectionRemovalDelay = Config.Bind<float>("Station Connections", "ConnectionRemoveDelay", 0.05f, "");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private static bool AllowByKey()
        {
            if (CheckKeyHeld(preventModKey.Value))
                return !switchAddAll.Value;
            return switchAddAll.Value;
        }

        private void OnDestroy()
        {
            StopConnectionEffects();
        }

        private static bool CheckKeyHeld(string value)
        {
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return false;
            }
        }
        public static List<Container> GetNearbyContainers(Vector3 center)
        {
            List<Container> containers = new List<Container>();
            foreach (Container container in containerList)
            {
                if (container != null && container.transform != null && container.GetInventory() != null && (m_range.Value <= 0 ||  Vector3.Distance(center, container.transform.position) < m_range.Value))
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
                if (__instance.name.StartsWith("piece_chest") && __instance.GetInventory() != null)
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
        
        [HarmonyPatch(typeof(Fireplace), "Interact")]
        static class Fireplace_Interact_Patch
        {
            static bool Prefix(Fireplace __instance, Humanoid user, bool hold, ref bool __result, ZNetView ___m_nview)
            {
                __result = false;
                if (!AllowByKey())
                    return true;
                if (hold)
                {
                    return false;
                }
                if (!___m_nview.HasOwner())
                {
                    ___m_nview.ClaimOwnership();
                }
                Inventory inventory = user.GetInventory();
                if (inventory == null)
                {
                    __result = true;
                    return false;
                }
                if (!inventory.HaveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name) && (float)Mathf.CeilToInt(___m_nview.GetZDO().GetFloat("fuel", 0f)) < __instance.m_maxFuel)
                {
                    List<Container> nearbyContainers = GetNearbyContainers(__instance.transform.position);

                    foreach (Container c in nearbyContainers)
                    {
                        ItemDrop.ItemData item = c.GetInventory().GetItem(__instance.m_fuelItem.m_itemData.m_shared.m_name);
                        if (item != null)
                        {
                            Dbgl($"container at {c.transform.position} has {item.m_stack} {__instance.m_fuelItem.m_itemData.m_shared.m_name}, taking one");
                            c.GetInventory().RemoveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name, 1);
                            typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
                            typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
                            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_fireadding", new string[]
                            {
                                __instance.m_fuelItem.m_itemData.m_shared.m_name
                            }), 0, null);
                            inventory.RemoveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name, 1);
                            ___m_nview.InvokeRPC("AddFuel", new object[] { });
                            __result = true;
                            return false;
                        }
                    }
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(CookingStation), "FindCookableItem")]
        static class CookingStation_FindCookableItem_Patch
        {
            static void Postfix(CookingStation __instance, ref ItemDrop.ItemData __result)
            {
                Dbgl($"looking for cookable");

                if (!AllowByKey() || __result != null || !((bool)typeof(CookingStation).GetMethod("IsFireLit", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { })) || ((int)typeof(CookingStation).GetMethod("GetFreeSlot", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { })) == -1)
                    return;

                Dbgl($"missing cookable in player inventory");


                List<Container> nearbyContainers = GetNearbyContainers(__instance.transform.position);

                foreach (CookingStation.ItemConversion itemConversion in __instance.m_conversion)
                {
                    foreach (Container c in nearbyContainers)
                    {
                        ItemDrop.ItemData item = c.GetInventory().GetItem(itemConversion.m_from.m_itemData.m_shared.m_name);
                        if (item != null)
                        {
                            Dbgl($"container at {c.transform.position} has {item.m_stack} {itemConversion.m_from.m_itemData.m_shared.m_name}, taking one");
                            __result = item;
                            c.GetInventory().RemoveItem(itemConversion.m_from.m_itemData.m_shared.m_name, 1);
                            typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
                            typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
                            return;
                        }
                    }
                }
            }

        }
        

        [HarmonyPatch(typeof(Smelter), "FindCookableItem")]
        static class Smelter_FindCookableItem_Patch
        {
            static void Postfix(Smelter __instance, ref ItemDrop.ItemData __result)
            {
                if (!AllowByKey() || __result != null || ((int)typeof(Smelter).GetMethod("GetQueueSize", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { })) >= __instance.m_maxOre)
                    return;

                Dbgl($"missing cookable in player inventory");


                List<Container> nearbyContainers = GetNearbyContainers(__instance.transform.position);

                foreach (Smelter.ItemConversion itemConversion in __instance.m_conversion)
                {
                    foreach (Container c in nearbyContainers)
                    {
                        ItemDrop.ItemData item = c.GetInventory().GetItem(itemConversion.m_from.m_itemData.m_shared.m_name);
                        if (item != null)
                        {
                            Dbgl($"container at {c.transform.position} has {item.m_stack} {itemConversion.m_from.m_itemData.m_shared.m_name}, taking one");
                            __result = item;
                            c.GetInventory().RemoveItem(itemConversion.m_from.m_itemData.m_shared.m_name, 1);
                            typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
                            typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
                            return;
                        }
                    }
                }
            }

        }
        

        [HarmonyPatch(typeof(Smelter), "OnAddFuel")]
        static class Smelter_OnAddFuel_Patch
        {
            static bool Prefix(Smelter __instance, ref bool __result, ZNetView ___m_nview, Humanoid user, ItemDrop.ItemData item)
            {
                if (!AllowByKey() || user.GetInventory().HaveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name) || item != null)
                    return true;

                if(((float)typeof(Smelter).GetMethod("GetFuel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { })) > __instance.m_maxFuel - 1)
                {
                    user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
                    return false;
                }

                Dbgl($"missing fuel in player inventory");

                List<Container> nearbyContainers = GetNearbyContainers(__instance.transform.position);

                foreach (Container c in nearbyContainers)
                {
                    ItemDrop.ItemData newItem = c.GetInventory().GetItem(__instance.m_fuelItem.m_itemData.m_shared.m_name);
                    if (newItem != null)
                    {
                        Dbgl($"container at {c.transform.position} has {newItem.m_stack} {__instance.m_fuelItem.m_itemData.m_shared.m_name}, taking one");

                        user.Message(MessageHud.MessageType.Center, "$msg_added " + __instance.m_fuelItem.m_itemData.m_shared.m_name, 0, null);
                        ___m_nview.InvokeRPC("AddFuel", new object[] { });
                        c.GetInventory().RemoveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name, 1);
                        typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
                        typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });

                        __result = true;
                        return false;
                    }
                }
                return true;
            }
        }
        
        // fix flashing red text, add amounts

        [HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
        static class InventoryGui_SetupRequirement_Patch
        {
            static void Postfix(InventoryGui __instance, Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality)
            {
                if (!AllowByKey())
                    return;
                Text component3 = elementRoot.transform.Find("res_amount").GetComponent<Text>();
                if (req.m_resItem != null)
                {
                    int invAmount = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
                    int amount = req.GetAmount(quality);
                    if (amount <= 0)
                    {
                        return;
                    }
                    component3.text = amount.ToString();
                    if (invAmount < amount)
                    {
                        List<Container> nearbyContainers = GetNearbyContainers(Player.m_localPlayer.transform.position);
                        foreach (Container c in nearbyContainers)
                            invAmount += c.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);

                        if (invAmount >= amount)
                            component3.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? flashColor.Value : unFlashColor.Value);
                    }
                    component3.text = string.Format(resourceString.Value, invAmount, component3.text);
                }
            }
        }



        [HarmonyPatch(typeof(Player), "HaveRequirements", new Type[] { typeof(Piece.Requirement[]), typeof(bool), typeof(int) })]
        static class HaveRequirements_Patch
        {
            static void Postfix(Player __instance, ref bool __result, Piece.Requirement[] resources, bool discover, int qualityLevel, HashSet<string> ___m_knownMaterial)
            {
                if (__result || discover || !AllowByKey())
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
                if (__result || __instance?.transform?.position == null || !AllowByKey())
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
                if (!AllowByKey())
                    return true;

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

                                        totalAmount += stackAmount;
                                        Dbgl($"total amount is now {totalAmount}/{totalRequirement} {reqName}");

                                        if (totalAmount >= totalRequirement)
                                            break;
                                    }
                                }
                                c.GetType().GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
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

        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        static class UpdatePlacementGhost_Patch
        {
            static void Postfix(Player __instance, bool flashGuardStone)
            {
                if (!showGhostConnections.Value)
                {
                    return;
                }

                FieldInfo placementGhostField = typeof(Player).GetField("m_placementGhost", BindingFlags.Instance | BindingFlags.NonPublic);
                GameObject placementGhost = placementGhostField != null ? (GameObject)placementGhostField.GetValue(__instance) : null;
                if (placementGhost == null)
                {
                    return;
                }

                Container ghostContainer = placementGhost.GetComponent<Container>();
                if (ghostContainer == null)
                {
                    return;
                }

                FieldInfo allStationsField = typeof(CraftingStation).GetField("m_allStations", BindingFlags.Static | BindingFlags.NonPublic);
                List<CraftingStation> allStations = allStationsField != null ? (List<CraftingStation>)allStationsField.GetValue(null) : null;

                if (connectionVfxPrefab == null)
                {
                    foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
                    {
                        if (go.name == "vfx_ExtensionConnection")
                        {
                            connectionVfxPrefab = go;
                            break;
                        }
                    }
                }

                if (connectionVfxPrefab == null)
                {
                    return;
                }

                if (allStations != null)
                {
                    bool bAddedConnections = false;
                    foreach (CraftingStation station in allStations)
                    {
                        if (Vector3.Distance(station.transform.position, placementGhost.transform.position) < m_range.Value)
                        {
                            bAddedConnections = true;

                            Vector3 connectionStartPos = station.GetConnectionEffectPoint();
                            Vector3 connectionEndPos = placementGhost.transform.position + Vector3.up * ghostConnectionStartOffset.Value;

                            ConnectionParams tempConnection = null;
                            int connectionIndex = ConnectionExists(station);
                            bool connectionAlreadyExists = connectionIndex == -1;
                            if (connectionAlreadyExists)
                            {
                                tempConnection = new ConnectionParams();
                                tempConnection.stationPos = station.GetConnectionEffectPoint();
                                tempConnection.connection = UnityEngine.Object.Instantiate<GameObject>(connectionVfxPrefab, connectionStartPos, Quaternion.identity);
                            }
                            else
                            {
                                tempConnection = containerConnections[connectionIndex];
                            }

                            if (tempConnection.connection != null)
                            {
                                Vector3 vector3 = connectionEndPos - connectionStartPos;
                                Quaternion quaternion = Quaternion.LookRotation(vector3.normalized);
                                tempConnection.connection.transform.position = connectionStartPos;
                                tempConnection.connection.transform.rotation = quaternion;
                                tempConnection.connection.transform.localScale = new Vector3(1f, 1f, vector3.magnitude);
                            }

                            if (connectionAlreadyExists)
                            {
                                containerConnections.Add(tempConnection);
                            }
                        }
                    }

                    if (bAddedConnections && modInstance != null)
                    {
                        modInstance.CancelInvoke("StopConnectionEffects");
                        modInstance.Invoke("StopConnectionEffects", ghostConnectionRemovalDelay.Value);
                    }
                }
            }
        }

        public static int ConnectionExists(CraftingStation station)
        {
            foreach (ConnectionParams c in containerConnections)
            {
                if (Vector3.Distance(c.stationPos, station.GetConnectionEffectPoint()) < 0.1f)
                {
                    return containerConnections.IndexOf(c);
                }
            }

            return -1;
        }

        public void StopConnectionEffects()
        {
            if (containerConnections.Count > 0)
            {
                foreach (ConnectionParams c in containerConnections)
                {
                    UnityEngine.Object.Destroy((UnityEngine.Object)c.connection);
                }
            }

            containerConnections.Clear();
        }

        [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
        static class DoCrafting_Patch
        {
            static bool Prefix(InventoryGui __instance, KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe, ItemDrop.ItemData ___m_craftUpgradeItem)
            {
                if ((!AllowByKey() && !CheckKeyHeld(pullItemsKey.Value)) || ___m_selectedRecipe.Key == null)
                    return true;

                int qualityLevel = (___m_craftUpgradeItem != null) ? (___m_craftUpgradeItem.m_quality + 1) : 1;
                if (qualityLevel > ___m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_maxQuality)
                {
                    return true;
                }
                Dbgl($"pulling resources to player inventory for crafting item {___m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_name}");

                PullResources(Player.m_localPlayer, ___m_selectedRecipe.Key.m_resources, qualityLevel);
                return false;
            }

        }

        [HarmonyPatch(typeof(Player), "UpdatePlacement")]
        static class UpdatePlacement_Patch
        {
            static bool Prefix(Player __instance, bool takeInput, float dt, PieceTable ___m_buildPieces, GameObject ___m_placementGhost)
            {

                if (!AllowByKey() || !CheckKeyHeld(pullItemsKey.Value) || !__instance.InPlaceMode() || !takeInput || Hud.IsPieceSelectionVisible())
                {
                    return true;
                }
                if (ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace"))
                {
                    Piece selectedPiece = ___m_buildPieces.GetSelectedPiece();
                    if (selectedPiece != null)
                    {
                        if (selectedPiece.m_repairPiece)
                            return true;
                        if (___m_placementGhost != null)
                        {
                            int placementStatus = (int)typeof(Player).GetField("m_placementStatus", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                            if (placementStatus == 0)
                            {
                                Dbgl($"pulling resources to player inventory for piece {selectedPiece.name}");
                                PullResources(__instance, selectedPiece.m_resources, 0);
                            }
                        }
                    }
                }
                return false;
            }
        }

        private static void PullResources(Player player, Piece.Requirement[] resources, int qualityLevel)
        {
            Inventory pInventory = Player.m_localPlayer.GetInventory();
            List<Container> nearbyContainers = GetNearbyContainers(Player.m_localPlayer.transform.position);
            foreach (Piece.Requirement requirement in resources)
            {
                if (requirement.m_resItem)
                {
                    int totalRequirement = requirement.GetAmount(qualityLevel);
                    if (totalRequirement <= 0)
                        continue;

                    string reqName = requirement.m_resItem.m_itemData.m_shared.m_name;
                    int totalAmount = pInventory.CountItems(reqName);
                    Dbgl($"have {totalAmount}/{totalRequirement} {reqName} in player inventory");

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
                                if (item.m_shared.m_name == reqName)
                                {
                                    Dbgl($"Got stack of {item.m_stack} {reqName}");
                                    int stackAmount = Mathf.Min(item.m_stack, totalRequirement - totalAmount);

                                    if (!pInventory.HaveEmptySlot())
                                        stackAmount = Math.Min(Traverse.Create(pInventory).Method("FindFreeStackSpace", new object[] { item.m_shared.m_name }).GetValue<int>(), stackAmount);

                                    Dbgl($"Sending {stackAmount} {reqName} to player");

                                    ItemDrop.ItemData sendItem = item.Clone();
                                    sendItem.m_stack = stackAmount;

                                    pInventory.AddItem(sendItem);

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
                            c.GetType().GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
                            cInventory.GetType().GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cInventory, new object[] { });

                            if (totalAmount >= totalRequirement)
                            {
                                Dbgl($"pulled enough {reqName}");
                                break;
                            }
                        }
                    }
                }
                if(pulledMessage.Value?.Length > 0)
                    player.Message(MessageHud.MessageType.Center, pulledMessage.Value, 0, null);
            }
        }
    }
}
