using System.Collections.Generic;

namespace QuestFramework
{
    public static class QuestFrameworkAPI
    {
        public static Dictionary<string, QuestData> GetCurrentQuests()
        {
            return BepInExPlugin.currentQuests.questDict;
        }
        public static QuestData GetQuestByID(string ID)
        {
            if (!BepInExPlugin.currentQuests.questDict.ContainsKey(ID))
            {
                BepInExPlugin.Dbgl($"Quest {ID} not active");
                return null;
            }
            return BepInExPlugin.currentQuests.questDict[ID];
        }
        public static bool AddQuest(QuestData qd, bool force = false)
        {
            if (BepInExPlugin.currentQuests.questDict.ContainsKey(qd.ID) && !force)
            {
                BepInExPlugin.Dbgl($"Quest {qd.ID} already active");
                return false;
            }
            BepInExPlugin.currentQuests.questDict[qd.ID] = qd;
            BepInExPlugin.Dbgl($"Quest {qd.ID} added");
            BepInExPlugin.RefreshQuestString();
            return true;
        }
        public static bool RemoveQuest(string ID)
        {
            if (!BepInExPlugin.currentQuests.questDict.ContainsKey(ID))
            {
                BepInExPlugin.Dbgl($"Quest {ID} not active");
                return false;
            }
            BepInExPlugin.currentQuests.questDict.Remove(ID);
            BepInExPlugin.Dbgl($"Quest {ID} removed");
            BepInExPlugin.RefreshQuestString();
            return true;
        }
        public static bool IsQuestActive(string ID)
        {
            return BepInExPlugin.currentQuests.questDict.ContainsKey(ID);
        }
        public static bool ChangeStage(string questID, string stageID)
        {
            if (!BepInExPlugin.currentQuests.questDict.ContainsKey(questID))
            {
                BepInExPlugin.Dbgl($"Quest {questID} not active");
                return false;
            }
            if (!BepInExPlugin.currentQuests.questDict[questID].questStages.ContainsKey(stageID))
            {
                BepInExPlugin.Dbgl($"Quest does not contain a stage called {stageID}");
                return false;
            }
            BepInExPlugin.currentQuests.questDict[questID].currentStage = stageID;
            BepInExPlugin.Dbgl($"Quest {questID} stage changed to {stageID}");
            BepInExPlugin.RefreshQuestString();
            return true;
        }
        public static string GetCurrentStage(string questID)
        {
            if (!BepInExPlugin.currentQuests.questDict.ContainsKey(questID))
            {
                BepInExPlugin.Dbgl($"Quest {questID} not active");
                return null;
            }
            return BepInExPlugin.currentQuests.questDict[questID].currentStage;
        }
        public static string GetQuestString(string questID)
        {
            if (!BepInExPlugin.currentQuests.questDict.ContainsKey(questID))
            {
                BepInExPlugin.Dbgl($"Quest {questID} not active");
                return null;
            }
            return BepInExPlugin.MakeQuestString(BepInExPlugin.currentQuests.questDict[questID]);
        }
        public static string GetQuestString(QuestData questData)
        {
            return BepInExPlugin.MakeQuestString(questData);
        }
    }
}
