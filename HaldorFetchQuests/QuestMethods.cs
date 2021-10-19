using BepInEx;
using HarmonyLib;
using QuestFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static ItemDrop.ItemData;
using Random = UnityEngine.Random;

namespace HaldorFetchQuests
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static void RefreshCurrentQuests()
        {
            currentQuestDict.Clear();
            for (int i = 0; i < maxQuests.Value; i++)
            {
                FetchType type = Random.value > killToFetchRatio.Value ? FetchType.Fetch : FetchType.Kill;
                int amount = Random.Range(minAmount.Value, maxAmount.Value);

                GameObject go;
                int reward;
                string name;

                if (type == FetchType.Kill)
                {
                    if (!possibleKillList.Any())
                    {
                        possibleKillList = ((Dictionary<int, GameObject>)AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs").GetValue(ZNetScene.instance)).Values.ToList().FindAll(g => g.GetComponent<MonsterAI>() || g.GetComponent<AnimalAI>());
                    }
                    go = possibleKillList[Random.Range(0, possibleKillList.Count - 1)];
                    reward = Mathf.RoundToInt(amount * go.GetComponent<Character>().m_health * killRewardMult.Value);
                    name = go.GetComponent<Character>().m_name;
                }
                else
                {
                    if (!possibleFetchList.Any())
                    {
                        possibleFetchList = ObjectDB.instance.m_items.FindAll(g => g.GetComponent<ItemDrop>() && (g.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType == ItemType.Material || g.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType == ItemType.Consumable) && !((Trader)AccessTools.Field(typeof(StoreGui), "m_trader").GetValue(StoreGui.instance)).m_items.Exists(t => t.m_prefab.m_itemData.m_shared.m_name == g.GetComponent<ItemDrop>().m_itemData.m_shared.m_name));
                    }
                    go = possibleFetchList[Random.Range(0, possibleFetchList.Count - 1)];

                    int value = go.GetComponent<ItemDrop>().m_itemData.m_shared.m_value;
                    if(value == 0)
                    {
                        if (worthlessItemValue.Value <= 0)
                            continue;
                        value = worthlessItemValue.Value;
                    }
                    reward = Mathf.CeilToInt(amount * value * fetchRewardMult.Value);
                    name = go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                }

                reward = Mathf.RoundToInt(reward * (1 + (rewardFluctuation.Value * 2 * Random.value - rewardFluctuation.Value)));


                FetchQuestData fqd = new FetchQuestData()
                {
                    type = type,
                    thing = name,
                    amount = amount,
                    reward = reward,
                    ID = $"{typeof(BepInExPlugin).Namespace}|{type}|{amount}|{name}|{reward}" 
                };
                currentQuestDict.Add(fqd.ID, fqd);
                Dbgl($"Added quest {fqd.ID}, reward {fqd.reward}");
            }
        }

        public static QuestData MakeQuestData(FetchQuestData fqd)
        {
            string objString = (fqd.type == FetchType.Fetch ? fetchQuestDescString.Value : killQuestDescString.Value).Replace("{amount}", $"{fqd.amount}").Replace("{thing}", Localization.instance.Localize(fqd.thing));
            string objProgressString = (fqd.type == FetchType.Fetch ? fetchQuestProgressString.Value : killQuestProgressString.Value).Replace("{current}", "0").Replace("{total}", $"{fqd.amount}");

            QuestData qd = new QuestData()
            {
                name = questNameString.Value,
                desc = questDescString.Value.Replace("{reward}", $"{fqd.reward}"),
                ID = fqd.ID,
                currentStage = "StageOne",
                data = new Dictionary<string, object>() { 
                    { "progress", 0 }, 
                    { "type", fqd.type }, 
                    { "thing", fqd.thing }, 
                    { "amount", fqd.amount }, 
                    { "reward", fqd.reward }, 
                },
                questStages = new Dictionary<string, QuestStage>()
                    {
                        {
                            "StageOne",
                            new QuestStage(){
                                name = objString,
                                desc = objProgressString,
                                ID = "StageOne",
                                objectives = new Dictionary<string, QuestObjective>()
                                {
                                    {
                                        "ObjectiveOne",
                                        new QuestObjective()
                                        {
                                            ID = "ObjectiveOne",
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "StageTwo",
                            new QuestStage(){
                                ID = "StageTwo",
                                objectives = new Dictionary<string, QuestObjective>()
                                {
                                    {
                                        "ObjectiveOne",
                                        new QuestObjective()
                                        {
                                            name = returnString.Value,
                                            desc = returnDescString.Value,
                                            ID = "ObjectiveOne",
                                        }
                                    }
                                }
                            }
                        }
                    }
            };
            return qd;
        }

        public static void AdvanceKillQuests(Character __instance)
        {
            var dict = QuestFrameworkAPI.GetCurrentQuests();
            string[] keys = dict.Keys.ToArray();
            foreach (string key in keys)
            {
                QuestData qd = dict[key];
                if (qd.ID.StartsWith($"{typeof(BepInExPlugin).Namespace}|{FetchType.Kill}") && qd.currentStage == "StageOne")
                {
                    if ((string)qd.data["thing"] == __instance.m_name)
                    {
                        qd.data["progress"] = (int)qd.data["progress"] + 1;
                        qd.questStages["StageOne"].objectives["ObjectiveOne"].name = killQuestProgressString.Value.Replace("{current}", $"{qd.data["progress"]}").Replace("{total}", $"{qd.data["amount"]}");
                        if ((int)qd.data["progress"] >= (int)qd.data["amount"])
                            qd.currentStage = "StageTwo";
                        QuestFrameworkAPI.AddQuest(qd, true);
                    }
                }
            }
        }

        public static void AdjustFetchQuests()
        {
            var dict = QuestFrameworkAPI.GetCurrentQuests();
            string[] keys = dict.Keys.ToArray();
            foreach (string key in keys)
            {
                QuestData qd = dict[key];
                if (qd.ID.StartsWith($"{typeof(BepInExPlugin).Namespace}|{FetchType.Fetch}"))
                {
                    int amount = Player.m_localPlayer.GetInventory().CountItems((string)qd.data["thing"]);
                    if (amount != (int)qd.data["progress"])
                    {
                        qd.data["progress"] = amount;
                        qd.questStages["StageOne"].objectives["ObjectiveOne"].name = fetchQuestProgressString.Value.Replace("{current}", $"{qd.data["progress"]}").Replace("{total}", $"{qd.data["amount"]}");
                        if ((int)qd.data["progress"] >= (int)qd.data["amount"])
                            qd.currentStage = "StageTwo";
                        else
                            qd.currentStage = "StageOne";
                        QuestFrameworkAPI.AddQuest(qd, true);
                    }
                }
            }
        }

        public static void CheckQuestFulfilled(StoreGui __instance)
        {
            var dict = QuestFrameworkAPI.GetCurrentQuests();
            string[] keys = dict.Keys.ToArray();
            foreach (string key in keys)
            {
                if (!dict.ContainsKey(key))
                    continue;

                QuestData qd = dict[key];

                if (qd.ID.StartsWith(typeof(BepInExPlugin).Namespace) && qd.currentStage == "StageTwo")
                { 
                    if((FetchType)qd.data["type"] == FetchType.Fetch)
                    {
                        if(Player.m_localPlayer.GetInventory().CountItems((string)qd.data["thing"]) < (int)qd.data["amount"])
                        {
                            Dbgl($"not enough to complete quest!");
                            AdjustFetchQuests();
                            continue;
                        }
                        Player.m_localPlayer.GetInventory().RemoveItem((string)qd.data["thing"], (int)qd.data["amount"]);
                    }
                    Player.m_localPlayer.GetInventory().AddItem(__instance.m_coinPrefab.gameObject.name, (int)qd.data["reward"], __instance.m_coinPrefab.m_itemData.m_quality, __instance.m_coinPrefab.m_itemData.m_variant, 0L, "");
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, completeString.Value, 0, null);
                    QuestFrameworkAPI.RemoveQuest(qd.ID);
                    Dbgl($"Haldor quest {qd.ID} completed");
                }
            }
        }
    }
}
