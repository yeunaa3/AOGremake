using System;
using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Arrow : MonoBehaviour
    {
        private Rigidbody2D body;
        private Collider2D hitbox;
        private Player owner;
        private ArrowData definition;
        private Vector2 startPoint;
        private Vector2 controlPoint;
        private Vector2 endPoint;
        private Vector2 previousPoint;
        private float elapsed;
        private float lifetime;
        private bool flying;
        private bool reachedEnd;
        private float endGraceRemaining;
        private int damage;
        private Action<Arrow> returnToPool;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<Collider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hitbox.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (!flying || definition == null) return;

            lifetime += Time.fixedDeltaTime;
            if (lifetime >= definition.MaximumLifetime)
            {
                Despawn();
                return;
            }

            if (reachedEnd)
            {
                endGraceRemaining -= Time.fixedDeltaTime;
                if (endGraceRemaining <= 0f) Despawn();
                return;
            }

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / definition.FlightDuration);
            Vector2 nextPoint = EvaluateBezier(t);
            body.MovePosition(nextPoint);

            Vector2 direction = nextPoint - previousPoint;
            if (definition.RotateAlongPath && direction.sqrMagnitude > 0.000001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                body.SetRotation(angle);
            }

            previousPoint = nextPoint;
            if (t >= 1f)
            {
                reachedEnd = true;
                endGraceRemaining = 0.08f;
            }
        }

        public void Launch(
            Player projectileOwner,
            ArrowData projectileDefinition,
            Vector2 start,
            Vector2 targetSnapshot,
            float arcHeightOffset,
            int shotDamage,
            Action<Arrow> onDespawn)
        {
            owner = projectileOwner;
            definition = projectileDefinition;
            startPoint = start;
            endPoint = targetSnapshot;
            controlPoint = (startPoint + endPoint) * 0.5f
                + Vector2.up * (definition.ArcHeight + arcHeightOffset);
            previousPoint = startPoint;
            elapsed = 0f;
            lifetime = 0f;
            reachedEnd = false;
            endGraceRemaining = 0f;
            damage = Mathf.Max(0, shotDamage);
            if (onDespawn != null)
            {
                returnToPool = onDespawn;
            }
            flying = true;

            transform.position = startPoint;
            gameObject.SetActive(true);
            hitbox.enabled = true;
        }

        public void PrepareForPool(Action<Arrow> onDespawn)
        {
            returnToPool = onDespawn;
        }

        public void ResetProjectile()
        {
            flying = false;
            reachedEnd = false;
            owner = null;
            definition = null;
            damage = 0;
            returnToPool = null;
            if (hitbox != null) hitbox.enabled = false;
        }

        private Vector2 EvaluateBezier(float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * startPoint
                + 2f * inverse * t * controlPoint
                + t * t * endPoint;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!flying || owner == null || definition == null) return;
            if (other.transform == owner.transform || other.transform.IsChildOf(owner.transform)) return;

            Health targetHealth = other.GetComponentInParent<Health>();
            if (targetHealth == null)
            {
                Despawn();
                return;
            }

            Player targetCharacter = targetHealth.GetComponentInParent<Player>();
            if (targetCharacter == null || targetCharacter.TeamId == owner.TeamId) return;

            targetHealth.TakeDamage(new DamageInfo(
                owner,
                damage,
                definition.IgnoreShield,
                transform.position));
            Despawn();
        }

        private void Despawn()
        {
            if (!flying) return;
            flying = false;
            if (hitbox != null) hitbox.enabled = false;

            Action<Arrow> callback = returnToPool;
            returnToPool = null;
            if (callback != null)
            {
                callback(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
