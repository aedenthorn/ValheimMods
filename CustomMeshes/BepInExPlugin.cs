using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace CustomMeshes
{
    [BepInPlugin("aedenthorn.CustomMeshes", "Custom Meshes", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        private static Dictionary<string, Dictionary<string, Dictionary<string, CustomItemMesh>>> customMeshes = new Dictionary<string, Dictionary<string, Dictionary<string, CustomItemMesh>>>();
        private static Dictionary<string, AssetBundle> customAssetBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, Dictionary<string, Dictionary<string, GameObject>>> customGameObjects = new Dictionary<string, Dictionary<string, Dictionary<string, GameObject>>>(); 
        public static ConfigEntry<int> nexusID;
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static Mesh customMesh { get; set; }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 184, "Nexus id for update checking");

            if (!modEnabled.Value)
                return;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            return;


        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            PreloadMeshes();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                return;
                if (Console.IsVisible())
                    return;

                Dbgl($"Pressed U.");

                return;
            }
        }

        private static void PreloadMeshes()
        {
            customMeshes.Clear();
            customGameObjects.Clear();

            Dbgl($"Importing meshes");

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomMeshes");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                string dirName = Path.GetFileName(dir);
                Dbgl($"Importing meshes: {dirName}");

                customMeshes[dirName] = new Dictionary<string, Dictionary<string, CustomItemMesh>>();
                customGameObjects[dirName] = new Dictionary<string, Dictionary<string, GameObject>>();

                foreach (string subdir in Directory.GetDirectories(dir))
                {
                    string subdirName = Path.GetFileName(subdir);
                    Dbgl($"Importing meshes: {dirName}\\{subdirName}");

                    customMeshes[dirName][subdirName] = new Dictionary<string, CustomItemMesh>();
                    customGameObjects[dirName][subdirName] = new Dictionary<string, GameObject>();

                    foreach (string file in Directory.GetFiles(subdir))
                    {
                        try
                        {
                            GameObject armature = null;
                            Mesh mesh = null;
                            Dbgl($"Importing {file} {Path.GetFileNameWithoutExtension(file)} {Path.GetFileName(file)} {Path.GetExtension(file).ToLower()}");
                            string name = Path.GetFileNameWithoutExtension(file);
                            if (name == Path.GetFileName(file))
                            {
                                AssetBundle ab = AssetBundle.LoadFromFile(file);
                                mesh = ab.LoadAsset<Mesh>("body");
                                //customAssetBundles.Add(name, ab);

                                if(mesh != null)
                                    Dbgl($"Imported {file} as asset bundle");
                            }
                            else if (Path.GetExtension(file).ToLower() == ".fbx")
                            {
                                GameObject obj = MeshImporter.Load(file)?.transform.Find("Player")?.Find("Visual")?.gameObject;
                                /*
                                int children = obj.transform.childCount;
                                for(int i = 0; i < children; i++)
                                {
                                    Dbgl($"fbx child: {obj.transform.GetChild(i).name}");
                                }
                                */
                                armature = obj.transform.Find("Armature")?.gameObject;
                                mesh = obj.GetComponentInChildren<MeshFilter>().mesh;
                                if (mesh != null)
                                    Dbgl($"Imported {file} fbx as mesh");
                                if(armature != null)
                                    Dbgl($"Imported {file} fbx as armature");
                            }
                            else if (Path.GetExtension(file).ToLower() == ".obj")
                            {
                                mesh = new ObjImporter().ImportFile(file);
                                if (mesh != null)
                                    Dbgl($"Imported {file} obj as mesh");
                            }
                            if (mesh != null)
                                customMeshes[dirName][subdirName].Add(name, new CustomItemMesh(dirName, name, mesh));
                            if (armature != null)
                                customGameObjects[dirName][subdirName].Add(name, armature);
                        }
                        catch(Exception ex) 
                        { 
                            //Dbgl($"Error:\n\n{ex}"); 
                        }
                    }
                }
            }
        }
        private static string GetPrefabName(string name)
        {
            char[] anyOf = new char[] { '(', ' ' };
            int num = name.IndexOfAny(anyOf);
            string result;
            if (num >= 0)
                result = name.Substring(0, num);
            else
                result = name;
            return result;
        }
        
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix() 
            {
                PreloadMeshes();
            }
        }        
        
        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        static class ItemDrop_Patch
        {
            static void Postfix(ItemDrop __instance) 
            {
                string name = __instance.m_itemData.m_dropPrefab.name;
                if (customMeshes.ContainsKey(name))
                {
                    Dbgl($"got item name: {name}");
                    MeshFilter[] mfs = __instance.m_itemData.m_dropPrefab.GetComponentsInChildren<MeshFilter>(true);
                    foreach (MeshFilter mf in mfs)
                    {
                        string parent = mf.transform.parent.gameObject.name;
                        Dbgl($"got item name: {name}, obj: {parent}, mf: {mf.name}");
                        if (name == GetPrefabName(parent) && customMeshes[name].ContainsKey(mf.name) && customMeshes[name][mf.name].ContainsKey(mf.name))
                        {
                            Dbgl($"replacing item mesh {mf.name}");
                            mf.mesh = customMeshes[name][mf.name][mf.name].mesh;
                        }
                        else if (customMeshes[name].ContainsKey(parent) && customMeshes[name][parent].ContainsKey(mf.name)) {
                            Dbgl($"replacing attached mesh {mf.name}");
                            mf.mesh = customMeshes[name][parent][mf.name].mesh;
                        }
                    }
                }
            }
        }        
                
        [HarmonyPatch(typeof(Piece), "Awake")]
        static class Piece_Patch
        {
            static void Postfix(Piece __instance) 
            {
                string name = GetPrefabName(__instance.gameObject.name);
                MeshFilter[] mfs = __instance.gameObject.GetComponentsInChildren<MeshFilter>(true);

                if (customMeshes.ContainsKey(name))
                {
                    foreach (MeshFilter mf in mfs)
                    {
                        string parent = mf.transform.parent.gameObject.name;
                        Dbgl($"got piece name: {name}, obj: {parent}, mf: {mf.name}");
                        if (customMeshes[name].ContainsKey(parent) && customMeshes[name][parent].ContainsKey(mf.name))
                        {
                            Dbgl($"replacing mesh {mf.name}");
                            mf.mesh = customMeshes[name][parent][mf.name].mesh;
                        }
                    }
                }
            }
        }
              

        [HarmonyPatch(typeof(VisEquipment), "Awake")]
        static class Awake_Patch
        {
            static void Postfix(VisEquipment __instance)
            {
                Dbgl($"Vis Awake .");

                if (!__instance.m_isPlayer || __instance.m_models.Length == 0)
                    return;

                if (customMeshes.ContainsKey("player"))
                {
                    if (customMeshes["player"].ContainsKey("model"))
                    {
                        if (customMeshes["player"]["model"].ContainsKey("0"))
                        {
                            Dbgl($"Replacing player model 0 with imported mesh.");
                            __instance.m_models[0].m_mesh = customMeshes["player"]["model"]["0"].mesh;
                        }
                        if (customMeshes["player"]["model"].ContainsKey("1"))
                        {
                            Dbgl($"Replacing player model 1 with imported mesh.");
                            __instance.m_models[1].m_mesh = customMeshes["player"]["model"]["1"].mesh;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(InventoryGui), "SetupDragItem")]
        static class SetupDragItem_Patch
        {
            static void Postfix(ItemDrop.ItemData item)
            {
                if (item == null)
                    return;
                string name = item.m_dropPrefab.name;
                MeshFilter[] mfs = item.m_dropPrefab.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter mf in mfs)
                {
                    Dbgl($"dragging item name: {name}, mf: {mf.name}");
                }
            }
        }
        [HarmonyPatch(typeof(Player), "Awake")]
        static class Player_Awake_Patch
        {
            static void Postfix(Player __instance)
            {
                Dbgl($"Player awake.");
                if (customGameObjects.ContainsKey("player"))
                {
                    if (customGameObjects["player"].ContainsKey("model"))
                    {
                        Dbgl($"Got game object.");

                        GameObject go = __instance.gameObject.transform.Find("Visual")?.Find("Armature")?.gameObject;
                        if (go == null)
                        {
                            Dbgl($"Wrong game object hierarchy.");
                            return;
                        }
                        Dbgl($"Got armature.");

                        if (customGameObjects["player"]["model"].ContainsKey("0"))
                        {
                            Dbgl($"Replacing player armature 0.");

                            GameObject newObject;
                            Transform parent = go.transform.parent;
                            Vector3 position = go.transform.position;
                            Quaternion rotation = go.transform.rotation;
                            DestroyImmediate(go);
                            newObject = Instantiate(customGameObjects["player"]["model"]["0"], parent);
                            newObject.transform.position = position;
                            newObject.transform.rotation = rotation;

                        }
                        if (customMeshes["player"]["model"].ContainsKey("1"))
                        {
                            Dbgl($"Replacing player armature 1.");

                            GameObject newObject;
                            newObject = Instantiate(customGameObjects["player"]["model"]["1"]);
                            newObject.transform.position = go.transform.position;
                            newObject.transform.rotation = go.transform.rotation;
                            Transform parent = go.transform.parent;
                            DestroyImmediate(go);
                            newObject.transform.parent = parent;
                        }
                    }
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
                if (text.ToLower().Equals("meshes dump"))
                {
                    List<string> dump = new List<string>();

                    foreach (Collider collider in Physics.OverlapSphere(Player.m_localPlayer.transform.position, 20f, LayerMask.GetMask(new string[] { "piece", "item" })))
                    {
                        GameObject go = collider.transform.parent.gameObject;
                        if (go != null)
                        {
                            string name = GetPrefabName(go.name);
                            if (name == "_NetSceneRoot")
                                continue;
                            MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();

                            foreach (MeshFilter mf in mfs)
                            {
                                string parent = mf.transform.parent.gameObject.name;

                                if (GetPrefabName(parent) == name)
                                    parent = mf.name;

                                dump.Add($"piece: {name}, object: {parent}, mesh: {mf.name}; use {name}\\{parent}\\{mf.name}.fbx or {name}\\{parent}\\{mf.name}.obj");
                            }
                        }
                    }
                    
                    Dbgl(string.Join("\r\n", dump));

                    try
                    {
                        Process.Start(Path.Combine(Application.persistentDataPath, "Player.log"));
                    }
                    catch { }

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Nearby piece info dumped to console" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
