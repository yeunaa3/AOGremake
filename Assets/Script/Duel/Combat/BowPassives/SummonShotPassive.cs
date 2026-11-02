using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Summon Shot", fileName = "SummonShotPassive")]
    public sealed class SummonShotPassive : BowPassive
    {
        [Tooltip("Số phát bắn tích lũy để triệu hồi prefab.")]
        [SerializeField, Min(1)] private int shotsRequired = 5;

        [Header("Prefab Theo Map")]
        [Tooltip("Prefab triệu hồi riêng cho Map 1 (Thiên Đường / Angel) - Rơi xuống đất nằm im nổ sau 4s.")]
        [SerializeField] private GameObject prefabMap1;
        [Tooltip("Prefab triệu hồi riêng cho Map 2 (Địa Ngục / Devil) - Đâm xuống đất rồi rút lại như ban đầu.")]
        [SerializeField] private GameObject prefabMap2;

        [Header("Vị Trí & Tích Lũy Triệu Hồi")]
        [Tooltip("Triệu hồi tại vị trí chân đối thủ (true) hay chân người bắn (false).")]
        [SerializeField] private bool summonAtTarget = true;
        [Tooltip("Độ lệch vị trí xuất hiện so với mục tiêu.")]
        [SerializeField] private Vector2 spawnOffset = Vector2.zero;

        [Tooltip("Số lượng prefab triệu hồi tối đa (mặc định 5 cái)")]
        [SerializeField, Min(1)] private int maxSpawnCount = 5;
        [Tooltip("Khoảng cách tối thiểu trên trục X để spawn (mặc định 2.0 cho mục tiêu bên phải)")]
        [SerializeField] private float minSpawnX = 2.0f;
        [Tooltip("Khoảng cách tối đa trên trục X để spawn (mặc định 6.5 cho mục tiêu bên phải)")]
        [SerializeField] private float maxSpawnX = 6.5f;
        [Tooltip("Khoảng cách dàn hàng ngang giữa các prefab")]
        [SerializeField, Min(0.1f)] private float spacing = 0.8f;

        public int ShotsRequired => shotsRequired;
        public GameObject PrefabMap1 => prefabMap1;
        public GameObject PrefabMap2 => prefabMap2;
        public int MaxSpawnCount => maxSpawnCount;
        public float MinSpawnX => minSpawnX;
        public float MaxSpawnX => maxSpawnX;
        public float Spacing => spacing;

        public GameObject GetPrefabForCurrentScene()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.IndexOf("Map2", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                sceneName.IndexOf("Devil", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                sceneName.IndexOf("Hell", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (prefabMap2 != null) return prefabMap2;
            }

            return prefabMap1 != null ? prefabMap1 : prefabMap2;
        }

        public override bool HasChargeRelease => true;
        public override string SpecialAnimationTrigger => "High";

        public override void ExecuteSpecialRelease(PlayerController player, BowRuntimeState state)
        {
            int count = 1;
            if (state != null)
            {
                // Cứ sau 1 lần tung nội tại thì cộng thêm 1 prefab, tối đa maxSpawnCount (5 cái)
                state.triggerCount++;
                count = Mathf.Clamp(state.triggerCount, 1, maxSpawnCount);
            }

            SummonPrefab(player, state, count);

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
            if (player == null || state == null) return;
            if (GetPrefabForCurrentScene() == null) return;

            if (state.shotCount < shotsRequired)
            {
                state.shotCount++;
                if (state.shotCount >= shotsRequired)
                {
                    state.specialSkillReady = true;
                }
            }
        }

        public void SummonPrefab(PlayerController player, BowRuntimeState state)
        {
            int count = 1;
            if (state != null && state.triggerCount > 0)
            {
                count = Mathf.Clamp(state.triggerCount, 1, maxSpawnCount);
            }
            SummonPrefab(player, state, count);
        }

        public void SummonPrefab(PlayerController player, BowRuntimeState state, int count)
        {
            GameObject targetPrefab = GetPrefabForCurrentScene();
            if (targetPrefab == null) return;

            Vector3 basePos = (summonAtTarget && player.Enemy != null)
                ? player.Enemy.transform.position
                : player.transform.position;

            List<float> spawnXList = CalculateSpawnPositionsX(player, count);

            for (int i = 0; i < spawnXList.Count; i++)
            {
                float x = spawnXList[i];
                Vector3 spawnPos = new Vector3(x, basePos.y + spawnOffset.y, 0f);

                GameObject obj = SummonPool.Instance != null
                    ? SummonPool.Instance.Spawn(targetPrefab, spawnPos, Quaternion.identity)
                    : Object.Instantiate(targetPrefab, spawnPos, Quaternion.identity);

                if (obj != null)
                {
                    SkillObject skillObj = obj.GetComponent<SkillObject>();
                    if (skillObj != null)
                    {
                        skillObj.Setup(player);
                    }

                    TrapObject trapObj = obj.GetComponent<TrapObject>();
                    if (trapObj != null)
                    {
                        trapObj.Setup(player);
                    }

                    SummonMap1Bomb map1Bomb = obj.GetComponent<SummonMap1Bomb>();
                    if (map1Bomb != null)
                    {
                        map1Bomb.Setup(player);
                    }

                    SummonMap2Plunge map2Plunge = obj.GetComponent<SummonMap2Plunge>();
                    if (map2Plunge != null)
                    {
                        map2Plunge.Setup(player);
                    }

                    if (state != null)
                    {
                        state.RegisterSummon(obj);
                    }
                }
            }

            SkillAudioManager.PlaySummon();
        }

        /// <summary>
        /// Tính toán danh sách tọa độ X cố định cho các prefab ngay tại thời điểm tung chiêu.
        /// - Khi người chơi (Player 1) tung chiêu lên đầu địch: vùng rơi là [2.0, 6.5] (phía địch).
        /// - Khi địch (Bot / Player 2) tung chiêu lên đầu người chơi: vùng rơi nhân -1 thành [-6.5, -2.0] (phía mình).
        /// Các prefab dàn hàng ngang cách nhau 1 khoảng (spacing) và cân bằng 50/50 quanh vị trí lúc bấm chiêu.
        /// Sau khi spawn ra, tọa độ này hoàn toàn cố định và không bám theo kẻ địch.
        /// </summary>
        public List<float> CalculateSpawnPositionsX(PlayerController player, int count)
        {
            List<float> positions = new List<float>(count);
            if (count <= 0) return positions;

            float minX = Mathf.Min(minSpawnX, maxSpawnX);
            float maxX = Mathf.Max(minSpawnX, maxSpawnX);

            PlayerController target = (summonAtTarget && player != null && player.Enemy != null)
                ? player.Enemy
                : player;

            // Kiểm tra xem có phải Địch sử dụng lên đầu Mình hay không:
            // Địch là Team != 0 (Player 2 / Bot), hoặc vị trí nạn nhân đang ở bên trái (X < 0)
            bool isAttackingLeftPlayer = false;
            if (player != null)
            {
                if (player.Team != 0)
                {
                    isAttackingLeftPlayer = true;
                }
                else if (target != null && target.transform.position.x < 0f)
                {
                    isAttackingLeftPlayer = true;
                }
            }

            float zoneMin, zoneMax;
            if (isAttackingLeftPlayer)
            {
                // Nhân -1: từ [2.0, 6.5] đổi thành [-6.5, -2.0]
                zoneMin = -maxX; // -6.5
                zoneMax = -minX; // -2.0
            }
            else
            {
                // Chiêu do mình dùng lên đầu địch: [2.0, 6.5]
                zoneMin = minX;  // 2.0
                zoneMax = maxX;  // 6.5
            }

            float targetX = target != null ? target.transform.position.x : (zoneMin + zoneMax) * 0.5f;

            if (count == 1)
            {
                // Chỉ 1 cái: chọn cố định vị trí của mục tiêu tại thời điểm này (kẹp trong [zoneMin, zoneMax])
                positions.Add(Mathf.Clamp(targetX, zoneMin, zoneMax));
                return positions;
            }

            float currentSpacing = spacing;
            float totalSpan = (count - 1) * currentSpacing;
            float zoneWidth = zoneMax - zoneMin;

            if (totalSpan > zoneWidth)
            {
                currentSpacing = (count > 1) ? (zoneWidth / (count - 1)) : 0f;
                totalSpan = zoneWidth;
            }

            float halfSpan = totalSpan * 0.5f;
            // Căn giữa vị trí đầu mục tiêu tại thời điểm bấm nhưng đảm bảo toàn bộ nhóm nằm gọn trong [zoneMin, zoneMax]
            float clampedCenter = Mathf.Clamp(targetX, zoneMin + halfSpan, zoneMax - halfSpan);

            for (int i = 0; i < count; i++)
            {
                float x = clampedCenter - halfSpan + i * currentSpacing;
                positions.Add(x);
            }

            return positions;
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
