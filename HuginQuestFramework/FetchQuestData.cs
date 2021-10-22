using System;
using static HuginQuestFramework.BepInExPlugin;

namespace HuginQuestFramework
{
    [Serializable]
    public class FetchQuestData
    {
        public string questName = "";
        public string questDesc = "";
        public string stageName = "";
        public string stageDesc = "";
        public string objectiveName = "";
        public string objectiveDesc = "";
        public string ID;
        public QuestType type = QuestType.Fetch;
        public string thing = "";
        public int amount = 0;
        public int rewardAmount = 0;
        public string rewardName = "";
    }
}