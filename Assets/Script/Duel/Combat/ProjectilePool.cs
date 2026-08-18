using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField, Min(0)] private int prewarmCount = 12;
        [SerializeField] private DuelProjectileDefinition[] definitionsToPrewarm;

        private readonly Dictionary<ArrowProjectile, Queue<ArrowProjectile>> available =
            new Dictionary<ArrowProjectile, Queue<ArrowProjectile>>();
        private readonly Dictionary<ArrowProjectile, ArrowProjectile> activeOrigins =
            new Dictionary<ArrowProjectile, ArrowProjectile>();

        private void Awake()
        {
            if (definitionsToPrewarm == null) return;

            foreach (DuelProjectileDefinition definition in definitionsToPrewarm)
            {
                if (definition == null || definition.ProjectilePrefab == null) continue;
                for (int i = 0; i < prewarmCount; i++)
                {
                    ArrowProjectile projectile = Create(definition.ProjectilePrefab);
                    Return(definition.ProjectilePrefab, projectile);
                }
            }
        }

        public ArrowProjectile Spawn(DuelProjectileDefinition definition)
        {
            if (definition == null || definition.ProjectilePrefab == null) return null;

            ArrowProjectile prefab = definition.ProjectilePrefab;
            Queue<ArrowProjectile> queue = GetQueue(prefab);
            ArrowProjectile projectile = queue.Count > 0 ? queue.Dequeue() : Create(prefab);
            activeOrigins[projectile] = prefab;
            projectile.gameObject.SetActive(true);
            projectile.PrepareForPool(p => Return(prefab, p));
            return projectile;
        }

        public void ReturnAll()
        {
            var snapshot = new List<KeyValuePair<ArrowProjectile, ArrowProjectile>>(activeOrigins);
            foreach (KeyValuePair<ArrowProjectile, ArrowProjectile> entry in snapshot)
            {
                Return(entry.Value, entry.Key);
            }
        }

        private ArrowProjectile Create(ArrowProjectile prefab)
        {
            ArrowProjectile projectile = Instantiate(prefab, transform);
            projectile.gameObject.name = prefab.gameObject.name;
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        private void Return(ArrowProjectile prefab, ArrowProjectile projectile)
        {
            if (projectile == null) return;
            activeOrigins.Remove(projectile);
            projectile.ResetProjectile();
            projectile.transform.SetParent(transform, false);
            projectile.gameObject.SetActive(false);
            GetQueue(prefab).Enqueue(projectile);
        }

        private Queue<ArrowProjectile> GetQueue(ArrowProjectile prefab)
        {
            if (!available.TryGetValue(prefab, out Queue<ArrowProjectile> queue))
            {
                queue = new Queue<ArrowProjectile>();
                available.Add(prefab, queue);
            }
            return queue;
        }

    }
}
