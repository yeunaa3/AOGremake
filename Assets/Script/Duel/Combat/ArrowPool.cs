using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class ArrowPool : MonoBehaviour
    {
        [SerializeField] private Arrow arrowPrefab;
        [SerializeField, Min(0)] private int prewarmCount = 12;

        private readonly Dictionary<Arrow, Queue<Arrow>> available = new Dictionary<Arrow, Queue<Arrow>>();
        private readonly Dictionary<Arrow, Arrow> sourcePrefab = new Dictionary<Arrow, Arrow>();
        private readonly HashSet<Arrow> active = new HashSet<Arrow>();

        public Vector3 DefaultProjectileScale => arrowPrefab == null
            ? Vector3.one
            : arrowPrefab.transform.localScale;

        private void Awake()
        {
            if (arrowPrefab == null) return;
            for (int i = 0; i < prewarmCount; i++) Return(Create(arrowPrefab));
        }

        public Arrow Spawn(Arrow requestedPrefab = null)
        {
            Arrow prefab = requestedPrefab != null ? requestedPrefab : arrowPrefab;
            if (prefab == null) return null;
            if (!available.TryGetValue(prefab, out Queue<Arrow> queue))
            {
                queue = new Queue<Arrow>();
                available.Add(prefab, queue);
            }
            Arrow projectile = queue.Count > 0 ? queue.Dequeue() : Create(prefab);
            active.Add(projectile);
            projectile.gameObject.SetActive(true);
            projectile.PrepareForPool(Return);
            return projectile;
        }

        public void ReturnAll()
        {
            var snapshot = new List<Arrow>(active);
            foreach (Arrow projectile in snapshot) Return(projectile);
        }

        private Arrow Create(Arrow prefab)
        {
            Arrow projectile = Instantiate(prefab, transform);
            projectile.gameObject.name = prefab.gameObject.name;
            projectile.gameObject.SetActive(false);
            sourcePrefab[projectile] = prefab;
            return projectile;
        }

        private void Return(Arrow projectile)
        {
            if (projectile == null) return;
            active.Remove(projectile);
            projectile.ResetProjectile();
            projectile.transform.SetParent(transform, false);
            projectile.gameObject.SetActive(false);
            if (!sourcePrefab.TryGetValue(projectile, out Arrow prefab)) prefab = arrowPrefab;
            if (prefab == null) return;
            if (!available.TryGetValue(prefab, out Queue<Arrow> queue))
            {
                queue = new Queue<Arrow>();
                available.Add(prefab, queue);
            }
            queue.Enqueue(projectile);
        }

    }
}
