using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    // Lật nhân vật, điều khiển animation và hiển thị hiệu ứng trạng thái.
    public sealed class PlayerAnim : MonoBehaviour
    {
        private Animator animator;
        private Animator[] effectAnimators = new Animator[0];
        private PlayerStats stats;
        [System.Serializable]
        private sealed class StatusParticleSetting
        {
            public ArrowEffect effect;
            public ParticleSystem prefab;
            [Tooltip("Điểm bám trong Visual/Effect. Bỏ trống thì dùng Visual/Effect.")]
            public Transform spawnPoint;
            [System.NonSerialized] public ParticleSystem instance;
            [System.NonSerialized] public bool playing;
        }

        [Header("Particle trạng thái trên người")]
        [SerializeField] private StatusParticleSetting[] statusParticles;
        [Header("Foot Smoke Prefabs")]
        [Tooltip("Prefab khói phía trước khi nhân vật dừng lại.")]
        [SerializeField] private GameObject smokeAheadPrefab;
        [Tooltip("Prefab khói phía sau khi bắt đầu di chuyển và lặp lại mỗi 1s.")]
        [SerializeField] private GameObject smokeBackPrefab;
        [Tooltip("Prefab khói ở giữa chân khi nhảy bằng skill Jump.")]
        [SerializeField] private GameObject smokeMidPrefab;

        [Header("Foot Smoke Spawn Points")]
        [Tooltip("Điểm xuất hiện khói dừng lại (SmokeAhead).")]
        [SerializeField] private Transform smokeAheadPoint;
        [Tooltip("Điểm xuất hiện khói chạy (SmokeBack).")]
        [SerializeField] private Transform smokeBackPoint;
        [Tooltip("Điểm xuất hiện khói nhảy (SmokeMid).")]
        [SerializeField] private Transform smokeMidPoint;

        private PlayerMove move;
        private bool wasMoving;
        private float moveSmokeTimer;
        private float lastJumpSmokeTime = -1f;

        private Transform visual;
        private float normalX;
        private Transform[] frozenParts;
        private Vector3[] frozenPositions;
        private Quaternion[] frozenRotations;
        private Vector3[] frozenScales;
        private bool poseFrozen;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            move = GetComponent<PlayerMove>();
            animator = GetComponentInChildren<Animator>();
            visual = animator == null ? null : animator.transform;
            if (visual == null) return;

            Transform effect = visual.Find("Effect");
            if (effect != null)
            {
                // Mỗi object con có Animator và SpriteRenderer riêng, chạy đồng thời.
                var effects = new List<Animator>();
                foreach (Animator item in effect.GetComponentsInChildren<Animator>(true))
                    if (item.transform != effect) effects.Add(item);
                effectAnimators = effects.ToArray();

                if (smokeAheadPoint == null) smokeAheadPoint = effect.Find("SmokeAhead");
                if (smokeBackPoint == null) smokeBackPoint = effect.Find("SmokeBack");
                if (smokeMidPoint == null) smokeMidPoint = effect.Find("SmokeMid");
            }

            if (smokeAheadPrefab == null) smokeAheadPrefab = Resources.Load<GameObject>("SmokeAhead");
            if (smokeBackPrefab == null) smokeBackPrefab = Resources.Load<GameObject>("SmokeBack");
            if (smokeMidPrefab == null) smokeMidPrefab = Resources.Load<GameObject>("SmokeMid");

            InitializeStatusParticles(effect != null ? effect : visual);

            normalX = Mathf.Abs(visual.localScale.x);

            // Giữ toàn bộ cơ thể, nhưng bỏ nhánh Effect để các clip hiệu ứng vẫn chạy riêng.
            var parts = new List<Transform>();
            foreach (Transform part in visual.GetComponentsInChildren<Transform>(true))
                if (!IsEffectPart(part)) parts.Add(part);

            frozenParts = parts.ToArray();
            frozenPositions = new Vector3[frozenParts.Length];
            frozenRotations = new Quaternion[frozenParts.Length];
            frozenScales = new Vector3[frozenParts.Length];
        }

        public void Face(float direction)
        {
            if (visual == null || direction == 0f) return;
            if (normalX <= 0.0001f)
            {
                normalX = Mathf.Abs(visual.localScale.x);
                if (normalX <= 0.0001f) normalX = 1f;
            }
            Vector3 scale = visual.localScale;
            scale.x = normalX * Mathf.Sign(direction);
            visual.localScale = scale;
        }

        public void Values(bool moving, bool stunned, bool frozen, bool burning,
            bool silenced, bool shielding, float attackSpeed)
        {
            Bool("IsMoving", moving);
            Bool("IsStunned", stunned);
            Bool("IsSilenced", silenced);
            Bool("IsShielding", shielding);
            Float("MoveSpeed", moving ? 1f : 0f);
            Float("AttackSpeed", attackSpeed);
            EffectBool("IsFrozen", frozen);
            EffectBool("IsBurning", burning);
            UpdateStatusParticles();
            EffectBool("IsPoisoned", stats != null && stats.Poisoned);
            UpdateFootSmoke(moving);

            if (frozen && !poseFrozen)
            {
                poseFrozen = true;
                for (int i = 0; i < frozenParts.Length; i++)
                {
                    frozenPositions[i] = frozenParts[i].localPosition;
                    frozenRotations[i] = frozenParts[i].localRotation;
                    frozenScales[i] = frozenParts[i].localScale;
                }
            }
            else if (!frozen)
            {
                poseFrozen = false;
            }
        }

        private void InitializeStatusParticles(Transform defaultPoint)
        {
            if (statusParticles == null) return;
            foreach (StatusParticleSetting setting in statusParticles)
            {
                if (setting == null || setting.prefab == null) continue;
                Transform point = setting.spawnPoint != null ? setting.spawnPoint : defaultPoint;
                setting.instance = Instantiate(setting.prefab, point.position, point.rotation, point);
                // Vị trí luôn do điểm bám quyết định, không cộng offset ẩn của prefab.
                setting.instance.transform.localPosition = Vector3.zero;
                foreach (ParticleSystem layer in setting.instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = layer.main;
                    main.playOnAwake = false;
                    main.stopAction = ParticleSystemStopAction.None;
                }
                setting.instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void UpdateStatusParticles()
        {
            if (statusParticles == null) return;
            foreach (StatusParticleSetting setting in statusParticles)
            {
                if (setting == null || setting.instance == null) continue;
                bool on = stats != null && stats.Alive && stats.HasStatusEffect(setting.effect);
                if (setting.playing == on) continue;
                setting.playing = on;
                if (on) setting.instance.Play(true);
                else setting.instance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnDisable()
        {
            moveSmokeTimer = 0f;
            if (statusParticles == null) return;
            foreach (StatusParticleSetting setting in statusParticles)
            {
                if (setting == null) continue;
                setting.playing = false;
                if (setting.instance != null)
                    setting.instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void UpdateFootSmoke(bool moving)
        {
            bool isGrounded = move == null || !move.IsAirborne;

            // 1. SmokeAhead: Khi dừng lại (từ đang chạy -> đứng yên trên mặt đất)
            if (wasMoving && !moving && isGrounded)
            {
                SpawnSmoke(smokeAheadPrefab, smokeAheadPoint, -0.7f);
            }

            // 2. SmokeBack: Khi bắt đầu di chuyển & lặp lại mỗi 1s khi đang di chuyển trên đất
            if (moving && isGrounded)
            {
                if (!wasMoving)
                {
                    // Vừa bắt đầu di chuyển: kích hoạt ngay lập tức
                    moveSmokeTimer = 0f;
                    SpawnSmoke(smokeBackPrefab, smokeBackPoint, 0.8f);
                }
                else
                {
                    // Đang tiếp tục di chuyển: đếm 1s nổ 1 lần
                    moveSmokeTimer += Time.deltaTime;
                    if (moveSmokeTimer >= 1.0f)
                    {
                        moveSmokeTimer = 0f;
                        SpawnSmoke(smokeBackPrefab, smokeBackPoint, 0.8f);
                    }
                }
            }
            else
            {
                moveSmokeTimer = 0f;
            }

            wasMoving = moving;
        }

        // 3. SmokeMid: Kích hoạt khi nhảy bằng skill Jump
        public void PlayJumpSmoke()
        {
            if (Time.time - lastJumpSmokeTime < 0.2f) return;
            lastJumpSmokeTime = Time.time;
            SpawnSmoke(smokeMidPrefab, smokeMidPoint, 1.0f);
        }

        private void SpawnSmoke(GameObject prefab, Transform point, float baseScaleX)
        {
            if (prefab == null) return;
            Vector3 spawnPos = point != null ? point.position : transform.position;
            float faceSign = visual != null ? Mathf.Sign(visual.localScale.x) : 1f;
            Vector3 scale = prefab.transform.localScale;
            scale.x = (baseScaleX != 0f ? baseScaleX : scale.x) * faceSign;
            EffectPool.Instance.Spawn(prefab, spawnPos, Quaternion.identity, scale);
        }

        // Animator chạy trước LateUpdate, nên đặt lại tư thế tại đây sẽ giữ nguyên từng khớp.
        private void LateUpdate()
        {
            if (!poseFrozen) return;

            for (int i = 0; i < frozenParts.Length; i++)
            {
                if (frozenParts[i] == null) continue;
                frozenParts[i].localPosition = frozenPositions[i];
                frozenParts[i].localRotation = frozenRotations[i];
                frozenParts[i].localScale = frozenScales[i];
            }
        }

        // Dùng trực tiếp các hàm này khi bạn thêm Parameter mới.
        public void Trigger(string name)
        {
            if (Has(animator, name, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(name);
            if (string.Equals(name, "High", System.StringComparison.OrdinalIgnoreCase))
            {
                GetComponent<PlayerController>()?.PlayHighBeamEffect();
            }
        }
        public void ReplayTrigger(string name)
        {
            if (animator == null) return;
            if (Has(animator, name, AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger(name);
                animator.SetTrigger(name);
            }
            animator.Play(name, 0, 0f);
        }
        public void Bool(string name, bool value) { if (Has(animator, name, AnimatorControllerParameterType.Bool)) animator.SetBool(name, value); }
        public void Float(string name, float value) { if (Has(animator, name, AnimatorControllerParameterType.Float)) animator.SetFloat(name, value); }
        public void Win() => Trigger("Win");
        public void Die() => Trigger("Die");
        public void SetPlaybackSpeed(float speed)
        {
            float s = Mathf.Max(0.01f, speed);
            if (animator != null) animator.speed = s;
            if (effectAnimators != null)
            {
                for (int i = 0; i < effectAnimators.Length; i++)
                    if (effectAnimators[i] != null) effectAnimators[i].speed = s;
            }
        }

        public void ResetPlaybackSpeed()
        {
            if (animator != null) animator.speed = 1f;
            if (effectAnimators != null)
            {
                for (int i = 0; i < effectAnimators.Length; i++)
                    if (effectAnimators[i] != null) effectAnimators[i].speed = 1f;
            }
        }

        public void ResetAnimation()
        {
            ResetPlaybackSpeed();
            if (animator == null) return;
            if (Has(animator, "Win", AnimatorControllerParameterType.Trigger)) animator.ResetTrigger("Win");
            if (Has(animator, "Die", AnimatorControllerParameterType.Trigger)) animator.ResetTrigger("Die");
            animator.Play("Character_idle", 0, 0f);
        }

        // Chỉ gửi vào những Animator con có đúng Parameter tương ứng.
        public void EffectTrigger(string name)
        {
            foreach (Animator item in effectAnimators)
                if (Has(item, name, AnimatorControllerParameterType.Trigger)) item.SetTrigger(name);
        }
        public void EffectBool(string name, bool value)
        {
            foreach (Animator item in effectAnimators)
                if (Has(item, name, AnimatorControllerParameterType.Bool)) item.SetBool(name, value);
        }

        // Code chủ động thoát clip skill, không phụ thuộc transition dễ bị kẹt trong Animator.
        public void EndSkillAnimation()
        {
            if (animator != null) animator.CrossFade("Character_shot", .05f, 0);
        }

        private bool Has(Animator target, string name, AnimatorControllerParameterType type)
        {
            if (target == null || target.runtimeAnimatorController == null) return false;
            foreach (AnimatorControllerParameter item in target.parameters)
                if (item.name == name && item.type == type) return true;
            return false;
        }

        private bool IsEffectPart(Transform part)
        {
            for (Transform current = part; current != null && current != visual; current = current.parent)
                if (current.name == "Effect" || current.name == "Effects" || current.name == "FreezeEffect")
                    return true;

            return false;
        }
    }
}
