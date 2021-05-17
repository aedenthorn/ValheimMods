using BepInEx;
using HarmonyLib;
using System;

namespace ServerRewards
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        [HarmonyPatch(typeof(ZNet), "Awake")]
        public static class ZNet_Awake_Patch
        {
            public static void Postfix(ZNet __instance)
            {
                if (!modEnabled.Value)
                    return;

                Dbgl($"ZNet Awake! Server? {__instance.IsServer()}");

                if (__instance.IsServer())
                    context.InvokeRepeating("UpdatePlayersRepeated", 1, updateInterval.Value);
            }
        }

        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        public static class ZNet_OnNewConnection_Patch
        {
            public static void Postfix(ZNet __instance, ZNetPeer peer)
            {
                if (!modEnabled.Value)
                    return;

                Dbgl("New connection");

                peer.m_rpc.Register<string>("SendServerRewardsJSON", new Action<ZRpc, string>(RPC_SendJSON));
                if (__instance.IsServer())
                {
                    peer.m_rpc.Register<string>("ServerRewardsConsoleCommand", new Action<ZRpc, string>(RPC_ConsoleCommand));
                    context.UpdatePlayers(true);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), "TakeInput")]
        static class TakeInput_Patch
        {
            static bool Prefix(ref bool __result)
            {
                if(!modEnabled.Value || !storeOpen)
                    return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), "InCutscene")]
        static class InCutscene_Patch
        {
            static bool Prefix(ref bool __result)
            {
                if(!modEnabled.Value || !storeOpen)
                    return true;
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(TombStone), "GetHoverText")]
        static class TombStone_GetHoverText_Patch
        {
            static bool Prefix(ZNetView ___m_nview, ref string __result)
            {
                if (!modEnabled.Value)
                    return true;
                string reward = ___m_nview?.GetZDO()?.GetString("ServerReward", null);
                if (reward != null)
                {
                    __result = reward;
                    return false;
                }
                return true;
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
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    ApplyConfig();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} "))
                {
                    ZRpc serverRPC = ZNet.instance.GetServerRPC();
                    if (serverRPC != null)
                        serverRPC.Invoke("ServerRewardsConsoleCommand", new object[] { text });
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    return false;
                }
                return true;
            }

        }
    }
}
