using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class TrapObject : MonoBehaviour
    {
        [Header("Trap Settings")]
        [SerializeField, Min(0)] private int damage = 20;
        [SerializeField] private ArrowEffect effect = ArrowEffect.Slow;
        [SerializeField, Min(0)] private int effectPower = 30; // 30% speed => 70% slow
        [SerializeField, Min(0f)] private float effectDuration = 2f;
        [SerializeField, Min(1f)] private float lifetime = 15f;
        [SerializeField, Min(0.1f)] private float fallSpeed = 6f;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private ParticleSystem triggerParticlePrefab;

        private PlayerController owner;
        private Collider2D trapCollider;
        private Rigidbody2D body;
        private bool landed;
        private bool triggered;
        private float lifeTimer;

        private void Awake()
        {
            trapCollider = GetComponent<Collider2D>();
            trapCollider.isTrigger = true;
            body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.gravityScale = 2f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            lifeTimer = lifetime;
        }

        public void Setup(PlayerController source)
        {
            owner = source;
            lifeTimer = lifetime;
            triggered = false;
        }

        private void Update()
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (!landed && body == null)
            {
                transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;

            if (!landed && IsGround(other))
            {
                landed = true;
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                    body.bodyType = RigidbodyType2D.Kinematic;
                }
                return;
            }

            PlayerController target = other.GetComponentInParent<PlayerController>();
            if (target != null && owner != null && target.Team != owner.Team)
            {
                TriggerTrap(target);
            }
        }

        private void TriggerTrap(PlayerController target)
        {
            triggered = true;
            target.Stats.TakeDamage(new DamageInfo(owner, damage, false, transform.position,
                effect, effectPower, effectDuration));

            if (triggerParticlePrefab != null)
            {
                ParticleSystem ps = Instantiate(triggerParticlePrefab, transform.position, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, 1.5f);
            }

            Destroy(gameObject);
        }

        private bool IsGround(Collider2D other)
        {
            int layerBit = 1 << other.gameObject.layer;
            if ((groundLayers.value & layerBit) != 0) return true;
            return string.Equals(LayerMask.LayerToName(other.gameObject.layer), "Ground",
                System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
