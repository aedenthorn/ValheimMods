using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace CustomContainerSizes
{
    [BepInPlugin("aedenthorn.CustomContainerSizes", "Custom Container Sizes", "0.8.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> chestWidth;
        public static ConfigEntry<int> chestHeight;
        public static ConfigEntry<int> vikingShipChestWidth;
        public static ConfigEntry<int> vikingShipChestHeight;
        public static ConfigEntry<int> drakkarShipChestWidth;
        public static ConfigEntry<int> drakkarShipChestHeight;
        public static ConfigEntry<int> privateChestWidth;
        public static ConfigEntry<int> privateChestHeight;
        public static ConfigEntry<int> reinforcedChestWidth;
        public static ConfigEntry<int> reinforcedChestHeight;
        public static ConfigEntry<int> blackMetalChestWidth;
        public static ConfigEntry<int> blackMetalChestHeight;
        public static ConfigEntry<int> karveChestWidth;
        public static ConfigEntry<int> karveChestHeight;
        public static ConfigEntry<int> wagonWidth;
        public static ConfigEntry<int> wagonHeight;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            chestWidth = Config.Bind<int>("Sizes", "ChestWidth", 5, "Number of items wide for chest containers");
            chestHeight = Config.Bind<int>("Sizes", "ChestHeight", 2, "Number of items tall for chest containers");
            karveChestWidth = Config.Bind<int>("Sizes", "KarveChestWidth", 2, "Number of items wide for Karve chest containers (max. 8)");
            karveChestHeight = Config.Bind<int>("Sizes", "KarveChestHeight", 2, "Number of items tall for karve chest containers");
            vikingShipChestWidth = Config.Bind<int>("Sizes", "VikingShipChestWidth", 6, "Number of items wide for longship chest containers (max. 8)");
            vikingShipChestHeight = Config.Bind<int>("Sizes", "VikingShipChestHeight", 3, "Number of items tall for longship chest containers");
            drakkarShipChestWidth = Config.Bind<int>("Sizes", "DrakkarShipChestWidth", 8, "Number of items wide for Drakkar chest containers (max. 8)");
            drakkarShipChestHeight = Config.Bind<int>("Sizes", "DrakkarShipChestHeight", 4, "Number of items tall for Drakkar chest containers");
            privateChestWidth = Config.Bind<int>("Sizes", "PrivateChestWidth", 3, "Number of items wide for public chest containers (max. 8)");
            privateChestHeight = Config.Bind<int>("Sizes", "PrivateChestHeight", 2, "Number of items tall for public chest containers");
            reinforcedChestWidth = Config.Bind<int>("Sizes", "ReinforcedChestWidth", 6, "Number of items wide for reinforced chest containers (max. 8)");
            reinforcedChestHeight = Config.Bind<int>("Sizes", "ReinforcedChestHeight", 4, "Number of items tall for reinforced chest containers");
            blackMetalChestWidth = Config.Bind<int>("Sizes", "BlackMetalChestWidth", 8, "Number of items wide for black metal chest containers (max. 8)");
            blackMetalChestHeight = Config.Bind<int>("Sizes", "BlackMetalChestHeight", 4, "Number of items tall for black metal chest containers");
            wagonWidth = Config.Bind<int>("Sizes", "WagonWidth", 6, "Number of items wide for chest containers (max. 8)");
            wagonHeight = Config.Bind<int>("Sizes", "WagonHeight", 3, "Number of items tall for chest containers");
            nexusID = Config.Bind<int>("General", "NexusID", 111, "Mod ID on the Nexus for update checks");
            
            nexusID.Value = 111;
            chestWidth.Value = Math.Min(chestWidth.Value, 8);
            karveChestWidth.Value = Math.Min(karveChestWidth.Value, 8);
            vikingShipChestWidth.Value = Math.Min(vikingShipChestWidth.Value, 8);
            drakkarShipChestWidth.Value = Math.Min(drakkarShipChestWidth.Value, 8);
            privateChestWidth.Value = Math.Min(privateChestWidth.Value, 8);
            reinforcedChestWidth.Value = Math.Min(reinforcedChestWidth.Value, 8);
            wagonWidth.Value = Math.Min(wagonWidth.Value, 8);
            Config.Save();

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        public static class Container_Awake_Patch
        {
            public static void Postfix(Container __instance, Inventory ___m_inventory)
            {
                if (___m_inventory == null)
                    return;

                //Dbgl($"spawning container {__instance.name}");
                Ship ship = __instance.gameObject.transform.parent?.GetComponent<Ship>();
                if (ship != null)
                {
                    Dbgl($"container is on a ship: {ship.name}");
                    if (ship.name.ToLower().Contains("karve"))
                    {
                        Dbgl($"setting Karve chest size to {karveChestWidth.Value},{karveChestHeight.Value}");

                        typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, karveChestWidth.Value);
                        typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, karveChestHeight.Value);
                    }
                    else if (ship.name.ToLower().Contains("ashland"))
                    {
                        Dbgl($"setting Drakkar chest size to {drakkarShipChestWidth.Value},{drakkarShipChestHeight.Value}");

                        typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, drakkarShipChestWidth.Value);
                        typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, drakkarShipChestHeight.Value);
                    }
                    else if (ship.name.ToLower().Contains("vikingship"))
                    {
                        Dbgl($"setting VikingShip chest size to {vikingShipChestWidth.Value},{vikingShipChestHeight.Value}");

                        typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, vikingShipChestWidth.Value);
                        typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, vikingShipChestHeight.Value);
                    }
                    
                }
                else if (__instance.m_wagon)
                {
                    Dbgl($"setting wagon size to {wagonWidth.Value},{wagonHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, wagonWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, wagonHeight.Value);
                }
                else if (__instance.name.StartsWith("piece_chest_wood("))
                {
                    Dbgl($"setting chest size to {chestWidth.Value},{chestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, chestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, chestHeight.Value);
                }
                else if (__instance.name.StartsWith("piece_chest_private("))
                {
                    Dbgl($"setting public chest size to {privateChestWidth.Value},{privateChestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, privateChestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, privateChestHeight.Value);
                }
                else if (__instance.name.StartsWith("piece_chest_blackmetal("))
                {
                    Dbgl($"setting black metal chest size to {blackMetalChestWidth.Value},{blackMetalChestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, blackMetalChestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, blackMetalChestHeight.Value);
                }
                else if (__instance.name.StartsWith("piece_chest("))
                {
                    Dbgl($"setting reinforced chest size to {reinforcedChestWidth.Value},{reinforcedChestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, reinforcedChestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, reinforcedChestHeight.Value);
                }
            }
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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
