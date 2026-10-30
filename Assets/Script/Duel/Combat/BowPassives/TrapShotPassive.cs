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

        public override bool HasChargeRelease => true;
        public override string SpecialAnimationTrigger => "High";

        public override void ExecuteSpecialRelease(PlayerController player, BowRuntimeState state)
        {
            ReleaseTrap(player, state);
            if (state != null)
            {
                state.shotCount = 0;
                state.specialSkillReady = false;
            }
        }

        public override void OnUnequip(PlayerController player, BowRuntimeState state)
        {
            if (state != null)
            {
                for (int i = 0; i < state.activeSummons.Count; i++)
                {
                    if (state.activeSummons[i] != null)
                    {
                        if (SummonPool.Instance != null) SummonPool.Instance.Return(state.activeSummons[i]);
                        else Destroy(state.activeSummons[i]);
                    }
                }
                state.Reset();
            }
        }

        public override void OnUpdate(PlayerController player, BowRuntimeState state, float deltaTime)
        {
        }

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isNormalShot)
        {
            if (player == null || trapPrefab == null || state == null) return;

            if (state.shotCount < shotsRequired)
            {
                state.shotCount++;
                if (state.shotCount >= shotsRequired)
                {
                    state.specialSkillReady = true;
                }
            }
        }

        public void ReleaseTrap(PlayerController player, BowRuntimeState state)
        {
            SkillAudioManager.PlayMine();
            float spawnH = usePrefabSettings ? 4.5f : dropHeight;
            Vector3 targetPos = (player.Enemy != null ? player.Enemy.transform.position : player.transform.position) + Vector3.up * spawnH;
            GameObject trapObj = SummonPool.Instance != null
                ? SummonPool.Instance.Spawn(trapPrefab, targetPos, Quaternion.identity)
                : Object.Instantiate(trapPrefab, targetPos, Quaternion.identity);
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
            if (state != null)
            {
                state.RegisterSummon(trapObj);
            }
        }

        public override float GetProgress(BowRuntimeState state)
        {
            if (state == null) return 0f;
            if (state.specialSkillReady)
            {
                return 1f;
            }
            return shotsRequired > 0 ? Mathf.Clamp01((float)state.shotCount / shotsRequired) : 1f;
        }
    }
}
