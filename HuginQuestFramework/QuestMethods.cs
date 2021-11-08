using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using QuestFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HuginQuestFramework
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static void GetQuests()
        {
            huginQuestDict.Clear();

            if (Directory.Exists(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "Quests")) && Directory.GetFiles(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "Quests")).Length > 0)
            {
                List<HuginQuestData> fqdList = new List<HuginQuestData>();
                foreach(string file in Directory.GetFiles(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "Quests")))
                {
                    try
                    {
                        fqdList.Add(JsonUtility.FromJson<HuginQuestData>(File.ReadAllText(file)));
                    }
                    catch(Exception ex)
                    {
                        try
                        {
                            string[] lines = File.ReadAllLines(file);
                            foreach(string line in lines)
                            {
                                fqdList.Add(JsonUtility.FromJson<HuginQuestData>(line));
                            }
                        }
                        catch
                        { 
                            Dbgl($"Error reading quests from {file}:\n\n{ex}");
                        }
                    }
                }
                if(fqdList.Count > 0)
                {
                    for (int i = 0; i < fqdList.Count; i++)
                    {
                        HuginQuestData fqd = fqdList[i];
                        fqd.ID = typeof(BepInExPlugin).Namespace + "_" + fqd.ID;
                        if (fqd.amount <= 0)
                        {
                            fqd.amount = Random.Range(minAmount.Value, maxAmount.Value);
                            fqd.rewardAmount *= fqd.amount;
                        }
                        fqd.rewardAmount = Mathf.RoundToInt(fqd.rewardAmount * (1 + (rewardFluctuation.Value * 2 * Random.value - rewardFluctuation.Value)));
                        if (fqd.type == QuestType.Fetch)
                        {
                            try
                            {
                                fqd.thing = ObjectDB.instance.GetItemPrefab(fqd.thing).GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                            }
                            catch { }
                        }
                        else if(fqd.type == QuestType.Kill)
                        {
                            try
                            {
                                fqd.thing = ZNetScene.instance.GetPrefab(fqd.thing).GetComponent<Character>().m_name;
                            }
                            catch { }
                        }


                        huginQuestDict[fqd.ID] = fqd;
                        Dbgl($"Added quest {fqd.ID}");
                    }
                }
            }
            if(huginQuestDict.Count == 0)
            {
                possibleKillList = ((Dictionary<int, GameObject>)AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs").GetValue(ZNetScene.instance)).Values.ToList().FindAll(g => g.GetComponent<MonsterAI>() || g.GetComponent<AnimalAI>());

                possibleFetchList.Clear();
                var fetchList = ObjectDB.instance.m_items.FindAll(g => g.GetComponent<ItemDrop>() && g.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material);
                foreach(GameObject go in fetchList)
                {
                    int value = GetItemValue(go.GetComponent<ItemDrop>());
                    if (value > 0 && (allowUnknownFetchMaterials.Value || AccessTools.FieldRefAccess<Player, HashSet<string>>(Player.m_localPlayer, "m_knownMaterial").Contains(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)))
                        possibleFetchList.Add(new GameObjectReward(go, value));
                }

                possibleBuildList.Clear();

                ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
                var buildList = new List<GameObject>(hammer.m_itemData.m_shared.m_buildPieces.m_pieces); 
                ItemDrop hoe = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
                buildList.AddRange(hoe.m_itemData.m_shared.m_buildPieces.m_pieces);
                foreach (GameObject go in buildList)
                {
                    var reqs = go.GetComponent<Piece>().m_resources;
                    int value = 0;
                    foreach(var req in reqs)
                    {
                        value += GetItemValue(req.m_resItem) * req.m_amount;
                    }
                    if (value > 0)
                        possibleBuildList.Add(new GameObjectReward(go, value));
                }

                Dbgl($"got {possibleFetchList.Count} possible fetch items, {possibleKillList.Count} possible kill items, and {possibleBuildList.Count} possible build items");

            }
        }

        private static int GetItemValue(ItemDrop itemDrop)
        {
            int value = itemDrop.m_itemData.m_shared.m_value;
            if (Chainloader.PluginInfos.ContainsKey("Menthus.bepinex.plugins.BetterTrader"))
            {
                var key = Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config.Keys.ToList().Find(c => c.Section.StartsWith("C_Items") && c.Section.EndsWith("." + itemDrop.name) && c.Key == "Sellable");
                if (key != null && (bool)Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config[key].BoxedValue)
                {
                    key = Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config.Keys.ToList().Find(c => c.Section == key.Section && c.Key == "Sell Price");
                    value = (int)Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config[key].BoxedValue;
                    //Dbgl($"Got Better Trader price for {itemDrop.m_itemData.m_shared.m_name} of {value}; section {key.Section} key {key.Key}");
                }
            }
            if (value <= 0 && randomWorthlessItemValue.Value > 0)
            {
                value = randomWorthlessItemValue.Value;
            }
            return value;
        }

        public static QuestData MakeRandomQuest()
        {
            if(huginQuestDict.Count > 0)
            {
                Dbgl("Making random custom quest");
                int idx = Random.Range(0, huginQuestDict.Count);
                return MakeQuestData(huginQuestDict[huginQuestDict.Keys.ToList()[idx]]);
            }
            else
            {
                Dbgl("Making random quest");
                float typeChance = Random.value * (randomFetchQuestWeight.Value + randomKillQuestWeight.Value + randomBuildQuestWeight.Value);
                QuestType type;
                if (typeChance <= randomFetchQuestWeight.Value)
                {
                    type = QuestType.Fetch;
                }
                else if (typeChance <= randomFetchQuestWeight.Value + randomKillQuestWeight.Value)
                {
                    type = QuestType.Kill;
                }
                else
                {
                    type = QuestType.Build;
                }

                int amount = Random.Range(minAmount.Value, maxAmount.Value);

                GameObject go;
                int reward;
                string name;
                string questName;
                string objectiveName;
                if (type == QuestType.Kill)
                {
                    go = possibleKillList[Random.Range(0, possibleKillList.Count)];
                    reward = Mathf.RoundToInt(amount * go.GetComponent<Character>().m_health * randomKillRewardMult.Value);
                    name = go.GetComponent<Character>().m_name;
                    questName = randomKillQuestName.Value.Replace("{thing}", Localization.instance.Localize(name));
                    objectiveName = randomKillQuestObjectiveName.Value.Replace("{thing}", Localization.instance.Localize(name));
                }
                else if(type == QuestType.Fetch)
                {
                    GameObjectReward gor = possibleFetchList[Random.Range(0, possibleFetchList.Count)];
                    go = gor.gameObject;
                    reward = Mathf.CeilToInt(amount * gor.reward * randomFetchRewardMult.Value);
                    name = go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                    questName = randomFetchQuestName.Value.Replace("{thing}", Localization.instance.Localize(name));
                    objectiveName = randomFetchQuestObjectiveName.Value.Replace("{thing}", Localization.instance.Localize(name));
                }
                else
                {
                    GameObjectReward gor = possibleBuildList[Random.Range(0, possibleBuildList.Count)];
                    go = gor.gameObject;
                    reward = Mathf.CeilToInt(amount * gor.reward * randomBuildRewardMult.Value);
                    name = go.name;
                    questName = randomBuildQuestName.Value.Replace("{thing}", Localization.instance.Localize(go.GetComponent<Piece>().m_name));
                    objectiveName = randomBuildQuestObjectiveName.Value.Replace("{thing}", Localization.instance.Localize(go.GetComponent<Piece>().m_name));
                }

                reward = Mathf.RoundToInt(reward * (1 + (rewardFluctuation.Value * 2 * Random.value - rewardFluctuation.Value)));

                int idx = 0;
                string ID = $"{typeof(BepInExPlugin).Namespace}|{type}|{amount}|{name}|{reward}|{idx}";
                while (QuestFrameworkAPI.IsQuestActive(ID))
                {
                    ID = $"{typeof(BepInExPlugin).Namespace}|{type}|{amount}|{name}|{reward}|{++idx}";
                }

                HuginQuestData fqd = new HuginQuestData()
                {
                    questName = questName,
                    questDesc = "",
                    stageName = objectiveName,
                    stageDesc = randomQuestRewardText.Value.Replace("{rewardName}", "gold coins"),
                    objectiveName = randomQuestProgressText.Value,
                    objectiveDesc = "",
                    type = type,
                    thing = name,
                    amount = amount,
                    rewardName = "Coins",
                    rewardAmount = reward,
                    ID = ID
                };
                Dbgl($"Created {type} quest { fqd.ID }");
                return MakeQuestData(fqd);
            }
        }
        public static QuestData MakeQuestData(HuginQuestData fqd)
        {
            QuestData qd = new QuestData()
            {
                name = fqd.questName.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                desc = fqd.questDesc.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                ID = fqd.ID,
                currentStage = "StageOne",
                data = new Dictionary<string, object>() { 
                    { "qname", fqd.questName }, 
                    { "qdesc", fqd.questDesc }, 
                    { "sname", fqd.stageName }, 
                    { "sdesc", fqd.stageDesc }, 
                    { "oname", fqd.objectiveName }, 
                    { "odesc", fqd.objectiveDesc }, 
                    { "progress", 0 }, 
                    { "type", fqd.type }, 
                    { "thing", fqd.thing }, 
                    { "amount", fqd.amount }, 
                    { "rewardAmount", fqd.rewardAmount }, 
                    { "rewardName", fqd.rewardName }, 
                },
                questStages = new Dictionary<string, QuestStage>()
                    {
                        {
                            "StageOne",
                            new QuestStage(){
                                name = fqd.stageName.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                                desc = fqd.stageDesc.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                                ID = "StageOne",
                                objectives = new Dictionary<string, QuestObjective>()
                                {
                                    {
                                        "ObjectiveOne",
                                        new QuestObjective()
                                        {
                                            name = fqd.objectiveName.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                                            desc = fqd.objectiveDesc.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
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
        private static void DeclineQuest()
        {
            Dbgl("Declining quest");
            currentText.m_topic = questDeclinedDialogue.Value;
            respondedToQuest = true;
            questDialogueTransform.gameObject.SetActive(false);
        }

        private static void AcceptQuest()
        {
            Dbgl("Accepting quest");
            currentText.m_topic = questAcceptedDialogue.Value;
            QuestFrameworkAPI.AddQuest(nextQuest);
            respondedToQuest = true;
            questDialogueTransform.gameObject.SetActive(false);
            AdjustFetchQuests();
        }

        public static void AdvanceKillQuests(Character character)
        {
            var dict = QuestFrameworkAPI.GetCurrentQuests();
            string[] keys = dict.Keys.ToArray();
            foreach (string key in keys)
            {
                QuestData qd = dict[key];
                if (qd.ID.StartsWith(typeof(BepInExPlugin).Namespace) && (QuestType)qd.data["type"] == QuestType.Kill && qd.currentStage == "StageOne")
                {
                    if ((string)qd.data["thing"] == character.m_name)
                    {
                        qd.data["progress"] = (int)qd.data["progress"] + 1;

                        UpdateQuestProgress(ref qd);

                        if ((int)qd.data["progress"] >= (int)qd.data["amount"])
                            qd.currentStage = "StageTwo";
                        QuestFrameworkAPI.AddQuest(qd, true);
                    }
                }
            }
        }

        public static void AdvanceBuildQuests(Piece piece)
        {
            var dict = QuestFrameworkAPI.GetCurrentQuests();
            string[] keys = dict.Keys.ToArray();
            foreach (string key in keys)
            {
                QuestData qd = dict[key];
                if (qd.ID.StartsWith(typeof(BepInExPlugin).Namespace) && (QuestType)qd.data["type"] == QuestType.Build && qd.currentStage == "StageOne")
                {
                    if ((string)qd.data["thing"] == Utils.GetPrefabName(piece.gameObject))
                    {
                        qd.data["progress"] = (int)qd.data["progress"] + 1;

                        UpdateQuestProgress(ref qd);

                        if ((int)qd.data["progress"] >= (int)qd.data["amount"])
                        {
                            qd.currentStage = "StageTwo";
                        }
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
                if (qd.ID.StartsWith(typeof(BepInExPlugin).Namespace) && (QuestType)qd.data["type"] == QuestType.Fetch)
                {
                    int amount = Player.m_localPlayer.GetInventory().CountItems((string)qd.data["thing"]);
                    if (amount != (int)qd.data["progress"])
                    {
                        qd.data["progress"] = amount;

                        UpdateQuestProgress(ref qd);

                        if ((int)qd.data["progress"] >= (int)qd.data["amount"])
                            qd.currentStage = "StageTwo";
                        else
                        {
                            qd.currentStage = "StageOne";
                            if (finishedQuest != null && finishedQuest.ID == qd.ID)
                                finishedQuest = null;
                        }
                        
                        QuestFrameworkAPI.AddQuest(qd, true);
                    }
                }
            }
        }

        private static void UpdateQuestProgress(ref QuestData qd)
        {
            qd.name = ((string)qd.data["qname"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.desc = ((string)qd.data["qdesc"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.questStages["StageOne"].name = ((string)qd.data["sname"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.questStages["StageOne"].desc = ((string)qd.data["sdesc"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.questStages["StageOne"].objectives["ObjectiveOne"].name = ((string)qd.data["oname"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.questStages["StageOne"].objectives["ObjectiveOne"].desc = ((string)qd.data["odesc"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
        }

        public static void FulfillQuest(QuestData qd)
        {
            if (qd.currentStage == "StageTwo")
            {
                if ((string)qd.data["rewardName"] == "Gold")
                        qd.data["rewardName"] = "Coins";
                ItemDrop itemDrop = ObjectDB.instance.GetItemPrefab((string)qd.data["rewardName"])?.GetComponent<ItemDrop>();
                if (!itemDrop)
                {
                    currentText.m_topic = "There is something wrong with your quest...";
                    Dbgl($"Error getting reward {qd.data["rewardName"]}");
                    return;
                }
                        
                int emptySlots = Player.m_localPlayer.GetInventory().GetWidth() * Player.m_localPlayer.GetInventory().GetHeight() - Player.m_localPlayer.GetInventory().GetAllItems().Count;
                int stackSpace = (int)AccessTools.Method(typeof(Inventory), "FindFreeStackSpace").Invoke(Player.m_localPlayer.GetInventory(), new object[] { itemDrop.m_itemData.m_shared.m_name });
                stackSpace += emptySlots * itemDrop.m_itemData.m_shared.m_maxStackSize;
                if (stackSpace < (int)qd.data["rewardAmount"])
                {
                    currentText.m_topic = noRoomDialogue.Value;
                    Dbgl($"No room for reward! {stackSpace} {(int)qd.data["rewardAmount"]}");
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No room for reward!", 0, null);
                    return;
                }
                if ((QuestType)qd.data["type"] == QuestType.Fetch)
                {
                    if (Player.m_localPlayer.GetInventory().CountItems((string)qd.data["thing"]) < (int)qd.data["amount"])
                    {
                        currentText.m_topic = "It seems you have not completed your quest...";
                        Dbgl($"not enough to complete quest!");
                        if (finishedQuest != null && finishedQuest.ID == qd.ID)
                            finishedQuest = null;
                        AdjustFetchQuests();
                        return;
                    }
                    Player.m_localPlayer.GetInventory().RemoveItem((string)qd.data["thing"], (int)qd.data["amount"]);
                }

                Player.m_localPlayer.GetInventory().AddItem(itemDrop.gameObject.name, (int)qd.data["rewardAmount"], itemDrop.m_itemData.m_quality, itemDrop.m_itemData.m_variant, 0L, "");
                Player.m_localPlayer.ShowPickupMessage(itemDrop.m_itemData, (int)qd.data["rewardAmount"]);
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, completeString.Value, 0, null);
                QuestFrameworkAPI.RemoveQuest(qd.ID);
                finishedQuest = null;
                currentText.m_topic = completedDialogue.Value;
                Dbgl($"Quest {qd.ID} completed");
            }
            else
            {
                Dbgl($"Quest {qd.ID} isn't ready to complete");

            }
        }
    }
}
