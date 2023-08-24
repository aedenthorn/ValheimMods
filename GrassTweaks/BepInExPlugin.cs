using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrassTweaks
{
    [BepInPlugin("aedenthorn.GrassTweaks", "Grass Tweaks", "0.4.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> useLod;
        public static ConfigEntry<bool> useXZLodDistance;
        private static ConfigEntry<float> lodMinDistanceMult;
        private static ConfigEntry<float> lodMaxDistanceMult;
        private static ConfigEntry<float> scaleMinMult;
        private static ConfigEntry<float> scaleMaxMult;
        private static ConfigEntry<float> amountMult;
        private static ConfigEntry<float> clutterDistance;
        private static ConfigEntry<float> grassPatchSize;
        private static ConfigEntry<float> playerPushFade;
        private static ConfigEntry<ShadowCastingMode> shadowCastingMode;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 306, "Nexus mod ID for updates");

            useLod = Config.Bind<bool>("Settings", "UseLod", true, "Use LOD");
            useXZLodDistance = Config.Bind<bool>("Settings", "UseXZLodDistance", true, "Use XZ LOD Distance");
            lodMinDistanceMult = Config.Bind<float>("Settings", "LodMinDistanceMult", 1f, "Min LOD distance multiplier");
            lodMaxDistanceMult = Config.Bind<float>("Settings", "LodMaxDistanceMult", 1f, "Max LOD distance multiplier");
            scaleMinMult = Config.Bind<float>("Settings", "ScaleMinMult", 1f, "Clutter scale minimum multiplier");
            scaleMaxMult = Config.Bind<float>("Settings", "ScaleMaxMult", 1f, "Clutter scale maximum multiplier");
            amountMult = Config.Bind<float>("Settings", "AmountMult", 1f, "Clutter amount multiplier");
            clutterDistance = Config.Bind<float>("Settings", "ClutterDistance", 40f, "Clutter distance");
            grassPatchSize = Config.Bind<float>("Settings", "GrassPatchSize", 8f, "Grass patch size");
            playerPushFade = Config.Bind<float>("Settings", "PlayerPushFade", 0.05f, "Player push fade");
            shadowCastingMode = Config.Bind<ShadowCastingMode>("Settings", "ShadowCastingMode", ShadowCastingMode.On, "Shadow Casting Mode");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        [HarmonyPatch(typeof(ClutterSystem), "Awake")]
        static class ClutterSystem_Awake_Patch
        {
            static void Prefix(ClutterSystem __instance)
            {
                if (!modEnabled.Value)
                    return;

                __instance.m_amountScale *= amountMult.Value;
                __instance.m_distance = clutterDistance.Value;
                __instance.m_grassPatchSize = grassPatchSize.Value;
                __instance.m_playerPushFade = playerPushFade.Value;

                for (int i = 0; i < __instance.m_clutter.Count; i++)
                {
                    __instance.m_clutter[i].m_scaleMin *= scaleMinMult.Value;
                    __instance.m_clutter[i].m_scaleMax *= scaleMaxMult.Value;
                }
            }
        }
        [HarmonyPatch(typeof(MonoBehaviour), MethodType.Constructor, new Type[] { })]
        static class MonoBehaviour_Patch
        {
            static void Postfix(MonoBehaviour __instance)
            {
                if (!modEnabled.Value || !(__instance is InstanceRenderer))
                    return;
                var i = __instance as InstanceRenderer;
                i.m_lodMinDistance *= lodMinDistanceMult.Value;
                i.m_lodMaxDistance *= lodMaxDistanceMult.Value;
                i.m_shadowCasting = shadowCastingMode.Value;
                i.m_useLod = useLod.Value;
                i.m_useXZLodDistance = useXZLodDistance.Value;
            }   
        }
        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("grasstweaks reset"))
                {

                    ClutterSystem.instance.m_amountScale /= amountMult.Value;
                    for (int i = 0; i < ClutterSystem.instance.m_clutter.Count; i++)
                    {
                        ClutterSystem.instance.m_clutter[i].m_scaleMin /= scaleMinMult.Value;
                        ClutterSystem.instance.m_clutter[i].m_scaleMax /= scaleMaxMult.Value;
                    }

                    context.Config.Reload();
                    context.Config.Save();

                    ClutterSystem.instance.m_amountScale *= amountMult.Value;
                    for (int i = 0; i < ClutterSystem.instance.m_clutter.Count; i++)
                    {
                        ClutterSystem.instance.m_clutter[i].m_scaleMin *= scaleMinMult.Value;
                        ClutterSystem.instance.m_clutter[i].m_scaleMax *= scaleMaxMult.Value;
                    }
                    ClutterSystem.instance.m_distance = clutterDistance.Value;
                    ClutterSystem.instance.m_grassPatchSize = grassPatchSize.Value;
                    ClutterSystem.instance.m_playerPushFade = playerPushFade.Value;


                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Grass Tweaks config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}