using System;
using System.Collections.Generic;

namespace QuestFramework
{
    [Serializable]
    public class QuestDataObject
    {
        public Dictionary<string, QuestData> questDict = new Dictionary<string, QuestData>();
    }
    [Serializable]
    public class QuestData
    {
        public string name = "";
        public string desc = "";
        public string ID = "";
        public string currentStage = "";
        public Dictionary<string, object> data = new Dictionary<string, object>();
        public Dictionary<string, QuestStage> questStages = new Dictionary<string, QuestStage>();
    }
    [Serializable]
    public class QuestStage
    {
        public string name = "";
        public string desc = "";
        public string ID = "";
        public Dictionary<string, object> data = new Dictionary<string, object>();
        public Dictionary<string, QuestObjective> objectives = new Dictionary<string, QuestObjective>();
    }
    [Serializable]
    public class QuestObjective
    {
        public string name = "";
        public string desc = "";
        public string ID = "";
        public Dictionary<string, object> data = new Dictionary<string, object>();
        public bool completed = false;
    }
}