using System;
using UnityEngine;

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
        private float channelTime;

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
            dashLeft = Mathf.Max(0f, dashLeft - Time.deltaTime);
            shieldLeft = Mathf.Max(0f, shieldLeft - Time.deltaTime);
            for (int i = 0; i < 4; i++) skillLeft[i] = Mathf.Max(0f, skillLeft[i] - Time.deltaTime);

            if (stats.Stunned && State != ActionState.Stunned && State != ActionState.Dead)
            {
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
                // Shot tự lặp; mỗi Animation Event sẽ bắn một mũi tên.
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
                if (skill.MoveMode == SkillMoveMode.CancelOnMove && move.WantsMove)
                {
                    EndSkillFromClip();
                    return;
                }
                if (skill is ChannelSkill channel)
                {
                    channelTime += Time.deltaTime;
                    if (channelTime >= channel.TickInterval)
                    {
                        channelTime -= channel.TickInterval;
                        skill.Use(new SkillCtx(player, player.Enemy, skillSlot, skillDirection));
                    }
                }
            }
        }

        public bool Attack()
        {
            if (!Ready || !player.CanAttack) return false;
            State = ActionState.AutoAttacking;
            time = 0f;
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
            if (!Ready || !player.CanSkill || skill == null || skill.Kind == SkillKind.Passive || skillLeft[slot] > 0f) return false;
            State = ActionState.UsingSkill;
            time = 0f;
            skillSlot = slot;
            skillDirection = Mathf.Sign(direction == 0f ? player.Face : direction);
            channelTime = 0f;
            skillLeft[slot] = skill.Cooldown;

            if (skill.MoveMode == SkillMoveMode.Free)
            {
                move.Lock(false);
            }
            else if (skill.MoveMode == SkillMoveMode.Directional)
            {
                move.Lock(true);
                move.Dash(skillDirection, skill.MoveSpeed);
            }
            else
            {
                move.Lock(true);
                move.Stop();
            }

            if (skill.Animation == SkillAnimation.None)
            {
                UseSkillFromClip();
                EndSkillFromClip();
            }
            else
            {
                anim.PlaySkill(skill.Animation);
            }
            return true;
        }

        public void UseSkillFromClip()
        {
            if (State != ActionState.UsingSkill) return;
            SkillData skill = GetSkill(skillSlot);
            skill?.Use(new SkillCtx(player, player.Enemy, skillSlot, skillDirection));
        }

        public void EndSkillFromClip()
        {
            if (State != ActionState.UsingSkill) return;
            move.Stop();
            move.Lock(false);
            skillSlot = -1;
            channelTime = 0f;
            State = ActionState.Ready;
        }

        public void StartPassives()
        {
            for (int i = 0; i < 4; i++)
            {
                SkillData skill = GetSkill(i);
                if (skill != null && skill.Kind == SkillKind.Passive)
                    skill.Use(new SkillCtx(player, player.Enemy, i, player.Face));
            }
        }

        public void ResetSkillCooldown(int slot)
        {
            if (slot >= 0 && slot < 4) skillLeft[slot] = 0f;
        }

        public void ReleaseArrowFromClip()
        {
            if (State == ActionState.AutoAttacking)
                player.Shoot();
        }

        public void CancelAttack()
        {
            if (State != ActionState.AutoAttacking) return;
            State = ActionState.Ready;
            player.ShowArrow(true);
        }

        public void Stun(float seconds) => stats.Stun(seconds);
        public void Silence(float seconds) => stats.Silence(seconds);
        public void Die() { State = ActionState.Dead; move.Lock(true); move.Stop(); anim.Trigger("Die"); }
        public void Stop() { if (State != ActionState.Dead) { State = ActionState.Ready; skillSlot = -1; move.Lock(false); move.Stop(); } }
        public void ResetAction() { State = ActionState.Ready; time = dashLeft = shieldLeft = channelTime = 0f; skillSlot = -1; skillDirection = 1f; Array.Clear(skillLeft, 0, 4); move.Lock(false); }
        public SkillData GetSkill(int slot)
        {
            if (slot < 0 || slot > 3) return null;
            return loadout != null ? loadout.GetSkill(slot) : skills[slot];
        }
        public float GetSkillCooldownRemaining(int slot) => slot >= 0 && slot < 4 ? skillLeft[slot] : 0f;
    }
}
