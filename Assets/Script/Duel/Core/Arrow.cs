using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AOG.Duel
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Arrow : MonoBehaviour
    {
        [Serializable]
        private sealed class TrailSetting
        {
            public ArrowEffect effect;
            public ParticleSystem prefab;
            [Tooltip("Điểm phát hạt. Bỏ trống thì dùng TrailPoint trên mũi tên.")]
            public Transform spawnPoint;
        }

        [Serializable]
        private sealed class FlightAnimationSetting
        {
            public ArrowEffect effect;
            public string trigger;
        }

        [Serializable]
        private sealed class GroundEffectSetting
        {
            public ArrowEffect effect;
            public AreaEffect prefab;
        }

        private Rigidbody2D body;
        private Collider2D hitbox;
        private SpriteRenderer visual;
        private Transform trailPoint;
        private Transform flightEffectRoot;
        private Animator flightEffectAnimator;
        [Header("Animation hiệu ứng khi bay")]
        [SerializeField] private FlightAnimationSetting[] flightAnimations;
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
        [Header("Mũi tên cắm xuống đất")]
        [Tooltip("Layer của mặt đất. Nếu Layer có tên Ground thì code cũng tự nhận.")]
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Tổng thời gian mũi tên nằm trên đất, gồm cả thời gian mờ dần.")]
        [SerializeField, Min(0.1f)] private float groundLifetime = 2f;
        [Tooltip("Khoảng cuối của Ground Lifetime dùng để giảm Alpha.")]
        [SerializeField, Min(0.01f)] private float groundFadeTime = .5f;
        [Header("Hiệu ứng tại điểm va chạm")]
        [Tooltip("Prefab vùng hiệu ứng được tạo tại điểm tên chạm người hoặc mặt đất.")]
        [InspectorName("Impact Effects")]
        [SerializeField] private GroundEffectSetting[] groundEffects;
        [FormerlySerializedAs("groundFirePrefab")]
        [SerializeField, HideInInspector] private AreaEffect legacyGroundFirePrefab;
        [Header("Vệt hiệu ứng khi bay")]
        [Tooltip("Mỗi dòng nối một Arrow Effect với một prefab Particle.")]
        [SerializeField] private TrailSetting[] trails;
        [Tooltip("Sau khi mũi tên biến mất, các hạt cũ còn lại bao lâu.")]
        [SerializeField, Min(0f)] private float trailRemainTime = 1f;
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
        private bool stuckInGround;
        private float groundTime;
        private float activeGroundLifetime = 2f;
        private Color normalColor = Color.white;
        private int damage;
        private ArrowEffect effect;
        private int effectPower;
        private float effectDuration;
        private float lifeSteal;
        private int resetCooldownSlot = -1;
        private GameObject worldEffectPrefab;
        private bool worldEffectCreated;
        private readonly RaycastHit2D[] groundProbe = new RaycastHit2D[1];
        private ParticleSystem activeTrail;
        private Action<Arrow> returnToPool;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<Collider2D>();
            visual = GetComponentInChildren<SpriteRenderer>(true);
            trailPoint = transform.Find("TrailPoint");
            flightEffectRoot = transform.Find("Effect");
            if (flightEffectRoot != null)
                flightEffectAnimator = flightEffectRoot.GetComponent<Animator>();
            StopFlightEffect();
            MigrateGroundEffects();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hitbox.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (!flying) return;

            if (stuckInGround)
            {
                groundTime += Time.fixedDeltaTime;
                float fadeStart = Mathf.Max(0f, activeGroundLifetime - groundFadeTime);
                if (visual != null && groundTime >= fadeStart)
                {
                    Color color = normalColor;
                    color.a *= 1f - Mathf.InverseLerp(fadeStart, activeGroundLifetime, groundTime);
                    visual.color = color;
                }
                if (groundTime >= activeGroundLifetime) Despawn();
                return;
            }

            lifetime += Time.fixedDeltaTime;
            if (lifetime >= maximumLifetime)
            {
                Despawn();
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
                // Nếu hụt Enemy, tiếp tục theo hướng cuối cho tới khi chạm Ground.
                straightDirection = direction.sqrMagnitude > .000001f
                    ? direction.normalized
                    : (endPoint - controlPoint).normalized;
                path = ProjectilePath.Straight;
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
            int shotResetCooldownSlot = -1,
            float shotGroundLifetime = -1f)
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
            stuckInGround = false;
            groundTime = 0f;
            activeGroundLifetime = shotGroundLifetime > 0f ? shotGroundLifetime : groundLifetime;
            damage = Mathf.Max(0, shotDamage);
            effect = shotEffect;
            effectPower = Mathf.Max(0, shotEffectPower);
            effectDuration = Mathf.Max(0f, shotEffectDuration);
            lifeSteal = Mathf.Clamp01(shotLifeSteal);
            resetCooldownSlot = shotResetCooldownSlot;
            worldEffectPrefab = shotWorldEffect;
            worldEffectCreated = false;
            if (visual != null)
            {
                if (arrowSprite != null) visual.sprite = arrowSprite;
                normalColor = visual.color;
                normalColor.a = 1f;
                visual.color = normalColor;
            }

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
            StartTrail(shotEffect);
            StartFlightEffect(shotEffect);
            flying = true;
        }

        public void PrepareForPool(Action<Arrow> onDespawn)
        {
            returnToPool = onDespawn;
        }

        public void ResetProjectile()
        {
            flying = false;
            stuckInGround = false;
            groundTime = 0f;
            activeGroundLifetime = groundLifetime;
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
            FinishTrail();
            StopFlightEffect();
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

        private void StartFlightEffect(ArrowEffect arrowEffect)
        {
            StopFlightEffect();
            if (flightEffectAnimator == null || flightEffectAnimator.runtimeAnimatorController == null
                || flightAnimations == null) return;
            foreach (FlightAnimationSetting setting in flightAnimations)
            {
                if (setting == null || setting.effect != arrowEffect || string.IsNullOrEmpty(setting.trigger)) continue;
                foreach (AnimatorControllerParameter parameter in flightEffectAnimator.parameters)
                {
                    if (parameter.name != setting.trigger || parameter.type != AnimatorControllerParameterType.Trigger) continue;
                    flightEffectRoot.gameObject.SetActive(true);
                    flightEffectAnimator.Rebind();
                    flightEffectAnimator.Update(0f);
                    flightEffectAnimator.SetTrigger(setting.trigger);
                    flightEffectAnimator.Update(0f);
                    return;
                }
            }
        }

        private void StopFlightEffect()
        {
            if (flightEffectRoot != null) flightEffectRoot.gameObject.SetActive(false);
        }

        private void StartTrail(ArrowEffect arrowEffect)
        {
            FinishTrail();

            ParticleSystem prefab = null;
            Transform configuredPoint = null;
            if (trails != null)
            {
                foreach (TrailSetting setting in trails)
                {
                    if (setting == null || setting.effect != arrowEffect) continue;
                    prefab = setting.prefab;
                    configuredPoint = setting.spawnPoint;
                    break;
                }
            }
            if (prefab == null) return;

            Transform point = configuredPoint != null ? configuredPoint
                : trailPoint != null ? trailPoint : transform;
            activeTrail = Instantiate(prefab, point.position, point.rotation, point);
            activeTrail.transform.localPosition = Vector3.zero;

            // Một prefab có thể chứa nhiều lớp Particle con (ví dụ hai ảnh tuyết).
            // Giữ Simulation Space của từng lớp: Local bám đầu tên, World lưu vệt phía sau.
            foreach (ParticleSystem layer in activeTrail.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = layer.main;
                main.stopAction = ParticleSystemStopAction.None;
            }
            activeTrail.Play(true);
        }

        private void FinishTrail()
        {
            if (activeTrail == null) return;

            ParticleSystem trail = activeTrail;
            activeTrail = null;
            trail.transform.SetParent(null, true);
            trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            // Cho các hạt đã sinh sống hết lifetime, kể cả khi tăng lifetime trên prefab.
            float remaining = trailRemainTime;
            foreach (ParticleSystem layer in trail.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = layer.main;
                remaining = Mathf.Max(remaining,
                    main.startLifetime.constantMax / Mathf.Max(.01f, main.simulationSpeed));
            }
            Destroy(trail.gameObject, remaining);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!flying || owner == null) return;
            if (other.transform == owner.transform || other.transform.IsChildOf(owner.transform)) return;
            // Vùng lửa/độc chỉ truyền trạng thái, không phải vật cản chặn tên.
            if (other.GetComponentInParent<AreaEffect>() != null) return;

            if (stuckInGround)
            {
                return;
            }

            ColliderDistance2D contactInfo = Physics2D.Distance(hitbox, other);
            Vector2 contact = contactInfo.isValid ? contactInfo.pointB : other.ClosestPoint(body.position);

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
            if (target == null)
            {
                if (IsGround(other)) StickInGround(contact);
                else { CreateWorldEffect(transform.position); Despawn(); }
                return;
            }
            if (target.Team == owner.Team) return;

            int dealt = target.Stats.TakeDamage(new DamageInfo(owner, damage, false, contact,
                effect, effectPower, effectDuration));
            OnHitPlayer(target, dealt);
            if (dealt > 0 && lifeSteal > 0f) owner.Stats.Heal(Mathf.RoundToInt(dealt * lifeSteal));
            if (dealt > 0 && resetCooldownSlot >= 0) owner.Action.ResetSkillCooldown(resetCooldownSlot);
            CreateImpactEffects(contact, target);
            Despawn();
        }

        private bool IsGround(Collider2D other)
        {
            int layerBit = 1 << other.gameObject.layer;
            if ((groundLayers.value & layerBit) != 0) return true;
            return string.Equals(LayerMask.LayerToName(other.gameObject.layer), "Ground",
                StringComparison.OrdinalIgnoreCase);
        }

        private void StickInGround(Vector2 contact)
        {
            stuckInGround = true;
            groundTime = 0f;
            CreateImpactEffects(contact);
            FinishTrail();
            StopFlightEffect();

            // Tên nằm đất chỉ còn hình ảnh; không nhận va chạm hay gây damage/status.
            if (hitbox != null) hitbox.enabled = false;
        }

        private Vector2 GroundBelow(PlayerController target)
        {
            int mask = groundLayers.value;
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) mask |= 1 << groundLayer;
            var filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = false;
            Vector2 origin = target.Aim + Vector2.up;
            if (Physics2D.Raycast(origin, Vector2.down, filter, groundProbe, 200f) > 0)
                return groundProbe[0].point;
            // Nếu map chưa gán Ground, không để cột lửa xuất hiện ở điểm trúng trên đầu.
            return target.transform.position;
        }

        private void CreateImpactEffects(Vector2 contact, PlayerController hitTarget = null)
        {
            if (effect == ArrowEffect.Burn && hitTarget != null)
                contact = GroundBelow(hitTarget);
            CreateWorldEffect(contact);
            AreaEffect prefab = FindGroundEffect();
            if (prefab == null) return;
            AreaEffect zone = Instantiate(prefab, contact, Quaternion.identity);
            zone.SetupStatusZone(owner, effect, effectPower, effectDuration, activeGroundLifetime);
        }

        private AreaEffect FindGroundEffect()
        {
            if (effect == ArrowEffect.None || groundEffects == null) return null;
            foreach (GroundEffectSetting setting in groundEffects)
                if (setting != null && setting.effect == effect) return setting.prefab;
            return null;
        }

        private void OnValidate() => MigrateGroundEffects();

        // Giữ reference cột lửa đã gán trước khi chuyển sang danh sách chung.
        private void MigrateGroundEffects()
        {
            if (legacyGroundFirePrefab == null) return;
            if (groundEffects == null) groundEffects = new GroundEffectSetting[0];
            foreach (GroundEffectSetting setting in groundEffects)
            {
                if (setting == null || setting.effect != ArrowEffect.Burn) continue;
                legacyGroundFirePrefab = null;
                return;
            }
            int index = groundEffects.Length;
            Array.Resize(ref groundEffects, index + 1);
            groundEffects[index] = new GroundEffectSetting
                { effect = ArrowEffect.Burn, prefab = legacyGroundFirePrefab };
            legacyGroundFirePrefab = null;
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
            FinishTrail();
            StopFlightEffect();

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
