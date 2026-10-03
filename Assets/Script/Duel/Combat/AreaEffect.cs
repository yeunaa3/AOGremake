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

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;
        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f) Destroy(gameObject);
        }

        public void Setup(PlayerController source) => owner = source;

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController target = other.GetComponentInParent<PlayerController>();
            if (target == null || owner == null || target.Team == owner.Team) return;
            if (nextHit.TryGetValue(target, out float time) && Time.time < time) return;
            nextHit[target] = Time.time + tickInterval;
            target.Stats.TakeDamage(new DamageInfo(owner, damage, false, transform.position,
                effect, effectPower, effectDuration));
        }
    }
}
