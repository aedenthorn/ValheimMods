using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ServerRewards
{
    [BepInPlugin("aedenthorn.ServerRewards", "Server Rewards", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<int> updateInterval;
        public static ConfigEntry<string> openUIKey;

        public static ConfigEntry<int> updateIntervalReward;
        public static ConfigEntry<int> staticLoginReward;
        public static ConfigEntry<string> consecutiveLoginReward;
        public static ConfigEntry<bool> consecutiveLoginRewardOnce;

        public static ConfigEntry<float> windowWidth;
        public static ConfigEntry<float> windowHeight;
        public static ConfigEntry<Vector2> windowPosition;
        public static ConfigEntry<int> titleFontSize;
        public static ConfigEntry<int> currencyFontSize;
        public static ConfigEntry<int> labelFontSize;
        public static ConfigEntry<string> storeTitle;
        public static ConfigEntry<string> currencyString;
        public static ConfigEntry<string> packageString;
        public static ConfigEntry<string> myCurrencyString;
        public static ConfigEntry<int> packagesPerRow;

        private static BepInExPlugin context;
        private static int myCurrency;
        private static bool storeOpen;

        private static Vector2 scrollPosition;
        private static GUIStyle titleStyle;
        private static GUIStyle currencyStyle;
        private static GUIStyle labelStyle;
        private static Rect windowRect;
        private static string windowTitleText;
        private static List<PackageInfo> storePackages = new List<PackageInfo>();
        private static Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            //nexusID = Config.Bind<int>("General", "NexusID", 1113, "Nexus mod ID for updates");

            openUIKey = Config.Bind<string>("Config", "OpenUIKey", "f10", "Key to open currency UI");
            updateInterval = Config.Bind<int>("Config", "UpdateInterval", 60, "Update interval in seconds (server only)");
            updateIntervalReward = Config.Bind<int>("Currency", "UpdateIntervalReward", 1, "Currency awarded every update interval.");
            staticLoginReward = Config.Bind<int>("Currency", "StaticLoginReward", 100, "Currency awarded for logging in.");
            consecutiveLoginReward = Config.Bind<string>("Currency", "ConsecutiveLoginReward", "100,200,300,400,500,600,700", "Login rewards for logging in a consecutive number of days.");
            consecutiveLoginRewardOnce = Config.Bind<bool>("Currency", "ConsecutiveLoginRewardOnce", true, "Consecutive login reward only applies once, otherwise repeats from start.");

            windowWidth = Config.Bind<float>("UI", "WindowWidth", Screen.width / 3, "Width of the store window");
            windowHeight = Config.Bind<float>("UI", "WindowHeight", Screen.height / 3, "Height of the store window");
            windowPosition = Config.Bind<Vector2>("UI", "WindowPosition", new Vector2(Screen.width / 3, Screen.height / 3), "Position of the store window");
            packagesPerRow = Config.Bind<int>("UI", "PackagesPerRow", 4, "Packages per row");
            titleFontSize = Config.Bind<int>("UI", "TitleFontSize", 24, "Size of the store window title");
            currencyFontSize = Config.Bind<int>("UI", "CurrencyFontSize", 20, "Size of the currency info");
            labelFontSize = Config.Bind<int>("UI", "LabelFontSize", 24, "Size of the package labels");
            //titleFontColor = Config.Bind<Color>("UI", "TitleFontColor", new Color(1, 1, 0.7f, 1), "Color of the store window title");
            storeTitle = Config.Bind<string>("UI", "StoreTitle", "<b><color=#9999FFFF>Server Store</color></b>", "Store window title");
            currencyString = Config.Bind<string>("UI", "CurrencyString", "${1}", "Currency string.");
            myCurrencyString = Config.Bind<string>("UI", "MyCurrencyString", "<b><color=#FFFFFFFF>My Balance:</color> <color=#FFFF00FF>{0}</color></b>", "My currency string");
            packageString = Config.Bind<string>("UI", "PackageString", "<b><color=#FFFFFFFF>{0}</color> <color=#FFFF00FF>{1}</color></b>", "Package string");

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards");
            if (!Directory.Exists(path))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(path);
            }
            string AssetsPath = Path.Combine(path, "Assets");
            if (!Directory.Exists(AssetsPath))
            {
                Directory.CreateDirectory(AssetsPath);
            }

            foreach(string file in Directory.GetFiles(AssetsPath, "*.png"))
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                byte[] data = File.ReadAllBytes(file);
                texture.LoadImage(data); 
                textureDict.Add(Path.GetFileNameWithoutExtension(file), texture);
            }

            string infoPath = Path.Combine(path, "PlayerInfo");
            if (!Directory.Exists(infoPath))
            {
                Directory.CreateDirectory(infoPath);
            }

            string storePath = Path.Combine(path, "StoreInfo");
            if (!Directory.Exists(storePath))
            {
                Directory.CreateDirectory(storePath);
                PackageInfo package = new PackageInfo()
                {
                    name = "Shields",
                    id = "Shields",
                    type = "Common",
                    price = 50,
                    items = new List<string>() { "ShieldWood,1,70,choice","ShieldBronze,1,30,choice" }
                };
                File.WriteAllText(Path.Combine(storePath, package.id + ".json"), JsonUtility.ToJson(package));
            }
            windowTitleText = storeTitle.Value;

            ApplyConfig();
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private static void ApplyConfig()
        {
            windowRect = new Rect(windowPosition.Value.x, windowPosition.Value.y, windowWidth.Value, windowHeight.Value);

            titleStyle = new GUIStyle
            {
                richText = true,
                fontSize = titleFontSize.Value,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            currencyStyle = new GUIStyle
            {
                richText = true,
                fontSize = currencyFontSize.Value,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle = new GUIStyle
            {
                richText = true,
                fontSize = labelFontSize.Value,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void Update()
        {
            /*
            if (AedenthornUtils.CheckKeyDown(openUIKey.Value))
            {
                storeOpen = !storeOpen;

            }
            return;
            */
            if (ZNet.instance && Player.m_localPlayer && !AedenthornUtils.IgnoreKeyPresses(true) && AedenthornUtils.CheckKeyDown(openUIKey.Value))
            {
                Dbgl("Pressed hotkey");
                if (storeOpen)
                {
                    Dbgl("Closing store");
                    storeOpen = false;
                }
                else
                {
                    ZRpc serverRPC = ZNet.instance.GetServerRPC();
                    if (serverRPC != null)
                    {
                        Dbgl("Requesting store data");
                        JsonCommand command = new JsonCommand()
                        {
                            command = "RequestStoreInfo",
                            id = SteamUser.GetSteamID().ToString()
                        };
                        string commandJson = JsonUtility.ToJson(command);
                        Dbgl(commandJson);
                        serverRPC.Invoke("SendServerRewardsJSON", new object[] { commandJson });
                    }
                }
            }

        }
        private void OnGUI()
        {
            if (!modEnabled.Value)
                return;
            /*
            if (storeOpen)
            {
                windowRect = GUI.Window(424243, windowRect, new GUI.WindowFunction(WindowBuilder), "");
                if (!Input.GetKey(KeyCode.Mouse0) && (windowRect.x != windowPosition.Value.x || windowRect.y != windowPosition.Value.y))
                {
                    windowPosition.Value = new Vector2(windowRect.x, windowRect.y);
                }

            }
            return;
            */
            if (!ZNet.instance?.IsServer() == true && Player.m_localPlayer) 
            {
                if (storeOpen)
                {
                    windowRect = GUI.Window(424243, windowRect, new GUI.WindowFunction(WindowBuilder), "");
                }
                if (!Input.GetKey(KeyCode.Mouse0) && (windowRect.x != windowPosition.Value.x || windowRect.y != windowPosition.Value.y))
                {
                    windowPosition.Value = new Vector2(windowRect.x, windowRect.y);
                }
            }
        }

        private void WindowBuilder(int id)
        {
            GUI.DragWindow(new Rect(0,0,windowWidth.Value, 20));
            GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(windowWidth.Value) });
            GUILayout.Label(windowTitleText, titleStyle);
            GUILayout.Label(string.Format(myCurrencyString.Value, string.Format(currencyString.Value, myCurrency)), currencyStyle);
            
            float width = windowWidth.Value - 70;
            float itemWidth = width / packagesPerRow.Value;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, new GUILayoutOption[] { GUILayout.Width(windowWidth.Value - 20) });
            GUILayout.Space(10);
            GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(width) });
            GUILayout.BeginHorizontal();
            for (int i = 0; i < storePackages.Count; i++)
            {
                if(i > 0 && i % packagesPerRow.Value == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                }
                PackageInfo pi = storePackages[i];
                string texture = textureDict.ContainsKey(pi.type) ? pi.type : "Common";
                GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(itemWidth) });
                if (GUILayout.Button(textureDict[texture], new GUILayoutOption[] { GUILayout.Width(itemWidth), GUILayout.Height(itemWidth) }))
                {
                    if (myCurrency >= pi.price)
                    {
                        ZRpc serverRPC = ZNet.instance.GetServerRPC();
                        if (serverRPC != null)
                        {
                            Dbgl("Requesting store data");
                            JsonCommand command = new JsonCommand()
                            {
                                command = "BuyPackage",
                                packageid = pi.id
                            };
                            string commandJson = JsonUtility.ToJson(command);
                            Dbgl(commandJson);
                            serverRPC.Invoke("SendServerRewardsJSON", new object[] { commandJson });
                        }
                    }
                }

                GUILayout.Label(string.Format(packageString.Value, pi.name, pi.price), labelStyle, new GUILayoutOption[] { GUILayout.Width(itemWidth) });
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        [HarmonyPatch(typeof(ZNet), "Awake")]
        public static class ZNet_Awake_Patch
        {
            public static void Postfix(ZNet __instance)
            {
                Dbgl($"ZNet Awake! Server? {__instance.IsServer()}");

                if (__instance.IsServer())
                    context.InvokeRepeating("UpdatePlayers", 1, updateInterval.Value);
            }
        }

        public void UpdatePlayers(bool forced = false)
        {
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
            var infoFiles = Directory.GetFiles(infoPath, "*.json");

            foreach (var peer in peerList)
            {
                var steamID = (peer.m_socket as ZSteamSocket).GetPeerID();

                if (infoFiles.FirstOrDefault(s => s.EndsWith(steamID + ".json")) == null)
                {
                    Dbgl($"Adding new player {steamID}");
                    var info = new PlayerInfo()
                    {
                        id = steamID.m_SteamID
                    };
                    string json = JsonUtility.ToJson(info);
                    string file = Path.Combine(infoPath, steamID + ".json");
                    File.WriteAllText(file, json);
                    infoFiles = Directory.GetFiles(infoPath, "*.json");
                }
            }

            Dbgl($"Got {infoFiles.Length} player files, {peerList.Count} online players");

            foreach (string file in infoFiles)
            {
                Dbgl($"Processing id {Path.GetFileNameWithoutExtension(file)}");

                string infoJson = File.ReadAllText(file);
                PlayerInfo playerInfo = JsonUtility.FromJson<PlayerInfo>(infoJson);

                // skip offline players

                if (!peerList.Exists(p => (p.m_socket as ZSteamSocket).GetPeerID().ToString() == Path.GetFileNameWithoutExtension(file)))
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

                    playerInfo.currency += staticLoginReward.Value;
                    if(consecutiveLoginReward.Value.Length > 0 && DateTime.Today - new DateTime(playerInfo.lastLogin).Date == TimeSpan.FromDays(1))
                    {
                        var dailyRewards = consecutiveLoginReward.Value.Split(',');
                        var rewardDay = -1;
                        playerInfo.consecutiveDays++;
                        if (consecutiveLoginRewardOnce.Value)
                        {
                            if (playerInfo.maxConsecutiveDays < dailyRewards.Length)
                                rewardDay = playerInfo.consecutiveDays;
                        }
                        else
                        {
                            rewardDay = playerInfo.consecutiveDays % dailyRewards.Length; 
                        }
                        if (rewardDay > -1)
                            playerInfo.currency += int.Parse(dailyRewards[rewardDay]);

                        if (playerInfo.maxConsecutiveDays < playerInfo.consecutiveDays)
                            playerInfo.maxConsecutiveDays = playerInfo.consecutiveDays;
                    }
                    if(staticLoginReward.Value > 0 && DateTime.Today - new DateTime(playerInfo.lastLogin).Date >= TimeSpan.FromDays(2))
                    {

                    }

                    playerInfo.lastLogin = DateTime.Now.Ticks;

                    playerInfo.online = true;
                }
                else if(!forced)
                    playerInfo.currency += updateIntervalReward.Value;

                Dbgl($"\tPlayer currency {playerInfo.currency}, writing json");
                WritePlayerData(playerInfo);

            }
        }

        private static void RPC_SendJSON(ZRpc rpc, string json)
        {
            JsonCommand command = JsonUtility.FromJson<JsonCommand>(json);
            Dbgl($"RPC_SendJSON received command {command.command} {json} from id {command.id}");

            ZNetPeer peer = Traverse.Create(ZNet.instance).Method("GetPeer", new object[] { rpc }).GetValue<ZNetPeer>();
            var steamID = (peer.m_socket as ZSteamSocket).GetPeerID();

            if (ZNet.instance.IsServer())
            {
                context.UpdatePlayers(true);

                if (command.command == "BuyPackage")
                {
                    var inv = GetStoreInventory();

                    PackageInfo package;
                    try
                    {
                        package = inv.First(p => p.id == command.packageid);
                    }
                    catch
                    {
                        Dbgl($"Package {command.packageid} not found");
                        return;
                    }

                    PlayerInfo player = GetPlayerInfo(steamID.ToString());

                    if(player.currency < package.price)
                    {
                        Dbgl($"Player doesn't have enough currency {player.currency}, price {package.price}");
                        return;
                    }
                    player.currency -= package.price;
                    WritePlayerData(player);

                    JsonCommand sendCommand = new JsonCommand()
                    {
                        command = "PurchaseResult",
                        currency = player.currency,
                        items = GetPackageItems(package, player)
                    };


                    rpc.Invoke("SendServerRewardsJSON", new object[] { JsonUtility.ToJson(sendCommand) });
                }
                else if (command.command == "RequestStoreInfo")
                {
                    int currency = GetUserCurrency(steamID.ToString());
                    if (currency == -1)
                    {
                        Dbgl("Error getting store info");
                        return;
                    }

                    JsonCommand sendCommand = new JsonCommand() 
                    {
                        command = "SendStoreInfo",
                        storeTitle = storeTitle.Value,
                        storeInventory = GetStoreInventoryString(),
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
                    myCurrency = command.currency;
                    foreach(string itemString in command.items.Split(';'))
                    {
                        Dbgl($"Receving {itemString}");

                        string[] itemAmount = itemString.Split(',');
                        string name = itemAmount[0];
                        GameObject prefab = ZNetScene.instance.GetPrefab(name);
                        if (!prefab)
                        {
                            Dbgl($"Item {name} not found!");
                            continue;
                        }

                        int amount = int.Parse(itemAmount[1]);
                        if (amount == 1)
                        {
                            Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity);
                            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Spawning object " + name, 0, null);
                        }
                        else
                        {
                            for (int j = 0; j < amount; j++)
                            {
                                Vector3 b = UnityEngine.Random.insideUnitSphere * 0.5f;
                                Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up + b, Quaternion.identity);
                                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Spawning object " + name, 0, null);
                            }
                        }
                    }
                }
                else if (command.command == "SendStoreInfo")
                {
                    if(command.currency == -1)
                    {
                        Dbgl("Error getting store info");
                        return;
                    }
                    myCurrency = command.currency;
                    windowTitleText = command.storeTitle;
                    currencyString.Value = command.currencyString;
                    storePackages = GetStorePackagesFromString(command.storeInventory);
                    Dbgl($"Got user currency: {myCurrency}");
                    if (Player.m_localPlayer)
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"You have {string.Format(command.currencyString, myCurrency)}");

                    storeOpen = true;
                }
            }
        }
    }
}
