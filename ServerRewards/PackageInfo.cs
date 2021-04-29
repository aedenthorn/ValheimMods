using System;
using System.Collections.Generic;

namespace ServerRewards
{
    public class PackageInfo
    {
        public string id;
        public string name;
        public int price;
        public string type;
        public List<string> items;

        public string StoreString()
        {
            return string.Join(",", new string[] { id, name, type, price+"" });
        }
    }
}