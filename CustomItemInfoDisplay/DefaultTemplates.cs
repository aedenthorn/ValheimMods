namespace CustomItemInfoDisplay
{
    public class DefaultTemplates
    {
        public static string GetTemplate(ItemDrop.ItemData.ItemType type)
        {
            switch (type)
            {
                case ItemDrop.ItemData.ItemType.Consumable:
                    return "[food]$item_food_health: <color=orange>{itemFoodHealth}</color>\\n$item_food_stamina: <color=orange>{itemFoodStamina}</color>\\n$item_food_duration: <color=orange>{itemFoodDuration}s</color>\\n$item_food_regen: <color=orange>{itemFoodRegen} hp/tick</color>\n[status]\\n{itemStatusInfo}";
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Torch:
                    return "{itemDamage}\n$item_blockpower: <color=orange>{itemBaseBlock}</color> <color=yellow>({itemBlock})</color>\n[timedBlock]$item_deflection: <color=orange>{itemDeflection}</color>\\n$item_parrybonus: <color=orange>{itemBlockBonus}x</color>\n$item_knockback: <color=orange>{itemAttackForce}</color>\n$item_backstab: <color=orange>{itemBackstab}x</color>\n[projectile]\\n{itemProjectileInfo}\n[status]\\n{itemStatusInfo}";
                case ItemDrop.ItemData.ItemType.Shield:
                    return "$item_blockpower: <color=orange>{itemBaseBlock}</color> <color=yellow>({itemBlock})</color>\n[timedBlock]$item_deflection: <color=orange>{itemDeflection}</color>\\n$item_parrybonus: <color=orange>{itemBlockBonus}x</color>";
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                    return "$item_armor: <color=orange>{itemArmor}</color>\n[damageMod]{itemDamageModInfo}\n[status]\\n{itemStatusInfo}";
                case ItemDrop.ItemData.ItemType.Ammo:
                    return "{itemDamage}\n$item_knockback: <color=orange>{itemAttackForce}</color>";
            }
            return "";
        }

        public static string GetTemplate()
        {
            return "[crafting]{itemDescription}\n[!crafting]{itemDescription} ({itemSpawnName})\n\n\n[dlc]<color=aqua>$item_dlc</color>\n[handed]{itemHanded}\n[crafted]$item_crafter: <color=orange>{itemCrafterName}</color>\n[!teleport]<color=orange>$item_noteleport</color>\n[value]$item_value: <color=orange>{itemValue}  ({itemBaseValue})</color>\n$item_weight: <color=orange>{itemWeight}</color>\n[quality]$item_quality: <color=orange>{itemQuality}</color>\n[durability,crafting]$item_durability: <color=orange>{itemMaxDurability}</color>\n[durability,!crafting]$item_durability: <color=orange>{itemPercentDurability}%</color> <color=yellow>({itemDurability}/{itemMaxDurability})</color>\n[repairable]$item_repairlevel: <color=orange>{itemStationLevel}</color>\n{itemTypeInfo}\n[movement]$item_movement_modifier: <color=orange>{itemMovementMod}%</color> ($item_total:<color=yellow>{totalMovementMod}%</color>)\n[setStatus]\\n$item_seteffect (<color=orange>{itemSetSize}</color> $item_parts):<color=orange>{itemSetStatusInfo}</color>";
        }
    }
}

