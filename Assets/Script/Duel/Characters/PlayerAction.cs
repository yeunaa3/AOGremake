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
        [Header("Dash")]
        [SerializeField] private float dashTime = .2f;
        [SerializeField] private float dashSpeed = 14f;
        [SerializeField] private float dashCooldown = 2f;
        [Header("Khiên")]
        [SerializeField] private float shieldTime = .7f;
        [SerializeField] private float shieldCooldown = 4f;
        [Header("4 Skill")]
        [SerializeField] private SkillLoadout loadout;
        [SerializeField] private SkillData[] skills = new SkillData[4];

        private PlayerController player;
        private PlayerMove move;
        private PlayerStats stats;
        private PlayerAnim anim;
        private float time;
        private float dashLeft;
        private float shieldLeft;
        private readonly float[] skillLeft = new float[4];
        private int skillSlot = -1;
        private float skillDirection = 1f;
        private bool normalArrowShown;
        private bool normalArrowFired;
        private bool skillEffectDone;
        private bool skillVisualShown;

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
        public bool ShieldOn => State == ActionState.Shielding;
        public float DashCooldownRemaining => dashLeft;
        public float ShieldCooldownRemaining => shieldLeft;
        public float DashCooldownDuration => dashCooldown;
        public float ShieldCooldownDuration => shieldCooldown;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            move = GetComponent<PlayerMove>();
            stats = GetComponent<PlayerStats>();
            anim = GetComponent<PlayerAnim>();
            if (skills == null || skills.Length != 4) Array.Resize(ref skills, 4);
        }

        private void Update()
        {
            // Không trừ hồi chiêu trong thời gian đếm ngược hoặc sau khi trận đã kết thúc.
            if (!player.Alive) return;

            dashLeft = Mathf.Max(0f, dashLeft - Time.deltaTime);
            shieldLeft = Mathf.Max(0f, shieldLeft - Time.deltaTime);
            for (int i = 0; i < 4; i++) skillLeft[i] = Mathf.Max(0f, skillLeft[i] - Time.deltaTime);

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
            if (!stats.Stunned && State == ActionState.Stunned) { State = ActionState.Ready; move.Lock(false); }
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
                    time -= cycleDuration;
                    normalArrowShown = false;
                    normalArrowFired = false;
                }
                return;
            }
            else if (State == ActionState.Dashing)
            {
                if (time >= dashTime) { move.Stop(); State = ActionState.Ready; }
            }
            else if (State == ActionState.Shielding)
            {
                if (time >= shieldTime) State = ActionState.Ready;
            }
            else if (State == ActionState.UsingSkill)
            {
                SkillData skill = GetSkill(skillSlot);
                if (skill == null) { EndSkillFromClip(); return; }
                if (skill.CancelOnMove && move.WantsMove)
                {
                    EndSkillFromClip();
                    return;
                }
                if (!skillVisualShown && skill.ShowVisualTime >= 0f && time >= skill.ShowVisualTime)
                {
                    skillVisualShown = true;
                    skill.ShowVisual(CurrentSkillContext());
                }
                if (skill.UseOnce && !skillEffectDone && time >= skill.EffectTime)
                    UseSkillFromClip();
                skill.Tick(CurrentSkillContext(), Time.deltaTime);
                if (time >= skill.Duration) EndSkillFromClip();
            }
        }

        public bool Attack()
        {
            if (!Ready || !player.CanAttack) return false;
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

        public bool Dash(float direction)
        {
            if (!Ready || !player.CanSkill || dashLeft > 0f) return false;
            State = ActionState.Dashing;
            time = 0f;
            dashLeft = dashTime + dashCooldown;
            move.Dash(direction, dashSpeed);
            anim.Trigger("Dash");
            return true;
        }

        public bool Shield()
        {
            if (!Ready || !player.CanSkill || shieldLeft > 0f) return false;
            State = ActionState.Shielding;
            time = 0f;
            shieldLeft = shieldTime + shieldCooldown;
            anim.Trigger("Shield");
            return true;
        }

        public bool Skill(int slot, float direction)
        {
            SkillData skill = GetSkill(slot);
            if (State == ActionState.AutoAttacking) CancelAttack();
            if (!Ready || !player.CanSkill || skill == null || skill.Passive || skillLeft[slot] > 0f) return false;
            State = ActionState.UsingSkill;
            time = 0f;
            skillSlot = slot;
            skillDirection = Mathf.Sign(direction == 0f ? player.Face : direction);
            skillEffectDone = false;
            skillVisualShown = false;
            skillLeft[slot] = skill.Cooldown;

            move.Lock(skill.LockMovement);
            if (skill.LockMovement) move.Stop();
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
            State = ActionState.Ready;
        }

        public void StartPassives()
        {
            for (int i = 0; i < 4; i++)
            {
                SkillData skill = GetSkill(i);
                if (skill != null && skill.Passive)
                    skill.Use(new SkillCtx(player, player.Enemy, i, player.Face));
            }
        }

        public void ResetSkillCooldown(int slot)
        {
            if (slot >= 0 && slot < 4) skillLeft[slot] = 0f;
        }

        public void CancelAttack()
        {
            if (State != ActionState.AutoAttacking) return;
            player.HideHeldArrows();
            State = ActionState.Ready;
        }

        public void Stun(float seconds) => stats.Stun(seconds);
        public void Silence(float seconds) => stats.Silence(seconds);
        public void Die() { player.HideHeldArrows(); EndCurrentSkill(); skillSlot = -1; State = ActionState.Dead; move.Lock(true); move.Stop(); anim.Trigger("Die"); }
        public void Stop() { if (State != ActionState.Dead) { player.HideHeldArrows(); EndCurrentSkill(); State = ActionState.Ready; skillSlot = -1; move.Lock(false); move.Stop(); } }
        public void ResetAction()
        {
            player.HideHeldArrows();
            State = ActionState.Ready;
            time = dashLeft = shieldLeft = 0f;
            skillSlot = -1;
            skillDirection = 1f;
            normalArrowShown = false;
            normalArrowFired = false;
            skillEffectDone = false;
            skillVisualShown = false;
            for (int i = 0; i < 4; i++)
            {
                SkillData skill = GetSkill(i);
                skillLeft[i] = skill != null && !skill.Passive ? skill.Cooldown : 0f;
            }
            move.Lock(false);
        }
        public SkillData GetSkill(int slot)
        {
            if (slot < 0 || slot > 3) return null;
            return loadout != null ? loadout.GetSkill(slot) : skills[slot];
        }
        public float GetSkillCooldownRemaining(int slot) => slot >= 0 && slot < 4 ? skillLeft[slot] : 0f;

        private SkillCtx CurrentSkillContext() => new SkillCtx(player, player.Enemy, skillSlot, skillDirection);

        private void EndCurrentSkill()
        {
            if (skillSlot < 0) return;
            GetSkill(skillSlot)?.End(CurrentSkillContext());
        }
    }
}
