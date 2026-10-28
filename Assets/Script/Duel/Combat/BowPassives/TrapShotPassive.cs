using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Trap Shot", fileName = "TrapShotPassive")]
    public sealed class TrapShotPassive : BowPassive
    {
        [Tooltip("Số phát bắn tích lũy để thả bẫy.")]
        [SerializeField, Min(1)] private int shotsRequired = 10;
        [Tooltip("Prefab bẫy rơi xuống chân địch.")]
        [SerializeField] private GameObject trapPrefab;
        [Tooltip("Bật để sử dụng cấu hình số lượng và vị trí từ chính Prefab Trap. Tắt nếu muốn ghi đè bằng cài đặt riêng bên dưới.")]
        [SerializeField] private bool usePrefabSettings = true;
        [Tooltip("Số lượng bẫy rơi ra (khi tắt usePrefabSettings).")]
        [SerializeField, Min(1)] private int trapCount = 2;
        [Tooltip("Độ cao xuất hiện phía trên địch trước khi rơi xuống (khi tắt usePrefabSettings).")]
        [SerializeField, Min(0f)] private float dropHeight = 3f;
        [Tooltip("Khoảng cách tách giữa các bẫy (khi tắt usePrefabSettings).")]
        [SerializeField, Min(0f)] private float spreadDistance = 0.8f;

        public int ShotsRequired => shotsRequired;
        public GameObject TrapPrefab => trapPrefab;

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isSecondaryShot)
        {
            if (isSecondaryShot || player == null || trapPrefab == null) return;

            state.shotCount++;
            if (state.shotCount >= shotsRequired)
            {
                state.shotCount = 0;
                GameObject trapObj = SummonPool.Instance != null
                    ? SummonPool.Instance.Spawn(trapPrefab, Vector3.zero, Quaternion.identity)
                    : Object.Instantiate(trapPrefab);
                TrapObject trap = trapObj.GetComponent<TrapObject>();
                if (trap != null)
                {
                    if (!usePrefabSettings)
                    {
                        trap.TrapCount = trapCount;
                        trap.SpreadDistance = spreadDistance;
                        trap.DropHeight = dropHeight;
                    }
                    trap.Setup(player);
                }
            }
        }

        public override float GetProgress(BowRuntimeState state)
        {
            if (state == null) return 0f;
            return shotsRequired > 0 ? Mathf.Clamp01((float)state.shotCount / shotsRequired) : 1f;
        }
    }
}
