using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace QuestFramework
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {
            public static void Prefix()
            {
                if (!modEnabled.Value)
                    return;
                ApplyConfig();

                LoadQuests(Game.instance.GetPlayerProfile().GetName(), ZNet.instance.GetWorldName());
            }
        }
        [HarmonyPatch(typeof(PlayerProfile), "SavePlayerToDisk")]
        public static class PlayerProfile_SavePlayerToDisk_Patch
        {
            public static void Prefix()
            {
                if (!modEnabled.Value || !ZNet.instance || !Player.m_localPlayer)
                    return;
                SaveQuests(Game.instance.GetPlayerProfile().GetName(), ZNet.instance.GetWorldName());
            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
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
                    __instance.AddString( text );
                    __instance.AddString( $"{context.Info.Metadata.Name} config reloaded" );
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} end "))
                {
                    __instance.AddString( text );
                    List<string> keys = currentQuests.questDict.Keys.ToList();
                    keys.Sort();
                    string id = text.Split(' ')[2];
                    if ((keys.Contains(id) && QuestFrameworkAPI.RemoveQuest(id)) || (int.TryParse(id, out int idx) && keys.Count > idx && QuestFrameworkAPI.RemoveQuest(keys[idx])))
                    {
                        __instance.AddString( $"{context.Info.Metadata.Name} quest removed" );
                    }
                    else 
                        __instance.AddString( $"{context.Info.Metadata.Name} error removing quest" );

                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} clear"))
                {
                    __instance.AddString( text );
                    currentQuests.questDict.Clear();
                    __instance.AddString( $"{context.Info.Metadata.Name} quests cleared" );
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} list"))
                {
                    __instance.AddString( text );
                    List<string> keys = currentQuests.questDict.Keys.ToList();
                    keys.Sort();
                    for(int i = 0; i < keys.Count; i++)
                    {
                        __instance.AddString( i + " " + keys[i] );
                    }
                    return false;
                }
                return true;
            }
        }
    }
}
