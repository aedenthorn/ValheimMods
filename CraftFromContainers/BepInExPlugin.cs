using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CraftFromContainers
{
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.3.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        private static BepInExPlugin instance = null;
        private static List<ConnectionParams> containerConnections = new List<ConnectionParams>();
        private static GameObject connectionVfxPrefab = null;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<bool> updateItemUi;
        public static ConfigEntry<bool> showGhostConnections;
        public static ConfigEntry<float> ghostConnectionStartOffset;
        public static ConfigEntry<float> ghostConnectionRemovalDelay;
        public static ConfigEntry<bool> modEnabled;
        public static List<Container> containerList = new List<Container>();

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
            instance = this;

            m_range = Config.Bind<float>("General", "ContainerRange", 10f, "The maximum range from which to pull items from");
            updateItemUi = Config.Bind<bool>("General", "UpdateUI", false, "If enabled, will stop the UI flashing red if there are enough items within nearby containers");
            showGhostConnections = Config.Bind<bool>("Station Connections", "ShowConnections", false, "If true, will display connections to nearby workstations within range when building containers");
            ghostConnectionStartOffset = Config.Bind<float>("Station Connections", "ConnectionStartOffset", 1.25f, "Height offset for the connection VFX start position");
            ghostConnectionRemovalDelay = Config.Bind<float>("Station Connections", "ConnectionRemoveDelay", 0.3f, "");
            modEnabled = Config.Bind<bool>("General", "enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void OnDestroy()
        {
            StopConnectionEffects();
        }

        public static List<Container> GetNearbyContainers(Vector3 center)
        {
            List<Container> containers = new List<Container>();
            foreach (Container container in containerList)
            {
                ZNetView znv = (ZNetView)typeof(Container).GetField("m_nview", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(container);
                if (znv == null)
                    continue;
                ZDO zdo = (ZDO)typeof(ZNetView).GetField("m_zdo", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(znv);
                if (zdo == null)
                    continue;
                if (container != null && container.transform != null && zdo.IsOwner() && Vector3.Distance(center, container.transform.position) < m_range.Value)
                    containers.Add(container);
            }
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

        static int GetNumItemsInInventoryAndNearbyContainers(Player player, Piece.Requirement requirement)
        {
            int totalAmount = 0;

            List<Container> nearbyContainers = GetNearbyContainers(player.transform.position);

            if (requirement.m_resItem)
            {
                totalAmount = player.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
             
                if (updateItemUi.Value)
                {
                    foreach (Container c in nearbyContainers)
                    {
                        totalAmount += c.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                    }
                }
            }

            return totalAmount;
        }

        [HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
        static class SetupRequirement_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                //Dbgl($"######## SetupRequirement_Patch START ########");
                int getInventoryInstrIndex = -1;
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    CodeInstruction instr = codes[i];

                    //Dbgl($"{i} {instr}");

                    if (instr.opcode == OpCodes.Callvirt)
                    {
                        String instrString = instr.ToString();
                        if (instrString.Contains("CountItems"))         // Looking for this line: int num = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
                        {
                            for (var j = i-1; j >= 0; j--)              // From there navigate back to the first instruction that we want to replace: callvirt Humanoid::GetInventory()
                            {
                               // Dbgl($"^{j} {codes[j].ToString()}");

                                if (codes[j].opcode == OpCodes.Callvirt)
                                {
                                    instrString = codes[j].ToString();
                                    if (instrString.Contains("GetInventory()"))
                                    {
                                        getInventoryInstrIndex = j;
                                        break;
                                    }
                                }
                            }

                            // Remove all instructions that are not loading the function arguments. We will reuse the same arguments, so we can keep those.
                            if (getInventoryInstrIndex > -1)
                            {
                                //Dbgl($"Removing instruction at {getInventoryInstrIndex}: {codes[getInventoryInstrIndex].ToString()}");
                                codes.RemoveAt(getInventoryInstrIndex);
                                i--;
                                for (var j = getInventoryInstrIndex; j <= codes.Count; j++)
                                {
                                    bool bLastInstruction = false;
                                    instrString = codes[j].ToString();
                                    if (instrString.Contains("CountItems"))
                                    {
                                        bLastInstruction = true;
                                    }
                                    
                                    //Dbgl($"v{j} {codes[j].ToString()}");

                                    if (codes[j].opcode != OpCodes.Ldarg &&
                                        codes[j].opcode != OpCodes.Ldarg_S &&
                                        codes[j].opcode != OpCodes.Ldarg_0 &&
                                        codes[j].opcode != OpCodes.Ldarg_1 &&
                                        codes[j].opcode != OpCodes.Ldarg_2 &&
                                        codes[j].opcode != OpCodes.Ldarg_3)
                                    {
                                       // Dbgl($"Removing instruction at {j}: {codes[j].ToString()}");

                                        codes.RemoveAt(j);
                                        i--;
                                        j--;
                                    }
                                    else
                                    {
                                        i++;
                                    }

                                    if (bLastInstruction)
                                        break;
                                }
                            }

                            // Insert a new instruction to call GetNumItemsInInventoryAndNearbyContainers(), which is going to be cached into same local variable as before.
                            //Dbgl($"Inserting instruction at {i}:");
                            //Dbgl($"Old: { codes[i].ToString()}");
                            codes.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), "GetNumItemsInInventoryAndNearbyContainers")));
                            //Dbgl($"New: { codes[i].ToString()}");
                        }
                    }
                }

                //Dbgl($"");
                //Dbgl($"#############################################################");
                //Dbgl($"######## MODIFIED INSTRUCTIONS - {codes.Count} ########");
                //Dbgl($"#############################################################");
                //Dbgl($"");
                //
                //for (var i = 0; i < codes.Count; i++)
                //{
                //    CodeInstruction instr = codes[i];
                //
                //    Dbgl($"{i} {instr}");
                //}
                //
                //Dbgl($"######## SetupRequirement_Patch END ########");

                return codes;
            }
        }

        [HarmonyPatch(typeof(Player), "HaveRequirements", new Type[] { typeof(Piece.Requirement[]), typeof(bool), typeof(int) })]
        static class HaveRequirements_Patch
        {
            static void Postfix(Player __instance, ref bool __result, Piece.Requirement[] resources, bool discover, int qualityLevel, HashSet<string> ___m_knownMaterial)
            {
                if (__result || discover)
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
                if (__result)
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
                                hasItems += c.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                                if (hasItems >= requirement.m_amount)
                                {
                                    break;
                                }
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

                    if (bAddedConnections && instance != null)
                    {
                        instance.CancelInvoke("StopConnectionEffects");
                        instance.Invoke("StopConnectionEffects", ghostConnectionRemovalDelay.Value);
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
    }
}
