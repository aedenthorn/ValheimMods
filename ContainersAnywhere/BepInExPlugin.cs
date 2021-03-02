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
    [BepInPlugin("aedenthorn.ContainersAnywhere", "Containers Anywhere", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> previousKey;
        public static ConfigEntry<string> nextKey;
        public static ConfigEntry<string> previousTypeKey;
        public static ConfigEntry<string> nextTypeKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static List<Container> containerList = new List<Container>();
        private static BepInExPlugin context;
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
            previousTypeKey = Config.Bind<string>("General", "PreviousTypeKey", "up", "Key press to switch to the last container of a different type. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            nextTypeKey = Config.Bind<string>("General", "NextTypeKey", "down", "Key press to switch to the next container of a different type. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 146, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
            if (Console.IsVisible())
                return;
            if (CheckKeyDown(hotKey.Value))
                OpenContainers(0);
            else if (InventoryGui.instance?.IsContainerOpen() == true)
            {
                if (CheckKeyDown(previousKey.Value))
                {
                    ((Container)typeof(InventoryGui).GetField("m_currentContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(InventoryGui.instance)).SetInUse(false);
                    OpenContainers(1);

                }
                else if (CheckKeyDown(nextKey.Value))
                {
                    ((Container)typeof(InventoryGui).GetField("m_currentContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(InventoryGui.instance)).SetInUse(false);
                    OpenContainers(-1);
                }
                if (CheckKeyDown(previousTypeKey.Value))
                {
                    ((Container)typeof(InventoryGui).GetField("m_currentContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(InventoryGui.instance)).SetInUse(false);
                    OpenContainerType(-1);

                }
                else if (CheckKeyDown(nextTypeKey.Value))
                {
                    ((Container)typeof(InventoryGui).GetField("m_currentContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(InventoryGui.instance)).SetInUse(false);
                    OpenContainerType(1);
                }
            }
        }

        private void OpenContainerType(int which)
        {
            List<Container> containers = GetContainers();

            currentContainer = currentContainer < 0 ? containers.Count - 1 : currentContainer % containers.Count;
            string currentType = containers[currentContainer].name;
            for(int i = 1; i < containers.Count; i++)
            {
                int idx = currentContainer + i * which;
                if (idx < 0)
                    idx = containers.Count - 1;
                idx %= containers.Count;

                if(containers[idx].name != currentType)
                {
                    Dbgl($"Opening container {idx}/{containers.Count}");
                    ((ZNetView)typeof(Container).GetField("m_nview", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(containers[idx])).InvokeRPC("RequestOpen", new object[] { Player.m_localPlayer.GetPlayerID() });
                }
            }
        }

        private void OpenContainers(int which)
        {
            List<Container> containers = GetContainers();


            int container = currentContainer + which < 0 ? containers.Count - 1 : (currentContainer + which) % containers.Count;
            currentContainer = container;
            Dbgl($"Opening container {container+1}/{containers.Count}");
            ((ZNetView)typeof(Container).GetField("m_nview", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(containers[container])).InvokeRPC("RequestOpen", new object[]{ Player.m_localPlayer.GetPlayerID() });
            
        }

        private List<Container> GetContainers()
        {
            Dictionary<string, List<Container>> containerTypes = new Dictionary<string, List<Container>>();
            foreach (Container c in containerList)
            {
                if (Traverse.Create(c).Method("CheckAccess", new object[] { Player.m_localPlayer.GetPlayerID() }).GetValue<bool>())
                {
                    if (!containerTypes.ContainsKey(c.name))
                        containerTypes[c.name] = new List<Container>();
                    containerTypes[c.name].Add(c);
                }
            }
            List<Container> newContainers = new List<Container>();
            foreach (List<Container> cl in containerTypes.Values)
                foreach (Container c in cl)
                    newContainers.Add(c);
            return newContainers;
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
                if ((__instance.name.StartsWith("piece_chest") || __instance.name.StartsWith("Container")) && __instance.GetInventory() != null)
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
            static void Postfix(InventoryGui __instance)
            {
                __instance.m_autoCloseDistance = float.MaxValue;
            }
        }
        [HarmonyPatch(typeof(InventoryGui), "Show")]
        static class InventoryGui_Show_Patch
        {
            static void Postfix(Container ___m_currentContainer)
            {
                if (___m_currentContainer && containerList.Contains(___m_currentContainer))
                    currentContainer = containerList.IndexOf(___m_currentContainer);
            }
        }
        
    }
}
