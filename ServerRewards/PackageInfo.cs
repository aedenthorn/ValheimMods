using System;
using System.Collections.Generic;

namespace ServerRewards
{
    public class PackageInfo
    {
        public string id;
        public string name;
        public string description;
        public int price;
        public int limit;
        public string type;
        public List<string> items;

        public string StoreString()
        {
            return string.Join(",", new string[] { id, name, description, type, price+"" });
        }
    }
}