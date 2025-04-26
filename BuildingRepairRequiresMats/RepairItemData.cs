using System.Collections.Generic;

namespace BuildingRepairRequiresMats
{
    public class RepairItemData : ItemDrop.ItemData
    {
        public List<string> reqstring;
        public ItemDrop.ItemData item;

        public RepairItemData(ItemDrop.ItemData item, List<string> reqstring = null)
        {
            this.reqstring = reqstring;
            this.item = item;
        }
    }
}