public class ItemData
{
    public ItemDrop.ItemData idd;
    public ItemDrop.ItemData.SharedData m_shared;
    public int m_stack;

    public ItemData(ItemDrop.ItemData idd)
    {
        m_shared = idd.m_shared;
        m_stack = idd.m_stack;
    }
}