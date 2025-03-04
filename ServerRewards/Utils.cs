using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ServerRewards
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {


        public static string GetPackageItems(PackageInfo package, PlayerInfo player)
        {
            int choiceAdd = 0;
            bool chose = false;
            List<string> output = new List<string>();
            foreach (string item in package.items)
            {
                Dbgl($"Checking {item}");
                int choice = UnityEngine.Random.Range(1, 100);

                string[] infos = item.Split(',');
                ItemInfo info = new ItemInfo()
                {
                    name = infos[0],
                    amount = infos[1],
                    chance = infos[2],
                    type = infos[3]
                };

                if(info.type.ToLower() == "choice")
                {
                    if (chose)
                        continue;
                    choiceAdd += int.Parse(info.chance);
                    Dbgl($"Checking choice {choiceAdd} > {choice}");
                    if (choice > choiceAdd)
                        continue;
                    chose = true;
                    Dbgl($"Won choice");
                }
                else
                {
                    int chance = UnityEngine.Random.Range(1, 100);
                    Dbgl($"Checking chance {info.chance} > {chance}");
                    if (chance > int.Parse(info.chance))
                        continue;
                    Dbgl($"Won chance");
                }

                int amount = 0;
                if (info.amount.Contains("-"))
                {
                    var a = info.amount.Split('-');
                    amount = UnityEngine.Random.Range(int.Parse(a[0]), int.Parse(a[1]));
                }
                else
                    amount = int.Parse(info.amount);

                if (amount > 0)
                    output.Add(info.name + "," + amount);


                Dbgl($"Added {amount} {info.name}");
            }
            return string.Join(";", output);
        }

        public static List<PackageInfo> GetStorePackagesFromString(List<string> storeInventory)
        {
            List<PackageInfo> packages = new List<PackageInfo>();
            foreach(string package in storeInventory)
            {
                string[] info = package.Split(',');
                packages.Add(new PackageInfo()
                {
                    id = info[0],
                    name = info[1],
                    description = info[2],
                    type = info[3],
                    price = int.Parse(info[4])
                });
            }
            return packages;
        }

        public static List<PackageInfo> GetAllPackages()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards", "StoreInfo");
            if (!Directory.Exists(path))
            {
                Dbgl("Missing store info");
                return null;
            }
            List<PackageInfo> packages = new List<PackageInfo>();
            foreach(string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                PackageInfo pi = JsonUtility.FromJson<PackageInfo>(json);
                packages.Add(pi);
            }
            return packages;
        }

        public static PackageInfo GetPackage(string packageID)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards", "StoreInfo");
            if (!Directory.Exists(path))
            {
                Dbgl("Missing store info");
                return null;
            }
            foreach(string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                PackageInfo pi = JsonUtility.FromJson<PackageInfo>(json);
                if (pi.id == packageID)
                    return pi;
            }
            return null;
        }


        public static List<string> GetStoreInventoryString(PlayerInfo player)
        {
            List<string> packages = new List<string>();
            foreach(PackageInfo pi in GetAllPackages())
            {
                if (CanBuyPackage(ref player, pi, false, true, out string result))
                    packages.Add(pi.StoreString());
            }
            return packages;
        }

        public static int GetUserCurrency(string steamID)
        {
            PlayerInfo playerInfo = GetPlayerInfo(steamID);
            if(playerInfo == null)
                Dbgl("Player info is null");

            return playerInfo != null ? playerInfo.currency : -1;
        }
        public static void AddNewPlayerInfo(string steamID)
        {

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
            var info = new PlayerInfo()
            {
                id = steamID,
                currency = playerStartCurrency.Value
            };
            string json = JsonUtility.ToJson(info);
            string file = Path.Combine(infoPath, steamID + ".json");
            File.WriteAllText(file, json);
        }
        public static PlayerInfo GetPlayerInfo(string steamID)
        {

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards", "PlayerInfo", steamID + ".json");

            if (!File.Exists(path))
            {
                Dbgl("Player file not found");
                return null;
            }

            string infoJson = File.ReadAllText(path);
            PlayerInfo playerInfo = JsonUtility.FromJson<PlayerInfo>(infoJson);
            return playerInfo;
        }
        public static string GetSteamID(string idOrName)
        {
            if(Regex.IsMatch(idOrName, @"[^0-9]"))
            {
                var peer = ZNet.instance.GetConnectedPeers().FirstOrDefault(p => p.m_playerName == idOrName);
                idOrName = GetPeerID(peer);
            }
            return idOrName;
        }
        public static List<string> GetAllPlayerIDs()
        {
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
            var output = new List<string>();
            foreach(string file in Directory.GetFiles(infoPath, "*.json"))
            {
                output.Add(Path.GetFileNameWithoutExtension(file));
            }
            return output;
        }

        public static void WritePlayerData(PlayerInfo playerInfo)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards", "PlayerInfo", playerInfo.id + ".json");
            string infoJson = JsonUtility.ToJson(playerInfo);
            File.WriteAllText(path, infoJson);
        }
        public static bool CanBuyPackage(ref PlayerInfo player, PackageInfo package, bool checkCurrency, bool checkLimit, out string result)
        {
            result = null;
            if (checkCurrency && player.currency < package.price)
            {
                result = $"Player doesn't have enough currency {player.currency}, price {package.price}";
                return false;
            }
            for(int i = 0; i < player.packages.Count; i++)
            {
                string[] info = player.packages[i].Split(',');
                if(info[0] == package.id)
                {
                    if(checkLimit && package.limit > 0 && int.Parse(info[1]) >= package.limit)
                    {
                        result = $"Player has bought more than the limit for this package.";
                        return false;
                    }
                    player.packages[i] = package.id + "," + (int.Parse(info[1]) + 1);
                    result = "Player can buy this package.";
                }

            }
            if (result == null)
            {
                player.packages.Add(package.id + ",1");
                result = $"Player can buy this package.";
            }
            return true;
        }

        public static bool AdjustCurrency(string steamID, int amount)
        {
            var peerList = ZNet.instance.GetConnectedPeers();
            foreach (var peer in peerList)
            {
                var id = GetPeerID(peer);
                if (steamID == "all" || id == steamID)
                {
                    var playerInfo = GetPlayerInfo(id);
                    if (playerInfo == null)
                    {
                        playerInfo = new PlayerInfo()
                        {
                            id = id,
                        };
                    }
                    playerInfo.currency += amount;
                    WritePlayerData(playerInfo);
                    if (steamID != "all")
                        return true;
                }
            }
            return steamID == "all";
        }
        public static bool SetCurrency(string steamID, int amount)
        {
            var peerList = ZNet.instance.GetConnectedPeers();
            foreach (var peer in peerList)
            {
                var id = GetPeerID(peer);
                if (steamID == "all" || id == steamID)
                {
                    var playerInfo = GetPlayerInfo(id);
                    if (playerInfo == null)
                    {
                        playerInfo = new PlayerInfo()
                        {
                            id = id,
                        };
                    }
                    playerInfo.currency = amount;
                    WritePlayerData(playerInfo);
                    if (steamID != "all")
                        return true;
                }
            }
            return steamID == "all";
        }

        public static string GivePackage(string steamID, string packageID)
        {
            PlayerInfo player = GetPlayerInfo(steamID);
            if (player == null)
                return "User not found!";
            PackageInfo pi = GetPackage(packageID);
            if (pi == null)
                return "Package not found!";

            var peer = ZNet.instance.GetConnectedPeers().Find(p => GetPeerID(p) == steamID);
            if(peer == null)
                return "User not online!";

            JsonCommand sendCommand = new JsonCommand()
            {
                command = "PurchaseResult",
                currency = player.currency,
                items = GetPackageItems(pi, player)
            };
            peer.m_rpc.Invoke("SendServerRewardsJSON", new object[] { JsonUtility.ToJson(sendCommand) });
            return null;
        }

        public static void PlayEffects()
        {
            EffectList effects = new EffectList();
            List<EffectList.EffectData> effectList = new List<EffectList.EffectData>();
            for (int i = 0; i < Player.m_localPlayer.m_deathEffects.m_effectPrefabs.Length; i++)
            {
                    effectList.Add(Player.m_localPlayer.m_deathEffects.m_effectPrefabs[i]);
            }
            effects.m_effectPrefabs = effectList.ToArray();
            effects.Create(Player.m_localPlayer.transform.position, Player.m_localPlayer.transform.rotation, Player.m_localPlayer.transform, 1f);
        }

        public static string GetPeerID(ZNetPeer peer)
        {
            ISocket socket = peer.m_socket;
            if (peer.m_socket.GetType().Name.EndsWith("BufferingSocket"))
            {
                Dbgl("ServerSync peer");
                try
                {
                    socket = (ISocket)AccessTools.Field(peer.m_socket.GetType(), "Original").GetValue(peer.m_socket);
                    Dbgl($"Peer type: {socket.GetType()}");
                }
                catch (Exception ex)
                {
                    Dbgl($"Failed to get socket from ServerSync: \n\n {ex}");
                }
            }
            if (socket is ZSteamSocket)
            {

                var steamID = (socket as ZSteamSocket).GetPeerID();
                return steamID.ToString();
            }
            else if (socket is ZPlayFabSocket)
            {

                return AccessTools.FieldRefAccess<ZPlayFabSocket, string>(socket as ZPlayFabSocket, "m_remotePlayerId");
            }
            else
            {
                Dbgl($"Wrong peer type: {socket.GetType()}");
                return null;
            }
        }

    }
}
