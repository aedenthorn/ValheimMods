using System.Collections.Generic;

namespace CustomArmorStats
{
    internal class ArmorData
    {
        public string name;

        public float armor;
        public float armorPerLevel;
        public float movementModifier;

        public List<string> damageModifiers = new List<string>();
    }
}