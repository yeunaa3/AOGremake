using System;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class CharacterEquipment : MonoBehaviour
    {
        [SerializeField] private BowDefinition equippedBow;
        [SerializeField] private SpriteRenderer bowRenderer;

        public BowDefinition EquippedBow => equippedBow;
        public bool HasBow => equippedBow != null;
        public int BowDamage => equippedBow != null ? equippedBow.Damage : 0;
        public float BowAttackSpeedMultiplier => equippedBow != null
            ? equippedBow.AttackSpeedMultiplier
            : 1f;
        public DuelProjectileDefinition BowProjectile => equippedBow != null
            ? equippedBow.Projectile
            : null;

        public event Action<BowDefinition> BowChanged;

        private void Awake()
        {
            ApplyBowVisual();
        }

        public void EquipBow(BowDefinition bow)
        {
            equippedBow = bow;
            ApplyBowVisual();
            BowChanged?.Invoke(equippedBow);
        }

        private void ApplyBowVisual()
        {
            if (bowRenderer != null && equippedBow != null && equippedBow.BowSprite != null)
            {
                bowRenderer.sprite = equippedBow.BowSprite;
            }
        }
    }
}
