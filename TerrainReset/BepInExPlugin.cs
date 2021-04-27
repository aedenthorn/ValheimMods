using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TerrainReset
{
    [BepInPlugin("aedenthorn.TerrainReset", "Terrain Reset", "0.3.1")]
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

            int resets = ResetTerrain(resetRadius.Value);
            if (resetMessage.Value.Length > 0 && resetMessage.Value.Contains("{0}"))
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(resetMessage.Value, resets));
        }

        
        private static int ResetTerrain(float radius)
        {
            int resets = 0;
            List<Heightmap> list = new List<Heightmap>();

            Vector3 playerPos = Player.m_localPlayer.transform.position;

            Heightmap.FindHeightmap(Player.m_localPlayer.transform.position, 150, list);


            List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
            foreach (TerrainModifier terrainModifier in allInstances)
            {
                Vector3 position = terrainModifier.transform.position;
                ZNetView nview = terrainModifier.GetComponent<ZNetView>();
                if (nview != null && nview.IsValid() && nview.IsOwner() && Utils.DistanceXZ(position, playerPos) <= radius)
                {
                    //Dbgl($"TerrainModifier {position}, player {playerPos}, distance: {Utils.DistanceXZ(position, playerPos)}");
                    resets++;
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

                    enumerator.Current.WorldToVertex(playerPos, out int x, out int y);

                    bool[] m_modifiedHeight = traverse.Field("m_modifiedHeight").GetValue<bool[]>();
                    float[] m_levelDelta = traverse.Field("m_levelDelta").GetValue<float[]>();
                    float[] m_smoothDelta = traverse.Field("m_smoothDelta").GetValue<float[]>();
                    bool[] m_modifiedPaint = traverse.Field("m_modifiedPaint").GetValue<bool[]>();
                    Color[] m_paintMask = traverse.Field("m_paintMask").GetValue<Color[]>();
                    
                    int m_width = traverse.Field("m_width").GetValue<int>();

                    Dbgl($"Checking heightmap at {terrainComp.transform.position}");
                    int thisResets = 0;
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

                            m_modifiedPaint[idx] = false;
                            m_paintMask[idx] = Color.clear;
                        }
                    }

                    Dbgl($"\tReset {thisResets} comp edits");

                    traverse.Field("m_modifiedHeight").SetValue(m_modifiedHeight);
                    traverse.Field("m_levelDelta").SetValue(m_levelDelta);
                    traverse.Field("m_smoothDelta").SetValue(m_smoothDelta);
                    traverse.Field("m_modifiedPaint").SetValue(m_modifiedPaint);
                    traverse.Field("m_paintMask").SetValue(m_paintMask);

                    continue;

                    Vector3 position = terrainComp.transform.position;

                    if (Utils.DistanceXZ(position, playerPos) <= radius)
                    {
                        Dbgl($"TerrainComp {position}, player {playerPos}. distance: {Utils.DistanceXZ(position, playerPos)}");
                        ZNetView nview = terrainComp.GetComponent<ZNetView>();
                        if (nview != null && nview.IsValid() && nview.IsOwner())
                        {
                            resets++;
                            nview.Destroy();
                        }
                    }

                }
            }

            if (resets > 0)
                context.StartCoroutine(ReloadTerrain());
            return resets;
        }

        private static float CoordDistance(float x, float y, float rx, float ry)
        {
            float num = x - rx;
            float num2 = y - ry;
            return Mathf.Sqrt(num * num + num2 * num2);
        }

        private static IEnumerator ReloadTerrain()
        {
            yield return null;
            List<Heightmap> list = new List<Heightmap>();
            Heightmap.FindHeightmap(Player.m_localPlayer.transform.position, 150, list);

            using (List<Heightmap>.Enumerator enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(enumerator.Current.transform.position);
                    if (!terrainComp)
                        continue;

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
