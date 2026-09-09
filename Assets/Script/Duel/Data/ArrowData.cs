using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Projectile Definition", fileName = "ProjectileDefinition")]
    public sealed class ArrowData : ScriptableObject
    {
        [Header("Prefab")]
        [SerializeField] private Arrow projectilePrefab;

        [Header("Fallback/skill combat")]
        [Tooltip("Damage mặc định cho skill. Đòn bắn thường sẽ lấy damage từ cung.")]
        [SerializeField, Min(0)] private int damage = 10;
        [SerializeField] private bool ignoreShield;

        [Header("Flight")]
        [SerializeField, Min(0.05f)] private float flightDuration = 0.8f;
        [SerializeField] private float arcHeight = 2.5f;
        [SerializeField, Min(0.1f)] private float maximumLifetime = 4f;
        [SerializeField] private bool rotateAlongPath = true;

        public Arrow ProjectilePrefab => projectilePrefab;
        public int Damage => damage;
        public bool IgnoreShield => ignoreShield;
        public float FlightDuration => flightDuration;
        public float ArcHeight => arcHeight;
        public float MaximumLifetime => maximumLifetime;
        public bool RotateAlongPath => rotateAlongPath;
    }
}
