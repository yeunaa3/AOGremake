using UnityEngine;

namespace AOG.Duel
{
    public sealed class AutoDestroy : MonoBehaviour
    {
        [Tooltip("Thời gian tồn tại trước khi tự hủy (giây)")]
        [SerializeField] private float lifetime = 1.5f;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }
}
