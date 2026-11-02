using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    // Trí tuệ nhân tạo (AI) mượt mà, thông minh cho Bot trong chế độ PVE:
    // - Khắc phục hoàn toàn hiện tượng dựt dựt/loạn tín hiệu nhờ cơ chế State Commitment & Hysteresis.
    // - Khóa hướng né (Dodge Lock): Khi đã bắt đầu né đòn, giữ nguyên hướng né trong thời gian cố định, không đổi hướng liên tục.
    // - Đứng lại xả tên (Stand & Shoot): Có nhịp đứng yên vững vàng (1 - 1.5s) để kéo cung và xả chiêu ổn định.
    // - Giữ cự ly vùng đệm (Hysteresis Spacing): Không bị dao động qua lại quanh 1 ngưỡng khoảng cách mỏng.
    public sealed class Bot : InputSource
    {
        [Header("Cấu Hình Nhận Thức")]
        [SerializeField, Min(0.2f)] private float decisionInterval = 0.45f;
        [SerializeField, Min(2f)] private float arrowDangerDistance = 7.0f;
        [SerializeField] private float retreatThreshold = 4.5f; // Quá gần -> lùi
        [SerializeField] private float advanceThreshold = 10.5f; // Quá xa -> tiến

        private PlayerController self;
        private PlayerController enemy;
        private PlayerStats selfStats;
        private PlayerStats enemyStats;
        private PlayerAction selfAction;
        private ArrowPool arrowPool;

        private float nextDecisionTime;
        private float currentMove;
        private InputCmd pendingCommand;

        // Trạng thái né tránh có khóa hướng (Anti-Jitter)
        private bool isEvading;
        private float evadeTimer;
        private float evadeDirection;
        private float dodgeCooldownTimer;

        // Bộ đếm cam kết hành động (Action Commitment)
        private float moveCommitTimer;
        private float standStillTimer;
        private float skillIntervalTimer;

        private void Awake()
        {
            CacheReferences();
        }

        private void CacheReferences()
        {
            if (self == null) self = GetComponent<PlayerController>();
            if (self != null)
            {
                if (enemy == null) enemy = self.Enemy;
                if (selfStats == null) selfStats = self.Stats;
                if (selfAction == null) selfAction = self.Action;
                if (arrowPool == null) arrowPool = self.Pool;
            }
            if (enemy != null && enemyStats == null)
            {
                enemyStats = enemy.Stats;
            }
            if (arrowPool == null)
            {
                arrowPool = FindFirstObjectByType<ArrowPool>();
            }
        }

        public override InputCmd ReadCommand()
        {
            CacheReferences();

            if (self == null || !self.Alive)
            {
                return default;
            }

            float dt = Time.deltaTime;

            // Giảm các bộ đếm thời gian
            if (dodgeCooldownTimer > 0f) dodgeCooldownTimer -= dt;
            if (moveCommitTimer > 0f) moveCommitTimer -= dt;
            if (standStillTimer > 0f) standStillTimer -= dt;
            if (skillIntervalTimer > 0f) skillIntervalTimer -= dt;

            // 1. Xử lý trạng thái né đòn (Nếu đang né thì KHÔNG quét tín hiệu mới để tránh đổi hướng liên tục)
            if (isEvading)
            {
                evadeTimer -= dt;
                if (evadeTimer <= 0f)
                {
                    isEvading = false;
                    dodgeCooldownTimer = 0.4f; // Sau khi né xong, chờ 0.4s mới kích hoạt né tiếp
                }
            }
            else if (dodgeCooldownTimer <= 0f)
            {
                // Chỉ quét mũi tên khi không đang trong nhịp né
                CheckIncomingProjectilesAndDodge();
            }

            // 2. Đánh giá chiến thuật di chuyển và dùng chiêu theo chu kỳ (tránh giật lag)
            if (!isEvading && Time.time >= nextDecisionTime)
            {
                nextDecisionTime = Time.time + decisionInterval;
                MakeTacticalDecision();
            }

            InputCmd result = pendingCommand;
            result.Move = isEvading ? evadeDirection : currentMove;
            pendingCommand = default;
            return result;
        }

        // =========================================================================
        // 1. HỆ THỐNG NÉ CHIÊU ỔN ĐỊNH (STABLE DODGE SYSTEM)
        // =========================================================================
        private void CheckIncomingProjectilesAndDodge()
        {
            if (arrowPool == null || self == null) return;

            float selfX = self.transform.position.x;
            Vector2 selfPos = self.transform.position;

            foreach (Arrow arrow in arrowPool.ActiveArrows)
            {
                if (arrow == null || !arrow.IsFlying || arrow.Owner == self) continue;

                Vector2 arrowPos = arrow.transform.position;
                float dist = Vector2.Distance(arrowPos, selfPos);

                // Mũi tên đang bay thẳng về phía bot
                bool flyingTowardsBot = (arrowPos.x < selfX && arrow.EndPoint.x >= selfX - 1.2f) ||
                                        (arrowPos.x > selfX && arrow.EndPoint.x <= selfX + 1.2f);

                if (dist <= arrowDangerDistance && flyingTowardsBot)
                {
                    // Tên đến rất gần (< 3.8m): Ưu tiên kích hoạt chiêu phòng ngự Bất Tử nếu có
                    if (dist <= 3.8f && TryUseEmergencyDefense())
                    {
                        return;
                    }

                    // Khóa một hướng né duy nhất:
                    // Điểm rơi phía trước -> lùi lại; điểm rơi phía sau -> tiến lên
                    float targetLandingX = arrow.EndPoint.x;
                    float dodgeDir = (selfX >= targetLandingX) ? 1f : -1f;

                    // Giới hạn không để bot đâm đầu vào vách đấu trường
                    if (selfX > 8.0f) dodgeDir = -1f;
                    else if (selfX < -8.0f) dodgeDir = 1f;

                    // Bắt đầu chu kỳ né cam kết (giữ nguyên hướng trong 0.4s, không đổi hướng giữa chừng)
                    isEvading = true;
                    evadeDirection = dodgeDir;
                    evadeTimer = 0.45f;
                    return;
                }
            }
        }

        private bool TryUseEmergencyDefense()
        {
            if (selfAction == null || !self.CanSkill) return false;

            for (int i = 0; i < 5; i++)
            {
                SkillData skill = selfAction.GetSkill(i);
                if (skill == null || skill.Passive) continue;
                if (selfAction.GetSkillCooldownRemaining(i) > 0f) continue;

                if (skill is BuffSkill buffSkill && buffSkill.Buff != null && buffSkill.Buff.invulnerable)
                {
                    PressSkill(i);
                    return true;
                }
            }
            return false;
        }

        // =========================================================================
        // 2. CHIẾN THUẬT DI CHUYỂN & SỬ DỤNG KỸ NĂNG CÓ CAM KẾT (TACTICAL COMMITMENT)
        // =========================================================================
        private void MakeTacticalDecision()
        {
            if (enemy == null)
            {
                currentMove = 0f;
                return;
            }

            float selfX = self.transform.position.x;
            float enemyX = enemy.transform.position.x;
            float distToEnemy = Mathf.Abs(selfX - enemyX);
            float dirToEnemy = Mathf.Sign(enemyX - selfX);

            // A. TUNG CHIÊU THÔNG MINH
            if (skillIntervalTimer <= 0f && self.CanSkill &&
                (selfAction.Ready || selfAction.State == ActionState.AutoAttacking))
            {
                if (TryCastSmartSkill(distToEnemy))
                {
                    skillIntervalTimer = Random.Range(0.8f, 1.4f);
                    // Dừng chân cam kết để hoàn thành animation xuất chiêu
                    standStillTimer = 0.5f;
                    currentMove = 0f;
                    return;
                }
            }

            // Nếu đang trong thời gian cam kết đứng bắn (standStillTimer) thì tiếp tục đứng yên
            if (standStillTimer > 0f)
            {
                currentMove = 0f;
                return;
            }

            // Nếu đang trong nhịp cam kết di chuyển (moveCommitTimer) thì tiếp tục giữ hướng đó
            if (moveCommitTimer > 0f)
            {
                return;
            }

            // Nếu nội tại đặc biệt của cung đã nạp đầy -> Đứng lại để AutoShoot giải phóng đòn bắn nội tại
            if (self.Gear != null && self.Gear.BowState.specialSkillReady)
            {
                currentMove = 0f;
                standStillTimer = 0.8f;
                return;
            }

            // B. QUẢN LÝ KHOẢNG CÁCH CÓ VÙNG ĐỆM (HYSTERESIS)
            if (distToEnemy < retreatThreshold)
            {
                // Quá gần -> Lùi lại và cam kết lùi trong 0.8s để dãn cách hoàn toàn, không nhấp nhả
                currentMove = -dirToEnemy;
                moveCommitTimer = 0.85f;
            }
            else if (distToEnemy > advanceThreshold)
            {
                // Quá xa -> Tiến lại và cam kết tiến trong 0.8s
                currentMove = dirToEnemy;
                moveCommitTimer = 0.85f;
            }
            else
            {
                // Ở cự ly bắn lý tưởng (5m - 10m):
                // 70% trường hợp: Đứng im vững vàng để AutoShoot kéo cung bắn thường liên tục
                // 30% trường hợp: Di chuyển đổi góc đứng một nhịp ngắn (0.6s)
                if (Random.value < 0.7f)
                {
                    currentMove = 0f;
                    standStillTimer = Random.Range(1.0f, 1.6f);
                }
                else
                {
                    currentMove = (Random.value < 0.5f ? -dirToEnemy : dirToEnemy);
                    moveCommitTimer = 0.6f;
                }
            }
        }

        private bool TryCastSmartSkill(float distToEnemy)
        {
            if (selfStats == null || selfAction == null) return false;

            float hpPercent = (float)selfStats.Hp / Mathf.Max(1, selfStats.MaxHp);
            bool hasDebuff = selfStats.Burning || selfStats.Frozen || selfStats.Poisoned;
            bool enemyIsChanneling = enemy != null && enemy.Action != null && enemy.Action.IsChanneling;

            // 1. Ưu tiên giải debuff (Cleanse) khi bị dính trạng thái bất lợi
            if (hasDebuff)
            {
                for (int i = 0; i < 5; i++)
                {
                    SkillData s = selfAction.GetSkill(i);
                    if (s is BuffSkill bs && bs.Buff != null && (bs.Buff.cleanse || bs.Buff.statusImmune))
                    {
                        if (selfAction.GetSkillCooldownRemaining(i) <= 0f)
                        {
                            PressSkill(i);
                            return true;
                        }
                    }
                }
            }

            // 2. Ưu tiên hồi máu khi HP < 50%
            if (hpPercent <= 0.5f)
            {
                for (int i = 0; i < 5; i++)
                {
                    SkillData s = selfAction.GetSkill(i);
                    if (s is BuffSkill bs && bs.Buff != null && (bs.Buff.heal > 0 || bs.Buff.healPerSecond > 0))
                    {
                        if (selfAction.GetSkillCooldownRemaining(i) <= 0f)
                        {
                            if (bs.Buff.isRestSkill && distToEnemy < 6.5f) continue;
                            PressSkill(i);
                            return true;
                        }
                    }
                }
            }

            // 3. Ưu tiên Bất Tử khi HP rất thấp (< 30%)
            if (hpPercent <= 0.3f)
            {
                for (int i = 0; i < 5; i++)
                {
                    SkillData s = selfAction.GetSkill(i);
                    if (s is BuffSkill bs && bs.Buff != null && bs.Buff.invulnerable)
                    {
                        if (selfAction.GetSkillCooldownRemaining(i) <= 0f)
                        {
                            PressSkill(i);
                            return true;
                        }
                    }
                }
            }

            // 4. Kỹ năng triệu hồi quái (Decoy, Turret, Roller)
            for (int i = 0; i < 5; i++)
            {
                SkillData s = selfAction.GetSkill(i);
                if (s is SummonObject || (s != null && s.DisplayName.ToLowerInvariant().Contains("summon")))
                {
                    if (selfAction.GetSkillCooldownRemaining(i) <= 0f && distToEnemy <= 12f)
                    {
                        PressSkill(i);
                        return true;
                    }
                }
            }

            // 5. Chiêu tấn công: Trừng phạt khi đối thủ đang đứng yên hoặc khi vào tầm bắn
            if (enemyIsChanneling || distToEnemy <= 11f)
            {
                for (int i = 0; i < 5; i++)
                {
                    SkillData s = selfAction.GetSkill(i);
                    if (s == null || s.Passive) continue;
                    if (selfAction.GetSkillCooldownRemaining(i) > 0f) continue;

                    // Không dùng hồi máu/bất tử vào việc tấn công
                    if (s is BuffSkill bs && bs.Buff != null && bs.Buff.heal > 0 && hpPercent > 0.7f) continue;
                    if (s is BuffSkill bs2 && bs2.Buff != null && bs2.Buff.invulnerable && hpPercent > 0.45f) continue;

                    PressSkill(i);
                    return true;
                }
            }

            return false;
        }

        private void PressSkill(int slot)
        {
            switch (slot)
            {
                case 0: pendingCommand.Skill1Pressed = true; break;
                case 1: pendingCommand.Skill2Pressed = true; break;
                case 2: pendingCommand.Skill3Pressed = true; break;
                case 3: pendingCommand.Skill4Pressed = true; break;
                case 4: pendingCommand.Skill5Pressed = true; break;
            }
        }
    }
}
