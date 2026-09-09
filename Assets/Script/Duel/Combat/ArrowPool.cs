using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class ArrowPool : MonoBehaviour
    {
        [SerializeField, Min(0)] private int prewarmCount = 12;
        [SerializeField] private ArrowData[] definitionsToPrewarm;

        private readonly Dictionary<Arrow, Queue<Arrow>> available =
            new Dictionary<Arrow, Queue<Arrow>>();
        private readonly Dictionary<Arrow, Arrow> activeOrigins =
            new Dictionary<Arrow, Arrow>();

        private void Awake()
        {
            if (definitionsToPrewarm == null) return;

            foreach (ArrowData definition in definitionsToPrewarm)
            {
                if (definition == null || definition.ProjectilePrefab == null) continue;
                for (int i = 0; i < prewarmCount; i++)
                {
                    Arrow projectile = Create(definition.ProjectilePrefab);
                    Return(definition.ProjectilePrefab, projectile);
                }
            }
        }

        public Arrow Spawn(ArrowData definition)
        {
            if (definition == null || definition.ProjectilePrefab == null) return null;

            Arrow prefab = definition.ProjectilePrefab;
            Queue<Arrow> queue = GetQueue(prefab);
            Arrow projectile = queue.Count > 0 ? queue.Dequeue() : Create(prefab);
            activeOrigins[projectile] = prefab;
            projectile.gameObject.SetActive(true);
            projectile.PrepareForPool(p => Return(prefab, p));
            return projectile;
        }

        public void ReturnAll()
        {
            var snapshot = new List<KeyValuePair<Arrow, Arrow>>(activeOrigins);
            foreach (KeyValuePair<Arrow, Arrow> entry in snapshot)
            {
                Return(entry.Value, entry.Key);
            }
        }

        private Arrow Create(Arrow prefab)
        {
            Arrow projectile = Instantiate(prefab, transform);
            projectile.gameObject.name = prefab.gameObject.name;
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        private void Return(Arrow prefab, Arrow projectile)
        {
            if (projectile == null) return;
            activeOrigins.Remove(projectile);
            projectile.ResetProjectile();
            projectile.transform.SetParent(transform, false);
            projectile.gameObject.SetActive(false);
            GetQueue(prefab).Enqueue(projectile);
        }

        private Queue<Arrow> GetQueue(Arrow prefab)
        {
            if (!available.TryGetValue(prefab, out Queue<Arrow> queue))
            {
                queue = new Queue<Arrow>();
                available.Add(prefab, queue);
            }
            return queue;
        }

    }
}
