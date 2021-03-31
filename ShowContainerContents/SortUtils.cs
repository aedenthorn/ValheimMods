using ShowContainerContents;
using System.Collections.Generic;
using UnityEngine;

public class SortUtils
{
    public static void SortByType(BepInExPlugin.SortType type, List<ItemDrop.ItemData> items, bool asc)
    {
        // combine
        SortByName(items, true);

        for (int i = 0; i < items.Count; i++)
        {
            while (i < items.Count - 1 && items[i].m_stack < items[i].m_shared.m_maxStackSize && items[i + 1].m_shared.m_name == items[i].m_shared.m_name)
            {
                int amount = Mathf.Min(items[i].m_shared.m_maxStackSize - items[i].m_stack, items[i + 1].m_stack);
                items[i].m_stack += amount;
                if (amount == items[i + 1].m_stack)
                {
                    items.RemoveAt(i + 1);
                }
                else
                    items[i + 1].m_stack -= amount;
            }
        }
        switch (type)
        {
            case BepInExPlugin.SortType.Name:
                SortByName(items, asc);
                break;
            case BepInExPlugin.SortType.Weight:
                SortByWeight(items, asc);
                break;
            case BepInExPlugin.SortType.Value:
                SortByValue(items, asc);
                break;
            case BepInExPlugin.SortType.Amount:
                SortByAmount(items, asc);
                break;
        }
    }

    public static void SortByName(List<ItemDrop.ItemData> items, bool asc)
    {
        items.Sort(delegate (ItemDrop.ItemData a, ItemDrop.ItemData b) {

            if (a.m_shared.m_name == b.m_shared.m_name)
            {
                return CompareInts(a.m_stack, b.m_stack, false);
            }
            return CompareStrings(Localization.instance.Localize(a.m_shared.m_name), Localization.instance.Localize(b.m_shared.m_name), asc);
        });
    }
    public static void SortByWeight(List<ItemDrop.ItemData> items, bool asc)
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
    public static void SortByValue(List<ItemDrop.ItemData> items, bool asc)
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

    public static void SortByAmount(List<ItemDrop.ItemData> items, bool asc)
    {
        items.Sort(delegate (ItemDrop.ItemData a, ItemDrop.ItemData b) {
            if (a.m_stack == b.m_stack)
            {
                return CompareStrings(Localization.instance.Localize(a.m_shared.m_name), Localization.instance.Localize(b.m_shared.m_name), true);
            }
            return CompareInts(a.m_stack, b.m_stack, asc);
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
