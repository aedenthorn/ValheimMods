using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace PreventBabySlaughter
{
    [BepInPlugin("aedenthorn.PreventBabySlaughter", "Prevent Baby Slaughter", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<string> modKey;
        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> holdToToggle;
        public static ConfigEntry<bool> reverseHoldToToggle;
        public static ConfigEntry<bool> currentlyProtected;
        public static ConfigEntry<int> nexusID;

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
            nexusID = Config.Bind<int>("General", "NexusID", 1573, "Nexus mod ID for updates");

            toggleKey = Config.Bind<string>("Options", "HotKey", "left shift", "The hotkey used to enable baby protection. Use syntax at https://docs.unity3d.com/Manual/class-InputManager.html");
            holdToToggle = Config.Bind<bool>("Options", "HoldToToggle", true, "If true, you must hold down the HotKey to enable protection. Otherwise, pressing the HotKey once will toggle whether babies are protected");
            reverseHoldToToggle = Config.Bind<bool>("Options", "ReverseHoldToToggle", true, "If true, holding down the HotKey will remove protection, instead of granting it.");
            currentlyProtected = Config.Bind<bool>("ZZAuto", "CurrentlyProtected", true, "Used if HoldToToggle is set to false, will change when pressing HotKey");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses(true))
                return;
            if (!holdToToggle.Value && AedenthornUtils.CheckKeyDown(toggleKey.Value))
            {
                currentlyProtected.Value = !currentlyProtected.Value;
                Dbgl($"Protecting babies: {currentlyProtected.Value}");
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        public static class Character_Damage_Patch
        {
            public static bool Prefix(Character __instance, HitData hit)
            {
                if (!modEnabled.Value || (!holdToToggle.Value && !currentlyProtected.Value) || (holdToToggle.Value && AedenthornUtils.CheckKeyHeld(toggleKey.Value) == reverseHoldToToggle.Value) || !(__instance is Character) || !__instance.IsTamed() || !hit.GetAttacker().IsPlayer() || !__instance.GetComponent<Growup>() || __instance.GetBaseAI().GetTimeSinceSpawned().TotalSeconds > __instance.GetComponent<Growup>().m_growTime)
                    return true;
                Dbgl($"Protecting baby {__instance.name} ");
                return false;
            }
        }

    }
}
