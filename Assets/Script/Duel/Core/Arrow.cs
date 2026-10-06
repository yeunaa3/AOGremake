using System;
using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Arrow : MonoBehaviour
    {
        private Rigidbody2D body;
        private Collider2D hitbox;
        private SpriteRenderer visual;
        private SpriteRenderer effectVisual;
        private static Sprite freezeTipSprite;
        private PlayerController owner;
        [Tooltip("Tốc độ bay theo đơn vị Unity mỗi giây. Gần hay xa đều giữ tốc độ này.")]
        [SerializeField, Min(0.1f)] private float flightSpeed = 5f;
        [Header("Độ cao đường bay vòng cung")]
        [Tooltip("Độ cao thấp nhất khi hai nhân vật ở gần nhau.")]
        [SerializeField, Min(0f)] private float minimumArcHeight = 1f;
        [Tooltip("Mỗi 1 đơn vị khoảng cách sẽ cộng thêm bao nhiêu độ cao.")]
        [SerializeField, Min(0f)] private float arcHeightPerDistance = .45f;
        [Tooltip("Giới hạn độ cao để mũi tên không bay quá cao khi hai nhân vật ở rất xa.")]
        [SerializeField, Min(0f)] private float maximumArcHeight = 7f;
        [SerializeField, Min(0.1f)] private float maximumLifetime = 30f;
        [SerializeField] private bool rotateAlongPath = true;
        private Vector2 startPoint;
        private Vector2 controlPoint;
        private Vector2 endPoint;
        private Vector2 straightDirection;
        private Vector2 previousPoint;
        private const int PathSamples = 24;
        private readonly float[] pathDistances = new float[PathSamples + 1];
        private float pathLength;
        private float traveledDistance;
        private int pathSampleIndex;
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
            float moveDistance = Time.fixedDeltaTime * flightSpeed * shotSpeedMultiplier * ownerSpeed;
            if (path == ProjectilePath.Straight)
            {
                Vector2 nextStraightPoint = body.position + straightDirection * moveDistance;
                body.MovePosition(nextStraightPoint);
                if (rotateAlongPath && straightDirection.sqrMagnitude > .000001f)
                    body.SetRotation(Mathf.Atan2(straightDirection.y, straightDirection.x) * Mathf.Rad2Deg);
                previousPoint = nextStraightPoint;
                return;
            }

            traveledDistance += moveDistance;
            float t;
            if (path == ProjectilePath.NormalArc)
            {
                while (pathSampleIndex < PathSamples &&
                       pathDistances[pathSampleIndex] < traveledDistance)
                    pathSampleIndex++;

                int upper = Mathf.Clamp(pathSampleIndex, 1, PathSamples);
                float part = Mathf.InverseLerp(
                    pathDistances[upper - 1],
                    pathDistances[upper],
                    traveledDistance);
                t = Mathf.Clamp01((upper - 1 + part) / PathSamples);
            }
            else
            {
                t = pathLength <= .0001f ? 1f : Mathf.Clamp01(traveledDistance / pathLength);
            }
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
            straightDirection = (targetSnapshot - startPoint).normalized;
            float distance = Vector2.Distance(startPoint, endPoint);
            float automaticArcHeight = Mathf.Clamp(
                distance * arcHeightPerDistance,
                minimumArcHeight,
                Mathf.Max(minimumArcHeight, maximumArcHeight));
            float peakHeight = Mathf.Max(0f, automaticArcHeight + arcHeightOffset);
            controlPoint = (startPoint + endPoint) * 0.5f
                + Vector2.up * (peakHeight * 2f);
            path = projectilePath;
            previousPoint = startPoint;
            pathLength = 0f;
            pathDistances[0] = 0f;
            Vector2 sampleBefore = startPoint;
            for (int i = 1; i <= PathSamples; i++)
            {
                float sampleT = i / (float)PathSamples;
                Vector2 sample = path == ProjectilePath.NormalArc
                    ? EvaluateBezier(sampleT)
                    : Vector2.Lerp(startPoint, endPoint, sampleT);
                pathLength += Vector2.Distance(sampleBefore, sample);
                pathDistances[i] = pathLength;
                sampleBefore = sample;
            }
            shotSpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            traveledDistance = 0f;
            pathSampleIndex = 1;
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
            if (visual != null && arrowSprite != null) visual.sprite = arrowSprite;
            ShowArrowEffect(shotEffect);

            Vector2 firstDirection = path == ProjectilePath.NormalArc
                ? controlPoint - startPoint
                : endPoint - startPoint;
            float firstAngle = firstDirection.sqrMagnitude > .000001f
                ? Mathf.Atan2(firstDirection.y, firstDirection.x) * Mathf.Rad2Deg
                : 0f;

            transform.SetPositionAndRotation(
                startPoint,
                rotateAlongPath ? Quaternion.Euler(0f, 0f, firstAngle) : transform.rotation);
            gameObject.SetActive(true);
            body.position = startPoint;
            if (rotateAlongPath) body.rotation = firstAngle;
            hitbox.enabled = true;
            flying = true;
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
            if (effectVisual != null) effectVisual.gameObject.SetActive(false);
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

        private void ShowArrowEffect(ArrowEffect arrowEffect)
        {
            if (arrowEffect != ArrowEffect.Freeze || visual == null || visual.sprite == null)
            {
                if (effectVisual != null) effectVisual.gameObject.SetActive(false);
                return;
            }

            if (freezeTipSprite == null)
            {
                Texture2D texture = new Texture2D(5, 5, TextureFormat.RGBA32, false);
                texture.name = "Freeze Arrow Tip";
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                Color clear = new Color(0f, 0f, 0f, 0f);
                for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++)
                {
                    int distance = Mathf.Abs(x - 2) + Mathf.Abs(y - 2);
                    texture.SetPixel(x, y, distance > 2
                        ? clear
                        : distance == 0 ? Color.white : new Color(.15f, .85f, 1f, 1f));
                }
                texture.Apply();
                freezeTipSprite = Sprite.Create(texture, new Rect(0, 0, 5, 5),
                    new Vector2(.5f, .5f), 20f);
                freezeTipSprite.name = "Freeze Arrow Tip";
            }

            if (effectVisual == null)
            {
                GameObject tip = new GameObject("FreezeTip");
                tip.transform.SetParent(visual.transform, false);
                effectVisual = tip.AddComponent<SpriteRenderer>();
            }

            effectVisual.sprite = freezeTipSprite;
            effectVisual.sortingLayerID = visual.sortingLayerID;
            effectVisual.sortingOrder = visual.sortingOrder + 1;
            effectVisual.transform.localPosition = new Vector3(visual.sprite.bounds.max.x, 0f, 0f);
            effectVisual.transform.localRotation = Quaternion.identity;
            effectVisual.transform.localScale = Vector3.one;
            effectVisual.gameObject.SetActive(true);
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
            OnHitPlayer(target, dealt);
            if (dealt > 0 && lifeSteal > 0f) owner.Stats.Heal(Mathf.RoundToInt(dealt * lifeSteal));
            if (dealt > 0 && resetCooldownSlot >= 0) owner.Action.ResetSkillCooldown(resetCooldownSlot);
            CreateWorldEffect(transform.position);
            Despawn();
        }

        // Prefab tên độc/băng/lửa có thể kế thừa Arrow và chỉ override phần này.
        protected virtual void OnHitPlayer(PlayerController target, int dealt) { }

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
