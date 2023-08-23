namespace CustomWeaponStats
{
    public class WeaponState : ItemDrop.ItemData.SharedData
    {

        public float useDurabilityDrain;
        public float holdDurationMin;
        public float holdStaminaDrain;
        public float attackForce;
        public float backstabBonus;
        public float damage;
        public float blunt;
        public float slash;
        public float pierce;
        public float chop;
        public float pickaxe;
        public float fire;
        public float frost;
        public float lightning;
        public float poison;
        public float spirit;
        
        public bool hitTerrain;
        public bool hitTerrainSecondary;

        public WeaponState(ItemDrop.ItemData weapon)
        {

            useDurabilityDrain = weapon.m_shared.m_useDurabilityDrain;
            attackForce = weapon.m_shared.m_attackForce;
            backstabBonus = weapon.m_shared.m_backstabBonus;
            damage = weapon.m_shared.m_damages.m_damage;
            blunt = weapon.m_shared.m_damages.m_blunt;
            slash = weapon.m_shared.m_damages.m_slash;
            pierce = weapon.m_shared.m_damages.m_pierce;
            chop = weapon.m_shared.m_damages.m_chop;
            pickaxe = weapon.m_shared.m_damages.m_pickaxe;
            fire = weapon.m_shared.m_damages.m_fire;
            frost = weapon.m_shared.m_damages.m_frost;
            lightning = weapon.m_shared.m_damages.m_lightning;
            poison = weapon.m_shared.m_damages.m_poison;
            spirit = weapon.m_shared.m_damages.m_spirit;

            hitTerrain = weapon.m_shared.m_attack?.m_hitTerrain == true;
            hitTerrainSecondary = weapon.m_shared.m_secondaryAttack?.m_hitTerrain == true;
        }
    }
}