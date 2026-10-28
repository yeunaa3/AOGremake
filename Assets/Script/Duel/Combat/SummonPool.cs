using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Pool quản lý tái sử dụng các vật thể triệu hồi (SkillObject, Totem, Trap, Boulder...),
    /// tránh liên tục Instantiate/Destroy gây tụt FPS và rác bộ nhớ (GC Alloc).
    /// </summary>
    public sealed class SummonPool : MonoBehaviour
    {
        private static SummonPool instance;
        public static SummonPool Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SummonPool");
                    instance = go.AddComponent<SummonPool>();
                }
                return instance;
            }
        }

        private readonly Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();
        private readonly HashSet<GameObject> activeSummons = new HashSet<GameObject>();

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

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            if (instanceToPrefab.TryGetValue(prefab, out GameObject sourcePrefab) && sourcePrefab != null)
            {
                prefab = sourcePrefab;
            }

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

            obj.SetActive(true);
            activeSummons.Add(obj);
            return obj;
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null) return null;
            GameObject obj = Spawn(prefab.gameObject, position, rotation);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            activeSummons.Remove(obj);
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

        public void ReturnAll()
        {
            if (activeSummons.Count == 0) return;
            var list = new List<GameObject>(activeSummons);
            for (int i = 0; i < list.Count; i++)
            {
                Return(list[i]);
            }
        }
    }
}
