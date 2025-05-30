﻿// Popup list created by Eric Haines
// ComboBox Extended by Hyungseok Seo.(Jerry) sdragoon@nate.com
// this oop version of ComboBox is refactored by zhujiangbo jumbozhu@gmail.com
// Based on code made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0


using System;
using UnityEngine;

namespace ConfigurationManager.Utilities
{
    public class ComboBox
    {
        public static bool forceToUnShow;
        public static int useControlID = -1;
        public readonly string boxStyle;
        public readonly string buttonStyle;
        public bool isClickedComboButton;
        public readonly GUIContent[] listContent;
        public readonly GUIStyle listStyle;
        public readonly int _windowYmax;

        public ComboBox(Rect rect, GUIContent buttonContent, GUIContent[] listContent, GUIStyle listStyle, float windowYmax)
        {
            Rect = rect;
            ButtonContent = buttonContent;
            this.listContent = listContent;
            buttonStyle = "button";
            boxStyle = "box";
            this.listStyle = listStyle;
            _windowYmax = (int)windowYmax;
        }

        public ComboBox(Rect rect, GUIContent buttonContent, GUIContent[] listContent, string buttonStyle,
            string boxStyle, GUIStyle listStyle)
        {
            Rect = rect;
            ButtonContent = buttonContent;
            this.listContent = listContent;
            this.buttonStyle = buttonStyle;
            this.boxStyle = boxStyle;
            this.listStyle = listStyle;
        }

        public Rect Rect { get; set; }

        public GUIContent ButtonContent { get; set; }

        public void Show(Action<int> onItemSelected)
        {
            if (forceToUnShow)
            {
                forceToUnShow = false;
                isClickedComboButton = false;
            }

            var done = false;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            Vector2 currentMousePosition = Vector2.zero;
            if (Event.current.GetTypeForControl(controlID) == EventType.MouseUp)
            {
                if (isClickedComboButton)
                {
                    done = true;
                    currentMousePosition = Event.current.mousePosition;
                }
            }

            if (GUI.Button(Rect, ButtonContent, buttonStyle))
            {
                if (useControlID == -1)
                {
                    useControlID = controlID;
                    isClickedComboButton = false;
                }

                if (useControlID != controlID)
                {
                    forceToUnShow = true;
                    useControlID = controlID;
                }
                isClickedComboButton = true;
            }

            if (isClickedComboButton)
            {
                GUI.enabled = false;
                GUI.color = new Color(1, 1, 1, 2);

                var location = GUIUtility.GUIToScreenPoint(new Vector2(Rect.x, Rect.y + listStyle.CalcHeight(listContent[0], 1.0f)));
                var size = new Vector2(Rect.width, listStyle.CalcHeight(listContent[0], 1.0f) * listContent.Length);

                var innerRect = new Rect(0, 0, size.x, size.y);

                var outerRectScreen = new Rect(location.x, location.y, size.x, size.y);
                if (outerRectScreen.yMax > _windowYmax)
                {
                    outerRectScreen.height = _windowYmax - outerRectScreen.y;
                    outerRectScreen.width += 20;
                }

                if (currentMousePosition != Vector2.zero && outerRectScreen.Contains(GUIUtility.GUIToScreenPoint(currentMousePosition)))
                    done = false;

                CurrentDropdownDrawer = () =>
                {
                    GUI.enabled = true;

                    var scrpos = GUIUtility.ScreenToGUIPoint(location);
                    var outerRectLocal = new Rect(scrpos.x, scrpos.y, outerRectScreen.width, outerRectScreen.height);

                    GUI.Box(outerRectLocal, GUIContent.none,
                        new GUIStyle { normal = new GUIStyleState { background = BepInExPlugin.WindowBackground } });

                    _scrollPosition = GUI.BeginScrollView(outerRectLocal, _scrollPosition, innerRect, false, false);
                    {
                        const int initialSelectedItem = -1;
                        var newSelectedItemIndex = GUI.SelectionGrid(innerRect, initialSelectedItem, listContent, 1, listStyle);
                        if (newSelectedItemIndex != initialSelectedItem)
                        {
                            onItemSelected(newSelectedItemIndex);
                            isClickedComboButton = false;
                        }
                    }
                    GUI.EndScrollView(true);
                };
            }

            if (done)
                isClickedComboButton = false;
        }

        public Vector2 _scrollPosition = Vector2.zero;
        public static Action CurrentDropdownDrawer { get; set; }
    }
}