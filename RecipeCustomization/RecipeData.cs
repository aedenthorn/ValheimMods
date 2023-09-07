using System.Collections.Generic;

namespace RecipeCustomization
{
    internal class RecipeData
    {
        public string name;
        public int originalAmount;
        public string craftingStation;
        public int minStationLevel;
        public int amount;
        public bool disabled;
        public List<string> reqs = new List<string>();
    }
}