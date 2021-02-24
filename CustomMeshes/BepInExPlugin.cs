using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomMeshes
{
    [BepInPlugin("aedenthorn.CustomMeshes", "Custom Meshes", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        private static List<Mesh> customMeshes = new List<Mesh>();
        private static List<GameObject> customGameObjects = new List<GameObject>();
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> modEnabled;

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
            
            PreloadMeshes();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
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
                customMeshes.Add(mesh);
            }

        }
        [HarmonyPatch(typeof(VisEquipment), "Awake")]
        static class Awake_Patch
        {
            static void Postfix(VisEquipment __instance)
            {
                string path = Path.Combine($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}","CustomMeshes");

                foreach (string file in Directory.GetFiles(path, "*.fbx"))
                {
                    GameObject obj = MeshImporter.Load(file);
                    obj.name = "player_fbx";
                    string name = Path.GetFileNameWithoutExtension(file);
                    if(name == "player_model_1")
                    {
                        Dbgl($"Adding mesh from {file}.");
                        
                        Transform oldArmature = __instance.gameObject.transform.Find("Visual").Find("Armature");

                        GameObject newArmature = obj.transform.Find("Armature.003").gameObject;
                        newArmature.name = "Armature";

                        //Instantiate(newArmature, oldArmature.parent);
                        //Destroy(oldArmature.gameObject);


                        SkinnedMeshRenderer smr = __instance.m_models[0].GetComponentInChildren<SkinnedMeshRenderer>();
                        MeshFilter mr = obj.GetComponentInChildren<MeshFilter>();
                        smr.sharedMesh = mr.sharedMesh;
                    }
                }
            }
        }
    }
}
