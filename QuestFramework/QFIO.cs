using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace QuestFramework
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static void LoadQuests(string player, string world)
        {
            currentQuests.questDict.Clear();

            if (File.Exists(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{player}_{world}")))
            {
                using (Stream stream = File.Open(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{player}_{world}"), FileMode.Open))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    currentQuests = (QuestDataObject)binaryFormatter.Deserialize(stream);
                }
                RefreshQuestString();
                Dbgl($"Got {currentQuests.questDict.Count} quests for player {player} in world {world}");
            }
        }
        public static void SaveQuests(string player, string world)
        {
            if (currentQuests.questDict.Count == 0 && !File.Exists(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{player}_{world}")))
                return;

            using (Stream stream = File.Open(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{player}_{world}"), FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, currentQuests);
                Dbgl($"Quests saved for {player} in world {world}");
            }
        }
    }
}
