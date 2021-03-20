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
        private static readonly bool isDebug = true;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<string> femaleModelName;
        public static ConfigEntry<string> maleModelName;

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
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
        static class VisEquipment_Awake_Patch
        {
            static void Prefix(VisEquipment __instance)
            {
                if (!__instance.m_isPlayer || __instance.m_models.Length == 0 || !ZNetScene.instance)
                    return;

                if(femaleModelName.Value.Length > 0)
                {
                    GameObject go = ZNetScene.instance.GetPrefab(femaleModelName.Value);

                    SkinnedMeshRenderer[] smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();

                    if(smrs.Length == 1)
                    {
                        Dbgl($"smr name {smrs[0].name}.");
                        Mesh mesh = smrs[0].sharedMesh;
                        __instance.m_models[1].m_mesh = mesh;

                    }
                    else
                    {

                        foreach (SkinnedMeshRenderer smr in smrs)
                        {
                            Dbgl($"switching female model to {smr.name}.");
                            if (smr.name.ToLower() == femaleModelName.Value.ToLower())
                            {
                                Dbgl("switching female model");
                                Mesh mesh = smr.sharedMesh;
                                __instance.m_models[1].m_mesh = mesh;
                                break;
                            }
                        }
                    }
                }
                if (maleModelName.Value.Length > 0)
                {
                    GameObject go = ZNetScene.instance.GetPrefab(maleModelName.Value);

                    SkinnedMeshRenderer[] smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
                    if (smrs.Length == 1)
                    {
                        Dbgl($"switching male model to {smrs[0].name}.");
                        Mesh mesh = smrs[0].sharedMesh;
                        __instance.m_models[0].m_mesh = mesh;
                    }
                    else
                    {
                        foreach (SkinnedMeshRenderer smr in smrs)
                        {
                            Dbgl($"smr name {smr.name}.");
                            if (smr.name.ToLower() == maleModelName.Value.ToLower())
                            {
                                Dbgl("switching male model");
                                Mesh mesh = smr.sharedMesh;
                                __instance.m_models[0].m_mesh = mesh;
                                break;
                            }
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
