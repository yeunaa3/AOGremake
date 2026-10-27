using UnityEngine;

namespace AOG.Duel
{
    public class HealingTotem : SkillObject
    {
        [Header("Healing Aura")]
        [Tooltip("Khoảng thời gian giữa các lần hồi máu (giây)")]
        [SerializeField, Min(0.1f)] private float healInterval = 1f;
        [Tooltip("Lượng máu hồi mỗi nhịp")]
        [SerializeField, Min(1)] private int healAmount = 15;
        [Tooltip("Bán kính vùng hồi máu xung quanh vật thể")]
        [SerializeField, Min(0.1f)] private float healRadius = 5f;
        [Tooltip("Hiệu ứng phát ra mỗi khi kích hoạt hồi máu")]
        [SerializeField] private ParticleSystem healPulsePrefab;

        private float healTimer;

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            healTimer += Time.deltaTime;
            if (healTimer >= healInterval)
            {
                healTimer = 0f;
                TriggerHealing();
            }
        }

        private void TriggerHealing()
        {
            bool healedAny = false;

            // Hồi máu cho chủ nhân nếu nằm trong bán kính
            if (owner != null && owner.Alive)
            {
                float dist = Vector2.Distance(transform.position, owner.transform.position);
                if (dist <= healRadius)
                {
                    owner.Stats.Heal(healAmount);
                    healedAny = true;
                }
            }

            // Hồi máu cho các vật thể đồng minh xung quanh
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, healRadius);
            foreach (Collider2D hit in hits)
            {
                SkillObject ally = hit.GetComponentInParent<SkillObject>();
                if (ally != null && ally != this && ally.Team == Team && !ally.IsDead)
                {
                    ally.Heal(healAmount);
                    healedAny = true;
                }
            }

            if (healedAny && healPulsePrefab != null)
            {
                ParticleSystem pulse = Instantiate(healPulsePrefab, transform.position, Quaternion.identity);
                pulse.Play(true);
                Destroy(pulse.gameObject, 1.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, healRadius);
        }
    }
}
