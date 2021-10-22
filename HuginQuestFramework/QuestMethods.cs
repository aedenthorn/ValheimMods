using BepInEx;
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
            fetchQuestDict.Clear();

            if (Directory.Exists(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "Quests")) && Directory.GetFiles(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "Quests")).Length > 0)
            {
                List<FetchQuestData> fqdList = new List<FetchQuestData>();
                foreach(string file in Directory.GetFiles(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "Quests")))
                {
                    try
                    {
                        fqdList.Add(JsonUtility.FromJson<FetchQuestData>(File.ReadAllText(file)));
                    }
                    catch(Exception ex)
                    {
                        try
                        {
                            string[] lines = File.ReadAllLines(file);
                            foreach(string line in lines)
                            {
                                fqdList.Add(JsonUtility.FromJson<FetchQuestData>(line));
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
                        FetchQuestData fqd = fqdList[i];
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


                        fetchQuestDict[fqd.ID] = fqd;
                        Dbgl($"Added quest {fqd.ID}");
                    }
                }
            }
        }

        public static QuestData MakeQuestData(FetchQuestData fqd)
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
                            qd.currentStage = "StageOne";
                        
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
                        AdjustFetchQuests();
                        return;
                    }
                    Player.m_localPlayer.GetInventory().RemoveItem((string)qd.data["thing"], (int)qd.data["amount"]);
                }

                Player.m_localPlayer.GetInventory().AddItem(itemDrop.gameObject.name, (int)qd.data["rewardAmount"], itemDrop.m_itemData.m_quality, itemDrop.m_itemData.m_variant, 0L, "");
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
