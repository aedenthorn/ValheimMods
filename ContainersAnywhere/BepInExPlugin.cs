using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ContainersAnywhere
{
    [BepInPlugin("aedenthorn.ContainersAnywhere", "Containers Anywhere", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> previousKey;
        public static ConfigEntry<string> nextKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static List<Container> containerList = new List<Container>();
        private static BepInExPlugin context;
        private static bool openingContainers = false;
        private static int currentContainer = 0;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            m_range = Config.Bind<float>("General", "ContainerRange", -1f, "The maximum range to add containers to the list. Set to -1 to add all active containers in the world");
            hotKey = Config.Bind<string>("General", "HotKey", "i", "Key press to open the containers. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            previousKey = Config.Bind<string>("General", "PreviousKey", "left", "Key press to switch to the previous container. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            nextKey = Config.Bind<string>("General", "NextKey", "right", "Key press to switch to the next container. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 40, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
            if (CheckKeyDown(hotKey.Value))
                OpenContainers(0);
            else if (InventoryGui.instance?.IsContainerOpen() != true)
                openingContainers = false;
            else if(openingContainers && CheckKeyDown(previousKey.Value))
                OpenContainers(1);
            else if(openingContainers && CheckKeyDown(nextKey.Value))
                OpenContainers(-1);
        }

        private void OpenContainers(int which)
        {
            openingContainers = true;
            int container = currentContainer + which < 0 ? containerList.Count - 1 : (currentContainer + which) % containerList.Count;
            currentContainer = container;
            Dbgl($"Opening container {container+1}/{containerList.Count}");
            ((ZNetView)typeof(Container).GetField("m_nview", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(containerList[container])).InvokeRPC("RequestOpen", new object[]{ Player.m_localPlayer.GetPlayerID() });
            
        }

        private static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        static class Container_Awake_Patch
        {
            static void Postfix(Container __instance, ZNetView ___m_nview)
            {
                if(__instance.name.StartsWith("piece_chest") && __instance.GetInventory() != null)
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
        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        static class InventoryGui_Awake_Patch
        {
            static void Prefix(InventoryGui __instance)
            {
                __instance.m_autoCloseDistance = float.MaxValue;

            }
        }
        
    }
}
