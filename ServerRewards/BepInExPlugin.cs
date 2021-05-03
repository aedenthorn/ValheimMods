using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ServerRewards
{
    [BepInPlugin("aedenthorn.ServerRewards", "Server Rewards", "0.3.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> testing;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<int> updateInterval;
        public static ConfigEntry<string> openUIKey;

        public static ConfigEntry<int> updateIntervalReward;
        public static ConfigEntry<int> staticLoginReward;
        public static ConfigEntry<string> consecutiveLoginReward;
        public static ConfigEntry<bool> consecutiveLoginRewardOnce;
        public static ConfigEntry<int> playerStartCurrency;

        public static ConfigEntry<bool> coinBeforeAmount;
        public static ConfigEntry<float> windowWidth;
        public static ConfigEntry<float> windowHeight;
        public static ConfigEntry<Vector2> windowPosition;
        public static ConfigEntry<int> titleFontSize;
        public static ConfigEntry<int> currencyFontSize;
        public static ConfigEntry<int> labelFontSize;
        public static ConfigEntry<int> packagesPerRow;

        public static ConfigEntry<string> storeTitleString;
        public static ConfigEntry<string> currencyString;
        public static ConfigEntry<string> packageString;
        public static ConfigEntry<string> myCurrencyString;
        public static ConfigEntry<string> packageInfoString;

        private static BepInExPlugin context;
        private static int myCurrency;
        private static bool storeOpen;

        private static float coinFactor = 0.75f;
        private static Vector2 scrollPosition;
        private static GUIStyle titleStyle;
        private static GUIStyle currencyStyle;
        private static GUIStyle labelStyle;
        private static GUIStyle coinStyle;
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
            testing = Config.Bind<bool>("General", "Testing", false, "Enable mod testing mode (populates store locally)");
            nexusID = Config.Bind<int>("General", "NexusID", 1131, "Nexus mod ID for updates");

            openUIKey = Config.Bind<string>("Config", "OpenUIKey", "f10", "Key to open currency UI");
            updateInterval = Config.Bind<int>("Config", "UpdateInterval", 60, "Update interval in seconds (server only)");
            
            updateIntervalReward = Config.Bind<int>("Currency", "UpdateIntervalReward", 1, "Currency awarded every update interval");
            staticLoginReward = Config.Bind<int>("Currency", "StaticLoginReward", 100, "Currency awarded for logging in");
            consecutiveLoginReward = Config.Bind<string>("Currency", "ConsecutiveLoginReward", "100,200,300,400,500,600,700", "Login rewards for logging in a consecutive number of days");
            consecutiveLoginRewardOnce = Config.Bind<bool>("Currency", "ConsecutiveLoginRewardOnce", true, "Consecutive login reward only applies once, otherwise repeats from start");
            playerStartCurrency = Config.Bind<int>("Currency", "PlayerStartCurrency", 0, "Players start with this amount of currency.");

            windowWidth = Config.Bind<float>("UI", "WindowWidth", Screen.width / 3, "Width of the store window");
            windowHeight = Config.Bind<float>("UI", "WindowHeight", Screen.height / 3, "Height of the store window");
            windowPosition = Config.Bind<Vector2>("UI", "WindowPosition", new Vector2(Screen.width / 3, Screen.height / 3), "Position of the store window");
            packagesPerRow = Config.Bind<int>("UI", "PackagesPerRow", 4, "Packages per row");
            titleFontSize = Config.Bind<int>("UI", "TitleFontSize", 24, "Size of the store window title");
            currencyFontSize = Config.Bind<int>("UI", "CurrencyFontSize", 20, "Size of the currency info");
            labelFontSize = Config.Bind<int>("UI", "LabelFontSize", 20, "Size of the package labels");
            coinBeforeAmount = Config.Bind<bool>("UI", "CoinBeforeAmount", true, "Display the currency icon before the amount? Otherwise after");
            
            storeTitleString = Config.Bind<string>("Text", "StoreTitle", "<b><color=#FFFFFFFF>Server Store</color></b>", "Store window title");
            currencyString = Config.Bind<string>("Text", "CurrencyString", "<b><color=#FFFF00FF>{0}</color></b>", "Currency string");
            myCurrencyString = Config.Bind<string>("Text", "MyCurrencyString", "<b><color=#FFFFFFFF>My Balance:</color></b>", "My currency string");
            packageString = Config.Bind<string>("Text", "PackageString", "<b><color=#FFFFFFFF>{0}</color></b>", "Package string");
            packageInfoString = Config.Bind<string>("Text", "PackageInfoString", "{0} chest purchased by", "Reward string");

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
                    items = new List<string>() { "ShieldWood,1,70,choice", "ShieldBronzeBuckler,1,30,choice" }
                };
                File.WriteAllText(Path.Combine(storePath, package.id + ".json"), JsonUtility.ToJson(package));
            }

            if (testing.Value)
            {
                storePackages = GetAllPackages();
                myCurrency = 805;
            }
            windowTitleText = storeTitleString.Value;
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
            coinStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void Update()
        {
            if (!modEnabled.Value)
                return;
            if (testing.Value) { 
                if (AedenthornUtils.CheckKeyDown(openUIKey.Value))
                {
                    storeOpen = !storeOpen;

                }
                return;
            }
            if (ZNet.instance && Player.m_localPlayer && !AedenthornUtils.IgnoreKeyPresses(true) && AedenthornUtils.CheckKeyDown(openUIKey.Value))
            {
                Dbgl("Pressed hotkey");
                if (storeOpen)
                {
                    Traverse.Create(GameCamera.instance).Field("m_mouseCapture").SetValue(true);
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

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

            if (testing.Value)
            { 
                if (storeOpen)
                {
                    if (GameCamera.instance)
                    {
                        Traverse.Create(GameCamera.instance).Field("m_mouseCapture").SetValue(false);
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = ZInput.IsMouseActive();
                    }
                    windowRect = GUI.Window(424243, windowRect, new GUI.WindowFunction(WindowBuilder), "");
                    if (!Input.GetKey(KeyCode.Mouse0) && (windowRect.x != windowPosition.Value.x || windowRect.y != windowPosition.Value.y))
                    {
                        windowPosition.Value = new Vector2(windowRect.x, windowRect.y);
                    }

                }
                return;
            }
            if (!ZNet.instance?.IsServer() == true && Player.m_localPlayer) 
            {
                if (storeOpen)
                {
                    Traverse.Create(GameCamera.instance).Field("m_mouseCapture").SetValue(false);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = ZInput.IsMouseActive();
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

            GUILayout.BeginHorizontal();
            GUILayout.Label(myCurrencyString.Value, currencyStyle);
            if (coinBeforeAmount.Value)
            {
                GUILayout.Space(5);
                GUILayout.Button(textureDict["currency"], coinStyle, new GUILayoutOption[] { GUILayout.Width(currencyFontSize.Value * coinFactor), GUILayout.Height(currencyFontSize.Value) });
            }
            GUILayout.Space(5);
            GUILayout.Label(string.Format(currencyString.Value, myCurrency), currencyStyle);
            if (!coinBeforeAmount.Value)
            {
                GUILayout.Space(5);
                GUILayout.Button(textureDict["currency"], coinStyle, new GUILayoutOption[] { GUILayout.Width(currencyFontSize.Value * coinFactor), GUILayout.Height(currencyFontSize.Value) });
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            float width = windowWidth.Value - 70;
            float itemWidth = width / packagesPerRow.Value;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, new GUILayoutOption[] { GUILayout.Width(windowWidth.Value - 20) });
            GUILayout.Space(10);
            GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(width) });
            GUILayout.BeginHorizontal();
            storePackages.Sort(delegate (PackageInfo a, PackageInfo b) { return a.price.CompareTo(b.price); });
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
                        if (testing.Value)
                        {
                            myCurrency -= pi.price;
                            storeOpen = false;
                            PlayEffects();
                        }
                        else
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
                }

                GUILayout.Label(string.Format(packageString.Value, pi.name), labelStyle, new GUILayoutOption[] { GUILayout.Width(itemWidth) });
                GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Width(itemWidth) });
                GUILayout.FlexibleSpace();
                if (coinBeforeAmount.Value)
                {
                    GUILayout.Button(textureDict["currency"], coinStyle, new GUILayoutOption[] { GUILayout.Width(labelFontSize.Value * coinFactor), GUILayout.Height(labelFontSize.Value) });
                    GUILayout.Space(5);
                }
                GUILayout.Label(string.Format(currencyString.Value, pi.price), labelStyle);
                if (!coinBeforeAmount.Value)
                {
                    GUILayout.Space(5);
                    GUILayout.Button(textureDict["currency"], coinStyle, new GUILayoutOption[] { GUILayout.Width(labelFontSize.Value * coinFactor), GUILayout.Height(labelFontSize.Value) });
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

    }
}
