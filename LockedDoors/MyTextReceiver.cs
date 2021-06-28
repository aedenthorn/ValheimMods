using UnityEngine;

namespace LockableDoors
{
    public class MyTextReceiver : TextReceiver
    {
        public string text;
        public string guid;

        public MyTextReceiver(string guid)
        {
            this.guid = guid;
        }

        public string GetText()
        {
            return text;
        }

        public void SetText(string text)
        {
            this.text = text;
            BepInExPlugin.SetDoorName(guid, text);
        }
    }
}