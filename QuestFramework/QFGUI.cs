using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuestFramework
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static Font hudFont;
        public static GUIStyle style;
        public static GUIStyle style2;
        public static bool configApplied = false;
        public static Vector2 hudPosition;
        public static Rect windowRect;
        public static string newQuestString;
        public static Rect timeRect;
        public static int windowId = 1890175403;

        public void OnGUI()
        {
            if (Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance?.InPasswordDialog() == true || Chat.instance?.HasFocus() == true || StoreGui.IsVisible() || InventoryGui.IsVisible() || Menu.IsVisible() || TextViewer.instance?.IsVisible() == true || currentQuests.questDict.Count == 0 ||
                (!showHUD.Value && !toggleHUDKeyOnPress.Value) ||
                (toggleHUDKeyOnPress.Value && (!AedenthornUtils.CheckKeyHeld(toggleHUDKey.Value) || !AedenthornUtils.CheckKeyHeld(toggleHUDKeyMod.Value)))
               )
                return;

            if (modEnabled.Value && configApplied && Player.m_localPlayer && Hud.instance)
            {
                float alpha = 1f;

                style.normal.textColor = new Color(hudFontColor.Value.r, hudFontColor.Value.g, hudFontColor.Value.b, hudFontColor.Value.a * alpha);
                style2.normal.textColor = new Color(hudShadowColor.Value.r, hudShadowColor.Value.g, hudShadowColor.Value.b, hudShadowColor.Value.a * alpha);
                if (((!toggleHUDKeyOnPress.Value && showHUD.Value) || (toggleHUDKeyOnPress.Value && AedenthornUtils.CheckKeyHeld(toggleClockKey.Value))) && (bool)AccessTools.Method(typeof(Hud), "IsVisible").Invoke(Hud.instance, new object[0]))
                {
                    GUI.backgroundColor = Color.clear;
                    windowRect = GUILayout.Window(windowId, new Rect(windowRect.position, timeRect.size), new GUI.WindowFunction(WindowBuilder), "");
                    //Dbgl(""+windowRect.size);
                }
            }
            if (!Input.GetKey(KeyCode.Mouse0) && (windowRect.x != hudPosition.x || windowRect.y != hudPosition.y))
            {
                hudPosition = new Vector2(windowRect.x, windowRect.y);
                hudLocationString.Value = $"{windowRect.x},{windowRect.y}";
                Config.Save();
            }
        }

        public static void RefreshQuestString()
        {
            if (currentQuests.questDict.Count == 0)
            {
                newQuestString = "";
                return;
            }
            string output = hudTemplate.Value;
            List<string> questList = new List<string>();
            foreach(QuestData qd in currentQuests.questDict.Values)
            {
                string questString = MakeQuestString(qd);

                questList.Add(questString);
            }
            newQuestString = output.Replace("{quests}", string.Join("\n\n", questList));
            Dbgl($"new quest string:\n\n{newQuestString}");
        }

        public static string MakeQuestString(QuestData qd)
        {
            List<string> questLines = new List<string>();
            if (qd.name.Length > 0)
                questLines.Add(questNameTemplate.Value.Replace("{name}", qd.name));
            if (qd.desc.Length > 0)
                questLines.Add(questDescTemplate.Value.Replace("{desc}", qd.desc));

            QuestStage qs = qd.questStages[qd.currentStage];
            List<string> stageString = new List<string>();
            if (qs.name.Length > 0)
                stageString.Add(stageNameTemplate.Value.Replace("{name}", qs.name));
            if (qs.desc.Length > 0)
                stageString.Add(stageDescTemplate.Value.Replace("{desc}", qs.desc));

            List<string> objectives = new List<string>();
            foreach (QuestObjective qo in qs.objectives.Values)
            {
                if (!qo.completed)
                {
                    List<string> objString = new List<string>();
                    if (qo.name.Length > 0)
                        objString.Add(objectiveNameTemplate.Value.Replace("{name}", qo.name));
                    if (qo.desc.Length > 0)
                        objString.Add(objectiveDescTemplate.Value.Replace("{desc}", qo.desc));

                    objectives.Add(string.Join("\n", objString));
                }
            }
            if (objectives.Count > 0)
                stageString.AddRange(objectives);
            if (stageString.Count > 0)
                questLines.AddRange(stageString);

            return string.Join("\n", questLines);
         }

        public void WindowBuilder(int id)
        {

            timeRect = GUILayoutUtility.GetRect(new GUIContent(newQuestString), style);

            GUI.DragWindow(timeRect);

            if (hudUseShadow.Value)
            {
                GUI.Label(new Rect(timeRect.position + new Vector2(-hudShadowOffset.Value, hudShadowOffset.Value), timeRect.size), newQuestString, style2);
            }
            GUI.Label(timeRect, newQuestString, style);
        }
        public static void ApplyConfig()
        {

            string[] split = hudLocationString.Value.Split(',');
            hudPosition = new Vector2(split[0].Trim().EndsWith("%") ? (float.Parse(split[0].Trim().Substring(0, split[0].Trim().Length - 1)) / 100f) * Screen.width : float.Parse(split[0].Trim()), split[1].Trim().EndsWith("%") ? (float.Parse(split[1].Trim().Substring(0, split[1].Trim().Length - 1)) / 100f) * Screen.height : float.Parse(split[1].Trim()));

            windowRect = new Rect(hudPosition, new Vector2(1000, 100));

            if (hudUseOSFont.Value)
                hudFont = Font.CreateDynamicFontFromOSFont(hudFontName.Value, hudFontSize.Value);
            else
            {
                Debug.Log($"getting fonts");
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                foreach (Font font in fonts)
                {
                    if (font.name == hudFontName.Value)
                    {
                        hudFont = font;
                        Debug.Log($"got font {font.name}");
                        break;
                    }
                }
            }
            style = new GUIStyle
            {
                richText = true,
                fontSize = hudFontSize.Value,
                alignment = hudTextAlignment.Value,
                font = hudFont
            };
            style2 = new GUIStyle
            {
                richText = true,
                fontSize = hudFontSize.Value,
                alignment = hudTextAlignment.Value,
                font = hudFont
            };

            configApplied = true;
        }

    }
}
