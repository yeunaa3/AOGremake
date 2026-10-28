using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AOG.Duel
{
    // Mọi hành động chiến đấu nằm ở đây, viết thẳng để dễ đọc.
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerMove))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerAnim))]
    public sealed class PlayerAction : MonoBehaviour
    {
        [Header("5 Skill")]
        [SerializeField] private SkillLoadout loadout;
        [SerializeField] private SkillData[] skills = new SkillData[5];

        private PlayerController player;
        private PlayerMove move;
        private PlayerStats stats;
        private PlayerAnim anim;
        private float time;
        private readonly float[] skillLeft = new float[5];
        private int skillSlot = -1;
        private float skillDirection = 1f;
        private bool normalArrowShown;
        private bool normalArrowFired;
        private bool skillEffectDone;
        private bool skillVisualShown;
        private int currentVolleyIndex;
        private SkillData specialSkill;

        [Header("Đánh thường - thời gian của clip gốc")]
        [InspectorName("Show Arrow Time")]
        [Tooltip("Giây trong clip mà hình mũi tên xuất hiện trên tay.")]
        [SerializeField, Min(0f)] private float normalShowArrowTime;
        [FormerlySerializedAs("normalShotTime")]
        [InspectorName("Shoot Time")]
        [Tooltip("Giây trong clip mà tên trên tay biến mất và mũi tên thật được bắn ra.")]
        [SerializeField, Min(0f)] private float normalShootTime = .32f;
        [FormerlySerializedAs("normalAttackDuration")]
        [InspectorName("Duration")]
        [Tooltip("Tổng thời lượng một vòng clip đánh thường, chưa nhân tốc độ đánh.")]
        [SerializeField, Min(.01f)] private float normalDuration = .55f;

        public ActionState State { get; private set; } = ActionState.Ready;
        public bool Ready => State == ActionState.Ready;
        public SkillData CurrentSkill => specialSkill != null ? specialSkill : (skillSlot >= 0 ? GetSkill(skillSlot) : null);
        public bool IsChanneling => State == ActionState.UsingSkill && CurrentSkill != null && CurrentSkill.ChannelUntilCancelled;
        public bool ShieldOn => false;
        public float DashCooldownRemaining => 0f;
        public float ShieldCooldownRemaining => 0f;
        public float DashCooldownDuration => 0f;
        public float ShieldCooldownDuration => 0f;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            move = GetComponent<PlayerMove>();
            stats = GetComponent<PlayerStats>();
            anim = GetComponent<PlayerAnim>();
            if (skills == null || skills.Length != 5) Array.Resize(ref skills, 5);
        }

        private void Update()
        {
            // Không trừ hồi chiêu trong thời gian đếm ngược hoặc sau khi trận đã kết thúc.
            if (!player.Alive) return;

            for (int i = 0; i < skillLeft.Length; i++) skillLeft[i] = Mathf.Max(0f, skillLeft[i] - Time.deltaTime);

            if (stats.Stunned && State != ActionState.Stunned && State != ActionState.Dead)
            {
                EndCurrentSkill();
                State = ActionState.Stunned;
                skillSlot = -1;
                time = 0f;
                move.Lock(true);
                move.Stop();
                anim.Trigger("Stun");
            }
            if (!stats.Stunned && State == ActionState.Stunned)
            {
                State = ActionState.Ready;
                move.Lock(false);
                anim.EndSkillAnimation();
            }
            if (Ready || State == ActionState.Stunned || State == ActionState.Dead) return;

            time += Time.deltaTime;

            if (State == ActionState.AutoAttacking)
            {
                float clipSpeed = Mathf.Max(.05f, stats.ClipSpeed);
                float showAt = normalShowArrowTime / clipSpeed;
                float shotAt = normalShootTime / clipSpeed;
                float cycleDuration = Mathf.Max(shotAt + .01f, normalDuration / clipSpeed);
                if (!normalArrowShown && time >= showAt)
                {
                    normalArrowShown = true;
                    player.ShowHeldArrows(1, 0f);
                }
                if (!normalArrowFired && time >= shotAt)
                {
                    normalArrowFired = true;
                    player.HideHeldArrows();
                    player.Shoot();
                }
                if (time >= cycleDuration)
                {
                    if (player.Gear != null && player.Gear.BowState.specialSkillReady)
                    {
                        if (player.Gear.EquippedBow != null && player.Gear.EquippedBow.Passive is IceShotPassive icePassive)
                        {
                            player.Gear.BowState.specialSkillReady = false;
                            if (TriggerSpecialSkill(icePassive.IceSkill, icePassive.AnimationTrigger))
                                return;
                        }
                    }

                    time -= cycleDuration;
                    normalArrowShown = false;
                    normalArrowFired = false;
                }
                return;
            }
            else if (State == ActionState.UsingSkill)
            {
                SkillData skill = GetSkill(skillSlot);
                if (skill == null)
                {
                    if (!skillEffectDone && time >= 0.76f)
                    {
                        skillEffectDone = true;
                        player.ShootArrow(ProjectilePath.Straight, 1f, 1.2f, ArrowEffect.Freeze, 0, 2f);
                    }
                    if (time >= 1f) EndSkillFromClip();
                    return;
                }
                if ((skill.CancelOnMove || skill.ChannelUntilCancelled) && move.WantsMove)
                {
                    EndSkillFromClip();
                    return;
                }

                int totalVolleys = skill.VolleyCount;
                if (totalVolleys <= 1)
                {
                    if (!skillVisualShown && skill.ShowVisualTime >= 0f && time >= skill.ShowVisualTime)
                    {
                        skillVisualShown = true;
                        skill.ShowVisual(CurrentSkillContext());
                    }
                    if (skill.UseOnce && !skillEffectDone && time >= skill.EffectTime)
                        UseSkillFromClip();
                    skill.Tick(CurrentSkillContext(), Time.deltaTime);
                    if (!skill.ChannelUntilCancelled && time >= skill.Duration) EndSkillFromClip();
                }
                else
                {
                    // Multi-volley: Bắn nhiều đợt liên tiếp sau 1 lần bấm.
                    // Tự động lặp lại đúng chu kỳ kéo cung (ShowVisual -> Use) cho từng phát bắn!
                    float cycleDuration = Mathf.Max(skill.EffectTime + 0.05f, skill.VolleyInterval > 0f ? skill.VolleyInterval : skill.Duration);
                    int volleyIndex = Mathf.Clamp(Mathf.FloorToInt(time / cycleDuration), 0, totalVolleys - 1);
                    float cycleTime = time - (volleyIndex * cycleDuration);

                    // Khi chuyển sang đợt mới: kích hoạt lại animation kéo cung và chuẩn bị cho phát tiếp theo
                    if (volleyIndex > currentVolleyIndex)
                    {
                        currentVolleyIndex = volleyIndex;
                        skillVisualShown = false;
                        skillEffectDone = false;
                        if (!string.IsNullOrWhiteSpace(skill.AnimationTrigger))
                        {
                            anim.Trigger(skill.AnimationTrigger);
                        }
                    }

                    // 1. Đến thời điểm showTime trong chu kỳ: hiện mũi tên trên tay
                    if (!skillVisualShown && skill.ShowVisualTime >= 0f && cycleTime >= skill.ShowVisualTime)
                    {
                        skillVisualShown = true;
                        skill.ShowVisual(CurrentSkillContext());
                    }

                    // 2. Đến thời điểm effectTime trong chu kỳ: bắn mũi tên đi
                    if (!skillEffectDone && cycleTime >= skill.EffectTime)
                    {
                        skillEffectDone = true;
                        SkillData currentSkill = GetSkill(skillSlot);
                        currentSkill?.Use(CurrentSkillContext());
                    }

                    skill.Tick(CurrentSkillContext(), Time.deltaTime);

                    // 3. Hoàn tất toàn bộ M đợt bắn: kết thúc skill và mở khóa di chuyển
                    if (time >= cycleDuration * totalVolleys)
                    {
                        EndSkillFromClip();
                    }
                }
            }
        }

        public bool Attack()
        {
            if (IsChanneling) EndSkillFromClip();
            if (!Ready || !player.CanAttack) return false;

            if (player.Gear != null && player.Gear.BowState.specialSkillReady)
            {
                if (player.Gear.EquippedBow != null && player.Gear.EquippedBow.Passive is IceShotPassive icePassive)
                {
                    player.Gear.BowState.specialSkillReady = false;
                    return TriggerSpecialSkill(icePassive.IceSkill, icePassive.AnimationTrigger);
                }
            }

            State = ActionState.AutoAttacking;
            time = 0f;
            normalArrowShown = false;
            normalArrowFired = false;
            if (normalShowArrowTime <= 0f)
            {
                normalArrowShown = true;
                player.ShowHeldArrows(1, 0f);
            }
            anim.Trigger("AutoAttack");
            return true;
        }

        public bool Dash(float direction) => false;
        public bool Shield() => false;

        public bool Skill(int slot, float direction)
        {
            SkillData skill = GetSkill(slot);
            bool isChanneling = IsChanneling;
            bool canInterrupt = Ready || State == ActionState.AutoAttacking || isChanneling;
            if (!canInterrupt || !player.CanSkill || skill == null || skill.Passive || skillLeft[slot] > 0f)
                return false;

            // Hủy trạng thái channeling (như Rest) hoặc hủy đánh thường khi dùng skill mới
            if (isChanneling)
            {
                EndSkillFromClip();
            }
            else if (State == ActionState.AutoAttacking)
            {
                CancelAttack();
            }
            State = ActionState.UsingSkill;
            time = 0f;
            skillSlot = slot;
            skillDirection = Mathf.Sign(direction == 0f ? player.Face : direction);
            skillEffectDone = false;
            skillVisualShown = false;
            currentVolleyIndex = 0;
            currentVolleyIndex = 0;
            skillLeft[slot] = skill.Cooldown;

            // Mọi skill đều khóa điều khiển di chuyển và khóa mặt về phía Enemy.
            // Skill đặc biệt vẫn có thể tự đẩy nhân vật bằng StartSkillMove().
            move.Lock(true);
            move.Stop();
            anim.Face(player.Face);
            SkillCtx context = CurrentSkillContext();
            skill.Begin(context);
            if (skill is MoveSkill ms && ms.Movement == MovementShotType.Jump ||
                (skill != null && skill.name.IndexOf("jump", System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                (skill != null && skill.DisplayName.IndexOf("jump", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                anim?.PlayJumpSmoke();
            }
            if (skill.ShowVisualTime == 0f)
            {
                skillVisualShown = true;
                skill.ShowVisual(context);
            }

            if (string.IsNullOrWhiteSpace(skill.AnimationTrigger))
            {
                UseSkillFromClip();
                EndSkillFromClip();
            }
            else
            {
                anim.Trigger(skill.AnimationTrigger);
            }
            return true;
        }

        public bool TriggerSpecialSkill(SkillData skill, string fallbackTrigger = "Low", float direction = 0f)
        {
            if (!player.Alive) return false;

            if (State == ActionState.AutoAttacking) CancelAttack();
            State = ActionState.UsingSkill;
            time = 0f;
            specialSkill = skill;
            skillSlot = -1;
            skillDirection = Mathf.Sign(direction == 0f ? player.Face : direction);
            skillEffectDone = false;
            skillVisualShown = false;
            currentVolleyIndex = 0;

            move.Lock(true);
            move.Stop();
            anim.Face(player.Face);

            if (skill != null)
            {
                SkillCtx context = CurrentSkillContext();
                skill.Begin(context);
                if (skill.ShowVisualTime == 0f)
                {
                    skillVisualShown = true;
                    skill.ShowVisual(context);
                }

                if (string.IsNullOrWhiteSpace(skill.AnimationTrigger))
                {
                    UseSkillFromClip();
                    EndSkillFromClip();
                }
                else
                {
                    anim.Trigger(skill.AnimationTrigger);
                }
            }
            else
            {
                anim.Trigger(fallbackTrigger);
            }
            return true;
        }

        public void UseSkillFromClip()
        {
            if (State != ActionState.UsingSkill) return;
            if (skillEffectDone) return;
            skillEffectDone = true;
            SkillData skill = GetSkill(skillSlot);
            skill?.Use(new SkillCtx(player, player.Enemy, skillSlot, skillDirection));
        }

        public void EndSkillFromClip()
        {
            if (State != ActionState.UsingSkill) return;
            EndCurrentSkill();
            move.Stop();
            move.Lock(false);
            skillSlot = -1;
            specialSkill = null;
            State = ActionState.Ready;
            anim.EndSkillAnimation();
        }

        public void StartPassives()
        {
            for (int i = 0; i < skills.Length; i++)
            {
                SkillData skill = GetSkill(i);
                if (skill != null && skill.Passive)
                    skill.Use(new SkillCtx(player, player.Enemy, i, player.Face));
            }
        }

        public void ResetSkillCooldown(int slot)
        {
            if (slot >= 0 && slot < skillLeft.Length) skillLeft[slot] = 0f;
        }

        public void CancelAttack()
        {
            if (State != ActionState.AutoAttacking) return;
            player.HideHeldArrows();
            State = ActionState.Ready;
        }

        public void Stun(float seconds) => stats.Stun(seconds);
        public void Silence(float seconds) => stats.Silence(seconds);
        public void Die() { player.HideHeldArrows(); EndCurrentSkill(); skillSlot = -1; specialSkill = null; State = ActionState.Dead; move.Lock(true); move.Stop(); anim.Trigger("Die"); }
        public void Stop() { if (State != ActionState.Dead) { player.HideHeldArrows(); EndCurrentSkill(); State = ActionState.Ready; skillSlot = -1; specialSkill = null; move.Lock(false); move.Stop(); } }
        public void ResetAction()
        {
            player.HideHeldArrows();
            State = ActionState.Ready;
            time = 0f;
            skillSlot = -1;
            specialSkill = null;
            skillDirection = 1f;
            normalArrowShown = false;
            normalArrowFired = false;
            skillEffectDone = false;
            skillVisualShown = false;
            currentVolleyIndex = 0;
            for (int i = 0; i < skills.Length; i++)
            {
                SkillData skill = GetSkill(i);
                skillLeft[i] = skill != null && !skill.Passive ? skill.Cooldown : 0f;
            }
            move.Lock(false);
        }
        public SkillData GetSkill(int slot)
        {
            if (specialSkill != null) return specialSkill;
            if (slot < 0 || slot >= skills.Length) return null;
            return loadout != null ? loadout.GetSkill(slot) : skills[slot];
        }
        public SkillData GetEquippedSkill(int slot)
        {
            if (slot < 0 || slot >= skills.Length) return null;
            return loadout != null ? loadout.GetSkill(slot) : skills[slot];
        }

        public void SetSkill(int slot, SkillData skill)
        {
            if (skills == null || skills.Length != 5) Array.Resize(ref skills, 5);
            if (slot < 0 || slot >= skills.Length) return;
            loadout = null;
            skills[slot] = skill;
            if (slot < skillLeft.Length) skillLeft[slot] = 0f;
        }

        public void SetSkills(SkillData[] newSkills)
        {
            loadout = null;
            if (skills == null || skills.Length != 5) Array.Resize(ref skills, 5);
            for (int i = 0; i < 5; i++)
            {
                skills[i] = (newSkills != null && i < newSkills.Length) ? newSkills[i] : null;
                if (i < skillLeft.Length) skillLeft[i] = 0f;
            }
        }

        public void SetLoadout(SkillLoadout newLoadout)
        {
            loadout = newLoadout;
        }
        public float GetSkillCooldownRemaining(int slot) => slot >= 0 && slot < skillLeft.Length ? skillLeft[slot] : 0f;

        private SkillCtx CurrentSkillContext() => new SkillCtx(player, player.Enemy, skillSlot, skillDirection);

        private void EndCurrentSkill()
        {
            if (specialSkill != null)
            {
                specialSkill.End(CurrentSkillContext());
                return;
            }
            if (skillSlot < 0) return;
            GetSkill(skillSlot)?.End(CurrentSkillContext());
        }
    }
}
