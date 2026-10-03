using System;
using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Arrow : MonoBehaviour
    {
        private Rigidbody2D body;
        private Collider2D hitbox;
        private SpriteRenderer visual;
        private PlayerController owner;
        [SerializeField, Min(0.05f)] private float flightDuration = 3.6f;
        [SerializeField] private float arcHeight = 5f;
        [SerializeField, Min(0.1f)] private float maximumLifetime = 30f;
        [SerializeField] private bool rotateAlongPath = true;
        private Vector2 startPoint;
        private Vector2 controlPoint;
        private Vector2 endPoint;
        private Vector2 previousPoint;
        private float elapsed;
        private float lifetime;
        private ProjectilePath path;
        private float shotSpeedMultiplier = 1f;
        private bool flying;
        private bool reachedEnd;
        private float endGraceRemaining;
        private int damage;
        private ArrowEffect effect;
        private int effectPower;
        private float effectDuration;
        private float lifeSteal;
        private int resetCooldownSlot = -1;
        private GameObject worldEffectPrefab;
        private bool worldEffectCreated;
        private Action<Arrow> returnToPool;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<Collider2D>();
            visual = GetComponentInChildren<SpriteRenderer>(true);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hitbox.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (!flying) return;

            lifetime += Time.fixedDeltaTime;
            if (lifetime >= maximumLifetime)
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

            float ownerSpeed = owner == null ? 1f : owner.Stats.ProjectileSpeedMultiplier;
            elapsed += Time.fixedDeltaTime * shotSpeedMultiplier * ownerSpeed;
            float t = Mathf.Clamp01(elapsed / flightDuration);
            Vector2 nextPoint = path == ProjectilePath.NormalArc
                ? EvaluateBezier(t)
                : Vector2.Lerp(startPoint, endPoint, t);
            body.MovePosition(nextPoint);

            Vector2 direction = nextPoint - previousPoint;
            if (rotateAlongPath && direction.sqrMagnitude > 0.000001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                body.SetRotation(angle);
            }

            previousPoint = nextPoint;
            if (t >= 1f)
            {
                reachedEnd = true;
                CreateWorldEffect(endPoint);
                endGraceRemaining = 0.08f;
            }
        }

        public void Launch(
            PlayerController projectileOwner,
            Vector2 start,
            Vector2 targetSnapshot,
            ProjectilePath projectilePath,
            float arcHeightOffset,
            int shotDamage,
            Sprite arrowSprite,
            float speedMultiplier = 1f,
            ArrowEffect shotEffect = ArrowEffect.None,
            int shotEffectPower = 0,
            float shotEffectDuration = 0f,
            float shotLifeSteal = 0f,
            GameObject shotWorldEffect = null,
            int shotResetCooldownSlot = -1)
        {
            owner = projectileOwner;
            startPoint = start;
            endPoint = targetSnapshot;
            controlPoint = (startPoint + endPoint) * 0.5f
                + Vector2.up * (arcHeight + arcHeightOffset);
            previousPoint = startPoint;
            path = projectilePath;
            shotSpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            elapsed = 0f;
            lifetime = 0f;
            reachedEnd = false;
            endGraceRemaining = 0f;
            damage = Mathf.Max(0, shotDamage);
            effect = shotEffect;
            effectPower = Mathf.Max(0, shotEffectPower);
            effectDuration = Mathf.Max(0f, shotEffectDuration);
            lifeSteal = Mathf.Clamp01(shotLifeSteal);
            resetCooldownSlot = shotResetCooldownSlot;
            worldEffectPrefab = shotWorldEffect;
            worldEffectCreated = false;
            if (visual != null) visual.sprite = arrowSprite;
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
            damage = 0;
            effect = ArrowEffect.None;
            effectPower = 0;
            effectDuration = 0f;
            lifeSteal = 0f;
            resetCooldownSlot = -1;
            shotSpeedMultiplier = 1f;
            worldEffectPrefab = null;
            worldEffectCreated = false;
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
            if (!flying || owner == null) return;
            if (other.transform == owner.transform || other.transform.IsChildOf(owner.transform)) return;

            SkillObject obstacle = other.GetComponentInParent<SkillObject>();
            if (obstacle != null)
            {
                if (obstacle.Team == owner.Team) return;
                obstacle.TakeDamage(damage);
                CreateWorldEffect(transform.position);
                Despawn();
                return;
            }

            PlayerController target = other.GetComponentInParent<PlayerController>();
            if (target == null) { CreateWorldEffect(transform.position); Despawn(); return; }
            if (target.Team == owner.Team) return;

            int dealt = target.Stats.TakeDamage(new DamageInfo(owner, damage, false, transform.position,
                effect, effectPower, effectDuration));
            if (dealt > 0 && lifeSteal > 0f) owner.Stats.Heal(Mathf.RoundToInt(dealt * lifeSteal));
            if (dealt > 0 && resetCooldownSlot >= 0) owner.Action.ResetSkillCooldown(resetCooldownSlot);
            CreateWorldEffect(transform.position);
            Despawn();
        }

        private void CreateWorldEffect(Vector2 position)
        {
            if (worldEffectCreated || worldEffectPrefab == null) return;
            worldEffectCreated = true;
            GameObject created = Instantiate(worldEffectPrefab, position, Quaternion.identity);
            AreaEffect area = created.GetComponent<AreaEffect>();
            if (area != null) area.Setup(owner);
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
