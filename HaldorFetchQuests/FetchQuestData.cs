using System;
using static HaldorFetchQuests.BepInExPlugin;

namespace HaldorFetchQuests
{
    [Serializable]
    public class FetchQuestData
    {
        public string ID;
        public FetchType type;
        public string thing;
        public int amount;
        public int reward;
    }
}