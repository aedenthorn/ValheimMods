using BepInEx.Configuration;

namespace ControllerButtonSwitch
{
    internal class ButtonInfo
    {
        internal string button;
        internal string key;
        internal float repeatDelay = 0;
        internal float repeatInterval = 0;
        internal bool inverted = false;

        public ButtonInfo(string name, ConfigEntry<string> entry)
        {
            button = name;
            string[] parts = entry.Value.Split(',');
            key = parts[0];
            if (parts.Length == 1)
                return;
            repeatDelay = float.Parse(parts[1]);
            if (parts.Length == 2)
                return;
            repeatInterval = float.Parse(parts[2]);
            if (parts.Length == 3)
                return;
            inverted = bool.Parse(parts[3]);
        }
    }
}