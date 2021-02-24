using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SuperJump
{
    [BepInPlugin("aedenthorn.SuperJump", "Super Jump", "0.3.1")]
    public class SuperJump: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<int> maxJumps;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> fallDamageMult;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(SuperJump).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            maxJumps = Config.Bind<int>("General", "MaxJumps", 2, "The maximum number of in-air jumps (-1 for infinite)");
            modEnabled = Config.Bind<bool>("General", "enabled", true, "Enable this mod");
            fallDamageMult = Config.Bind<float>("General", "FallDamageMult", 1f, "Fall damage multiplier (set to 0 to turn off fall damage)");
            nexusID = Config.Bind<int>("General", "NexusID", 6, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static int JumpNumber { get; private set; }


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
