using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace QuestFramework
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            static void Prefix()
            {
                if (!modEnabled.Value)
                    return;
                ApplyConfig();

                LoadQuests(Game.instance.GetPlayerProfile().GetName(), ZNet.instance.GetWorldName());
            }
        }
        [HarmonyPatch(typeof(PlayerProfile), "SavePlayerToDisk")]
        static class PlayerProfile_SavePlayerToDisk_Patch
        {
            static void Prefix()
            {
                if (!modEnabled.Value || !ZNet.instance || !Player.m_localPlayer)
                    return;
                SaveQuests(Game.instance.GetPlayerProfile().GetName(), ZNet.instance.GetWorldName());
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
                    if (Game.instance?.GetPlayerProfile()?.GetName() != null && ZNet.instance?.GetWorldName() != null)
                        LoadQuests(Game.instance.GetPlayerProfile().GetName(), ZNet.instance.GetWorldName());
                    RefreshQuestString();
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} config reloaded" });
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} end "))
                {
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    List<string> keys = currentQuests.questDict.Keys.ToList();
                    keys.Sort();
                    string id = text.Split(' ')[2];
                    if ((keys.Contains(id) && QuestFrameworkAPI.RemoveQuest(id)) || (int.TryParse(id, out int idx) && keys.Count > idx && QuestFrameworkAPI.RemoveQuest(keys[idx])))
                    {
                        AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} quest removed" });
                    }
                    else 
                        AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} error removing quest" });

                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} clear"))
                {
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    currentQuests.questDict.Clear();
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} quests cleared" });
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} list"))
                {
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    List<string> keys = currentQuests.questDict.Keys.ToList();
                    keys.Sort();
                    for(int i = 0; i < keys.Count; i++)
                    {
                        AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { i + " " + keys[i] });
                    }
                    return false;
                }
                return true;
            }
        }
    }
}
