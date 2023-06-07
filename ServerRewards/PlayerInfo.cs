using System.Collections.Generic;

namespace ServerRewards
{
    public class PlayerInfo
    {
        public bool online = false;
        public int currency = 0;
        public long lastLogin = 0;
        public int consecutiveDays = 0;
        public int maxConsecutiveDays = 0;
        public List<string> packages = new List<string>();
        public string id;
    }
}