using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace CustomContainerSizes
{
    [BepInPlugin("aedenthorn.CustomContainerSizes", "Custom Container Sizes", "0.6.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<int> chestWidth;
        private static ConfigEntry<int> chestHeight;
        private static ConfigEntry<int> vikingShipChestWidth;
        private static ConfigEntry<int> vikingShipChestHeight;
        private static ConfigEntry<int> privateChestWidth;
        private static ConfigEntry<int> privateChestHeight;
        private static ConfigEntry<int> reinforcedChestWidth;
        private static ConfigEntry<int> reinforcedChestHeight;
        private static ConfigEntry<int> blackMetalChestWidth;
        private static ConfigEntry<int> blackMetalChestHeight;
        private static ConfigEntry<int> karveChestWidth;
        private static ConfigEntry<int> karveChestHeight;
        private static ConfigEntry<int> wagonWidth;
        private static ConfigEntry<int> wagonHeight;
        private static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            chestWidth = Config.Bind<int>("Sizes", "ChestWidth", 5, "Number of items wide for chest containers");
            chestHeight = Config.Bind<int>("Sizes", "ChestHeight", 2, "Number of items tall for chest containers");
            karveChestWidth = Config.Bind<int>("Sizes", "KarveChestWidth", 2, "Number of items wide for Karve chest containers (max. 8)");
            karveChestHeight = Config.Bind<int>("Sizes", "KarveChestHeight", 2, "Number of items tall for karve chest containers");
            vikingShipChestWidth = Config.Bind<int>("Sizes", "VikingShipChestWidth", 6, "Number of items wide for longship chest containers (max. 8)");
            vikingShipChestHeight = Config.Bind<int>("Sizes", "VikingShipChestHeight", 3, "Number of items tall for longship chest containers");
            privateChestWidth = Config.Bind<int>("Sizes", "PrivateChestWidth", 3, "Number of items wide for private chest containers (max. 8)");
            privateChestHeight = Config.Bind<int>("Sizes", "PrivateChestHeight", 2, "Number of items tall for private chest containers");
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
            privateChestWidth.Value = Math.Min(privateChestWidth.Value, 8);
            reinforcedChestWidth.Value = Math.Min(reinforcedChestWidth.Value, 8);
            wagonWidth.Value = Math.Min(wagonWidth.Value, 8);
            Config.Save();

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        static class Container_Awake_Patch
        {
            static void Postfix(Container __instance, Inventory ___m_inventory)
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
                    Dbgl($"setting private chest size to {privateChestWidth.Value},{privateChestHeight.Value}");

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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
