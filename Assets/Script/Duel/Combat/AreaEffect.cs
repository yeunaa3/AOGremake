using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class AreaEffect : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float lifetime = 3f;
        [SerializeField, Min(0.05f)] private float tickInterval = 1f;
        [SerializeField, Min(0)] private int damage = 3;
        [SerializeField] private ArrowEffect effect;
        [SerializeField, Min(0)] private int effectPower;
        [SerializeField, Min(0f)] private float effectDuration;

        private readonly Dictionary<PlayerController, float> nextHit = new Dictionary<PlayerController, float>();
        private PlayerController owner;
        private bool statusOnly;
        private readonly HashSet<PlayerController> affectedTargets = new HashSet<PlayerController>();

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;
        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f) Destroy(gameObject);
        }

        public void Setup(PlayerController source) => owner = source;

        // Vùng dưới đất truyền hiệu ứng tương ứng, không gây damage va chạm của tên.
        public void SetupStatusZone(PlayerController source, ArrowEffect status,
            int power, float duration, float seconds)
        {
            owner = source;
            statusOnly = true;
            effect = status;
            effectPower = power;
            effectDuration = duration;
            lifetime = Mathf.Max(.05f, seconds);
            affectedTargets.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (statusOnly) ApplyGroundStatus(other);
        }

        private void ApplyGroundStatus(Collider2D other)
        {
            PlayerController target = other.GetComponentInParent<PlayerController>();
            if (target != null && owner != null && target.Team != owner.Team)
            {
                if (!affectedTargets.Add(target)) return;
                target.Stats.ApplyStatus(owner, effect, effectPower, effectDuration);
                return;
            }

            SkillObject obstacle = other.GetComponentInParent<SkillObject>();
            if (obstacle != null && owner != null && obstacle.Team != owner.Team)
            {
                obstacle.ApplyStatus(owner, effect, effectPower, effectDuration);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (statusOnly)
            {
                ApplyGroundStatus(other);
                return;
            }
            PlayerController target = other.GetComponentInParent<PlayerController>();
            if (target != null && owner != null && target.Team != owner.Team)
            {
                if (!nextHit.TryGetValue(target, out float time) || Time.time >= time)
                {
                    nextHit[target] = Time.time + tickInterval;
                    target.Stats.TakeDamage(new DamageInfo(owner, damage, false, transform.position,
                        effect, effectPower, effectDuration));
                }
                return;
            }

            SkillObject obstacle = other.GetComponentInParent<SkillObject>();
            if (obstacle != null && owner != null && obstacle.Team != owner.Team)
            {
                Vector2 hitDir = (Vector2)(obstacle.transform.position - transform.position).normalized;
                obstacle.TakeDamage(damage, owner, hitDir, effect, effectPower, effectDuration);
            }
        }
    }
}
