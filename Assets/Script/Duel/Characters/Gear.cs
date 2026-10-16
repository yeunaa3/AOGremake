using System;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class Gear : MonoBehaviour
    {
        [Header("Equipment")]
        [SerializeField] private BowData equippedBow;
        private SpriteRenderer bowRenderer;
        private PlayerController player;
        private readonly BowRuntimeState bowState = new BowRuntimeState();

        public bool HasBow => equippedBow != null;
        public BowData EquippedBow => equippedBow;
        public BowRuntimeState BowState => bowState;
        public int BowDamage => equippedBow != null ? equippedBow.Damage : 0;
        public float BowAttackSpeedMultiplier => equippedBow != null ? equippedBow.AttackSpeedMultiplier : 1f;
        public Sprite ArrowSprite => equippedBow != null ? equippedBow.ArrowSprite : null;
        public event Action<BowData> BowChanged;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            Transform bow = FindChild("bow");
            bowRenderer = bow != null ? bow.GetComponent<SpriteRenderer>() : null;
            ApplyBowVisual();
            if (equippedBow != null && equippedBow.Passive != null && player != null)
            {
                equippedBow.Passive.OnEquip(player, bowState);
            }
        }

        private void Update()
        {
            if (equippedBow != null && equippedBow.Passive != null && player != null && player.Alive)
            {
                equippedBow.Passive.OnUpdate(player, bowState, Time.deltaTime);
            }
        }

        public void EquipBow(BowData bow)
        {
            if (equippedBow != null && equippedBow.Passive != null && player != null)
            {
                equippedBow.Passive.OnUnequip(player, bowState);
            }
            equippedBow = bow;
            bowState.Reset();
            ApplyBowVisual();
            if (equippedBow != null && equippedBow.Passive != null && player != null)
            {
                equippedBow.Passive.OnEquip(player, bowState);
            }
            BowChanged?.Invoke(equippedBow);
        }

        public void TriggerOnShoot(Arrow arrow, bool isSecondaryShot)
        {
            if (equippedBow != null && equippedBow.Passive != null && player != null)
            {
                equippedBow.Passive.OnShoot(player, bowState, arrow, isSecondaryShot);
            }
        }

        public void TriggerOnArrowHit(Arrow arrow, PlayerController target, int dealt)
        {
            if (equippedBow != null && equippedBow.Passive != null && player != null)
            {
                equippedBow.Passive.OnArrowHit(player, bowState, arrow, target, dealt);
            }
        }

        public void ResetState()
        {
            bowState.Reset();
        }

        private void ApplyBowVisual()
        {
            if (bowRenderer != null && equippedBow != null && equippedBow.BowSprite != null)
                bowRenderer.sprite = equippedBow.BowSprite;
        }

        private Transform FindChild(string childName)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase)) return child;
            return null;
        }
    }
}
