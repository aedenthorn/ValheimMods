using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ServerRewards
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static void RPC_ConsoleCommand(ZRpc rpc, string command)
        {
            if (!modEnabled.Value || !ZNet.instance.IsServer())
                return;

            ZNetPeer peer = Traverse.Create(ZNet.instance).Method("GetPeer", new object[] { rpc }).GetValue<ZNetPeer>();
            var steamID = GetPeerID(peer);
            Dbgl($"RPC_ConsoleCommand received command {command} from {steamID}");
            if (!Traverse.Create(ZNet.instance).Field("m_adminList").GetValue<SyncedList>().Contains(rpc.GetSocket().GetHostName()))
            {
                Dbgl("User is not admin!");
                return;
            }

            var parts = command.Split(' ').Skip(1).ToArray();
            string result = "";
            if (parts[0] == "help")
            {
                result = "Usage:\r\n" +
                    "serverrewards list users\r\n" +
                    "serverrewards list packages\r\n" +
                    "serverrewards give <steamID> <currency>\r\n" +
                    "serverrewards give all <currency>\r\n" +
                    "serverrewards set <steamID> <currency>\r\n" +
                    "serverrewards set all <currency>\r\n" +
                    "serverrewards givepackage <steamID> <packageID>\r\n" +
                    "serverrewards givepackage all <packageID>\r\n" +
                    "serverrewards spawn <spawnName>";
            }
            else if (parts[0] == "list" && parts.Length == 2)
            {
                if (parts[1] == "users")
                {
                    List<string> userList = new List<string>();
                    List<string> users = GetAllPlayerIDs();

                    var peerList = ZNet.instance.GetConnectedPeers();
                    foreach (string user in users)
                    {
                        string online = "(offline)";
                        var tp = peerList.Find(p => GetPeerID(p) == user);
                        if (tp != null)
                        {
                            online = tp.m_playerName + " (online)";
                        }
                        userList.Add(user + " " + online);
                    }
                    result = string.Join("\r\n", userList);
                }
                else if (parts[1] == "packages")
                {
                    List<string> packageList = new List<string>();
                    var packages = GetAllPackages();
                    foreach(PackageInfo p in packages)
                    {
                        packageList.Add(p.id + " " + p.price);
                    }
                    result = string.Join("\r\n", packageList);
                }
                else
                {
                    result = "Syntax error.";
                }
            }
            else if (parts[0] == "give" && parts.Length == 3)
            {
                try
                {
                    string id = GetSteamID(parts[1]);
                    if(id == null)
                        result = "User not found.";
                    else if (AdjustCurrency(id, int.Parse(parts[2])))
                        result = "Balance adjusted.";
                    else
                        result = "Error adjusting player balance.";
                }
                catch
                {
                    result = "Syntax error.";
                }

            }
            else if (parts[0] == "set" && parts.Length == 3)
            {
                try
                {
                    string id = GetSteamID(parts[1]);
                    if (id == null)
                        result = "User not found.";
                    else if (SetCurrency(id, int.Parse(parts[2])))
                        result = "Balance set.";
                    else
                        result = "Error setting player balance.";
                }
                catch
                {
                    result = "Syntax error.";
                }
            }
            else if (parts[0] == "givepackage" && parts.Length == 3)
            {
                if(parts[1] == "all")
                {
                    IEnumerable<string> users = GetAllPlayerIDs();
                    int count = 0;
                    foreach(string user in users)
                    {
                        string r = GivePackage(parts[1], parts[2]);
                        if (r == null)
                            count++;
                    }
                    result = $"Package sent to {count} users!";
                }
                else
                {
                    string id = GetSteamID(parts[1]);
                    if (id == null)
                        result = "User not found.";
                    else
                    {
                        result = GivePackage(id, parts[2]);
                        if (result == null)
                            result = "Package sent!";
                    }
                }
            }
            else if (parts[0] == "spawn" && parts.Length == 2)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(parts[1]);
                if (!prefab)
                {
                    result = $"Item {parts[1]} not found!";
                }
                else
                {
                    var go = Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity);
                    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format(packageInfoString.Value, Localization.instance.Localize(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)), 0, null);
                }
            }
            else
            {
                result = "Syntax error.";
            }
            JsonCommand sendCommand = new JsonCommand()
            {
                command = "SendConsoleString",
                data = result
            };
            rpc.Invoke("SendServerRewardsJSON", new object[] { JsonUtility.ToJson(sendCommand) });
            Dbgl(result);
        }

        public static void RPC_SendJSON(ZRpc rpc, string json)
        {
            if (!modEnabled.Value)
                return;

            JsonCommand command = JsonUtility.FromJson<JsonCommand>(json);
            Dbgl($"RPC_SendJSON received command {command.command} {json} from id {command.id}");

            ZNetPeer peer = ((ZNetPeer)AccessTools.Method(typeof(ZNet), "GetPeer", new Type[] { typeof(ZRpc) }).Invoke(ZNet.instance, new object[] { rpc }));

            if(peer is null)
            {
                Dbgl("Peer is null, aborting");
                return;
            }

            var steamID = GetPeerID(peer);

            if (ZNet.instance.IsServer())
            {
                if (steamID is null)
                {
                    Dbgl("steamID is null, aborting");
                    return;
                }
                context.UpdatePlayers(true);

                if (command.command == "BuyPackage")
                {
                    var packages = GetAllPackages();

                    PackageInfo package;
                    try
                    {
                        package = packages.First(p => p.id == command.packageid);
                    }
                    catch
                    {
                        Dbgl($"Package {command.packageid} not found");
                        return;
                    }

                    PlayerInfo player = GetPlayerInfo(steamID);

                    if(!CanBuyPackage(ref player, package, true, true, out string result))
                    {
                        WritePlayerData(player);
                        return;
                    }
                    Dbgl(result);

                    player.currency -= package.price;
                    WritePlayerData(player);

                    JsonCommand sendCommand = new JsonCommand()
                    {
                        command = "PurchaseResult",
                        currency = player.currency,
                        packageid = package.id,
                        packagename = package.name,
                        packagedescription = package.description,
                        items = GetPackageItems(package, player)
                    };

                    rpc.Invoke("SendServerRewardsJSON", new object[] { JsonUtility.ToJson(sendCommand) });
                }
                else if (command.command == "RequestStoreInfo")
                {
                    int currency = GetUserCurrency(steamID);
                    if (currency == -1)
                    {
                        Dbgl("Error getting store info");
                        return;
                    }
                    PlayerInfo player = GetPlayerInfo(steamID);

                    JsonCommand sendCommand = new JsonCommand()
                    {
                        command = "SendStoreInfo",
                        storeTitle = storeTitleString.Value,
                        storeInventory = GetStoreInventoryString(player),
                        currencyString = currencyString.Value,
                        currency = currency,
                    };


                    rpc.Invoke("SendServerRewardsJSON", new object[] { JsonUtility.ToJson(sendCommand) });
                }
            }
            else
            {

                if (command.command == "PurchaseResult")
                {
                    Traverse.Create(GameCamera.instance).Field("m_mouseCapture").SetValue(true);
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    storeOpen = false;
                    //PlayEffects();

                    var items = command.items.Split(';');

                    GameObject chest = null;
                    PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                    Traverse inventoryT = null;

                    if (useTombstone.Value)
                    {
                        chest = Instantiate(Player.m_localPlayer.m_tombstone, Player.m_localPlayer.GetCenterPoint() + Player.m_localPlayer.transform.forward, Player.m_localPlayer.transform.rotation);
                        chest.GetComponent<Container>().m_name = command.packagename;
                        inventoryT = Traverse.Create(chest.GetComponent<Container>().GetInventory());
                        inventoryT.Field("m_name").SetValue(command.packagename);
                        inventoryT.Field("m_width").SetValue(8);
                        int rows = items.Count() / 8 + 2;
                        if (items.Count() % 8 != 0)
                            rows++;
                        inventoryT.Field("m_height").SetValue(rows);

                        TombStone tombstone = chest.GetComponent<TombStone>();
                        tombstone.Setup(command.packagename, playerProfile.GetPlayerID());
                    }

                    myCurrency = command.currency;

                    List<string> itemStrings = new List<string>();
                    foreach (string itemString in items)
                    {
                        Dbgl($"Receving {itemString}");

                        string[] nameAmount = itemString.Split(',');
                        string name = nameAmount[0];
                        GameObject prefab = ZNetScene.instance.GetPrefab(name);
                        if (!prefab)
                        {
                            Dbgl($"Item {name} not found!");
                            continue;
                        }
                        
                        int amount = int.Parse(nameAmount[1]);

                        if (useTombstone.Value)
                        {
                            ItemDrop.ItemData item = prefab.GetComponent<ItemDrop>().m_itemData;

                            double slotsNeed = amount / item.m_shared.m_maxStackSize / 8;
                            inventoryT.Field("m_height").SetValue((int)inventoryT.Field("m_height").GetValue() + (int)Math.Round(slotsNeed, 0));
                            while (amount >= item.m_shared.m_maxStackSize)
                            {
                                int stack = Mathf.Min(item.m_shared.m_maxStackSize, amount);

                                //chest.GetComponent<Container>().GetInventory().AddItem(_newItem);
                                chest.GetComponent<Container>().GetInventory().AddItem(name, stack, item.m_quality, item.m_variant, Player.m_localPlayer.GetPlayerID(), Player.m_localPlayer.GetPlayerName());
                                amount = amount - stack;
                            }

                            if (amount > 0)
                            {
                                //chest.GetComponent<Container>().GetInventory().AddItem(_newItem2);
                                chest.GetComponent<Container>().GetInventory().AddItem(name, amount, item.m_quality, item.m_variant, Player.m_localPlayer.GetPlayerID(), Player.m_localPlayer.GetPlayerName());
                            }

                            itemStrings.Add($"{Localization.instance.Localize(item.m_shared.m_name)} {nameAmount[0]}");
                        }
                        else
                        {
                            if (amount == 1)
                            {
                                var go = Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity);
                                var item = go.GetComponent<ItemDrop>().m_itemData;
                                item.m_durability = item.m_shared.m_maxDurability;
                                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format(rewardString.Value, Localization.instance.Localize(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)), 0, null);
                            }
                            else
                            {
                                for (int j = 0; j < amount; j++)
                                {
                                    Vector3 b = UnityEngine.Random.insideUnitSphere * 0.5f;
                                    var go = Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up + b, Quaternion.identity);
                                    var item = go.GetComponent<ItemDrop>().m_itemData;
                                    item.m_durability = item.m_shared.m_maxDurability;
                                    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name), 0, null);
                                }
                            }
                        }



                    }
                    if (useTombstone.Value)
                    {
                        inventoryT.Method("Changed").GetValue();
                        chest.GetComponent<ZNetView>().GetZDO().Set("ServerReward", string.Format(packageInfoString.Value, command.packagename, playerProfile.GetName()) + "\r\n" + string.Join("\r\n", itemStrings));
                    }
                
                }
                else if (command.command == "SendStoreInfo")
                {
                    if (command.currency == -1)
                    {
                        Dbgl("Error getting store info");
                        return;
                    }
                    myCurrency = command.currency;
                    windowTitleText = command.storeTitle;
                    currencyString.Value = command.currencyString;
                    storePackages = GetStorePackagesFromString(command.storeInventory);
                    Dbgl($"Got store inventory {storePackages.Count}, user currency: {myCurrency}");

                    storeOpen = true;
                }
                else if (command.command == "SendConsoleString")
                {
                    Traverse.Create(Console.instance).Method("AddString", new object[] { command.data }).GetValue();
                    Dbgl(command.data);
                }
            }
        }

    }
}
