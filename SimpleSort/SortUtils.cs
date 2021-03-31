using HarmonyLib;
using System.Collections.Generic;

public class SortUtils
{

    public static void SortByName(List<ItemDrop.ItemData> items, bool asc, bool player)
    {
        items.Sort(delegate (ItemDrop.ItemData a, ItemDrop.ItemData b) {

            if (a.m_shared.m_name == b.m_shared.m_name)
            {
                return CompareInts(a.m_stack, b.m_stack, false);
            }
            return CompareStrings(Localization.instance.Localize(a.m_shared.m_name), Localization.instance.Localize(b.m_shared.m_name), asc);
        });
    }
    public static void SortByWeight(List<ItemDrop.ItemData> items, bool asc, bool player)
    {
        items.Sort(delegate (ItemDrop.ItemData a, ItemDrop.ItemData b) {
            if (a.m_shared.m_weight == b.m_shared.m_weight)
            {
                if (a.m_shared.m_name == b.m_shared.m_name)
                    return CompareInts(a.m_stack, b.m_stack, false);
                return CompareStrings(Localization.instance.Localize(a.m_shared.m_name), Localization.instance.Localize(b.m_shared.m_name), true);
            }
            return CompareFloats(a.m_shared.m_weight, b.m_shared.m_weight, asc);
        });
    }
    public static void SortByValue(List<ItemDrop.ItemData> items, bool asc, bool player)
    {
        items.Sort(delegate (ItemDrop.ItemData a, ItemDrop.ItemData b) {
            if (a.m_shared.m_value == b.m_shared.m_value)
            {
                if (a.m_shared.m_name == b.m_shared.m_name)
                    return CompareInts(a.m_stack, b.m_stack, false);
                return CompareStrings(Localization.instance.Localize(a.m_shared.m_name), Localization.instance.Localize(b.m_shared.m_name), true);
            }
            return CompareInts(a.m_shared.m_value, b.m_shared.m_value, asc);
        });
    }

    public static int CompareStrings(string a, string b, bool asc)
    {
        if (asc)
            return a.CompareTo(b);
        else
            return b.CompareTo(a);
    }

    public static int CompareFloats(float a, float b, bool asc)
    {
        if (asc)
            return a.CompareTo(b);
        else
            return b.CompareTo(a);
    }

    public static int CompareInts(float a, float b, bool asc)
    {
        if (asc)
            return a.CompareTo(b);
        else
            return b.CompareTo(a);
    }
}
