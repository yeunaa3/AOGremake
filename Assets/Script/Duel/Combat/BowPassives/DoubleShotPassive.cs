using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Double Shot", fileName = "DoubleShotPassive")]
    public sealed class DoubleShotPassive : BowPassive
    {
        [Tooltip("Tỉ lệ sinh ra mũi tên bởi mũi tên đi trước (0.3 = 30%).")]
        [SerializeField, Range(0f, 1f)] private float chance = 0.3f;
        [Tooltip("Độ trễ trước khi mũi tên tiếp theo nhân đôi từ mũi tên đang bay (giây).")]
        [SerializeField, Min(0.01f)] private float delay = 0.08f;
        [Tooltip("Khoảng cách bám đuôi ngay sau mũi tên trước để nhìn rõ 2 mũi tên bay cùng hướng.")]
        [SerializeField, Min(0f)] private float followerLagDistance = 0.08f;
        [Tooltip("Giới hạn số lần sinh liên tiếp tối đa để tránh lặp vô hạn.")]
        [SerializeField, Range(1, 10)] private int maxChainDepth = 5;

        public float Chance => chance;
        public float Delay => delay;
        public float FollowerLagDistance => followerLagDistance;
        public int MaxChainDepth => maxChainDepth;

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isNormalShot)
        {
            if (arrow == null || player == null || !player.Alive) return;

            // Nội tại: Mũi tên đang bay có tỉ lệ nhân đôi ra mũi tên tiếp theo cùng vị trí, góc bay
            TrySpawnFollowerArrow(player, arrow, 1);
        }

        private void TrySpawnFollowerArrow(PlayerController player, Arrow leadingArrow, int currentDepth)
        {
            if (currentDepth > maxChainDepth) return;
            if (Random.value > chance) return;

            player.StartCoroutine(RoutineSpawnFollowerArrow(player, leadingArrow, currentDepth));
        }

        private IEnumerator RoutineSpawnFollowerArrow(PlayerController player, Arrow leadingArrow, int currentDepth)
        {
            float waitTime = delay;
            // Với các mũi tên chuỗi nối tiếp (depth > 1), tạo ngay sát nhau hơn
            if (currentDepth > 1)
            {
                waitTime = Mathf.Min(delay, 0.05f);
            }
            else if (leadingArrow != null && leadingArrow.PathLength > 0.1f)
            {
                float totalTime = leadingArrow.PathLength / Mathf.Max(0.1f, leadingArrow.EffectiveFlightSpeed);
                // Giới hạn thời gian chờ tối đa 25% hành trình để nhân đôi sớm ngay khi mũi tên vừa bay
                if (waitTime >= totalTime * 0.4f)
                {
                    waitTime = totalTime * 0.25f;
                }
            }

            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }

            // Kiểm tra nghiêm ngặt: Mũi tên đi trước phải CÒN ĐANG BAY trong không trung thì mới được nhân đôi
            if (player == null || !player.Alive || leadingArrow == null) yield break;
            if (!leadingArrow.gameObject.activeInHierarchy || !leadingArrow.IsFlying || leadingArrow.IsStuckInGround) yield break;

            Arrow follower = player.Pool == null ? null : player.Pool.Spawn();
            if (follower == null) yield break;

            // Nhân đôi trực tiếp từ mũi tên đang bay: Cùng vị trí, cùng góc bay, cùng quỹ đạo tiếp tục bay tới mục tiêu
            follower.LaunchFromFlyingArrow(leadingArrow, followerLagDistance);
            SkillAudioManager.PlayNormalArrowShoot();

            // Mũi tên vừa sinh ra tiếp tục có tỉ lệ nhân đôi tiếp nếu chưa đạt maxChainDepth
            TrySpawnFollowerArrow(player, follower, currentDepth + 1);
        }
    }
}
