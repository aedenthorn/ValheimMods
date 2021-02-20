using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CustomTextures
{
    [BepInPlugin("aedenthorn.CustomTextures", "Custom Textures", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<bool> modEnabled;
        public static Dictionary<string, Texture2D> customTextures = new Dictionary<string, Texture2D>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;

            LoadCustomTextures();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        private static void LoadCustomTextures()
        {
            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\CustomTextures";

            if (!Directory.Exists(path))
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
                return;
            }

            customTextures.Clear();

            Regex pattern = new Regex(@"^model_[0-9]+_texture\.png$");
            Regex pattern2 = new Regex(@"^model_[0-9]+_bump\.png$");

            foreach (string file in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(file);
                if (pattern.IsMatch(fileName) || pattern2.IsMatch(fileName))
                {
                    Dbgl($"adding {fileName} custom texture.");

                    string id = fileName.Substring(0, fileName.Length - 4);
                    Texture2D tex = new Texture2D(2, 2);
                    byte[] imageData = File.ReadAllBytes(file);
                    tex.LoadImage(imageData);
                    customTextures[id] = tex;
                }
            }
        }

        [HarmonyPatch(typeof(VisEquipment), "Awake")]
        static class VisEquipment_UpdateMaseModel_Patch
        {
            static void Postfix(VisEquipment __instance)
            {
                for(int i = 0; i < __instance.m_models.Length; i++)
                {
                    if (customTextures.ContainsKey($"model_{i}_texture"))
                    {
                        __instance.m_models[i].m_baseMaterial.SetTexture("_MainTex", customTextures[$"model_{i}_texture"]);
                        Dbgl($"set model_{i}_texture custom texture.");
                    }
                    if (customTextures.ContainsKey($"model_{i}_bump"))
                    {
                        __instance.m_models[i].m_baseMaterial.SetTexture("_SkinBumpMap", customTextures[$"model_{i}_bump"]);
                        Dbgl($"set model_{i}_bump custom skin bump map.");
                    }
                }
            }
        }
    }
}
