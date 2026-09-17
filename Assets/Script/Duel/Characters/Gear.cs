using System;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class Gear : MonoBehaviour
    {
        [Header("Equipment")]
        [SerializeField] private BowData equippedBow;
        private SpriteRenderer bowRenderer;
        private SpriteRenderer arrowRenderer;

        public bool HasBow => equippedBow != null;
        public int BowDamage => equippedBow != null ? equippedBow.Damage : 0;
        public float BowAttackSpeedMultiplier => equippedBow != null ? equippedBow.AttackSpeedMultiplier : 1f;
        public Sprite ArrowSprite => equippedBow != null ? equippedBow.ArrowSprite : null;
        public event Action<BowData> BowChanged;

        private void Awake()
        {
            Transform bow = FindChild("bow");
            Transform arrow = FindChild("arrowVisual");
            bowRenderer = bow != null ? bow.GetComponent<SpriteRenderer>() : null;
            arrowRenderer = arrow != null ? arrow.GetComponent<SpriteRenderer>() : null;
            ApplyBowVisual();
        }
        public void EquipBow(BowData bow)
        {
            equippedBow = bow;
            ApplyBowVisual();
            BowChanged?.Invoke(equippedBow);
        }
        private void ApplyBowVisual()
        {
            if (bowRenderer != null && equippedBow != null && equippedBow.BowSprite != null) bowRenderer.sprite = equippedBow.BowSprite;
            if (arrowRenderer != null && equippedBow != null && equippedBow.ArrowSprite != null) arrowRenderer.sprite = equippedBow.ArrowSprite;
        }
        private Transform FindChild(string childName)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase)) return child;
            return null;
        }
    }
}
