using BepInEx;
using HarmonyLib;
using Steamworks;
using System;
using System.IO;
using System.Reflection;

namespace ServerRewards
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public void UpdatePlayersRepeated()
        {
            UpdatePlayers(false);
        }
        public void UpdatePlayers(bool forced)
        {
            if (!modEnabled.Value || !ZNet.instance.IsServer())
                return;

            Dbgl("Updating players");
            //var playerList = ZNet.instance.GetPlayerList();
            var peerList = ZNet.instance.GetConnectedPeers();

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards");
            if (!Directory.Exists(path))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(path);
            }
            string infoPath = Path.Combine(path, "PlayerInfo");
            if (!Directory.Exists(infoPath))
            {
                Directory.CreateDirectory(infoPath);
            }
            var filePlayers = GetAllPlayerIDs();

            Dbgl($"Got {filePlayers.Count} player files, {peerList.Count} online players");

            if(peerList.Count > 0)
            {
                foreach (var peer in peerList)
                {
                    {
                        var id = GetPeerID(peer);
                        if (!(id is null) && !filePlayers.Contains(id))
                        {
                            Dbgl($"Adding peer {id}");
                            AddNewPlayerInfo(id);
                        }
                    }
                }
            }
            if(filePlayers.Count > 0)
            {
                foreach (string id in filePlayers)
                {
                    Dbgl($"Processing id {id}");

                    PlayerInfo playerInfo = GetPlayerInfo(id);

                    // skip offline players

                    if (!peerList.Exists(p => GetPeerID(p) == id))
                    {
                        if (playerInfo.online)
                        {
                            Dbgl($"\tPlayer went offline, removing");
                            playerInfo.online = false;
                            WritePlayerData(playerInfo);
                        }
                        else
                            Dbgl($"\tPlayer not online, skipping");
                        continue;
                    }

                    if (!playerInfo.online)
                    {
                        Dbgl($"\tPlayer coming online, processing daily rewards");

                        if (consecutiveLoginReward.Value.Length > 0 && (playerInfo.lastLogin == 0 || DateTime.Today - new DateTime(playerInfo.lastLogin).Date == TimeSpan.FromDays(1)))
                        {
                            Dbgl($"\tPlayer logged in yesterday");
                            var dailyRewards = consecutiveLoginReward.Value.Split(',');
                            var rewardDay = -1;
                            playerInfo.consecutiveDays++;


                            if (consecutiveLoginRewardOnce.Value)
                            {
                                if (playerInfo.maxConsecutiveDays < playerInfo.consecutiveDays && playerInfo.consecutiveDays < dailyRewards.Length && playerInfo.maxConsecutiveDays < playerInfo.consecutiveDays)
                                    rewardDay = playerInfo.consecutiveDays;
                            }
                            else
                            {
                                rewardDay = playerInfo.consecutiveDays % dailyRewards.Length;
                            }
                            if (rewardDay > -1)
                            {
                                Dbgl($"\tgiving consecutive login reward {dailyRewards[rewardDay]}");
                                playerInfo.currency += int.Parse(dailyRewards[rewardDay]);
                            }
                            if (playerInfo.maxConsecutiveDays < playerInfo.consecutiveDays)
                                playerInfo.maxConsecutiveDays = playerInfo.consecutiveDays;

                        }
                        if (staticLoginReward.Value > 0 && DateTime.Today - new DateTime(playerInfo.lastLogin).Date >= TimeSpan.FromDays(1))
                        {
                            Dbgl($"\tPlayer has not logged in today, giving static login reward");
                            playerInfo.currency += staticLoginReward.Value;
                        }

                        playerInfo.lastLogin = DateTime.Now.Ticks;

                        playerInfo.online = true;
                    }
                    else if (!forced)
                        playerInfo.currency += updateIntervalReward.Value;

                    Dbgl($"\tPlayer currency {playerInfo.currency}, writing json");
                    WritePlayerData(playerInfo);

                }

            }
        }

    }
}
