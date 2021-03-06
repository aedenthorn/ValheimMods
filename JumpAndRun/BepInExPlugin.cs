using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SuperJump
{
    [BepInPlugin("aedenthorn.SuperJump", "Super Jump", "0.3.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<int> maxJumps;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> fallDamageMult;
        public static ConfigEntry<float> jumpVelocityMult;
        public static ConfigEntry<float> runSpeedMult;
        public static ConfigEntry<float> walkSpeedMult;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin ).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            maxJumps = Config.Bind<int>("Jump", "MaxJumps", 2, "The maximum number of sequential jumps (-1 for infinite)");
            jumpVelocityMult = Config.Bind<float>("Jump", "JumpVelocityMult", 1f, "Jump velocity multiplier");
            fallDamageMult = Config.Bind<float>("Jump", "FallDamageMult", 1f, "Fall damage multiplier (set to 0 to turn off fall damage)");
            runSpeedMult = Config.Bind<float>("Run", "RunSpeedMult", 1f, "Run speed multiplier");
            walkSpeedMult = Config.Bind<float>("Run", "WalkSpeedMult", 1f, "Walk speed multiplier");
            modEnabled = Config.Bind<bool>("General", "enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 6, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static int JumpNumber { get; private set; }


        [HarmonyPatch(typeof(Player), "GetJogSpeedFactor")]
        static class GetJogSpeedFactor_Patch
        {
            static void Postfix(ref float __result)
            {
                if (modEnabled.Value)
                {
                    __result *= walkSpeedMult.Value;
                }
            }
        }
        [HarmonyPatch(typeof(Player), "GetRunSpeedFactor")]
        static class GetRunSpeedFactor_Patch
        {
            static void Postfix(ref float __result)
            {
                if (modEnabled.Value)
                {
                    __result *= runSpeedMult.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
        static class UpdateGroundContact_Patch
        {

            static void Prefix(Character __instance, ref float ___m_maxAirAltitude)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    ___m_maxAirAltitude = __instance.transform.position.y + (___m_maxAirAltitude - __instance.transform.position.y) * fallDamageMult.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        static class Jump_Patch
        {

            static void Prefix(Character __instance, ref float ___m_lastGroundTouch, ref float ___m_maxAirAltitude)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    if (JumpNumber > 0 && __instance.IsOnGround())
                    {
                        JumpNumber = 0;
                    }

                    if (maxJumps.Value < 0 || JumpNumber < maxJumps.Value)
                    {
                        ___m_maxAirAltitude = __instance.transform.position.y;
                        ___m_lastGroundTouch = 0.1f;
                        JumpNumber++;
                    }
                }
            }
        }
    }
}
