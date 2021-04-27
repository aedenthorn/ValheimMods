using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TerrainReset
{
    [BepInPlugin("aedenthorn.TerrainReset", "Terrain Reset", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> resetRadius;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> consoleCommand;
        public static ConfigEntry<string> resetMessage;

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1113, "Nexus mod ID for updates");
            resetRadius = Config.Bind<float>("Config", "ResetRadius", 150f, "Reset radius for hotkey command");
            hotKey = Config.Bind<string>("Config", "HotKey", "", "Hotkey to reset terrain. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            consoleCommand = Config.Bind<string>("Config", "ConsoleCommand", "resetterrain", "Console command to reset terrain. Usage: <command> <radius>");
            resetMessage = Config.Bind<string>("Config", "ResetMessage", "{0} edits reset.", "Reset message. {0} is replaced by the number of edits. Set to empty to disable message");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }
        private void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses(true) || !AedenthornUtils.CheckKeyDown(hotKey.Value) || !Player.m_localPlayer)
                return;

            int resets = ResetTerrainFake(resetRadius.Value);
            if (resetMessage.Value.Length > 0 && resetMessage.Value.Contains("{0}"))
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(resetMessage.Value, resets));
        }

        private static int ResetTerrainFake(float radius)
        {
            int resets = 69;
            List<Heightmap> list = new List<Heightmap>();
            Heightmap.FindHeightmap(Player.m_localPlayer.transform.position, radius, list);

            using (List<Heightmap>.Enumerator enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    enumerator.Current.Poke(false);
                    if (ClutterSystem.instance)
                        ClutterSystem.instance.ResetGrass(enumerator.Current.transform.position, enumerator.Current.m_width * enumerator.Current.m_scale / 2f);
                }
            }

            return resets;
        }
        
        private static int ResetTerrain(float radius)
        {
            int resets = 0;
            List<Heightmap> list = new List<Heightmap>();
            Heightmap.FindHeightmap(Player.m_localPlayer.transform.position, radius, list);
            List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();

            List<TerrainComp> comps = (List<TerrainComp>)typeof(TerrainComp).GetField("m_instances", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            foreach (TerrainComp terrainComp in comps)
            {
                Vector3 position = terrainComp.transform.position;
                if (Vector3.Distance(terrainComp.transform.position, Player.m_localPlayer.transform.position) <= radius)
                {
                    ZNetView nview = terrainComp.GetComponent<ZNetView>();
                    if (nview != null && nview.IsValid() && nview.IsOwner())
                    {
                        resets++;
                        nview.Destroy();
                    }
                }
            }

            using (List<Heightmap>.Enumerator enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    foreach (TerrainModifier terrainModifier in allInstances)
                    {
                        ZNetView nview = terrainModifier.GetComponent<ZNetView>();
                        if (nview != null && nview.IsValid() && nview.IsOwner() && Vector3.Distance(terrainModifier.transform.position, enumerator.Current.transform.position) <= radius)
                        {
                            resets++;
                            nview.Destroy();
                        }
                    }
                }
            }

            if (resets > 0)
                context.StartCoroutine(ReloadTerrain(radius));
            return resets;
        }

        private static IEnumerator ReloadTerrain(float radius)
        {
            yield return null;
            List<Heightmap> list = new List<Heightmap>();
            Heightmap.FindHeightmap(Player.m_localPlayer.transform.position, radius, list);

            using (List<Heightmap>.Enumerator enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    enumerator.Current.Poke(false);
                    if (ClutterSystem.instance)
                        ClutterSystem.instance.ResetGrass(enumerator.Current.transform.position, enumerator.Current.m_width * enumerator.Current.m_scale / 2f);
                }
            }

        }

        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
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
                if (text.ToLower().StartsWith(consoleCommand.Value + " "))
                {
                    if (float.TryParse(text.ToLower().Split(' ')[1], out float radius))
                    {
                        int resets = ResetTerrain(radius);
                        Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                        if (resetMessage.Value.Length > 0 && resetMessage.Value.Contains("{0}"))
                            Traverse.Create(__instance).Method("AddString", new object[] { string.Format(resetMessage.Value, resets) }).GetValue();
                    }
                    else
                        Traverse.Create(__instance).Method("AddString", new object[] { $"Format error. Usage: {consoleCommand.Value} <radius>" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
