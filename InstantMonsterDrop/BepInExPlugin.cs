using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace InstantMonsterDrop
{
    [BepInPlugin("aedenthorn.InstantMonsterDrop", "Instant Monster Drop", "0.6.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> dropDelay;
        public static ConfigEntry<float> destroyDelay;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug");
            dropDelay = Config.Bind<float>("General", "DropDelay", 0.01f, "Delay before dropping loot");
            destroyDelay = Config.Bind<float>("General", "DestroyDelay", 0.05f, "Delay before destroying ragdoll");
            nexusID = Config.Bind<int>("General", "NexusID", 164, "Mod ID on the Nexus for update checks");
            nexusID.Value = 164;
            Config.Save(); 
            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Ragdoll), "Awake")]
        public static class Ragdoll_Awake_Patch
        {
            public static void Postfix(Ragdoll __instance, ZNetView ___m_nview, EffectList ___m_removeEffect)
            {
                if (!ZNetScene.instance)
                    return;
                Dbgl($"Changing death time from {__instance.m_ttl} to {destroyDelay.Value}, drop time from {__instance.m_ttl} to {dropDelay.Value}");
                context.StartCoroutine(DropNow(__instance, ___m_nview, ___m_removeEffect));
            }
        }
        
        [HarmonyPatch(typeof(Ragdoll), "DestroyNow")]
        public static class Ragdoll_DestroyNow_Patch
        {
            public static bool Prefix(Ragdoll __instance)
            {
                //Dbgl($"cancelling destroynow");
                return !modEnabled.Value;
            }
        }

        public static IEnumerator DropNow(Ragdoll ragdoll, ZNetView nview, EffectList removeEffect)
        {
            if(dropDelay.Value < 0)
            {
                context.StartCoroutine(DestroyNow(ragdoll, nview, removeEffect));
                yield break;
            }

            Dbgl($"delaying dropping loot");
            yield return new WaitForSeconds(dropDelay.Value);

            if (!modEnabled.Value)
                yield break;

            if (!nview.IsValid() || !nview.IsOwner())
            {
                yield break;
            }
            Dbgl($"dropping loot");
            Vector3 averageBodyPosition = ragdoll.GetAverageBodyPosition();
            Traverse.Create(ragdoll).Method("SpawnLoot", new object[] { averageBodyPosition }).GetValue();
            context.StartCoroutine(DestroyNow(ragdoll, nview, removeEffect));
        }

        public static IEnumerator DestroyNow(Ragdoll ragdoll, ZNetView nview, EffectList m_removeEffect)
        {
            Dbgl($"delaying destroying ragdoll");
            yield return new WaitForSeconds(Mathf.Max(destroyDelay.Value - dropDelay.Value, 0));

            if (!modEnabled.Value)
                yield break;

            if (!nview.IsValid() || !nview.IsOwner())
            {
                yield break;
            }
            Dbgl($"destroying ragdoll");
            Vector3 averageBodyPosition = ragdoll.GetAverageBodyPosition();
            m_removeEffect.Create(averageBodyPosition, Quaternion.identity, null, 1f, -1);
            ZNetScene.instance.Destroy(ragdoll.gameObject);
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
