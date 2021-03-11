using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomTextures
{
    [BepInPlugin("aedenthorn.CustomTextures", "Custom Textures", "1.6.1")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> dumpSceneTextures;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<int> nexusID;
        public static Dictionary<string, string> customTextures = new Dictionary<string, string>();
        public static Dictionary<string, DateTime> fileWriteTimes = new Dictionary<string, DateTime>();
        public static List<string> texturesToLoad = new List<string>();
        public static Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();
        public static List<string> outputDump = new List<string>();
        public static List<string> logDump = new List<string>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            hotKey = Config.Bind<string>("General", "HotKey", "page down", "Key to reload textures");
            dumpSceneTextures = Config.Bind<bool>("General", "DumpSceneTextures", false, "Dump scene textures to BepInEx/plugins/CustomTextures/scene_dump.txt");
            nexusID = Config.Bind<int>("General", "NexusID", 48, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            LoadCustomTextures();

            //SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (ZNetScene.instance != null && CheckKeyDown(hotKey.Value))
            {
                Dbgl($"Pressed reload key.");

                outputDump.Clear();
                logDump.Clear();

                LoadCustomTextures();
                ReplaceObjectDBTextures();

                Dbgl($"textures to load \n\n{string.Join("\n", texturesToLoad)}");

                List<GameObject> gos = new List<GameObject>();

                GameObject root = (GameObject)typeof(ZNetScene).GetField("m_netSceneRoot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ZNetScene.instance);

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in transforms)
                {
                    if (t.parent == root.transform)
                        gos.Add(t.gameObject);
                }

                LoadSceneTextures(gos.ToArray());
                LoadSceneTextures(((Dictionary<int, GameObject>)typeof(ZNetScene).GetField("m_namedPrefabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ZNetScene.instance)).Values.ToArray());

                foreach (ClutterSystem.Clutter clutter in ClutterSystem.instance.m_clutter)
                {
                    gos.Add(clutter.m_prefab);
                }
                LoadSceneTextures(gos.ToArray());

                LoadSceneTextures(Traverse.Create(ZNetScene.instance).Field("m_namedPrefabs").GetValue<Dictionary<int, GameObject>>().Values.ToArray());

                foreach (Player player in Player.GetAllPlayers())
                {
                    VisEquipment ve = (VisEquipment)typeof(Humanoid).GetField("m_visEquipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);
                    if(ve != null)
                    {
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_leftItem").GetValue<string>(), Traverse.Create(ve).Field("m_leftItemInstance").GetValue<GameObject>());
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_rightItem").GetValue<string>(), Traverse.Create(ve).Field("m_rightItemInstance").GetValue<GameObject>());
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_helmetItem").GetValue<string>(), Traverse.Create(ve).Field("m_helmetItemInstance").GetValue<GameObject>());
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_leftBackItem").GetValue<string>(), Traverse.Create(ve).Field("m_leftBackItemInstance").GetValue<GameObject>());
                        SetEquipmentTexture(Traverse.Create(ve).Field("m_rightBackItem").GetValue<string>(), Traverse.Create(ve).Field("m_rightBackItemInstance").GetValue<GameObject>());
                        SetEquipmentListTexture(Traverse.Create(ve).Field("m_shoulderItem").GetValue<string>(), Traverse.Create(ve).Field("m_shoulderItemInstances").GetValue<List<GameObject>>());
                        SetEquipmentListTexture(Traverse.Create(ve).Field("m_utilityItem").GetValue<string>(), Traverse.Create(ve).Field("m_utilityItemInstances").GetValue<List<GameObject>>());
                        SetBodyEquipmentTexture(ve, Traverse.Create(ve).Field("m_legItem").GetValue<string>(), ve.m_bodyModel, Traverse.Create(ve).Field("m_legItemInstances").GetValue<List<GameObject>>());
                        SetBodyEquipmentTexture(ve, Traverse.Create(ve).Field("m_chestItem").GetValue<string>(), ve.m_bodyModel, Traverse.Create(ve).Field("m_chestItemInstances").GetValue<List<GameObject>>());
                    }
                }

                if (logDump.Any())
                    Dbgl("\n" + string.Join("\n", logDump));
                if (dumpSceneTextures.Value)
                {
                    string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomTextures", "scene_dump.txt");
                    Dbgl($"Writing {path}");
                    File.WriteAllLines(path, outputDump);
                }
            }

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
    }
}
