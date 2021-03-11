using BepInEx;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomTextures
{
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        private static void LoadCustomTextures()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"CustomTextures");

            if (!Directory.Exists(path))
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
                return;
            }
            texturesToLoad.Clear();

            foreach (string file in Directory.GetFiles(path, "*.png", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                string id = Path.GetFileNameWithoutExtension(fileName);
                if (!fileWriteTimes.ContainsKey(id) || (cachedTextures.ContainsKey(id) && !DateTime.Equals(File.GetLastWriteTimeUtc(file), fileWriteTimes[id])))
                {
                    cachedTextures.Remove(id);
                    texturesToLoad.Add(id);
                    fileWriteTimes[id] = File.GetLastWriteTimeUtc(file);
                    Dbgl($"adding new {fileName} custom texture.");
                }
                customTextures[id] = file;
            }
        }

        private static bool HasCustomTexture(string id)
        {
            return (customTextures.ContainsKey(id) || customTextures.Any(p => p.Key.StartsWith(id)));
        }
        private static bool ShouldLoadCustomTexture(string id)
        {
            return (texturesToLoad.Contains(id) || texturesToLoad.Any(p => p.StartsWith(id)));
        }

        private static void LoadSceneTextures(GameObject[] gos)
        {

            Dbgl($"loading {gos.Length} scene textures");

            foreach (GameObject gameObject in gos)
            {
                
                if (gameObject.name == "_NetSceneRoot")
                    continue;

                LoadOneTexture(gameObject, gameObject.name, "object");

            }

        }
    }
}
