using BepInEx;
using HarmonyLib;
using QuestFramework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static ItemDrop.ItemData;

namespace HaldorFetchQuests
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix(StoreGui __instance)
            {
                if (!modEnabled.Value)
                    return;
                currentQuestDict = null;

                possibleKillList = ((Dictionary<int, GameObject>)AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs").GetValue(ZNetScene.instance)).Values.ToList().FindAll(g => g.GetComponent<MonsterAI>() || g.GetComponent<AnimalAI>());
                possibleFetchList = ObjectDB.instance.m_items.FindAll(g => g.GetComponent<ItemDrop>() && (g.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType == ItemType.Material || g.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType == ItemType.Consumable));
                Dbgl($"got {possibleFetchList.Count} possible fetch items and {possibleKillList.Count} possible kill items");


                if (File.Exists(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{Game.instance.GetPlayerProfile().GetName()}_{ZNet.instance.GetWorldName()}")))
                {
                    using (Stream stream = File.Open(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{Game.instance.GetPlayerProfile().GetName()}_{ZNet.instance.GetWorldName()}"), FileMode.Open))
                    {
                        var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        var obj = (FetchQuestDataObject)binaryFormatter.Deserialize(stream);
                        currentQuestDict = obj.questDict;
                        lastRefreshTime = obj.lastRefresh;
                    }
                    Dbgl($"Got {currentQuestDict.Count} saved available quests, last refresh {lastRefreshTime}, current time {ZNet.instance.GetTimeSeconds()}");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerProfile), "SavePlayerToDisk")]
        static class PlayerProfile_SavePlayerToDisk_Patch
        {
            static void Prefix()
            {
                if (!modEnabled.Value || !ZNet.instance || !Player.m_localPlayer)
                    return;
                if (currentQuestDict == null && !File.Exists(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{Game.instance.GetPlayerProfile().GetName()}_{ZNet.instance.GetWorldName()}")))
                        return;
                using (Stream stream = File.Open(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{Game.instance.GetPlayerProfile().GetName()}_{ZNet.instance.GetWorldName()}"), FileMode.Create))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    var obj = new FetchQuestDataObject()
                    {
                        questDict = currentQuestDict,
                        lastRefresh = lastRefreshTime
                    };
                    binaryFormatter.Serialize(stream, obj);
                    Dbgl($"Available quests saved");
                }
            }
        }

        [HarmonyPatch(typeof(Character), "RPC_Damage")]
        static class Character_RPC_Damage_Patch
        {
            static void Postfix(Character __instance, HitData hit)
            {
                if (!modEnabled.Value || __instance.GetHealth() > 0 || hit.GetAttacker() != Player.m_localPlayer)
                    return;
                AdvanceKillQuests(__instance);
            }
        }
        
        [HarmonyPatch(typeof(Inventory), "Changed")]
        static class Inventory_Changed_Patch
        {
            static void Postfix(Inventory __instance)
            {
                if (!modEnabled.Value || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                    return;
                AdjustFetchQuests();
            }
        }

        [HarmonyPatch(typeof(StoreGui), "Show")]
        static class StoreGui_Show_Patch
        {
            static void Prefix(StoreGui __instance)
            {
                if (!modEnabled.Value)
                    return;
                CheckQuestFulfilled(__instance);
            }
        }
        
        [HarmonyPatch(typeof(StoreGui), "UpdateBuyButton")]
        static class StoreGui_UpdateBuyButton_Patch
        {
            static void Postfix(StoreGui __instance, Trader.TradeItem ___m_selectedItem)
            {
                if (!modEnabled.Value || ___m_selectedItem == null)
                    return;

                if(buyButtonText == "")
                {
                    buyButtonText = __instance.m_buyButton.GetComponentInChildren<Text>().text;
                }

                if (currentQuestDict != null && currentQuestDict.ContainsKey(___m_selectedItem.m_prefab.m_itemData.m_crafterName))
                {
                    __instance.m_buyButton.GetComponentInChildren<Text>().text = acceptButtonText.Value;
                    __instance.m_buyButton.interactable = true;
                    __instance.m_buyButton.GetComponent<UITooltip>().m_text = "";
                }
                else
                {
                    __instance.m_buyButton.GetComponentInChildren<Text>().text = buyButtonText;
                }
            }
        }

        [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
        static class StoreGui_BuySelectedItem_Patch
        {
            static bool Prefix(StoreGui __instance, Trader ___m_trader, Trader.TradeItem ___m_selectedItem)
            {
                if (!modEnabled.Value)
                    return true;
                string name = ___m_selectedItem.m_prefab.m_itemData.m_crafterName;
                if (currentQuestDict.ContainsKey(name))
                {
                    if (QuestFrameworkAPI.IsQuestActive(name))
                    {
                        Dbgl($"Quest {name} already started");
                        return false;
                    }

                    QuestFrameworkAPI.AddQuest(MakeQuestData(currentQuestDict[name]));
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, startString.Value, 0, null);
                    ___m_trader.OnBought(___m_selectedItem);
                    __instance.m_buyEffects.Create(__instance.transform.position, Quaternion.identity, null, 1f, -1);
                    Dbgl($"Quest {name} started");
                    currentQuestDict.Remove(name);
                    for (int i = ___m_trader.m_items.Count - 1; i >= 0; i--)
                    {
                        if (name == ___m_trader.m_items[i].m_prefab.m_itemData.m_crafterName)
                            ___m_trader.m_items.RemoveAt(i);
                    }
                    AccessTools.Method(typeof(StoreGui), "FillList").Invoke(__instance, new object[] { });
                    AdjustFetchQuests();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(StoreGui), "FillList")]
        static class StoreGui_FillList_Patch
        {
            static void Prefix(StoreGui __instance, Trader ___m_trader)
            {
                for(int i = ___m_trader.m_items.Count - 1; i >= 0; i--)
                {
                    if (___m_trader.m_items[i].m_prefab.m_itemData.m_crafterName.StartsWith(typeof(BepInExPlugin).Namespace))
                        ___m_trader.m_items.RemoveAt(i);
                }
            }
            static void Postfix(StoreGui __instance, Trader ___m_trader, List<GameObject> ___m_itemList)
            {
                if (!modEnabled.Value)
                    return;

                if(currentQuestDict == null || ZNet.instance.GetTimeSeconds() >= lastRefreshTime + questRefreshInterval.Value)
                {
                    lastRefreshTime = ZNet.instance.GetTimeSeconds();
                    RefreshCurrentQuests();
                }

                int i = ___m_trader.m_items.Count;

                Dbgl($"Adding {currentQuestDict.Count} quests to trader");
                foreach (FetchQuestData fqd in currentQuestDict.Values)
                {
                    Dbgl($"{fqd.ID} reward {fqd.reward}");

                    ItemDrop id = new ItemDrop();
                    id.m_itemData.m_crafterName = fqd.ID;
                    ___m_trader.m_items.Add(new Trader.TradeItem()
                    {
                        m_prefab = id,
                        m_stack = 1,
                        m_price = 0
                    });

                    bool active = QuestFrameworkAPI.IsQuestActive(fqd.ID);

                    GameObject buttonObject = Instantiate(__instance.m_listElement, __instance.m_listRoot);
                    buttonObject.SetActive(true);
                    (buttonObject.transform as RectTransform).anchoredPosition = new Vector2(0f, i++ * -__instance.m_itemSpacing);
                    Image component = buttonObject.transform.Find("icon").GetComponent<Image>();
                    if(fqd.type == FetchType.Fetch)
                    {
                        component.sprite = ObjectDB.instance.m_items.Find(g => g.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == fqd.thing).GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0];
                    }
                    component.color =  active ? new Color(1f, 0f, 1f, 0f) : Color.white;

                    string name = fqd.type == FetchType.Fetch ? fetchQuestString.Value : killQuestString.Value;
                    Text nameText = buttonObject.transform.Find("name").GetComponent<Text>();
                    nameText.text = name;
                    nameText.color = active ? Color.grey : Color.white;

                    string desc = fqd.type == FetchType.Fetch ? fetchQuestDescString.Value : killQuestDescString.Value;
                    desc = desc.Replace("{amount}", fqd.amount + "").Replace("{thing}", Localization.instance.Localize(fqd.thing));
                    UITooltip tooltip = buttonObject.GetComponent<UITooltip>();
                    tooltip.m_topic = name;
                    tooltip.m_text = desc;
                    Text rewardText = Utils.FindChild(buttonObject.transform, "price").GetComponent<Text>();
                    rewardText.text = fqd.reward+"";
                    if(active)
                        rewardText.color = Color.grey;

                    buttonObject.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        AccessTools.Method(typeof(StoreGui), "OnSelectedItem").Invoke(__instance, new object[] { buttonObject });
                    });
                    ___m_itemList.Add(buttonObject);
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} config reloaded" });
                    return false;
                }
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} refresh"))
                {
                    RefreshCurrentQuests();
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} quests refreshed" });
                    return false;
                }
                return true;
            }
        }
    }
}
