using UnityEngine;

namespace AOG.Duel
{
    public sealed class SkillObject : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHp = 30;
        [SerializeField, Min(0f)] private float lifetime = 5f;
        [SerializeField, Min(0f)] private float hpLossPerSecond;
        [SerializeField] private float moveSpeed;
        [SerializeField, Min(0f)] private float patrolDistance = 3f;

        private PlayerController owner;
        private float hp;
        private float startX;
        private float direction = 1f;

        public int Team => owner == null ? -1 : owner.Team;

        public void Setup(PlayerController source)
        {
            owner = source;
            hp = maxHp;
            startX = transform.position.x;
        }

        private void Update()
        {
            if (lifetime > 0f)
            {
                lifetime -= Time.deltaTime;
                if (lifetime <= 0f) { Destroy(gameObject); return; }
            }

            hp -= hpLossPerSecond * Time.deltaTime;
            if (hp <= 0f) { Destroy(gameObject); return; }

            if (moveSpeed == 0f) return;
            transform.position += Vector3.right * (direction * moveSpeed * Time.deltaTime);
            if (Mathf.Abs(transform.position.x - startX) >= patrolDistance) direction *= -1f;
        }

        public void TakeDamage(int amount)
        {
            hp -= Mathf.Max(0, amount);
            if (hp <= 0f) Destroy(gameObject);
        }
    }
}
