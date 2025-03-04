using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace PlayerModelSwitch
{
    [BepInPlugin("aedenthorn.PlayerModelSwitch", "Player Model Switch", "0.1.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<string> femaleModelName;
        public static ConfigEntry<string> maleModelName;

        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 592, "Nexus mod ID for updates");

            femaleModelName = Config.Bind<string>("General", "FemaleModelName", "Skeleton", "Switch the female player model to this. Should be a Humanoid (e.g. Skeleton, etc.).");
            maleModelName = Config.Bind<string>("General", "MaleModelName", "Skeleton", "Switch the male player model to this. Should be a Humanoid (e.g. Skeleton, etc.).");

            if (!modEnabled.Value) 
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(VisEquipment), "Awake")]
        public static class VisEquipment_Awake_Patch
        {
            public static void Prefix(VisEquipment __instance)
            {
                if (!__instance.m_isPlayer || __instance.m_models.Length == 0 || !ZNetScene.instance)
                    return;

                if(femaleModelName.Value.Length > 0)
                {
                    ChangeModel(ref __instance, femaleModelName.Value, 1);
                }
                if (maleModelName.Value.Length > 0)
                {
                    ChangeModel(ref __instance, maleModelName.Value, 0);
                }
            }

            public static void ChangeModel(ref VisEquipment vis, string value, int which)
            {
                GameObject go = ZNetScene.instance.GetPrefab(value);

                if (go == null)
                {
                    Dbgl($"couldn't find object {value}.");
                    return;
                }

                SkinnedMeshRenderer[] smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();

                if (smrs.Length == 1)
                {
                    Dbgl($"switching model {which} to {smrs[0].name}.");
                    Dbgl($"smr name {smrs[0].name}.");
                    vis.m_models[which].m_mesh = smrs[0].sharedMesh;
                    return;
                }
                else if (smrs.Length > 1)
                {
                    bool switched = false;
                    foreach (SkinnedMeshRenderer smr in smrs)
                    {
                        if (smr.name.ToLower() == value.ToLower())
                        {
                            switched = true;
                            Dbgl($"switching model {which} model");
                            Mesh mesh = smr.sharedMesh;
                            vis.m_models[which].m_mesh = mesh;
                            return;
                        }
                    }
                    if (!switched)
                    {
                        Dbgl($"switching model {which} to {smrs[0].name}.");
                        vis.m_models[which].m_mesh = smrs[0].sharedMesh;
                        return;
                    }
                }
                Dbgl($"No model {value} found for {which}.");
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
                if (text.ToLower().Equals("playermodelswitch reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "player model switch config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
