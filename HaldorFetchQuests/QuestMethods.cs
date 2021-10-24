using BepInEx;
using BepInEx.Bootstrap;
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
            currentQuestDict = new Dictionary<string, FetchQuestData>();

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
                    AedenthornUtils.ShuffleList(fqdList);
                    using(List<FetchQuestData>.Enumerator enumerator = new List<FetchQuestData>(fqdList.Take(maxQuests.Value)).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            FetchQuestData fqd = enumerator.Current;
                            if (fqd.amount <= 0)
                            {
                                fqd.amount = Random.Range(minAmount.Value, maxAmount.Value);
                                fqd.reward *= fqd.amount;
                            }
                            fqd.reward = Mathf.RoundToInt(fqd.reward * (1 + (rewardFluctuation.Value * 2 * Random.value - rewardFluctuation.Value)));
                            if (fqd.type == FetchType.Fetch)
                            {
                                try
                                {
                                    fqd.thing = ObjectDB.instance.GetItemPrefab(fqd.thing).GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                                }
                                catch { }
                            }
                            else
                            {
                                try
                                {
                                    fqd.thing = ZNetScene.instance.GetPrefab(fqd.thing).GetComponent<Character>().m_name;
                                }
                                catch { }
                            }
                            int idx = 0;
                            fqd.ID = $"{typeof(BepInExPlugin).Namespace}|{fqd.type}|{fqd.amount}|{fqd.thing}|{fqd.reward}|{idx}";
                            while (currentQuestDict.ContainsKey(fqd.ID) || QuestFrameworkAPI.IsQuestActive(fqd.ID))
                            {
                                fqd.ID = $"{typeof(BepInExPlugin).Namespace}|{fqd.type}|{fqd.amount}|{fqd.thing}|{fqd.reward}|{++idx}";
                            }
                            currentQuestDict.Add(fqd.ID, fqd);
                            Dbgl($"Added quest {fqd.ID}");
                        }
                    }
                    return;
                }
            }


            for (int i = 0; i < maxQuests.Value; i++)
            {
                FetchType type = Random.value > killToFetchRatio.Value ? FetchType.Fetch : FetchType.Kill;
                int amount = Random.Range(minAmount.Value, maxAmount.Value);

                int reward = 0;
                string name = "";

                if (type == FetchType.Kill)
                {
                    if (!possibleKillList.Any())
                    {
                        possibleKillList = ((Dictionary<int, GameObject>)AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs").GetValue(ZNetScene.instance)).Values.ToList().FindAll(g => g.GetComponent<MonsterAI>() || g.GetComponent<AnimalAI>());
                    }
                    GameObject go = possibleKillList[Random.Range(0, possibleKillList.Count)];
                    reward = Mathf.RoundToInt(amount * go.GetComponent<Character>().m_health * killRewardMult.Value);
                    name = go.GetComponent<Character>().m_name;
                }
                else
                {
                    if (!possibleFetchList.Any())
                    {
                        possibleFetchList = ObjectDB.instance.m_items.FindAll(g => g.GetComponent<ItemDrop>() && (g.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType == ItemType.Material || g.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType == ItemType.Consumable) && !((Trader)AccessTools.Field(typeof(StoreGui), "m_trader").GetValue(StoreGui.instance)).m_items.Exists(t => t.m_prefab.m_itemData.m_shared.m_name == g.GetComponent<ItemDrop>().m_itemData.m_shared.m_name));
                    }

                    AedenthornUtils.ShuffleList(possibleFetchList);
                    foreach(var go in possibleFetchList)
                    {
                        int value = go.GetComponent<ItemDrop>().m_itemData.m_shared.m_value;
                        if (value == 0)
                        {
                            if (Chainloader.PluginInfos.ContainsKey("Menthus.bepinex.plugins.BetterTrader"))
                            {
                                var key = Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config.Keys.ToList().Find(c => c.Section.StartsWith("C_Items") && c.Section.EndsWith("." + go.name) && c.Key == "Sellable");
                                if (key != null && (bool)Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config[key].BoxedValue)
                                {
                                    key = Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config.Keys.ToList().Find(c => c.Section == key.Section && c.Key == "Sell Price");
                                    value = (int)Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config[key].BoxedValue;
                                    Dbgl($"Got Better Trader price for {go.name} of {value}; section {key.Section} key {key.Key}");
                                }
                            }
                            if (value <= 0)
                            {
                                if (worthlessItemValue.Value <= 0)
                                    continue;
                                else
                                    value = worthlessItemValue.Value;
                            }
                        }
                        reward = Mathf.CeilToInt(amount * value * fetchRewardMult.Value);
                        name = go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                        break;
                    }
                    if(reward == 0)
                    {
                        Dbgl($"No possible fetch items???");
                        continue;
                    }
                }

                reward = Mathf.RoundToInt(reward * (1 + (rewardFluctuation.Value * 2 * Random.value - rewardFluctuation.Value)));

                int idx = 0;
                string ID = $"{typeof(BepInExPlugin).Namespace}|{type}|{amount}|{name}|{reward}|{idx}";
                while (currentQuestDict.ContainsKey(ID) || QuestFrameworkAPI.IsQuestActive(ID))
                {
                    ID = $"{typeof(BepInExPlugin).Namespace}|{type}|{amount}|{name}|{reward}|{++idx}";
                }

                FetchQuestData fqd = new FetchQuestData()
                {
                    type = type,
                    thing = name,
                    amount = amount,
                    reward = reward,
                    ID = ID 
                };

                currentQuestDict.Add(fqd.ID, fqd);
                Dbgl($"Added quest {fqd.ID}");
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
                    int emptySlots = Player.m_localPlayer.GetInventory().GetWidth() * Player.m_localPlayer.GetInventory().GetHeight() - Player.m_localPlayer.GetInventory().GetAllItems().Count;
                    int stackSpace = (int)AccessTools.Method(typeof(Inventory), "FindFreeStackSpace").Invoke(Player.m_localPlayer.GetInventory(), new object[] { __instance.m_coinPrefab.m_itemData.m_shared.m_name });
                    stackSpace += emptySlots * __instance.m_coinPrefab.m_itemData.m_shared.m_maxStackSize;
                    if (stackSpace < (int)qd.data["reward"])
                    {
                        Dbgl($"No room for reward! {stackSpace} {(int)qd.data["reward"]}");
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No room for reward!", 0, null);
                        continue;
                    }
                    if ((FetchType)qd.data["type"] == FetchType.Fetch)
                    {
                        if (Player.m_localPlayer.GetInventory().CountItems((string)qd.data["thing"]) < (int)qd.data["amount"])
                        {
                            Dbgl($"not enough to complete quest!");
                            AdjustFetchQuests();
                            continue;
                        }
                        Player.m_localPlayer.GetInventory().RemoveItem((string)qd.data["thing"], (int)qd.data["amount"]);
                    }
                    Player.m_localPlayer.GetInventory().AddItem(__instance.m_coinPrefab.gameObject.name, (int)qd.data["reward"], __instance.m_coinPrefab.m_itemData.m_quality, __instance.m_coinPrefab.m_itemData.m_variant, 0L, "");
                    Player.m_localPlayer.ShowPickupMessage(__instance.m_coinPrefab.m_itemData, (int)qd.data["reward"]);
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, completeString.Value, 0, null);
                    QuestFrameworkAPI.RemoveQuest(qd.ID);
                    Dbgl($"Haldor quest {qd.ID} completed");
                }
            }
        }
    }
}
