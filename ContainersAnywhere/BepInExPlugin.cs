using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ContainersAnywhere
{
    [BepInPlugin("aedenthorn.ContainersAnywhere", "Containers Anywhere", "0.3.8")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> previousKey;
        public static ConfigEntry<string> nextKey;
        public static ConfigEntry<string> previousTypeKey;
        public static ConfigEntry<string> nextTypeKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        
        public static Dictionary<string, List<Container>> containerDict = new Dictionary<string, List<Container>>();
        public static BepInExPlugin context;
        public static int currentContainerIndex = 0;
        public static string currentType = "";

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
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
        public void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses())
                return;
            if (CheckKeyDown(hotKey.Value))
                OpenContainers(0);
            else if (InventoryGui.instance?.IsContainerOpen() == true)
            {
                if (CheckKeyDown(previousKey.Value))
                {
                    ((Container)typeof(InventoryGui).GetField("m_currentContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(InventoryGui.instance)).SetInUse(false);
                    OpenContainers(-1);

                }
                else if (CheckKeyDown(nextKey.Value))
                {
                    ((Container)typeof(InventoryGui).GetField("m_currentContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(InventoryGui.instance)).SetInUse(false);
                    OpenContainers(1);
                }
                else if (CheckKeyDown(previousTypeKey.Value))
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

        public void OpenContainerType(int which)
        {
            if (!containerDict.Any())
            {
                Dbgl("No containers!");
                return;
            }

            CheckOpenContainer();


            List<string> keys = new List<string>();

            foreach(string key in containerDict.Keys)
            {
                if (GetContainers(key).Any())
                    keys.Add(key);
            }

            if (!keys.Contains(currentType))
            {
                currentType = keys[0];
            }
            else
            {
                int typeNo = keys.IndexOf(currentType);

                currentType = keys[typeNo + which < 0 ? keys.Count - 1 : (typeNo + which) % keys.Count];
            }

            List<Container> containers = GetContainers(currentType);

            currentContainerIndex = 0;

            Dbgl($"Opening {currentType} container 1/{containers.Count}");
            ((ZNetView)typeof(Container).GetField("m_nview", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(containers[0])).InvokeRPC("RequestOpen", new object[] { Player.m_localPlayer.GetPlayerID() });
        }

        public void CheckOpenContainer()
        {
            if (InventoryGui.instance?.IsContainerOpen() == true)
            {
                Container c = Traverse.Create(InventoryGui.instance).Field("m_currentContainer").GetValue<Container>();
                if (containerDict.ContainsKey(c.name))
                {
                    var cl = GetContainers(c.name);
                    if (cl.Contains(c))
                    {
                        currentType = c.name;
                        currentContainerIndex = cl.IndexOf(c);
                    }
                }
            }
        }

        public void OpenContainers(int which)
        {
            CheckOpenContainer();

            if (!containerDict.Any())
            {
                Dbgl("No container types in dicitionary!");
                return;
            }

            List<string> keys = containerDict.Keys.ToList();
            if (!containerDict.ContainsKey(currentType) || !containerDict[currentType].Any())
            {
                currentType = "";
                for(int i = 0; i < keys.Count; i++)
                {
                    if (containerDict[keys[i]].Any())
                    {
                        currentType = keys[i];
                        break;
                    }
                }
                if(currentType == "")
                {
                    Dbgl("No containers in any container type in dicitionary!");
                    return;
                }
            }

            List<Container> containers = GetContainers(currentType);

            if (containers.Count == 0)
                return;

            int container = currentContainerIndex + which < 0 ? containers.Count - 1 : (currentContainerIndex + which) % containers.Count;
            currentContainerIndex = container;
            Dbgl($"Opening {currentType} container {container+1}/{containers.Count} {containers[container].transform.parent.name} {containers[container].transform.position}");
            ((ZNetView)typeof(Container).GetField("m_nview", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(containers[container])).InvokeRPC("RequestOpen", new object[]{ Player.m_localPlayer.GetPlayerID() });
            
        }

        public static List<Container> GetContainers(string type)
        {
            if (!containerDict.ContainsKey(type))
            {
                Dbgl($"no containers of type {type}");
                return new List<Container>();
            }

            List<Container> newContainers = new List<Container>();
            foreach (Container c in containerDict[type])
            {
                if (c == null || Traverse.Create(c).Field("m_nview").GetValue() == null)
                    continue;
                if (Traverse.Create(c).Method("CheckAccess", new object[] { Player.m_localPlayer.GetPlayerID() }).GetValue<bool>() && c.IsOwner())
                {
                    newContainers.Add(c);
                }
            }
            return newContainers;
        }
        public static bool CheckKeyDown(string value)
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
        public static class Container_Awake_Patch
        {
            public static void Postfix(Container __instance, ZNetView ___m_nview)
            {
                if ((__instance.name.StartsWith("piece_chest") || __instance.name.StartsWith("Container")) && __instance.GetInventory() != null)
                {
                    if (!containerDict.ContainsKey(__instance.name))
                        containerDict.Add(__instance.name, new List<Container>());
                    containerDict[__instance.name].Add(__instance);
                }
            }
        }
        [HarmonyPatch(typeof(Container), "OnDestroyed")]
        public static class Container_OnDestroyed_Patch
        {
            public static void Prefix(Container __instance)
            {
                if (containerDict.ContainsKey(__instance.name))
                {
                    containerDict[__instance.name].Remove(__instance);
                    if (!containerDict[__instance.name].Any())
                        containerDict.Remove(__instance.name);
                }
            }
        }
        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        public static class InventoryGui_Awake_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                __instance.m_autoCloseDistance = float.MaxValue;
            }
        }
        [HarmonyPatch(typeof(InventoryGui), "Show")]
        public static class InventoryGui_Show_Patch
        {
            public static void Postfix(Container ___m_currentContainer)
            {
                if (!___m_currentContainer)
                    return;

                var containers = GetContainers(___m_currentContainer.name);
                if(containers.Contains(___m_currentContainer))
                    currentContainerIndex = containers.IndexOf(___m_currentContainer);
            }
        }
        
    }
}
