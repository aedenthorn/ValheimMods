using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Durability
{
    [BepInPlugin("aedenthorn.SuperJump", "Super Jump", "0.2.0")]
    public class SuperJump: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<int> maxJumps;
        public static ConfigEntry<bool> modEnabled;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(SuperJump).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            maxJumps = Config.Bind<int>("General", "MaxJumps", 2, "The maximum number of in-air jumps (-1 for infinite)");
            modEnabled = Config.Bind<bool>("General", "enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static int JumpNumber { get; private set; }


        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        static class Jump_Patch
        {

            static void Prefix(Character __instance, ref float ___m_lastGroundTouch)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    if (JumpNumber > 0 && __instance.IsOnGround())
                    {
                        JumpNumber = 0;
                    }

                    if (maxJumps.Value < 0 || JumpNumber < maxJumps.Value)
                    {
                        ___m_lastGroundTouch = 0.1f;
                        JumpNumber++;
                    }
                }
            }
        }
    }
}
