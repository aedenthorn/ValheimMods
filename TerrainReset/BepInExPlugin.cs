using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TerrainReset
{
    [BepInPlugin("aedenthorn.TerrainReset", "Terrain Reset", "0.9.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> hotKeyRadius;
        public static ConfigEntry<float> toolRadius;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> consoleCommand;
        public static ConfigEntry<string> resetMessage;
        public static ConfigEntry<string> modKey;

        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1113, "Nexus mod ID for updates");
            hotKeyRadius = Config.Bind<float>("Config", "HotKeyRadius", 150f, "Reset radius for hotkey command");
            toolRadius = Config.Bind<float>("Config", "ToolRadius", 0, "Reset radius for tool. Set to 0 to use the tool's actual radius.");
            hotKey = Config.Bind<string>("Config", "HotKey", "", "Hotkey to reset terrain. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            modKey = Config.Bind<string>("Config", "ModKey", "left alt", "Modifer key to reset terrain when using the level ground hoe tool. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            consoleCommand = Config.Bind<string>("Config", "ConsoleCommand", "resetterrain", "Console command to reset terrain. Usage: <command> <radius>");
            resetMessage = Config.Bind<string>("Config", "ResetMessage", "{0} edits reset.", "Reset message. {0} is replaced by the number of edits. Set to empty to disable message");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }
        public void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses(true) || !AedenthornUtils.CheckKeyDown(hotKey.Value) || !Player.m_localPlayer)
                return;

            int resets = ResetTerrain(Player.m_localPlayer.transform.position, hotKeyRadius.Value);
            if (resetMessage.Value.Length > 0 && resetMessage.Value.Contains("{0}"))
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(resetMessage.Value, resets));
        }

        
        public static int ResetTerrain(Vector3 center, float radius)
        {
            int resets = 0;
            List<Heightmap> list = new List<Heightmap>();


            Heightmap.FindHeightmap(center, radius + 100, list);


            List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
            foreach (TerrainModifier terrainModifier in allInstances)
            {
                Vector3 position = terrainModifier.transform.position;
                ZNetView nview = terrainModifier.GetComponent<ZNetView>();
                if (nview != null && nview.IsValid() && nview.IsOwner() && Utils.DistanceXZ(position, center) <= radius)
                {
                    //Dbgl($"TerrainModifier {position}, player {playerPos}, distance: {Utils.DistanceXZ(position, playerPos)}");
                    resets++;
                    foreach (Heightmap heightmap in list)
                    {
                        if (heightmap.TerrainVSModifier(terrainModifier))
                            heightmap.Poke(true);
                    }
                    nview.Destroy();
                }
            }
            Dbgl($"Reset {resets} mod edits");

            using (List<Heightmap>.Enumerator enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(enumerator.Current.transform.position);
                    if (!terrainComp)
                        continue;
                    Traverse traverse = Traverse.Create(terrainComp);

                    if (!traverse.Field("m_initialized").GetValue<bool>())
                        continue;

                    enumerator.Current.WorldToVertex(center, out int x, out int y);

                    bool[] m_modifiedHeight = traverse.Field("m_modifiedHeight").GetValue<bool[]>();
                    float[] m_levelDelta = traverse.Field("m_levelDelta").GetValue<float[]>();
                    float[] m_smoothDelta = traverse.Field("m_smoothDelta").GetValue<float[]>();
                    bool[] m_modifiedPaint = traverse.Field("m_modifiedPaint").GetValue<bool[]>();
                    Color[] m_paintMask = traverse.Field("m_paintMask").GetValue<Color[]>();
                    
                    int m_width = traverse.Field("m_width").GetValue<int>();

                    Dbgl($"Checking heightmap at {terrainComp.transform.position}");
                    int thisResets = 0;
                    bool thisReset = false;
                    int num = m_width + 1;
                    for (int h = 0; h < num; h++)
                    {
                        for (int w = 0; w < num; w++)
                        {

                            int idx = h * num + w;

                            if (!m_modifiedHeight[idx])
                                continue;

                            //Dbgl($"Player coord {x},{y} coord {w},{h}, distance {CoordDistance(x, y, w, h)} has edits. ");

                            if (CoordDistance(x, y, w, h) > radius)
                                continue;

                            //Dbgl("In range, resetting");

                            resets++;
                            thisResets++;
                            thisReset = true;

                            m_modifiedHeight[idx] = false;
                            m_levelDelta[idx] = 0;
                            m_smoothDelta[idx] = 0;
                        }
                    }

                    num = m_width;
                    for (int h = 0; h < num; h++)
                    {
                        for (int w = 0; w < num; w++)
                        {

                            int idx = h * num + w;

                            if (!m_modifiedPaint[idx])
                                continue;

                            if (CoordDistance(x, y, w, h) > radius)
                                continue;

                            thisReset = true;
                            m_modifiedPaint[idx] = false;
                            m_paintMask[idx] = Color.clear;
                        }
                    }

                    if (thisReset)
                    {
                        Dbgl($"\tReset {thisResets} comp edits");

                        traverse.Field("m_modifiedHeight").SetValue(m_modifiedHeight);
                        traverse.Field("m_levelDelta").SetValue(m_levelDelta);
                        traverse.Field("m_smoothDelta").SetValue(m_smoothDelta);
                        traverse.Field("m_modifiedPaint").SetValue(m_modifiedPaint);
                        traverse.Field("m_paintMask").SetValue(m_paintMask);

                        traverse.Method("Save").GetValue();
                        enumerator.Current.Poke(true);
                    }

                }
            }

            if (resets > 0 && ClutterSystem.instance)
                ClutterSystem.instance.ResetGrass(center, radius);
            
            return resets;
        }

        public static float CoordDistance(float x, float y, float rx, float ry)
        {
            float num = x - rx;
            float num2 = y - ry;
            return Mathf.Sqrt(num * num + num2 * num2);
        }

        [HarmonyPatch(typeof(TerrainComp), "DoOperation")]
        public static class DoOperation_Patch
        {
            public static bool Prefix(Vector3 pos, TerrainOp.Settings modifier, bool ___m_initialized)
            {
                if (!___m_initialized)
                    return false;

                if (!modEnabled.Value || !modifier.m_smooth || !AedenthornUtils.CheckKeyHeld(modKey.Value))
                    return true;

                ResetTerrain(pos, toolRadius.Value > 0 ? toolRadius.Value : modifier.GetRadius());

                return false;
            }
        }

        [HarmonyPatch(typeof(TerrainOp), "OnPlaced")]
        public static class OnPlaced_Patch
        {
            public static bool Prefix(TerrainOp.Settings ___m_settings)
            {
                //Dbgl($"{___m_settings.m_smooth} {AedenthornUtils.CheckKeyHeld(modKey.Value)}");
                if (!modEnabled.Value || !___m_settings.m_smooth || !AedenthornUtils.CheckKeyHeld(modKey.Value))
                    return true;
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
                        int resets = ResetTerrain(Player.m_localPlayer.transform.position, radius);
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
