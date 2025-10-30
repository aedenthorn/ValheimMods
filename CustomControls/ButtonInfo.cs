using BepInEx.Configuration;
using System;
using System.Globalization;

namespace CustomControls
{
    [Serializable()]
    public class ButtonInfo
    {
        public string name;
        public string path;
        public bool altKey;
        public bool showHints;
        public bool rebindable;
        public float repeatDelay;
        public float repeatInterval;

        public ButtonInfo()
        {
        }
    }
}