using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace CustomMeshes
{
    [BepInPlugin("aedenthorn.CustomMeshes", "Custom Meshes", "0.4.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static Dictionary<string, Dictionary<string, Dictionary<string, CustomMeshData>>> customMeshes = new Dictionary<string, Dictionary<string, Dictionary<string, CustomMeshData>>>();
        public static Dictionary<string, AssetBundle> customAssetBundles = new Dictionary<string, AssetBundle>();
        public static Dictionary<string, Dictionary<string, Dictionary<string, GameObject>>> customGameObjects = new Dictionary<string, Dictionary<string, Dictionary<string, GameObject>>>();
        public static BepInExPlugin context;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static Mesh customMesh { get; set; }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 184, "Nexus id for update checking");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug");

            if (!modEnabled.Value)
                return;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
            }
        }

        public void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            PreloadMeshes();
        }


        public static void PreloadMeshes()
        {
            foreach (AssetBundle ab in customAssetBundles.Values)
                ab.Unload(true);
            customMeshes.Clear();
            customGameObjects.Clear();
            customAssetBundles.Clear();

            Dbgl($"Importing meshes");

            string path = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "CustomMeshes");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                string dirName = Path.GetFileName(dir);
                Dbgl($"Importing meshes: {dirName}");

                customMeshes[dirName] = new Dictionary<string, Dictionary<string, CustomMeshData>>();
                customGameObjects[dirName] = new Dictionary<string, Dictionary<string, GameObject>>();

                foreach (string subdir in Directory.GetDirectories(dir))
                {
                    string subdirName = Path.GetFileName(subdir);
                    Dbgl($"Importing meshes: {dirName}\\{subdirName}");

                    customMeshes[dirName][subdirName] = new Dictionary<string, CustomMeshData>();
                    customGameObjects[dirName][subdirName] = new Dictionary<string, GameObject>();

                    foreach (string file in Directory.GetFiles(subdir))
                    {
                        try
                        {
                            SkinnedMeshRenderer renderer = null;
                            Mesh mesh = null;
                            Dbgl($"Importing {file} {Path.GetFileNameWithoutExtension(file)} {Path.GetFileName(file)} {Path.GetExtension(file).ToLower()}");
                            string name = Path.GetFileNameWithoutExtension(file);
                            if (name == Path.GetFileName(file))
                            {
                                AssetBundle ab = AssetBundle.LoadFromFile(file);
                                customAssetBundles.Add(name, ab);

                                GameObject prefab = ab.LoadAsset<GameObject>("Player");
                                if (prefab != null)
                                {
                                    renderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
                                    if (renderer != null)
                                    {
                                        mesh = renderer.sharedMesh;
                                        Dbgl($"Importing {file} asset bundle as player");
                                    }
                                    else
                                    {
                                        Dbgl($"No SkinnedMeshRenderer on {prefab}");
                                    }
                                    if (mesh == null)
                                        mesh = ab.LoadAsset<Mesh>("body");
                                }
                                else
                                {
                                    mesh = ab.LoadAsset<Mesh>("body");

                                    if (mesh != null)
                                    {
                                        Dbgl($"Importing {file} asset bundle as mesh");
                                    }
                                    else
                                    {
                                        Dbgl("Failed to find body");
                                    }
                                }
                            }
                            else if (Path.GetExtension(file).ToLower() == ".fbx")
                            {
                                GameObject obj = MeshImporter.Load(file);
                                GameObject obj2 = obj?.transform.Find("Player")?.Find("Visual")?.gameObject;
                                //
                                /*
                                int children = obj.transform.childCount;
                                for(int i = 0; i < children; i++)
                                {
                                    Dbgl($"fbx child: {obj.transform.GetChild(i).name}");
                                }
                                */
                                mesh = obj.GetComponentInChildren<MeshFilter>().mesh;
                                if(obj2 != null)
                                    renderer = obj2.GetComponentInChildren<SkinnedMeshRenderer>();
                                if (mesh != null)
                                {
                                    if (renderer != null)
                                        Dbgl($"Importing {file} fbx as player");
                                    else
                                        Dbgl($"Importing {file} fbx as mesh");
                                }
                            }
                            else if (Path.GetExtension(file).ToLower() == ".obj")
                            {
                                mesh = new ObjImporter().ImportFile(file);
                                if (mesh != null)
                                    Dbgl($"Imported {file} obj as mesh");
                            }
                            if (mesh != null)
                            {
                                customMeshes[dirName][subdirName].Add(name, new CustomMeshData(dirName, name, mesh, renderer));
                                Dbgl($"Added mesh data to customMeshes[{dirName}][{subdirName}][{name}]");
                            }
                        }
                        catch { }
                    }
                }
            }
        }
        public static string GetPrefabName(string name)
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
        public static class ZNetScene_Awake_Patch
        {
            public static void Postfix()
            {


            }
        }

        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        public static class ItemDrop_Patch
        {
            public static void Postfix(ItemDrop __instance)
            {
                string name = __instance.m_itemData?.m_dropPrefab?.name;
                if (name != null && customMeshes.ContainsKey(name))
                {
                    //Dbgl($"got item name: {name}");
                    MeshFilter[] mfs = __instance.m_itemData.m_dropPrefab.GetComponentsInChildren<MeshFilter>(true);
                    foreach (MeshFilter mf in mfs)
                    {
                        string parent = mf.transform.parent.gameObject.name;
                        Dbgl($"got mesh filter, item name: {name}, obj: {parent}, mf: {mf.name}");
                        if (name == GetPrefabName(parent) && customMeshes[name].ContainsKey(mf.name) && customMeshes[name][mf.name].ContainsKey(mf.name))
                        {
                            Dbgl($"replacing item mesh {mf.name}");
                            mf.mesh = customMeshes[name][mf.name][mf.name].mesh;
                        }
                        else if (customMeshes[name].ContainsKey(parent) && customMeshes[name][parent].ContainsKey(mf.name))
                        {
                            Dbgl($"replacing attached mesh {mf.name}");
                            mf.mesh = customMeshes[name][parent][mf.name].mesh;
                        }
                    }
                    SkinnedMeshRenderer[] smrs = __instance.m_itemData.m_dropPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (SkinnedMeshRenderer smr in smrs)
                    {
                        string parent = smr.transform.parent.gameObject.name;
                        Dbgl($"got skinned mesh renderer, item name: {name}, obj: {parent}, smr: {smr.name}");
                        if (name == GetPrefabName(parent) && customMeshes[name].ContainsKey(smr.name) && customMeshes[name][smr.name].ContainsKey(smr.name))
                        {
                            Dbgl($"replacing item mesh {smr.name}");
                            smr.sharedMesh = customMeshes[name][smr.name][smr.name].mesh;
                        }
                        else if (customMeshes[name].ContainsKey(parent) && customMeshes[name][parent].ContainsKey(smr.name))
                        {
                            Dbgl($"replacing attached mesh {smr.name}");
                            smr.sharedMesh = customMeshes[name][parent][smr.name].mesh;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Piece), "Awake")]
        public static class Piece_Patch
        {
            public static void Postfix(Piece __instance)
            {
                string name = GetPrefabName(__instance.gameObject.name);
                MeshFilter[] mfs = __instance.gameObject.GetComponentsInChildren<MeshFilter>(true);

                if (customMeshes.ContainsKey(name))
                {
                    foreach (MeshFilter mf in mfs)
                    {
                        string parent = mf.transform.parent.gameObject.name;
                        //Dbgl($"got piece name: {name}, obj: {parent}, mf: {mf.name}");
                        if (customMeshes[name].ContainsKey(parent) && customMeshes[name][parent].ContainsKey(mf.name))
                        {
                            //Dbgl($"replacing mesh {mf.name}");
                            mf.mesh = customMeshes[name][parent][mf.name].mesh;
                        }
                    }
                }
            }
        }

        public static Transform RecursiveFind(Transform parent, string childName)
        {
            Transform child = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                child = parent.GetChild(i);
                if (child.name == childName)
                    break;
                child = RecursiveFind(child, childName);
                if (child != null)
                    break;
            }
            return child;
        }

        [HarmonyPatch(typeof(VisEquipment), "Awake")]
        public static class Awake_Patch
        {
            public static void Postfix(VisEquipment __instance)
            {
                Dbgl($"Vis Awake .");

                if (!__instance.m_isPlayer || __instance.m_models.Length == 0)
                    return;
                Dbgl($"Checking for custom player models.");



                if (customMeshes.ContainsKey("player"))
                {
                    Dbgl($"Has player.");
                    if (customMeshes["player"].ContainsKey("model"))
                    {
                        Dbgl($"Has player model.");
                        SkinnedMeshRenderer renderer = null;
                        if (customMeshes["player"]["model"].ContainsKey("0"))
                        {
                            Dbgl($"Replacing player model 0 with imported mesh.");
                            CustomMeshData custom = customMeshes["player"]["model"]["0"];
                            __instance.m_models[0].m_mesh = custom.mesh;
                            renderer = custom.renderer;
                        }
                        if (customMeshes["player"]["model"].ContainsKey("1"))
                        {
                            Dbgl($"Replacing player model 1 with imported mesh.");
                            CustomMeshData custom = customMeshes["player"]["model"]["1"];
                            __instance.m_models[1].m_mesh = custom.mesh;
                            renderer = custom.renderer;
                        }
                        if (renderer != null)
                        {
                            Transform armature = __instance.m_bodyModel.rootBone.parent;
                            Dbgl($"Setting up new bones array");
                            Transform[] newBones = new Transform[renderer.bones.Length];
                            for (int i = 0; i < newBones.Length; i++)
                            {
                                newBones[i] = RecursiveFind(armature, renderer.bones[i].name);
                                if (newBones[i] == null)
                                {
                                    Dbgl($"Could not find existing bone {renderer.bones[i].name}");
                                }
                            }
                            __instance.m_bodyModel.bones = newBones;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(InventoryGui), "SetupDragItem")]
        public static class SetupDragItem_Patch
        {
            public static void Postfix(ItemDrop.ItemData item)
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
        public static class Player_Awake_Patch
        {
            public static void Postfix(Player __instance)
            {
                return;
                Dbgl($"Player awake.");
                /*
                foreach (GameObject go in Traverse.Create(ZNetScene.instance).Field("m_namedPrefabs").GetValue<Dictionary<int, GameObject>>().Values)
                {
                    if (go.name.StartsWith("Skeleton"))
                    {
                        try
                        {
                            SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                            Dbgl($"smr is null: {smr == null}");
                            SkinnedMeshRenderer smr2 = __instance.transform.Find("Visual").Find("body").GetComponent<SkinnedMeshRenderer>();
                            Dbgl($"smr2 is null: {smr2 == null}");
                            if (smr != null && smr2 != null)
                            {
                                smr2 = smr;
                                return;

                            }

                        }
                        catch { }
                    }
                }
                */
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
                            newObject.transform.SetParent(parent);
                        }
                    }
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
