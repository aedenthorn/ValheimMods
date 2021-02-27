using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace CustomMeshes
{
    [BepInPlugin("aedenthorn.CustomMeshes", "Custom Meshes", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        private static Dictionary<string, Mesh> customMeshes = new Dictionary<string, Mesh>();
        private static List<GameObject> customGameObjects = new List<GameObject>();
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> modEnabled;

        public static Mesh customMesh { get; set; }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;
            return;
            PreloadMeshes();
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomMeshes", "player_model_0.obj");
            GameObject obj = OBJLoader.LoadOBJFile(path);

            //GameObject obj = MeshImporter.Load(path);
            MeshFilter[] mrs = obj.GetComponentsInChildren<MeshFilter>();
            MeshFilter mr = mrs[0];
            customMesh = mr.mesh;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            return;


        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                Dbgl($"changing.");


                Transform visual = Player.m_localPlayer.gameObject.transform.Find("Visual");
                Dbgl($"1.");
                Transform body = visual.Find("body");
                Dbgl($"2.");
                SkinnedMeshRenderer smr = body.GetComponent<SkinnedMeshRenderer>();
                Dbgl($"3.");
                //smr.sharedMesh = customMeshes["player_model_0"];
                List<string> output = new List<string>();

                foreach (Vector3 v in smr.sharedMesh.vertices)
                    output.Add($"{smr.sharedMesh.name}: {v.x},{v.y},{v.z}");
                Dbgl($"4.");

                foreach (Vector3 v in customMesh.vertices)
                    output.Add($"custom mesh: {v.x}");
                Dbgl($"5.");
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomMeshes");

                File.WriteAllLines(Path.Combine(path, "dump.txt"), output);
            }
        }

        private static void PreloadMeshes()
        {
            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\CustomMeshes";

            if (!Directory.Exists(path))
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
                return;
            }

            customMeshes.Clear();

            foreach (string file in Directory.GetFiles(path, "*.obj"))
            {
                Dbgl($"Adding mesh {file}.");
                Mesh mesh = new ObjImporter().ImportFile(file);
                customMeshes.Add(Path.GetFileNameWithoutExtension(file), mesh);
            }

        }
        //[HarmonyPatch(typeof(Player), "Awake")]
        static class Player_FixedUpdate_Patch
        {
            public static bool done = false;
            static void Postfix(Player __instance) 
            {

                //smr.sharedMesh = customMesh;
            }
        }

        [HarmonyPatch(typeof(VisEquipment), "Awake")]
        static class Awake_Patch
        {
            public static bool done = false;
            static void Postfix(VisEquipment __instance)
            {
                Dbgl($"Vis Awake .");
                List<string> output = new List<string>();
                foreach (Vector3 v in __instance.m_models[0].m_mesh.vertices)
                    output.Add($"{__instance.m_models[0].m_mesh.name}: ({v.x},{v.y},{v.z})");
                Dbgl($"4.");

                foreach (Vector3 v in customMesh.vertices)
                    output.Add($"custom mesh: {v}");
                Dbgl($"5.");
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomMeshes");

                File.WriteAllLines(Path.Combine(path, "dump.txt"), output);
/*
                __instance.m_models[0].m_mesh.vertices = customMesh.vertices;
                __instance.m_models[0].m_mesh.RecalculateNormals();
                return;

                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"CustomMeshes");

                foreach (string file in Directory.GetFiles(path, "*.fbx"))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if(name == "player_model_0")
                    {
                        GameObject obj = MeshImporter.Load(file);
                        obj.name = "player_fbx";
                        Dbgl($"Adding mesh from {file}.");

                        MeshFilter[] mrs = obj.GetComponentsInChildren<MeshFilter>();
                        MeshFilter mr = mrs[0];

                        List<string> output = new List<string>();

                        foreach (Vector3 v in __instance.m_models[0].m_mesh.vertices)
                            output.Add($"vanilla: {v}");

                        foreach (Vector3 v in mr.mesh.vertices)
                            output.Add($"imported: {v}");

                        //File.WriteAllLines(Path.Combine(path, "dump.txt"), output);

                        __instance.m_models[0].m_mesh = mr.mesh;
                        __instance.m_models[0].m_mesh.vertices = mr.mesh.vertices;
                        //__instance.m_models[0].m_mesh.RecalculateNormals();
                        continue;
                    }
                    if(name == "player_model_1")
                    {
                        GameObject obj = MeshImporter.Load(file);
                        obj.name = "player_fbx";
                        Dbgl($"Adding mesh from {file}.");

                        MeshFilter[] mrs = obj.GetComponentsInChildren<MeshFilter>();
                        MeshFilter mr = mrs[0];
                        __instance.m_models[1].m_mesh = mr.mesh;

                        //as__instance.m_models[1].m_mesh.vertices = mr.mesh.vertices;
                        //__instance.m_models[1].m_mesh.RecalculateNormals();
                        continue;
                    }
                }
*/
            }
        }
        //[HarmonyPatch(typeof(VisEquipment), "UpdateBaseModel")]
        static class UpdateBaseModel_Patch
        {
            private static bool done;

            static bool Prefix(VisEquipment __instance, ref int ___m_currentModelIndex, int ___m_modelIndex, ZNetView ___m_nview)
            {
                return true;
                if (__instance.m_models.Length == 0)
                {
                    return false;
                }
                int num = ___m_modelIndex;
                if (___m_nview.GetZDO() != null)
                {
                    num = ___m_nview.GetZDO().GetInt("ModelIndex", 0);
                }
                if (___m_currentModelIndex == num || __instance.m_bodyModel.sharedMesh == __instance.m_models[num].m_mesh)
                    return false;

                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"CustomMeshes");

                foreach (string file in Directory.GetFiles(path, "*.fbx"))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if(num == 0 && name == "player_model_0")
                    {
                        GameObject obj = MeshImporter.Load(file);
                        obj.name = "player_fbx";
                        Dbgl($"Adding mesh from {file}.");

                        MeshFilter[] mrs = obj.GetComponentsInChildren<MeshFilter>();
                        MeshFilter mr = mrs[0];

                        ___m_currentModelIndex = num;
                        __instance.m_bodyModel.sharedMesh.vertices = mr.mesh.vertices;
                        __instance.m_bodyModel.sharedMesh.RecalculateNormals();
                        __instance.m_bodyModel.materials[0].SetTexture("_MainTex", __instance.m_models[num].m_baseMaterial.GetTexture("_MainTex"));
                        __instance.m_bodyModel.materials[0].SetTexture("_SkinBumpMap", __instance.m_models[num].m_baseMaterial.GetTexture("_SkinBumpMap"));
                        return false;


                        List<string> output = new List<string>();

                        foreach (Vector3 v in __instance.m_models[0].m_mesh.vertices)
                            output.Add($"vanilla: {v}");

                        foreach (Vector3 v in mr.mesh.vertices)
                            output.Add($"imported: {v}");

                        File.WriteAllLines(Path.Combine(path, "dump.txt"), output);

                        __instance.m_models[0].m_mesh.vertices = mr.mesh.vertices;
                        __instance.m_models[0].m_mesh.RecalculateNormals();
                        return false;
                    }
                    if(num == 110000 && name == "player_model_1")
                    {
                        GameObject obj = MeshImporter.Load(file);
                        obj.name = "player_fbx";
                        Dbgl($"Adding mesh from {file}.");

                        MeshFilter[] mrs = obj.GetComponentsInChildren<MeshFilter>();
                        MeshFilter mr = mrs[0];

                        ___m_currentModelIndex = num;
                        __instance.m_bodyModel.sharedMesh = mr.mesh;
                        __instance.m_bodyModel.materials[0].SetTexture("_MainTex", __instance.m_models[num].m_baseMaterial.GetTexture("_MainTex"));
                        __instance.m_bodyModel.materials[0].SetTexture("_SkinBumpMap", __instance.m_models[num].m_baseMaterial.GetTexture("_SkinBumpMap"));
                        return false;

                        __instance.m_models[1].m_mesh.vertices = mr.mesh.vertices;
                        __instance.m_models[1].m_mesh.RecalculateNormals();
                        return false;
                    }
                }
                return true;
            }
            public static int ticks = 0;
            static void Postfix(VisEquipment __instance, ref int ___m_currentModelIndex, int ___m_modelIndex, ZNetView ___m_nview)
            {
                return;
                Dbgl($"Update.");
                if (ticks++ == 60)
                {
                    Dbgl($"distorting.");
                    for (int i = 0; i < __instance.m_bodyModel.sharedMesh.vertices.Length; i++)
                        __instance.m_bodyModel.sharedMesh.vertices[i] *= Random.Range(0.1f,2.0f);
                    ticks = 0;
                }

                return;

                return;
                if (done)
                    return;
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomMeshes");

                done = true;
                List<string> output = new List<string>();

                foreach (Vector3 v in __instance.m_bodyModel.sharedMesh.vertices)
                    output.Add($"vanilla: {v}");
                
                File.WriteAllLines(Path.Combine(path, "dump.txt"), output);
            }
        }
    }
}
