using BepInEx;
using HarmonyLib;
using System;

namespace ServerRewards
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {


        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        public static class ZNet_OnNewConnection_Patch
        {
            public static void Postfix(ZNet __instance, ZNetPeer peer)
            {
                if (!modEnabled.Value)
                    return;

                Dbgl("New connection");

                peer.m_rpc.Register<string>("SendServerRewardsJSON", new Action<ZRpc, string>(RPC_SendJSON));
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_ServerHandshake")]
        public static class ZNet_RPC_ServerHandshake_Patch
        {
            public static void Prefix(ZNet __instance, ZRpc rpc)
            {
                ZNetPeer peer = Traverse.Create(__instance).Method("GetPeer", new object[] { rpc }).GetValue<ZNetPeer>();

                Dbgl("Server Handshake");

                if (peer == null)
                {
                    Dbgl("peer is null");
                    return;
                }

                var steamID = (peer.m_socket as ZSteamSocket).GetPeerID();

                Dbgl($"Peer connected, steam ID: {steamID}");

                /*
                peer.m_rpc.Invoke("SendServerRewardsJSON", new object[]
                {
                    "{ command:\"SendID\", data:\""+SteamUser.GetSteamID().GetAccountID().m_AccountID+"\" }"
                });
                */
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake")]
        public static class ZNet_RPC_ClientHandshake_Patch
        {
            public static void Prefix(ZNet __instance, ZRpc rpc)
            {
                ZNetPeer peer = Traverse.Create(__instance).Method("GetPeer", new object[] { rpc }).GetValue<ZNetPeer>();

                Dbgl("Client Handshake");

                if (peer == null)
                {
                    Dbgl("peer is null");
                    return;
                }

                /*
                peer.m_rpc.Invoke("SendServerRewardsJSON", new object[]
                {
                    "{ command:\"SendID\", data:\""+SteamUser.GetSteamID().GetAccountID().m_AccountID+"\" }"
                });
                */
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
                return true;
            }
        }
    }
}
