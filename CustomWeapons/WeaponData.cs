namespace CustomWeaponStats
{
    internal class WeaponData
    {
        public string name;
        public bool useDurability;
        public bool blockable;
        public bool dodgeable;
        public Skills.SkillType skillType;
        public float useDurabilityDrain;
        public float durabilityPerLevel;
        public float holdDurationMin;
        public float holdStaminaDrain;
        public int toolTier;
        public string ammoType;
        public string statusEffect;
        
        public float backStabBonus;
        public float blockPower;
        public float deflectionForce;
        public float attackForce;

        public float blunt;
        public float damage;
        public float pierce;
        public float slash;
        public float chop;
        public float pickaxe;
        public float fire;
        public float frost;
        public float lightning;
        public float poison;
        public float spirit;
        
        public float blockPowerPerLevel;
        public float deflectionForcePerLevel;
        public float bluntPerLevel;
        public float damagePerLevel;
        public float piercePerLevel;
        public float slashPerLevel;
        public float chopPerLevel;
        public float pickaxePerLevel;
        public float firePerLevel;
        public float frostPerLevel;
        public float lightningPerLevel;
        public float poisonPerLevel;
        public float spiritPerLevel;

        public bool hitTerrain;
        public bool hitTerrainSecondary;
    }
}