using UnityEngine;

namespace MapPinExport
{
    public class MyPinData
    {
        public string name;
        public Minimap.PinType type;
        public Vector3 position;
        public bool isChecked;

        public MyPinData(Minimap.PinData pin)
        {
            name = pin.m_name;
            position = pin.m_pos;
            type = pin.m_type;
            isChecked = pin.m_checked;
        }
    }
}