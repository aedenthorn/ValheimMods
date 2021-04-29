using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ServerRewards
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {

        private static void WritePlayerData(PlayerInfo playerInfo)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards", "PlayerInfo", playerInfo.id + ".json");
            string infoJson = JsonUtility.ToJson(playerInfo);
            File.WriteAllText(path, infoJson);
        }

        private static string GetPackageItems(PackageInfo package, PlayerInfo player)
        {
            List<string> output = new List<string>();
            foreach (string item in package.items)
            {
                Dbgl($"Checking {item}");
                int choice = UnityEngine.Random.Range(1, 100);
                int choiceAdd = 0;

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
                    choiceAdd += int.Parse(info.chance);
                    Dbgl($"Checking choice {choiceAdd} > {choice}");
                    if (choice > choiceAdd)
                        continue;
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

        private static List<PackageInfo> GetStorePackagesFromString(string storeInventory)
        {
            List<PackageInfo> packages = new List<PackageInfo>();
            foreach(string package in storeInventory.Split(';'))
            {
                string[] info = package.Split(',');
                packages.Add(new PackageInfo()
                {
                    id = info[0],
                    name = info[1],
                    price = int.Parse(info[2])
                });
            }
            return packages;
        }

        private static List<PackageInfo> GetStoreInventory()
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

        private static string GetStoreInventoryString()
        {
            List<string> packages = new List<string>();
            foreach(PackageInfo pi in GetStoreInventory())
            {
                packages.Add(pi.StoreString());
            }
            return string.Join(";", packages);
        }

        private static int GetUserCurrency(string steamID)
        {
            PlayerInfo playerInfo = GetPlayerInfo(steamID);
            if(playerInfo == null)
                Dbgl("Player info is null");

            return playerInfo != null ? playerInfo.currency : -1;
        }

        private static PlayerInfo GetPlayerInfo(string steamID)
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


    }
}
