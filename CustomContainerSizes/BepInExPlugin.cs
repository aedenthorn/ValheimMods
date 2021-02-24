using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CustomContainerSizes
{
    [BepInPlugin("aedenthorn.CustomContainerSizes", "Custom Container Sizes", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<int> chestWidth;
        private static ConfigEntry<int> chestHeight;
        private static ConfigEntry<int> privateChestWidth;
        private static ConfigEntry<int> privateChestHeight;
        private static ConfigEntry<int> reinforcedChestWidth;
        private static ConfigEntry<int> reinforcedChestHeight;
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
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            chestWidth = Config.Bind<int>("Sizes", "ChestWidth", 8, "Number of items wide for chest containers");
            chestHeight = Config.Bind<int>("Sizes", "ChestHeight", 4, "Number of items wide for chest containers");
            privateChestWidth = Config.Bind<int>("Sizes", "PrivateChestWidth", 6, "Number of items wide for private chest containers");
            privateChestHeight = Config.Bind<int>("Sizes", "PrivateChestHeight", 3, "Number of items wide for private chest containers");
            reinforcedChestWidth = Config.Bind<int>("Sizes", "ReinforcedChestWidth", 8, "Number of items wide for reinforced chest containers");
            reinforcedChestHeight = Config.Bind<int>("Sizes", "ReinforcedChestHeight", 8, "Number of items wide for reinforced chest containers");
            wagonWidth = Config.Bind<int>("Sizes", "WagonWidth", 8, "Number of items wide for chest containers");
            wagonHeight = Config.Bind<int>("Sizes", "WagonHeight", 4, "Number of items wide for chest containers");
            nexusID = Config.Bind<int>("General", "NexusID", 111, "Mod ID on the Nexus for update checks");
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

                Dbgl($"container {__instance.name}");
                if (__instance.name.StartsWith("piece_chest_wood"))
                {
                    Dbgl($"setting chest size to {chestWidth.Value},{chestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, chestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, chestHeight.Value);
                }
                else if (__instance.name.StartsWith("piece_chest_private"))
                {
                    Dbgl($"setting private chest size to {chestWidth.Value},{chestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, privateChestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, privateChestHeight.Value);
                }
                else if (__instance.name.StartsWith("piece_chest"))
                {
                    Dbgl($"setting reinforced chest size to {chestWidth.Value},{chestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, reinforcedChestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, reinforcedChestHeight.Value);
                }
                else if (__instance.m_wagon)
                {
                    Dbgl($"setting wagon size to {wagonWidth.Value},{wagonHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, chestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, chestHeight.Value);
                }
            }
        }
    }
}
