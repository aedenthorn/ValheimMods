using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HaldorFetchQuests
{
    [BepInPlugin("aedenthorn.HaldorFetchQuests", "Haldor Fetch Quests", "0.4.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<string> modKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> maxQuests;
        public static ConfigEntry<float> questRefreshInterval;
        public static ConfigEntry<int> minAmount;
        public static ConfigEntry<int> maxAmount;
        public static ConfigEntry<int> maxReward;
        public static ConfigEntry<int> worthlessItemValue;
        public static ConfigEntry<float> fetchRewardMult;
        public static ConfigEntry<float> killRewardMult;
        public static ConfigEntry<float> rewardFluctuation;
        public static ConfigEntry<float> killToFetchRatio;
        
        public static ConfigEntry<string> acceptButtonText;
        public static ConfigEntry<string> questNameString;
        public static ConfigEntry<string> questDescString;
        public static ConfigEntry<string> returnString;
        public static ConfigEntry<string> returnDescString;
        public static ConfigEntry<string> startString;
        public static ConfigEntry<string> completeString;
        public static ConfigEntry<string> killQuestString;
        public static ConfigEntry<string> fetchQuestString;
        public static ConfigEntry<string> killQuestDescString;
        public static ConfigEntry<string> fetchQuestDescString;
        public static ConfigEntry<string> killQuestProgressString;
        public static ConfigEntry<string> fetchQuestProgressString;
        public static BepInExPlugin context;
        
        public static double lastRefreshTime = 0;

        public static string buyButtonText = "";
        public static Dictionary<string, FetchQuestData> currentQuestDict;
        public static List<GameObject> possibleKillList = new List<GameObject>();
        public static List<GameObject> possibleFetchList = new List<GameObject>();
        public static Assembly betterTraderAssembly;

        public enum FetchType
        {
            Fetch,
            Kill
        }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            //nexusID = Config.Bind<int>("General", "NexusID", 1573, "Nexus mod ID for updates");

            questRefreshInterval = Config.Bind<float>("Options", "QuestRefreshInterval", 60 * 60 * 24, "Number of game seconds between refreshing the list of quests (default every 24 hours).");
            maxQuests = Config.Bind<int>("Options", "MaxQuests", 3, "Number of quests to offer at once.");
            minAmount = Config.Bind<int>("Options", "MinAmount", 3, "Minimum number of things in quest.");
            maxAmount = Config.Bind<int>("Options", "MaxAmount", 10, "Maximum number of things in quest.");
            
            killToFetchRatio = Config.Bind<float>("Options", "killToFetchRatio", 0.5f, "Kill to fetch ration (0.5 = 50/50, 0 = all fetch, 1 = all kill).");
            
            worthlessItemValue = Config.Bind<int>("Options", "WorthlessItemValue", 1, "Fetch value of items that have no specified value (most items).");
            fetchRewardMult = Config.Bind<float>("Options", "FetchRewardMult", 10f, "Multiple of item cost as reward.");
            killRewardMult = Config.Bind<float>("Options", "KillRewardMult", 0.2f, "Multiple of creature max health as reward.");
            rewardFluctuation = Config.Bind<float>("Options", "RewardFluctuation", 0.5f, "Reward can fluctuate by this much fraction.");

            acceptButtonText = Config.Bind<string>("Text", "AcceptButtonText", "Accept", "Text for button to accept quest.");
            questNameString = Config.Bind<string>("Text", "QuestString", "Haldor's Request", "Main quest header string.");
            questDescString = Config.Bind<string>("Text", "QuestDescString", "Complete this quest for a reward of {reward} gold.", "Main quest subheader string. {reward} is replaced by the reward.");
            returnString = Config.Bind<string>("Text", "ReturnString", "Return to Haldor.", "Return objective.");
            returnDescString = Config.Bind<string>("Text", "ReturnDescString", "Talk to the merchant for your reward.", "Return objective description.");
            startString = Config.Bind<string>("Text", "StartString", "Quest Started.", "HUD message on quest start.");
            completeString = Config.Bind<string>("Text", "CompleteString", "Quest Completed.", "HUD message on quest completion.");
            killQuestString = Config.Bind<string>("Text", "KillQuestString", "Kill Quest", "Kill quest string.");
            fetchQuestString = Config.Bind<string>("Text", "FetchQuestString", "Fetch Quest", "Fetch quest string.");
            killQuestDescString = Config.Bind<string>("Text", "KillQuestDescString", "Kill {amount} {thing}", "Kill quest string. {amount} is replaced with the amount to kill. {thing} is replaced with the thing to kill.");
            fetchQuestDescString = Config.Bind<string>("Text", "FetchQuestDescString", "Bring {amount} {thing} to Haldor.", "Fetch quest string. {amount} is replaced with the amount to fetch. {thing} is replaced with the thing to fetch.");
            killQuestProgressString = Config.Bind<string>("Text", "KillQuestProgressString", "Killed {current}/{total}", "Kill quest progress string. {current} is replaced with the amount alread killed. {total} is replaced with the total amount to kill.");
            fetchQuestProgressString = Config.Bind<string>("Text", "FetchQuestProgressString", "Have {current}/{total}", "Fetch quest progress string. {current} is replaced with the amount carried. {total} is replaced with the total amount to fetch.");

        }
        public void Start()
        {
            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            if(Chainloader.PluginInfos.ContainsKey("Menthus.bepinex.plugins.BetterTrader"))
            {
                betterTraderAssembly = Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.GetType().Assembly;
                harmony.Patch(
                    original: AccessTools.Method(betterTraderAssembly.GetType("BetterTrader.ItemElementUI"), "UpdateTradePrice"),
                    prefix: new HarmonyMethod(typeof(BepInExPlugin), nameof(BepInExPlugin.BetterTrader_ItemElementUI_UpdateTradePrice_Prefix))
                );
                harmony.Patch(
                    original: AccessTools.Method(betterTraderAssembly.GetType("BetterTrader.ItemElementUI"), "UpdateTint"),
                    prefix: new HarmonyMethod(typeof(BepInExPlugin), nameof(BepInExPlugin.BetterTrader_ItemElementUI_UpdateTint_Prefix))
                );
                harmony.Patch(
                    original: AccessTools.Method(betterTraderAssembly.GetType("BetterTrader.ItemElementUI"), "SetSelectionIndicatorActive"),
                    prefix: new HarmonyMethod(typeof(BepInExPlugin), nameof(BepInExPlugin.BetterTrader_ItemElementUI_SetSelectionIndicatorActive_Prefix))
                );
                harmony.Patch(
                    original: AccessTools.Method(betterTraderAssembly.GetType("BetterTrader.ItemElementUIListView"), "SetupElements"),
                    prefix: new HarmonyMethod(typeof(BepInExPlugin), nameof(BepInExPlugin.BetterTrader_ItemElementUIListView_SetupElements_Prefix))
                );
            }
        }
    }
}
