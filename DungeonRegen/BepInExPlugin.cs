using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonRegen
{
    [BepInPlugin("aedenthorn.DungeonRegen", "Dungeon Regen", "0.3.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> regenOnReload;
        public static ConfigEntry<bool> regenRandomly;
        public static ConfigEntry<int> daysToRegen;
        public static ConfigEntry<string> keywordRequirements;
        public static ConfigEntry<string> manualRegenKey;
        public static ConfigEntry<string> regenText;

        public static ConfigEntry<int> lastRegenDate;
        public static ConfigEntry<string> regeneratedLocations;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 827, "Nexus mod ID for updates");
            
            //regenOnReload = Config.Bind<bool>("Regen", "RegenOnReload", true, "Enable dungeon regen when the dungeon reloads, either from loading the game or entering the area around the dungeon.");
            regenRandomly = Config.Bind<bool>("Regen", "RegenRandomly", true, "Enable random dungeon layout on regeneration.");
            daysToRegen = Config.Bind<int>("Regen", "DaysToRegen", 7, "Number of days between regen");
            keywordRequirements = Config.Bind<string>("Regen", "KeywordRequirements", "Crypt", "Dungeon name must have one of these words in it to be regenerated (Comma-separated).");
            manualRegenKey = Config.Bind<string>("Regen", "ManualRegenKey", "f6", "Key to manually initiate dungeon regen. Leave blank to disable. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            regenText = Config.Bind<string>("Regen", "RegenText", "Dungeons Have Regenerated", "Text to show when dungeons have been regenerated. Leave blank to disable.");

            lastRegenDate = Config.Bind<int>("ZAuto", "LastRegenDate", 0, "Last regen date (auto updated)");
            regeneratedLocations = Config.Bind<string>("ZAuto", "RegeneratedLocations", "", "Locations already regenerated (auto updated)");
            
            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }


        public void Update()
        {
            if (!modEnabled.Value || Player.m_localPlayer == null || AedenthornUtils.IgnoreKeyPresses())
                return;


            if (AedenthornUtils.CheckKeyDown(manualRegenKey.Value))
            {
                if (Player.m_localPlayer.InInterior())
                {
                    Dbgl($"Player in dungeon, aborting ({Player.m_localPlayer.transform.position})");
                    return;
                }
                RegenDungeons(true);
            }
        }

        public static List<string> GetLocationNames()
        {
            List<string> names = new List<string>();
            foreach (ZoneSystem.ZoneLocation zoneLocation in from a in ZoneSystem.instance.m_locations
                                                             orderby a.m_prioritized descending
                                                             select a)
            {
                if (zoneLocation.m_enable && zoneLocation.m_quantity != 0)
                {
                    names.Add(zoneLocation.m_prefabName);
                }
            }
            return names;
        }

        public static void RegenDungeons(bool force)
        {
            List<string> op = new List<string>();

            var dict = Traverse.Create(ZoneSystem.instance).Field("m_locationInstances").GetValue<Dictionary<Vector2i, ZoneSystem.LocationInstance>>();

            Dictionary<Vector3, DungeonGenerator> dungeons = new Dictionary<Vector3, DungeonGenerator>();

            UnityEngine.Random.State state = UnityEngine.Random.state;
            foreach (ZoneSystem.LocationInstance zoneLocation in dict.Values)
            {
                if (zoneLocation.m_location.m_enable && zoneLocation.m_location.m_quantity != 0)
                {
                    foreach (ZNetView znv in zoneLocation.m_location.m_netViews)
                    {
                        if (znv.gameObject.GetComponent<DungeonGenerator>())
                        {
                            Vector3 position = new Vector3(zoneLocation.m_position.x, znv.transform.position.y, zoneLocation.m_position.z);
                            op.Add($"got dungeon {znv.gameObject.name} {position}");
                            dungeons[position] = znv.gameObject.GetComponent<DungeonGenerator>();
                        }
                    }
                }
            }

            Dbgl($"Got {dungeons.Count} dungeons");
            //Dbgl(string.Join("\n", op));


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
            Dbgl($"Got {ts.Count} objects");



            List<string> reloaded = regeneratedLocations.Value.Trim() == "" ? new List<string>() : regeneratedLocations.Value.Split('^').ToList();

            op = new List<string>();

            foreach (var kvp in dungeons)
            {
                int dest = 0;

                DungeonGenerator dg = kvp.Value;

                Vector2i zone = ZoneSystem.instance.GetZone(kvp.Key);

                dg.m_zoneCenter = ZoneSystem.instance.GetZonePos(zone);
                dg.m_zoneCenter.y = kvp.Key.y;

                Bounds bounds = new Bounds(dg.m_zoneCenter, dg.m_zoneSize);

                op.Add($"Bounds {bounds.center} size {bounds.size} pos {kvp.Key}");

                op.Add($"Destroying old dungeon {dg.name}");

                try
                {

                    foreach (Transform t in ts)
                    {
                        //Dbgl($"name {t.name} position {t.position}");
                        //if (!t.GetComponent<DungeonGenerator>() && )
                        if (bounds.Contains(t.position) && !t.GetComponent<DungeonGenerator>())
                        {
                            dest++;
                            op.Add($"deleting {t.name} position {t.position}");
                            ZNetScene.instance.Destroy(t.gameObject);
                        }
                        else
                        {
                            op.Add($"skipping {t.name} position {t.position}");
                        }
                    }
                    op.Add($"Got {ts.Count} objects, destroyed {dest}");
                }
                catch (Exception ex)
                {
                    Dbgl($"Error destroying dungeon {dg.name}\n{ex}");
                }

                op.Add($"Regenerating dungeon {dg.name}");

                int seed1;
                if (regenRandomly.Value)
                {
                    UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                    seed1 = World.GenerateSeed().GetStableHashCode();
                }
                else
                    seed1 = WorldGenerator.instance.GetSeed();

                int seed = seed1 + zone.x * 4271 + zone.y * 9187;

                try
                {
                    dg.Generate(seed, ZoneSystem.SpawnMode.Full);

                    if (!reloaded.Contains(dg.transform.position.ToString()))
                        reloaded.Add(dg.transform.position.ToString());
                }
                catch (Exception ex)
                {
                    Dbgl($"Error regenerating dungeon {dg.name}\n{ex}");
                }
            }

            //Dbgl(string.Join("\n", op));

            regeneratedLocations.Value = string.Join("^", reloaded);

            Player.m_localPlayer.Message(MessageHud.MessageType.Center, regenText.Value);
        }
        public static void RegenDungeons2(bool force)
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
            Dbgl($"Got {ts.Count} objects");

            Dictionary<Transform, DungeonGenerator> dungeons = new Dictionary<Transform, DungeonGenerator>();

            var reqs = keywordRequirements.Value.Split(',');

            List<string> op = new List<string>();
            List<string> reloaded = regeneratedLocations.Value.Trim() == "" ? new List<string>() : regeneratedLocations.Value.Split('^').ToList();
            var dict = Traverse.Create(ZoneSystem.instance).Field("m_locationInstances").GetValue<Dictionary<Vector2i, ZoneSystem.LocationInstance>>();
            foreach (ZoneSystem.LocationInstance location in dict.Values)
            {
                foreach (ZNetView znetView in location.m_location.m_netViews)
                {
                    if (znetView.gameObject.activeSelf)
                    {
                        DungeonGenerator dg = znetView.gameObject.GetComponent<DungeonGenerator>();
                        if (dg && (force || !reloaded.Contains(dg.transform.position.ToString())) && reqs.FirstOrDefault(s => dg.name.Contains(s)) != null)
                        {
                            //op.Add(dg.name);
                            dungeons[dg.transform] = dg;
                        }
                    }
                }
            }
            Dbgl($"Got {dungeons.Count} dungeons");
            //Dbgl($"Got {op.Count} dungeons\n" + string.Join("\n", op));

            foreach (DungeonGenerator dg in dungeons.Values)
            {
                //Dbgl($"Destroying old dungeon {dg.name}");
                try
                {
                    Bounds bounds = new Bounds(dg.transform.position, dg.m_zoneSize);
                    op = new List<string>();
                    foreach (Transform t in ts)
                    {
                        //Dbgl($"name {t.name} position {t.position}");
                        //if (!t.GetComponent<DungeonGenerator>() && )
                        if (bounds.Contains(t.position) && !t.GetComponent<DungeonGenerator>())
                        {
                            ZNetScene.instance.Destroy(t.gameObject);
                        }
                    }
                    Dbgl($"Destroyed {op.Count} objects");
                    //Dbgl(string.Join("\n", op));
                }
                catch (Exception ex)
                {
                    Dbgl($"Error destroying dungeon {dg.name}\n{ex}");
                }
            }
            foreach (DungeonGenerator dg in dungeons.Values)
            {
                Dbgl($"Regenerating dungeon {dg.name}");

                int seed1;
                if (regenRandomly.Value)
                {
                    UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                    seed1 = World.GenerateSeed().GetStableHashCode();
                }
                else
                    seed1 = WorldGenerator.instance.GetSeed();

                Vector2i zone = ZoneSystem.instance.GetZone(dg.transform.position);
                int seed = seed1 + zone.x * 4271 + zone.y * 9187;

                try
                {
                    dg.Generate(seed, ZoneSystem.SpawnMode.Full);

                    if (!reloaded.Contains(dg.transform.position.ToString()))
                        reloaded.Add(dg.transform.position.ToString());
                }
                catch (Exception ex)
                {
                    Dbgl($"Error regenerating dungeon {dg.name}\n{ex}");
                }
            }
            regeneratedLocations.Value = string.Join("^", reloaded);

            Player.m_localPlayer.Message(MessageHud.MessageType.Center, regenText.Value);
        }

        public static void RegenDungeon(DungeonGenerator dg)
        {
            Dbgl($"Regenerating dungeon {dg.name} {dg.transform.position}");

            /*
            List<Transform> ts = new List<Transform>();
            GameObject[] array = ;
            foreach (var location in ZoneSystem.instance.m_locations)
            {
                GameObject go = location.m_prefab;
                if (gameObject.transform.position.y > 3000)
                {
                    ts.Add(gameObject.transform);
                }
            }
            /*
            */
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
            int seed1;
            if (regenRandomly.Value)
            {
                UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                seed1 = World.GenerateSeed().GetStableHashCode();
            }
            else
                seed1 = WorldGenerator.instance.GetSeed();

            Vector2i zone = ZoneSystem.instance.GetZone(dg.transform.position);
            int seed = seed1 + zone.x * 4271 + zone.y * 9187;
            dg.m_zoneCenter = ZoneSystem.instance.GetZonePos(zone);
            dg.m_zoneCenter.y = dg.transform.position.y;

            List<string> op = new List<string>();
            Bounds bounds = new Bounds(dg.m_zoneCenter, dg.m_zoneSize);
            Dbgl($"bounds {bounds.center} size {bounds.size} dg {dg.transform.position} {bounds.Contains(dg.transform.position)}");
            int dest = 0;
            foreach (Transform t in ts)
            {
                if (!t.GetComponent<DungeonGenerator>() && bounds.Contains(t.position))
                {
                    dest++;
                    ZNetScene.instance.Destroy(t.gameObject);
                }
                else
                    op.Add($"skipped {t.name} position {t.position}");
            }
            Dbgl($"got {op.Count} objects, destroyed {dest}");
            Dbgl(string.Join("\n", op));

            dg.Generate(seed, ZoneSystem.SpawnMode.Full);
            //Player.m_localPlayer.Message(MessageHud.MessageType.Center, regenText.Value);
        }

        //[HarmonyPatch(typeof(DungeonGenerator), "Load")]
        public static class DungeonGenerator_Load_Patch
        {
            public static bool Prefix(DungeonGenerator __instance)
            {
                return true;
                if (!modEnabled.Value)
                    return true;
                Dbgl($"Loading dungeon {__instance.name} {__instance.transform.position} {__instance.transform.localPosition}");

                var reqs = keywordRequirements.Value.Split(',');
                List<string> reloaded = regeneratedLocations.Value.Trim() == "" ? new List<string>() : regeneratedLocations.Value.Split('^').ToList();
                if ((daysToRegen.Value < 1 || !reloaded.Contains(__instance.transform.position.ToString())) && reqs.FirstOrDefault(s => __instance.name.Contains(s)) != null)
                {
                    if (!reloaded.Contains(__instance.transform.position.ToString()))
                    {
                        reloaded.Add(__instance.transform.position.ToString());
                        regeneratedLocations.Value = string.Join("^", reloaded);
                    }
                    RegenDungeon(__instance);
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Teleport), "Interact")]
        public static class Teleport_Interact_Patch
        {
            public static void Prefix(Teleport __instance)
            {
                if (!modEnabled.Value || __instance.m_targetPoint.transform.position.y < 3000)
                    return;

                Dbgl($"Checking for dungeon");

                DungeonGenerator dg = __instance.transform.parent.GetComponentInChildren<DungeonGenerator>(true);

                if (!dg)
                    dg = __instance.transform.parent.parent.GetComponentInChildren<DungeonGenerator>(true);
                if (!dg)
                    return;

                Dbgl($"got dungeon {dg.name}");

                context.StartCoroutine(ReloadDungeon(__instance, dg));
            }
        }

        public static IEnumerator ReloadDungeon(Teleport teleport, DungeonGenerator dg)
        {
            yield return null;
            int seed1;
            if (regenRandomly.Value)
            {
                UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                seed1 = World.GenerateSeed().GetStableHashCode();
            }
            else
                seed1 = WorldGenerator.instance.GetSeed();

            Vector2i zone = ZoneSystem.instance.GetZone(teleport.m_targetPoint.transform.position);
            dg.m_zoneCenter = ZoneSystem.instance.GetZonePos(zone);
            dg.m_zoneCenter.y = dg.transform.position.y;

            Bounds bounds = new Bounds(dg.m_zoneCenter, dg.m_zoneSize);

            GameObject root = Traverse.Create(ZNetScene.instance).Field("m_netSceneRoot").GetValue<GameObject>();

            int count = root.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                if (root.transform.GetChild(i).position.y > 3000 && bounds.Contains(root.transform.GetChild(i).position) && root.transform.GetChild(i).GetComponentInChildren<DungeonGenerator>() == null)
                {
                    Dbgl($"Destroying {root.transform.GetChild(i).name}.");
                    if (root.transform.GetChild(i).GetComponent<ZNetView>()?.IsValid() == true)
                        root.transform.GetChild(i).GetComponent<ZNetView>().Destroy();
                    else
                        Destroy(root.transform.GetChild(i).GetComponent<ZNetView>().gameObject);
                }
            }
            int seed = seed1 + zone.x * 4271 + zone.y * 9187;
            dg.Generate(seed, ZoneSystem.SpawnMode.Full);

            yield break;
        }

        [HarmonyPatch(typeof(EnvMan), "OnMorning")]
        public static class OnMorning_Patch
        {
            public static void Postfix(EnvMan __instance)
            {
                if (!modEnabled.Value)
                    return;
                int day = Traverse.Create(__instance).Method("GetCurrentDay").GetValue<int>();
                Dbgl($"Day {day} begins.");
                if (day - lastRegenDate.Value >= daysToRegen.Value)
                {
                    Dbgl("Reloading dungeons on new day.");
                    lastRegenDate.Value = day;
                    regeneratedLocations.Value = "";
                    RegenDungeons(false);
                }
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
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} dump"))
                {
                    List<string> names = GetLocationNames();

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();

                    Dbgl($"{names.Count} locations found:\n{string.Join("\n",names)}");

                    Traverse.Create(__instance).Method("AddString", new object[] { $"{names.Count} locations dumped to console" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
