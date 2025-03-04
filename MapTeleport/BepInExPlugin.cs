using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace MapTeleport
{
    [BepInPlugin("aedenthorn.MapTeleport", "Map Teleport", "0.7.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<string> modKey;
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
            modKey = Config.Bind<string>("General", "ModKey", "left shift", "Modifier key. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            nexusID = Config.Bind<int>("General", "NexusID", 251, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        [HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
        public static class OnMapLeftClick_Patch
        {
            public static bool Prefix(Minimap __instance)
            {
                if (!modEnabled.Value || !CheckKeyHeld(modKey.Value) || !Player.m_localPlayer)
                    return true;

                Vector3 pos = Traverse.Create(__instance).Method("ScreenToWorldPoint", new object[] { Input.mousePosition }).GetValue<Vector3>();

                Dbgl($"trying to teleport from {Player.m_localPlayer.transform.position} to {pos}");

                if (pos != Vector3.zero)
                {
                    if (Player.m_localPlayer)
                    {
                        __instance.SetMapMode(Minimap.MapMode.Small);
                        Minimap.instance.m_smallRoot.SetActive(true);

                        HeightmapBuilder.HMBuildData data = new HeightmapBuilder.HMBuildData(pos, 1, 1, false, WorldGenerator.instance);
                        Traverse.Create(HeightmapBuilder.instance).Method("Build", new object[] { data }).GetValue();

                        pos.y = data.m_baseHeights[0];
                        Dbgl($"teleporting from {Player.m_localPlayer.transform.position} to {pos}");
                        Player.m_localPlayer.TeleportTo(pos, Player.m_localPlayer.transform.rotation, true);
                    }
                }

                return false;
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
                if (text.ToLower().Equals("mapteleport reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Map Teleport config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
        public static bool CheckKeyHeld(string value)
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
    }
}