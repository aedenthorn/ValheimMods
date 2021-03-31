using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonRegen
{
    [BepInPlugin("aedenthorn.DungeonRegen", "Dungeon Regen", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private static int seed;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> regenOnReload;
        public static ConfigEntry<bool> regenRandomly;
        public static ConfigEntry<int> daysToRegen;
        public static ConfigEntry<int> lastRegenDate;
        public static ConfigEntry<string> keywordRequirements;
        public static ConfigEntry<string> manualRegenKey;
        public static ConfigEntry<string> regenText;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            
            regenOnReload = Config.Bind<bool>("Regen", "RegenOnReload", true, "Enable dungeon regen when the dungeon reloads, either from loading the game or entering the area around the dungeon.");
            regenRandomly = Config.Bind<bool>("Regen", "RegenRandomly", true, "Enable random dungeon layout on regeneration.");
            nexusID = Config.Bind<int>("General", "NexusID", 827, "Nexus mod ID for updates");

            //daysToRegen = Config.Bind<int>("Regen", "DaysToRegen", 7, "Number of days between regen");
            keywordRequirements = Config.Bind<string>("Regen", "KeywordRequirements", "Crypt", "Dungeon name must have one of these words in it to be regenerated (Comma-separated).");
            manualRegenKey = Config.Bind<string>("Regen", "ManualRegenKey", "f6", "Key to manually initiate dungeon regen. Leave blank to disable. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            regenText = Config.Bind<string>("Regen", "RegenText", "Dungeons Have Regenerated", "Text to show when dungeons have been regenerated. Leave blank to disable.");

            //lastRegenDate = Config.Bind<int>("ZAuto", "LastRegenDate", 0, "Last regen date (auto updated)");
            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }


        private void Update()
        {
            if (!modEnabled.Value || Player.m_localPlayer == null || Player.m_localPlayer.InInterior() || AedenthornUtils.IgnoreKeyPresses() || !AedenthornUtils.CheckKeyDown(manualRegenKey.Value))
                return;

            RegenDungeons();
        }

        private static void RegenDungeons()
        {
            GameObject root = Traverse.Create(ZNetScene.instance).Field("m_netSceneRoot").GetValue<GameObject>();

            int count = root.transform.childCount;
            List<Transform> ts = new List<Transform>();
            for (int i = 0; i < count; i++)
            {
                if (root.transform.GetChild(i).position.y > 3000)
                {
                    ts.Add(root.transform.GetChild(i));
                }
            }

            Dictionary<Transform, DungeonGenerator> dungeons = new Dictionary<Transform, DungeonGenerator>();

            var reqs = keywordRequirements.Value.Split(',');

            var dict = Traverse.Create(ZoneSystem.instance).Field("m_locationInstances").GetValue<Dictionary<Vector2i, ZoneSystem.LocationInstance>>();
            foreach(ZoneSystem.LocationInstance li in dict.Values)
            {
                foreach (ZNetView znetView in li.m_location.m_netViews)
                {
                    if (znetView.gameObject.activeSelf)
                    {
                        DungeonGenerator dg = znetView.gameObject.GetComponent<DungeonGenerator>();
                        if (dg && reqs.FirstOrDefault(s => dg.name.Contains(s)) != null)
                        {
                            dungeons[dg.transform] = dg;
                        }
                    }
                }
            }

            if (regenRandomly.Value)
                seed = World.GenerateSeed().GetStableHashCode();
            else
                seed = WorldGenerator.instance.GetSeed();
            foreach (DungeonGenerator dg in dungeons.Values)
            {
                try
                {
                    Dbgl($"Regenerating dungeon {dg.name}");
                    Bounds bounds = new Bounds(dg.transform.position, dg.m_zoneSize);
                    //Dbgl($"position {bounds.center} size {bounds.size}");
                    List<string> op = new List<string>();
                    foreach (Transform t in ts)
                    {
                        //Dbgl($"name {t.name} position {t.position}");
                        if (bounds.Contains(t.position) && t != dg.transform)
                        {
                            op.Add($"{t.name} {t.position}");

                            if (t.GetComponent<ZNetView>()?.IsValid() == true)
                            {

                                t.GetComponent<ZNetView>().Destroy();
                            }
                            else
                                Destroy(t.gameObject);
                        }
                    }
                    Dbgl($"Destroyed {op.Count} objects");
                    //Dbgl(string.Join("\n", op));

                    dg.Generate(seed, ZoneSystem.SpawnMode.Full);
                }
                catch { }
            }
            
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, regenText.Value);
        }

        private static void RegenDungeon(DungeonGenerator dg)
        {
            Dbgl($"Regenerating dungeon {dg.name}");

            GameObject root = Traverse.Create(ZNetScene.instance).Field("m_netSceneRoot").GetValue<GameObject>();

            int count = root.transform.childCount;
            List<Transform> ts = new List<Transform>();
            for (int i = 0; i < count; i++)
            {
                if (root.transform.GetChild(i).position.y > 3000)
                {
                    ts.Add(root.transform.GetChild(i));
                }
            }

            if (regenRandomly.Value)
                seed = World.GenerateSeed().GetStableHashCode();
            else
                seed = WorldGenerator.instance.GetSeed();

            Bounds bounds = new Bounds(dg.transform.position, dg.m_zoneSize);
            //Dbgl($"position {bounds.center} size {bounds.size}");
            List<string> op = new List<string>();
            foreach (Transform t in ts)
            {
                //Dbgl($"name {t.name} position {t.position}");
                if (bounds.Contains(t.position) && t != dg.transform)
                {
                    op.Add($"{t.name} {t.position}");

                    if (t.GetComponent<ZNetView>()?.IsValid() == true)
                    {

                        t.GetComponent<ZNetView>().Destroy();
                    }
                    else
                        Destroy(t.gameObject);
                }
            }
            Dbgl($"Destroyed {op.Count} objects");
            //Dbgl(string.Join("\n", op));

            dg.Generate(seed, ZoneSystem.SpawnMode.Full);
            //Player.m_localPlayer.Message(MessageHud.MessageType.Center, regenText.Value);
        }

        [HarmonyPatch(typeof(DungeonGenerator), "Load")]
        static class DungeonGenerator_Load_Patch
        {
            static void Prefix(DungeonGenerator __instance)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl($"Loading dungeon {__instance.name}");
                
                if(regenOnReload.Value)
                    RegenDungeon(__instance);
            }
        }
        //[HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            static void Prefix(DungeonGenerator __instance)
            {
                if (!modEnabled.Value)
                    return;
            }
        }
        //[HarmonyPatch(typeof(EnvMan), "OnMorning")]
        static class OnMorning_Patch
        {
            static void Prefix(EnvMan __instance)
            {
                if (!modEnabled.Value)
                    return;
                int day = Traverse.Create(__instance).Method("GetCurrentDay").GetValue<int>();
                if (day - lastRegenDate.Value >= daysToRegen.Value)
                {
                    lastRegenDate.Value = day;
                    RegenDungeons();
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
                return true;
            }
        }
    }
}
