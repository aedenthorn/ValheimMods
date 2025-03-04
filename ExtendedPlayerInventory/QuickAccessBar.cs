using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedPlayerInventory
{
    public class QuickAccessBar : MonoBehaviour
    {
		public void Update()
		{
			Player localPlayer = Player.m_localPlayer;
			UpdateIcons(localPlayer);
		}

		public void UpdateIcons(Player player)
		{
			if (!player || player.IsDead())
			{
				foreach (ElementData elementData in m_elements)
				{
					Destroy(elementData.m_go);
				}
				m_elements.Clear();
				return;
			}

			m_items.Clear();

			Inventory inv = player.GetInventory();
			if(inv.GetItemAt(5, inv.GetHeight() - 1) != null)
				m_items.Add(inv.GetItemAt(5, inv.GetHeight() - 1));
			if(inv.GetItemAt(6, inv.GetHeight() - 1) != null)
				m_items.Add(inv.GetItemAt(6, inv.GetHeight() - 1));
			if(inv.GetItemAt(7, inv.GetHeight() - 1) != null)
				m_items.Add(inv.GetItemAt(7, inv.GetHeight() - 1));

			m_items.Sort((ItemDrop.ItemData x, ItemDrop.ItemData y) => x.m_gridPos.x.CompareTo(y.m_gridPos.x));
			int num = 0;
			foreach (ItemDrop.ItemData itemData in m_items)
			{
				if (itemData.m_gridPos.x - 4 > num)
				{
					num = itemData.m_gridPos.x - 4;
				}
			}

			if (m_elements.Count != num)
			{
				foreach (ElementData elementData in m_elements)
				{
                    Destroy(elementData.m_go);
				}
				m_elements.Clear();
				for (int i = 0; i < num; i++)
				{
					ElementData elementData = new ElementData();
					elementData.m_go = Instantiate(m_elementPrefab, transform);
					elementData.m_go.transform.localPosition = new Vector3(i * m_elementSpace, 0f, 0f);
					BepInExPlugin.SetSlotText(BepInExPlugin.hotkeys[i].Value, elementData.m_go.transform, false);
					elementData.m_icon = elementData.m_go.transform.transform.Find("icon").GetComponent<Image>();
					elementData.m_durability = elementData.m_go.transform.Find("durability").GetComponent<GuiBar>();
					elementData.m_amount = elementData.m_go.transform.Find("amount").GetComponent<TMP_Text>();
					elementData.m_equiped = elementData.m_go.transform.Find("equiped").gameObject;
					elementData.m_queued = elementData.m_go.transform.Find("queued").gameObject;
					elementData.m_selection = elementData.m_go.transform.Find("selected").gameObject;
					m_elements.Add(elementData);
				}
			}

			foreach (ElementData elementData in m_elements)
			{
				elementData.m_used = false;
			}
			bool flag = ZInput.IsGamepadActive();
			for (int j = 0; j < m_items.Count; j++)
			{
				ItemDrop.ItemData itemData2 = m_items[j];
                ElementData elementData = m_elements[itemData2.m_gridPos.x - 5];
				elementData.m_used = true;
				elementData.m_icon.gameObject.SetActive(true);
				elementData.m_icon.sprite = itemData2.GetIcon();
				elementData.m_durability.gameObject.SetActive(itemData2.m_shared.m_useDurability);
				if (itemData2.m_shared.m_useDurability)
				{
					if (itemData2.m_durability <= 0f)
					{
						elementData.m_durability.SetValue(1f);
						elementData.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
					}
					else
					{
						elementData.m_durability.SetValue(itemData2.GetDurabilityPercentage());
						elementData.m_durability.ResetColor();
					}
				}
				elementData.m_equiped.SetActive(itemData2.m_equipped);
				elementData.m_queued.SetActive(player.IsEquipActionQueued(itemData2));
				if (itemData2.m_shared.m_maxStackSize > 1)
				{
					elementData.m_amount.gameObject.SetActive(true);
					elementData.m_amount.text = itemData2.m_stack.ToString() + "/" + itemData2.m_shared.m_maxStackSize.ToString();
				}
				else
				{
					elementData.m_amount.gameObject.SetActive(false);
				}
			}

			for (int k = 0; k < m_elements.Count; k++)
			{
                ElementData elementData = m_elements[k];
				elementData.m_selection.SetActive(flag && k == m_selected);
				if (!elementData.m_used)
				{
					elementData.m_icon.gameObject.SetActive(false);
					elementData.m_durability.gameObject.SetActive(false);
					elementData.m_equiped.SetActive(false);
					elementData.m_queued.SetActive(false);
					elementData.m_amount.gameObject.SetActive(false);
				}
			}
		}

		public GameObject m_elementPrefab;

		public float m_elementSpace = 70f;

		public int m_selected;

		public List<ElementData> m_elements = new List<ElementData>();

		public List<ItemDrop.ItemData> m_items = new List<ItemDrop.ItemData>();

		public class ElementData
		{
				public bool m_used;

				public GameObject m_go;

				public Image m_icon;

				public GuiBar m_durability;

				public TMP_Text m_amount;

				public GameObject m_equiped;

				public GameObject m_queued;

				public GameObject m_selection;
		}
	}
}