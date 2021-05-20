using System;
using System.Collections.Generic;

namespace CraftingFilter
{
    public class CategoryData
    {
        public List<string> categories = new List<string>();
        public CategoryData()
        {
            foreach(string type in Enum.GetNames(typeof(ItemDrop.ItemData.ItemType)))
            {
                categories.Add(type + ":" + type);
            }
        }
    }
}