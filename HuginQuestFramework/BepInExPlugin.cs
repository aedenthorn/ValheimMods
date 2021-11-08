using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using QuestFramework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HuginQuestFramework
{
    [BepInPlugin("aedenthorn.HuginQuestFramework", "Hugin Quest Framework", "0.2.5")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<string> modKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> maxQuests;
        public static ConfigEntry<float> questCheckInterval;
        public static ConfigEntry<float> questChance;
        public static ConfigEntry<float> randomFetchQuestWeight;
        public static ConfigEntry<float> randomKillQuestWeight;
        public static ConfigEntry<float> randomBuildQuestWeight;
        public static ConfigEntry<float> randomFetchRewardMult;
        public static ConfigEntry<float> randomKillRewardMult;
        public static ConfigEntry<float> randomBuildRewardMult;
        public static ConfigEntry<int> randomWorthlessItemValue;
        public static ConfigEntry<int> minAmount;
        public static ConfigEntry<int> maxAmount;
        public static ConfigEntry<int> maxReward;
        public static ConfigEntry<float> rewardFluctuation;
        public static ConfigEntry<bool> allowUnknownFetchMaterials;
        
        public static ConfigEntry<string> randomFetchQuestName;
        public static ConfigEntry<string> randomKillQuestName;
        public static ConfigEntry<string> randomBuildQuestName;
        public static ConfigEntry<string> randomFetchQuestObjectiveName;
        public static ConfigEntry<string> randomKillQuestObjectiveName;
        public static ConfigEntry<string> randomBuildQuestObjectiveName;
        public static ConfigEntry<string> randomQuestRewardText;
        public static ConfigEntry<string> randomQuestProgressText;
        public static ConfigEntry<string> questDeclinedDialogue;
        public static ConfigEntry<string> questAcceptedDialogue;
        public static ConfigEntry<string> noRoomDialogue;
        public static ConfigEntry<string> completedDialogue;
        public static ConfigEntry<string> haveRewardDialogue;
        public static ConfigEntry<string> haveQuestDialogue;
        public static ConfigEntry<string> declineButtonText;
        public static ConfigEntry<string> acceptButtonText;
        public static ConfigEntry<string> questNameString;
        public static ConfigEntry<string> questDescString;
        public static ConfigEntry<string> returnString;
        public static ConfigEntry<string> returnDescString;
        public static ConfigEntry<string> startString;
        public static ConfigEntry<string> completeString;
        public static ConfigEntry<string> killQuestString;
        public static ConfigEntry<string> fetchQuestString;
        

        private static BepInExPlugin context;
        
        public static double lastCheckTime = 0;
        public static Raven.RavenText currentText = new Raven.RavenText();
        public static bool showQuestAcceptWindow;
        public static bool respondedToQuest = false;
        public static QuestData nextQuest;
        public static QuestData finishedQuest;

        public static GUIStyle titleStyle;
        public static GUIStyle subTitleStyle;
        public static GUIStyle descStyle;
        public static float windowWidth = 400;
        public static float windowHeight = 300;
        public static int windowID = 1890175404;
        public static Transform questDialogueTransform;
        public static Transform questDialogueSubtitleTransform;
        public static Transform questDialogueTitleTransform;

        public static Dictionary<string, HuginQuestData> huginQuestDict = new Dictionary<string, HuginQuestData>();
        public static List<GameObject> possibleKillList = new List<GameObject>();
        public static List<GameObjectReward> possibleFetchList = new List<GameObjectReward>();
        public static List<GameObjectReward> possibleBuildList = new List<GameObjectReward>();
        public enum QuestType
        {
            Fetch,
            Kill,
            Build
        }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1588, "Nexus mod ID for updates");

            questCheckInterval = Config.Bind<float>("Options", "QuestCheckInterval", 60f * 60f, "Number of game seconds between checking for a new quest (default every game hour).");
            questChance = Config.Bind<float>("Options", "QuestChance", 0.1f, "Chance of a quest being offered on each interval.");
            maxQuests = Config.Bind<int>("Options", "MaxQuests", 1, "Number of quests to allow at once.");
            allowUnknownFetchMaterials = Config.Bind<bool>("Options", "AllowUnknownFetchMaterials", true, "All fetch quests for unknown materials.");

            randomFetchQuestWeight = Config.Bind<float>("Random", "RandomFetchQuestWeight", 1f, "Chance of a random quest being a fetch quest.");
            randomKillQuestWeight = Config.Bind<float>("Random", "RandomKillQuestWeight", 1f, "Chance of a random quest being a kill quest.");
            randomBuildQuestWeight = Config.Bind<float>("Random", "RandomBuildQuestWeight", 1f, "Chance of a random quest being a build quest.");
            randomFetchRewardMult = Config.Bind<float>("Random", "RandomFetchRewardMult", 3f, "Multiple of item cost as reward for random fetch quests.");
            randomKillRewardMult = Config.Bind<float>("Random", "RandomKillRewardMult", 0.2f, "Multiple of creature max health as reward for random kill quests.");
            randomBuildRewardMult = Config.Bind<float>("Random", "RandomBuildRewardMult", 5f, "Multiple of build ingredients cost as reward for random build quests.");
            randomWorthlessItemValue = Config.Bind<int>("Random", "RandomWorthlessItemValue", 1, "Fetch value of items that have no specified value.");

            minAmount = Config.Bind<int>("Options", "MinAmount", 3, "Minimum number of things in quests without specified amounts.");
            maxAmount = Config.Bind<int>("Options", "MaxAmount", 10, "Maximum number of things in quest without specified amounts.");
            
            rewardFluctuation = Config.Bind<float>("Options", "RewardFluctuation", 0.5f, "Reward can fluctuate by this much fraction.");

            randomFetchQuestName = Config.Bind<string>("Text", "RandomFetchQuestName", "{thing} Collector", "Name of random fetch quests.");
            randomKillQuestName = Config.Bind<string>("Text", "RandomKillQuestName", "{thing} Killer", "Name of random kill quests.");
            randomBuildQuestName = Config.Bind<string>("Text", "RandomBuildQuestName", "{thing} Builder", "Name of random build quests.");
            randomFetchQuestObjectiveName = Config.Bind<string>("Text", "RandomFetchQuestObjectiveName", "Fetch {amount} {thing}", "Objective name of random fetch quests.");
            randomKillQuestObjectiveName = Config.Bind<string>("Text", "RandomKillQuestObjectiveName", "Kill {amount} {thing}", "Objective name of random kill quests.");
            randomBuildQuestObjectiveName = Config.Bind<string>("Text", "RandomBuildQuestObjectiveName", "Build {amount} {thing}", "Objective name of random build quests.");
            randomQuestProgressText = Config.Bind<string>("Text", "RandomQuestProgressText", "Progress {progress}/{amount}", "Progress text of random quests.");
            randomQuestRewardText = Config.Bind<string>("Text", "RandomQuestRewardText", "Hugin will reward you {rewardAmount} {rewardName}", "Progress text of random quests.");
            questAcceptedDialogue = Config.Bind<string>("Text", "QuestAcceptedDialogue", "May you be victorious in your endeavor...", "Hugin's dialogue when accepting a quest.");
            questDeclinedDialogue = Config.Bind<string>("Text", "QuestDeclinedDialogue", "Perhaps next time...", "Hugin's dialogue when declining a quest.");
            noRoomDialogue = Config.Bind<string>("Text", "NoRoomDialogue", "You have no room for your reward...", "Hugin's dialogue if you have no room for your reward.");
            completedDialogue = Config.Bind<string>("Text", "CompletedDialogue", "Well done... until next we meet!", "Hugin's dialogue after completing quest.");
            haveRewardDialogue = Config.Bind<string>("Text", "HaveRewardDialogue", "Come take your reward...", "Hugin's dialogue when there's a completed quest.");
            haveQuestDialogue = Config.Bind<string>("Text", "HaveQuestDialogue", "I have a quest for you...", "Hugin's dialogue when there's a quest.");
            declineButtonText = Config.Bind<string>("Text", "DeclineButtonText", "Decline", "Text for button to decline quest.");
            acceptButtonText = Config.Bind<string>("Text", "AcceptButtonText", "Accept", "Text for button to accept quest.");
            questNameString = Config.Bind<string>("Text", "QuestString", "Hugin's Request", "Main quest header string.");
            returnString = Config.Bind<string>("Text", "ReturnString", "Talk to Hugin.", "Return objective.");
            returnDescString = Config.Bind<string>("Text", "ReturnDescString", "Talk to the raven for your reward.", "Return objective description.");
            startString = Config.Bind<string>("Text", "StartString", "Quest Started.", "HUD message on quest start.");
            completeString = Config.Bind<string>("Text", "CompleteString", "Quest Completed.", "HUD message on quest completion.");
            killQuestString = Config.Bind<string>("Text", "KillQuestString", "Kill Quest", "Kill quest string.");
            fetchQuestString = Config.Bind<string>("Text", "FetchQuestString", "Fetch Quest", "Fetch quest string.");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

    }
}
