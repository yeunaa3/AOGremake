using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class ArrowPool : MonoBehaviour
    {
        [SerializeField] private Arrow arrowPrefab;
        [SerializeField, Min(0)] private int prewarmCount = 12;

        private readonly Queue<Arrow> available = new Queue<Arrow>();
        private readonly HashSet<Arrow> active = new HashSet<Arrow>();

        private void Awake()
        {
            if (arrowPrefab == null) return;
            for (int i = 0; i < prewarmCount; i++) Return(Create());
        }

        public Arrow Spawn()
        {
            if (arrowPrefab == null) return null;
            Arrow projectile = available.Count > 0 ? available.Dequeue() : Create();
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

        private Arrow Create()
        {
            Arrow projectile = Instantiate(arrowPrefab, transform);
            projectile.gameObject.name = arrowPrefab.gameObject.name;
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        private void Return(Arrow projectile)
        {
            if (projectile == null) return;
            active.Remove(projectile);
            projectile.ResetProjectile();
            projectile.transform.SetParent(transform, false);
            projectile.gameObject.SetActive(false);
            available.Enqueue(projectile);
        }

    }
}
