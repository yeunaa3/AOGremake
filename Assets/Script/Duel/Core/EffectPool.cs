using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Pool quản lý tái sử dụng các hiệu ứng VFX (như khói chân, v.v.),
    /// tránh liên tục Instantiate/Destroy gây rác bộ nhớ (GC Alloc).
    /// </summary>
    public sealed class EffectPool : MonoBehaviour
    {
        private static EffectPool instance;
        public static EffectPool Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("EffectPool");
                    instance = go.AddComponent<EffectPool>();
                }
                return instance;
            }
        }

        private readonly Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (prefab == null) return null;

            if (!pool.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pool[prefab] = queue;
            }

            GameObject obj = null;
            while (queue.Count > 0)
            {
                GameObject candidate = queue.Dequeue();
                if (candidate != null)
                {
                    obj = candidate;
                    break;
                }
            }

            if (obj == null)
            {
                obj = Instantiate(prefab, position, rotation, transform);
                instanceToPrefab[obj] = prefab;
            }
            else
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }

            obj.transform.localScale = scale;
            obj.SetActive(true);

            // Bắt đầu phát hiệu ứng
            PooledEffect effect = obj.GetComponent<PooledEffect>();
            if (effect != null)
            {
                effect.Play(Return);
            }

            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(transform, false);

            if (instanceToPrefab.TryGetValue(obj, out GameObject prefab) && prefab != null)
            {
                if (!pool.TryGetValue(prefab, out Queue<GameObject> queue))
                {
                    queue = new Queue<GameObject>();
                    pool[prefab] = queue;
                }
                queue.Enqueue(obj);
            }
        }
    }
}
