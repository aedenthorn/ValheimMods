using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SleepWithoutSpawn
{
    [BepInPlugin("aedenthorn.SleepWithoutSpawn", "Sleep Without Spawn", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> unclaimedOnly;
        public static ConfigEntry<bool> allowDaySleep;
        public static ConfigEntry<string> modKey;

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
            unclaimedOnly = Config.Bind<bool>("General", "UnclaimedOnly", false, "Only sleep on unclaimed beds");
            allowDaySleep = Config.Bind<bool>("General", "AllowDaySleep", false, "Allow sleeping during the day");
            modKey = Config.Bind<string>("General", "ModKey", "left alt", "Modifier key to sleep without setting spawn point. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            nexusID = Config.Bind<int>("General", "NexusID", 261, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(Bed), "GetHoverText")]
        public static class Bed_GetHoverText_Patch
        {
            public static void Postfix(Bed __instance, ref string __result, ZNetView ___m_nview)
            {
                if ((unclaimedOnly.Value && ___m_nview.GetZDO().GetLong("owner", 0L) != 0) || Traverse.Create(__instance).Method("IsCurrent").GetValue<bool>())
                {
                    return;
                }

                __result += Localization.instance.Localize($"\n[{modKey.Value}+<color=yellow><b>$KEY_Use</b></color>] $piece_bed_sleep");
            }
        }
        
        
        [HarmonyPatch(typeof(Bed), "Interact")]
        public static class Bed_Interact_Patch
        {
            public static bool Prefix(Bed __instance, Humanoid human, bool repeat, ref bool __result, ZNetView ___m_nview)
            {
                if (((!allowDaySleep.Value || EnvMan.instance.IsAfternoon() || EnvMan.instance.IsNight()) && Traverse.Create(__instance).Method("IsCurrent").GetValue<bool>()) || repeat || !AedenthornUtils.CheckKeyHeld(modKey.Value) || (unclaimedOnly.Value && ___m_nview.GetZDO().GetLong("owner", 0L) != 0))
                {
                    return true;
                }
                Dbgl($"trying to sleep on bed");

                Player player = human as Player;

                if (!allowDaySleep.Value && !EnvMan.instance.IsAfternoon() && !EnvMan.instance.IsNight())
                {
                    human.Message(MessageHud.MessageType.Center, "$msg_cantsleep", 0, null);
                    __result = false;
                    return false;
                }
                if (!Traverse.Create(__instance).Method("CheckEnemies", new object[] { player }).GetValue<bool>())
                {
                    __result = false;
                    return false;
                }
                if (!Traverse.Create(__instance).Method("CheckExposure", new object[] { player }).GetValue<bool>())
                {
                    __result = false;
                    return false;
                }
                if (!Traverse.Create(__instance).Method("CheckFire", new object[] { player }).GetValue<bool>())
                {
                    __result = false;
                    return false;
                }
                if (!Traverse.Create(__instance).Method("CheckWet", new object[] { player }).GetValue<bool>())
                {
                    __result = false;
                    return false;
                }
                human.AttachStart(__instance.m_spawnPoint, __instance.gameObject, true, true, false, "attach_bed", new Vector3(0f, 0.5f, 0f));
                __result = false;
                return false;
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
