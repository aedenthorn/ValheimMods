using HarmonyLib;
using QuestFramework;
using System;
using UnityEngine;

namespace HuginQuestFramework
{
    public partial class BepInExPlugin
    {
        private static Rect windowRect;

        private void OnGUI()
        {
            if (!modEnabled.Value || !Player.m_localPlayer || !showQuestAcceptWindow)
                return;

            //GUI.backgroundColor = windowBackgroundColor.Value;
            windowRect = GUI.Window(windowID, windowRect, new GUI.WindowFunction(WindowBuilder), "");
        }
        private void WindowBuilder(int id)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

            GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(windowWidth) });
            GUILayout.Label(questNameString.Value, titleStyle);

            GUILayout.Space(10);

            GUILayout.Label("Hugin has a quest for you!", subTitleStyle);

            GUILayout.Space(10);

            GUILayout.Label(nextQuestString);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(declineButtonText.Value, buttonStyle, new GUILayoutOption[] { GUILayout.Width(windowWidth / 3f), GUILayout.Height(windowWidth / 9f) }))
            {
                showQuestAcceptWindow = false;
                currentText.m_topic = "Maybe next time!";
                respondedToQuest = true;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(acceptButtonText.Value, buttonStyle, new GUILayoutOption[] { GUILayout.Width(windowWidth / 3f), GUILayout.Height(windowWidth / 9f) }))
            {
                showQuestAcceptWindow = false;
                currentText.m_topic = "Great, here's your quest!";
                QuestFrameworkAPI.AddQuest(nextQuest);
                respondedToQuest = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }


        private static void ApplyConfig()
        {

            windowRect = new Rect((Screen.width - windowWidth) / 2, (Screen.height - windowHeight) / 2, windowWidth, windowHeight);
            Font myFont = null;
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            foreach (Font font in fonts)
            {
                if (font.name == "AveriaSerifLibre-Bold")
                {
                    myFont = font;
                    Debug.Log($"got font {font.name}");
                    break;
                }
            }
            titleStyle = new GUIStyle
            {
                richText = true,
                fontSize = 36,
                font = myFont,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            subTitleStyle = new GUIStyle
            {
                richText = true,
                fontSize = 24,
                font = myFont,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            descStyle = new GUIStyle
            {
                richText = true,
                fontSize = 20,
                font = myFont,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}