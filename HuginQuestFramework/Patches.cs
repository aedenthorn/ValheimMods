using BepInEx;
using HarmonyLib;
using QuestFramework;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HuginQuestFramework
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
                GetQuests();
                lastCheckTime = ZNet.instance.GetTimeSeconds();
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
        
        [HarmonyPatch(typeof(Player), "PlacePiece")]
        static class Player_PlacePiece_Patch
        {
            static void Postfix(Piece piece, bool __result)
            {
                if (!modEnabled.Value || !__result)
                    return;
                AdvanceBuildQuests(piece);
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
        
        [HarmonyPatch(typeof(Menu), nameof(Menu.IsVisible))]
        static class Menu_IsVisible_Patch
        {
            static bool Prefix(Menu __instance, ref bool __result)
            {
                if (!modEnabled.Value || questDialogueTransform?.gameObject.activeSelf != true)
                    return true;
                __result = true;
                return false;
            }
        }
        
        [HarmonyPatch(typeof(Raven), "Say")]
        static class Raven_Say_Patch
        {

            static void Prefix(ref string topic, ref string text, bool showName, bool longTimeout, bool large)
            {
                if (!modEnabled.Value || showName || longTimeout || large)
                    return;

                if(finishedQuest != null)
                {
                    topic = haveRewardDialogue.Value;
                    text = "";
                }
                else if(nextQuest != null)
                {
                    topic = haveQuestDialogue.Value;
                    text = "";
                }
            }
        }
        
        [HarmonyPatch(typeof(Raven), "FlyAway")]
        static class Raven_FlyAway_Patch
        {

            static bool Prefix()
            {
                return !modEnabled.Value || questDialogueTransform == null || !questDialogueTransform.gameObject.activeSelf;
            }
        }
        
        [HarmonyPatch(typeof(Raven), "GetBestText")]
        static class Raven_GetBestText_Patch
        {

            static bool Prefix(Raven __instance, ref Raven.RavenText __result)
            {
                if (!modEnabled.Value || __instance.m_isMunin || Player.m_localPlayer == null)
                    return true;

                if(finishedQuest == null)
                {
                    foreach(var quest in QuestFrameworkAPI.GetCurrentQuests().Values)
                    {
                        if (quest.ID.StartsWith(typeof(BepInExPlugin).Namespace) && quest.currentStage == "StageTwo")
                        {
                            finishedQuest = quest;
                            break;
                        }
                    }
                }
                if(finishedQuest != null || nextQuest != null)
                {
                    __result = new Raven.RavenText();
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(Raven), "Talk")]
        static class Raven_Talk_Patch
        {

            static bool Prefix(Raven __instance, ref Raven.RavenText ___m_currentText, ref bool ___m_hasTalked)
            {
                if (!modEnabled.Value || ___m_currentText == null || Player.m_localPlayer == null)
                    return true;

                if (finishedQuest == null)
                {
                    foreach (var quest in QuestFrameworkAPI.GetCurrentQuests().Values)
                    {
                        if (quest.ID.StartsWith(typeof(BepInExPlugin).Namespace) && quest.currentStage == "StageTwo")
                        {
                            Dbgl($"Setting finished quest to {quest.ID}");

                            finishedQuest = quest;
                            break;
                        }
                    }
                }
                if (finishedQuest != null)
                {
                    if(finishedQuest.currentStage == "StageTwo")
                    {
                        FulfillQuest(finishedQuest);
                        AccessTools.Method(typeof(Raven), "Say").Invoke(__instance, new object[] { currentText.m_topic, "", false, true, true });
                        ___m_hasTalked = true;
                        return false;
                    }
                    Dbgl("Finished quest isn't finished");
                    finishedQuest = null;
                }
                if(nextQuest != null)
                {
                    if (questDialogueTransform == null)
                    {
                        Dbgl("Creating Quest Accept Window");

                        questDialogueTransform = Instantiate(Menu.instance.m_logoutDialog, Menu.instance.m_logoutDialog.parent.parent);
                        questDialogueTransform.gameObject.name = "QuestAcceptWindow";
                        questDialogueTransform.Find("dialog").GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, 200);

                        var buttonDecline = questDialogueTransform.Find("dialog/Button_yes");
                        buttonDecline.name = "Button_Decline";
                        buttonDecline.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, 32);
                        buttonDecline.GetComponentInChildren<Text>().text = declineButtonText.Value;
                        buttonDecline.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        buttonDecline.GetComponent<Button>().onClick.AddListener(DeclineQuest);

                        var buttonAccept = questDialogueTransform.Find("dialog/Button_no");
                        buttonAccept.name = "Button_Accept";
                        buttonAccept.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, 32);
                        buttonAccept.GetComponentInChildren<Text>().text = acceptButtonText.Value;
                        buttonAccept.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        buttonAccept.GetComponent<Button>().onClick.AddListener(AcceptQuest);

                        questDialogueTitleTransform = questDialogueTransform.Find("dialog/Quit");
                        questDialogueTitleTransform.name = "Title";
                        questDialogueTitleTransform.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 20);
                        questDialogueSubtitleTransform = Instantiate(questDialogueTransform.Find("dialog/Title"), questDialogueTransform.Find("dialog"));
                        questDialogueSubtitleTransform.name = "Subtitle";
                        questDialogueSubtitleTransform.GetComponent<Text>().color = Color.white;
                        questDialogueSubtitleTransform.GetComponent<Text>().fontSize = 16;
                        questDialogueSubtitleTransform.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, questDialogueTitleTransform.GetComponent<RectTransform>().rect.height);
                    }
                    questDialogueTitleTransform.GetComponent<Text>().text = nextQuest.name;
                    questDialogueSubtitleTransform.GetComponent<Text>().text = nextQuest.questStages["StageOne"].name + "\n" + nextQuest.questStages["StageOne"].desc;

                    Dbgl("Showing Quest Accept Window");
                    questDialogueTransform.gameObject.SetActive(true);
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(Raven), "Update")]
        static class Raven_Update_Patch
        {

            static void Prefix(Raven __instance, ref bool ___m_hasTalked, ref Raven.RavenText ___m_currentText)
            {
                if (!modEnabled.Value || __instance.m_isMunin || Player.m_localPlayer == null)
                    return;

                if(respondedToQuest)
                {
                    Dbgl($"Responded to quest");
                    AccessTools.Field(typeof(GameCamera), "m_mouseCapture").SetValue(GameCamera.instance, true);
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    respondedToQuest = false;
                    ___m_hasTalked = true;
                    AccessTools.Method(typeof(Raven), "Say").Invoke(__instance, new object[] { currentText.m_topic, currentText.m_text, false, true, true });
                    nextQuest = null;
                    return;
                }

                if(nextQuest == null && ZNet.instance.GetTimeSeconds() > questCheckInterval.Value + lastCheckTime)
                {

                    Dbgl($"Checking for quest");
                    lastCheckTime = ZNet.instance.GetTimeSeconds();

                    if (QuestFrameworkAPI.GetCurrentQuests().Keys.ToList().Exists(s => s.StartsWith(typeof(BepInExPlugin).Namespace)))
                        return;

                    if(Random.value < questChance.Value)
                    {
                        nextQuest = MakeRandomQuest();
                    }
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
                return true;
            }
        }
    }
}
