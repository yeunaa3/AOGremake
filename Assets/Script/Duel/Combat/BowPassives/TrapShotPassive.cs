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
        [Tooltip("Số lượng bẫy rơi ra.")]
        [SerializeField, Min(1)] private int trapCount = 2;
        [Tooltip("Độ cao xuất hiện phía trên địch trước khi rơi xuống.")]
        [SerializeField, Min(0f)] private float dropHeight = 3f;
        [Tooltip("Khoảng cách tách giữa các bẫy.")]
        [SerializeField, Min(0f)] private float spreadDistance = 0.8f;

        public int ShotsRequired => shotsRequired;
        public GameObject TrapPrefab => trapPrefab;

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isSecondaryShot)
        {
            if (isSecondaryShot || player == null || player.Enemy == null || trapPrefab == null) return;

            state.shotCount++;
            if (state.shotCount >= shotsRequired)
            {
                state.shotCount = 0;
                Vector3 enemyPos = player.Enemy.transform.position;
                float first = -(trapCount - 1) * spreadDistance * 0.5f;
                for (int i = 0; i < trapCount; i++)
                {
                    Vector3 spawnPos = enemyPos + new Vector3(first + i * spreadDistance, dropHeight, 0f);
                    GameObject trapObj = Object.Instantiate(trapPrefab, spawnPos, Quaternion.identity);
                    TrapObject trap = trapObj.GetComponent<TrapObject>();
                    if (trap != null)
                    {
                        trap.Setup(player);
                    }
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
