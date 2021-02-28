using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomMeshes
{
    [BepInPlugin("aedenthorn.CustomMeshes", "Custom Meshes", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        private static Dictionary<string, Mesh> customMeshes = new Dictionary<string, Mesh>();
        private static Dictionary<string, AssetBundle> customAssetBundles = new Dictionary<string, AssetBundle>();
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

            PreloadMeshes();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            return;


        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                return;
                Dbgl($"Pressed U.");

                Dbgl($"changing.");

                Player player = Player.m_localPlayer;

                Dbgl($"{player.gameObject.name} player_model_{player.GetPlayerModel()}");

                if (customAssetBundles.ContainsKey($"player_model_{player.GetPlayerModel()}"))
                {
                    Transform visual = player.gameObject.transform.Find("Visual");
                    Transform armature = visual.Find("Armature");
                    Destroy(armature.gameObject);

                    AssetBundle ab = customAssetBundles[$"player_model_{player.GetPlayerModel()}"];
                    GameObject newPlayer = ab.LoadAsset<GameObject>("Player");
                    GameObject newVisual = newPlayer.transform.Find("Visual").gameObject;
                    GameObject newArmature = newVisual.transform.Find("Armature").gameObject;

                    newArmature.name = "Armature";
                    Instantiate(newArmature, visual);

                    Dbgl("Replaced armature");
                }

                return;

                /*
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
                */
            }
        }

        private static void PreloadMeshes()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomMeshes");

            foreach (string file in Directory.GetFiles(path))
            {
                Dbgl($"Importing {file} {Path.GetFileNameWithoutExtension(file)} {Path.GetFileName(file)} {Path.GetExtension(file).ToLower()}");
                string name = Path.GetFileNameWithoutExtension(file);
                if(name == Path.GetFileName(file)) { 
                    AssetBundle ab = AssetBundle.LoadFromFile(file);
                    Mesh mesh = ab.LoadAsset<Mesh>("body");
                    customMeshes.Add(name, mesh);
                    customAssetBundles.Add(name, ab);

                    Dbgl($"Imported {name} as asset bundle");
                }
                else if (Path.GetExtension(file).ToLower() == ".fbx")
                {
                    GameObject obj = MeshImporter.Load(file);
                    MeshFilter[] mrs = obj.GetComponentsInChildren<MeshFilter>();
                    MeshFilter mr = mrs[0];
                    customMeshes.Add(name, mr.mesh);
                    Dbgl($"Imported {name} as game object");
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

        [HarmonyPatch(typeof(Piece), "Awake")]
        static class Piece_Patch
        {
            static void Postfix(Piece __instance) 
            {
                string name = GetPrefabName(__instance.gameObject.name);
                Dbgl($"piece name: {name}");
                if (customMeshes.ContainsKey(name))
                {
                    MeshFilter[] mrs = __instance.gameObject.GetComponentsInChildren<MeshFilter>();
                    foreach (MeshFilter mr in mrs)
                        mr.mesh = customMeshes[name];
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

                if (customMeshes.ContainsKey("player_model_0"))
                {
                    Dbgl($"Replacing player model 0 with imported mesh. {__instance.m_models.Length}");
                    __instance.m_models[0].m_mesh = customMeshes["player_model_0"];

                }
                if (customMeshes.ContainsKey("player_model_1"))
                {
                    Dbgl($"Replacing player model 1 with imported mesh. {__instance.m_models.Length}");
                    __instance.m_models[1].m_mesh = customMeshes["player_model_1"];

                }

            }
        }

    }
}
