using System.Collections.Generic;
using UnityEngine;

namespace CustomItems
{
    public class CustomItem
    {
        public string id;
        public string baseItemName;
        public string name = "";
        public string dlc = "";
        public ItemDrop.ItemData.ItemType itemType = ItemDrop.ItemData.ItemType.Misc;
        public Sprite[] icons = new Sprite[0];
        public ItemDrop.ItemData.ItemType attachOverride;
        public string description_key = "";
        public string description = "";
        public int maxStackSize = 1;
        public int maxQuality = 1;
        public float weight = 1f;
        public int value;
        public bool teleportable = true;
        public bool questItem;
        public float equipDuration = 1f;
        public int variants;
        public Vector2Int trophyPos = Vector2Int.zero;
        public PieceTable buildPieces;
        public bool centerCamera;
        public string setName = "";
        public int setSize;
        public StatusEffect setStatusEffect;
        public StatusEffect equipStatusEffect;
        public float movementModifier;
        public float food;
        public float foodStamina;
        public float foodBurnTime;
        public float foodRegen;
        public Color foodColor = Color.white;
        public Material armorMaterial;
        public bool helmetHideHair = true;
        public float armor = 10f;
        public float armorPerLevel = 1f;
        public List<HitData.DamageModPair> damageModifiers = new List<HitData.DamageModPair>();
        public float blockPower = 10f;
        public float blockPowerPerLevel;
        public float deflectionForce;
        public float deflectionForcePerLevel;
        public float timedBlockBonus = 1.5f;
        public ItemDrop.ItemData.AnimationState animationState = ItemDrop.ItemData.AnimationState.OneHanded;
        public Skills.SkillType skillType = Skills.SkillType.Swords;
        public int toolTier;
        public HitData.DamageTypes damages;
        public HitData.DamageTypes damagesPerLevel;
        public float attackForce = 30f;
        public float backstabBonus = 4f;
        public bool dodgeable;
        public bool blockable;
        public StatusEffect attackStatusEffect;
        public GameObject spawnOnHit;
        public GameObject spawnOnHitTerrain;
        public Attack attack;
        public Attack secondaryAttack;
        public bool useDurability;
        public bool destroyBroken = true;
        public bool canBeReparied = true;
        public float maxDurability = 100f;
        public float durabilityPerLevel = 50f;
        public float useDurabilityDrain = 1f;
        public float durabilityDrain;
        public float holdDurationMin;
        public float holdStaminaDrain;
        public string holdAnimationState = "";
        public string ammoType = "";
        public float aiAttackRange = 2f;
        public float aiAttackRangeMin;
        public float aiAttackInterval = 2f;
        public float aiAttackMaxAngle = 5f;
        public bool aiWhenFlying = true;
        public bool aiWhenWalking = true;
        public bool aiWhenSwiming = true;
        public bool aiPrioritized;
        public ItemDrop.ItemData.AiTarget aiTargetType;
        public EffectList hitEffect = new EffectList();
        public EffectList hitTerrainEffect = new EffectList();
        public EffectList blockEffect = new EffectList();
        public EffectList startEffect = new EffectList();
        public EffectList holdStartEffect = new EffectList();
        public EffectList triggerEffect = new EffectList();
        public EffectList trailStartEffect = new EffectList();
        public StatusEffect consumeStatusEffect;
        public int recipe_amount;
        public int minStationLevel;
        public List<RequirementData> requirements;
    }

    public class RequirementData
    {
        public int amount;
        public int amountPerLevel;
        public bool recover;
        public string name;
    }
}