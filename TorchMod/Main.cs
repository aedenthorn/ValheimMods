using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace TorchMod
{
    public class Main
    {
        private static readonly bool isDebug = true;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(Main).Namespace + " " : "") + str);
        }

        public static Settings settings { get; private set; }
        public static bool enabled;
        private static void Load(UnityModManager.ModEntry modEntry)
        {
            settings = Settings.Load<Settings>(modEntry);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnToggle = OnToggle;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return;
        }

        // Called when the mod is turned to on/off.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            enabled = value;
            return true; // Permit or not.
        }
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            //GUILayout.Label(string.Format("Maximum Jumps: <b>{0}</b>", settings.MaxJumps), new GUILayoutOption[0]);
            //settings.MaxJumps = (int)GUILayout.HorizontalSlider(settings.MaxJumps, -1f, 10f, new GUILayoutOption[0]);
        }

        [HarmonyPatch(typeof(LightLod), "UpdateLoop")]
        static class UpdateLoop_Patch
        {

            static void Postfix(ref Light ___m_light)
            {
                if (!enabled || ___m_light == null)
                    return;
                ___m_light.intensity = 10f;
            }
        }
        [HarmonyPatch(typeof(VisEquipment), "EnableEquipedEffects")]
        static class EnableEquipedEffects_Patch
        {

            static void Postfix(GameObject instance)
            {
                if (!enabled || instance == null)
                    return;
                Dbgl($"attached item {instance.name}");
                LightLod lod = instance.GetComponentInChildren<LightLod>();
                if(lod != null)
                {
                    Dbgl("Got lightlod for attached item");
                    Light lodlight = (Light)typeof(LightLod).GetField("m_light", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(lod);
                    if(lodlight != null)
                    {
                        Dbgl("Got light for lod");
                        lodlight.intensity = 10f;
                    }

                }
                Light light = instance.GetComponentInChildren<Light>();
                if(light != null)
                {
                    Dbgl("Got light for attached item");
                    light.intensity = 10f;
                }
            }
        }

    }
}
