using BepInEx.Configuration;
using System;
using System.Globalization;

namespace ControllerButtonSwitch
{
    public class ButtonInfo
    {
        public string button;
        public string key;
        public float repeatDelay = 0;
        public float repeatInterval = 0;
        public bool inverted = false;

        public ButtonInfo(string name, ConfigEntry<string> entry)
        {
            try
            {
                button = name;
                string[] parts = entry.Value.Split(',');
                key = parts[0];
                if (parts.Length == 1)
                    return;
                repeatDelay = float.Parse(parts[1], CultureInfo.InvariantCulture.NumberFormat);
                if (parts.Length == 2)
                    return;
                repeatInterval = float.Parse(parts[2], CultureInfo.InvariantCulture.NumberFormat);
                if (parts.Length == 3)
                    return;
                inverted = bool.Parse(parts[3]);
            }
            catch(Exception ex)
            {
                BepInExPlugin.Dbgl($"Exception parsing config entry {name} string {entry.Value}\n{ex}");
            }
        }
    }
}