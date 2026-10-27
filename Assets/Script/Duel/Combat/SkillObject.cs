using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    public class SkillObject : MonoBehaviour
    {
        [Header("Health & Decay")]
        [SerializeField, Min(1)] protected int maxHp = 100;
        [Tooltip("Phần trăm máu tối đa mất đi mỗi giây (ví dụ: 10 nghĩa là mất 10% max HP/giây -> tự mất sau 10s nếu không nhận thêm sát thương)")]
        [SerializeField, Range(0f, 100f)] protected float decayPercentPerSecond = 10f;
        [Tooltip("Giới hạn thời gian sống tối đa (giây). 0 = chỉ phụ thuộc vào decay máu.")]
        [SerializeField, Min(0f)] protected float maxLifetime = 0f;

        [Header("Effects & Feedback")]
        [SerializeField] protected ParticleSystem deathEffectPrefab;
        [SerializeField] protected SpriteRenderer mainVisual;
        [SerializeField] protected Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);

        protected PlayerController owner;
        protected float currentHp;
        protected float lifetimeTimer;
        protected bool isDead;
        private Color defaultColor = Color.white;
        private Coroutine flashRoutine;

        public PlayerController Owner => owner;
        public int Team => owner == null ? -1 : owner.Team;
        public int MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public bool IsDead => isDead;

        protected virtual void Awake()
        {
            if (mainVisual == null)
                mainVisual = GetComponentInChildren<SpriteRenderer>();
            if (mainVisual != null)
                defaultColor = mainVisual.color;
        }

        public virtual void Setup(PlayerController source)
        {
            owner = source;
            currentHp = maxHp;
            lifetimeTimer = 0f;
            isDead = false;
        }

        protected virtual void Update()
        {
            if (isDead) return;

            // Tự giảm % máu tối đa theo thời gian
            if (decayPercentPerSecond > 0f)
            {
                float loss = (maxHp * (decayPercentPerSecond / 100f)) * Time.deltaTime;
                currentHp -= loss;
                if (currentHp <= 0f)
                {
                    Die();
                    return;
                }
            }

            // Giới hạn thời gian sống dự phòng
            if (maxLifetime > 0f)
            {
                lifetimeTimer += Time.deltaTime;
                if (lifetimeTimer >= maxLifetime)
                {
                    Die();
                    return;
                }
            }
        }

        public virtual void TakeDamage(int amount, PlayerController attacker = null)
        {
            if (isDead || amount <= 0) return;
            currentHp -= amount;
            PlayHitFlash();
            if (currentHp <= 0f)
            {
                Die();
            }
        }

        public void TakeDamage(int amount) => TakeDamage(amount, null);

        public virtual void Heal(int amount)
        {
            if (isDead || amount <= 0) return;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }

        public virtual void Die()
        {
            if (isDead) return;
            isDead = true;

            if (deathEffectPrefab != null)
            {
                ParticleSystem ps = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, 1.5f);
            }

            Destroy(gameObject);
        }

        protected void PlayHitFlash()
        {
            if (mainVisual == null || !gameObject.activeInHierarchy) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            mainVisual.color = hitFlashColor;
            yield return new WaitForSeconds(0.08f);
            if (mainVisual != null) mainVisual.color = defaultColor;
            flashRoutine = null;
        }
    }
}
