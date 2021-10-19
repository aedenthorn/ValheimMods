using System;
using System.Collections.Generic;

namespace HaldorFetchQuests
{
    [Serializable]
    public class FetchQuestDataObject
    {
        public Dictionary<string, FetchQuestData> questDict = new Dictionary<string, FetchQuestData>();
        public double lastRefresh = 0;
    }
}