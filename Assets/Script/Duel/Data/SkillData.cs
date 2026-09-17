using UnityEngine;

namespace AOG.Duel
{
    public abstract class SkillData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillId = "skill_id";
        [SerializeField] private string displayName = "Skill";
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;

        [Header("Timing")]
        [Tooltip("Thời gian khóa các hành động khác.")]
        [SerializeField, Min(0.01f)] private float actionDuration = 0.5f;
        [Tooltip("Mốc hiệu ứng xảy ra, tính từ lúc bắt đầu chiêu.")]
        [SerializeField, Min(0f)] private float activeTime = 0.25f;
        [Tooltip("Bắt đầu tính sau khi hành động kết thúc.")]
        [SerializeField, Min(0f)] private float cooldown = 5f;

        [Header("Animation")]
        [SerializeField] private string animationTrigger = "Skill1";
        [SerializeField] private bool allowMovement = true;

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public float ActionDuration => actionDuration;
        public float ActiveTime => Mathf.Min(activeTime, actionDuration);
        public float Cooldown => cooldown;
        public string AnimationTrigger => animationTrigger;
        public bool AllowMovement => allowMovement;

        public abstract void Execute(SkillCtx context);

        protected virtual void OnValidate()
        {
            activeTime = Mathf.Clamp(activeTime, 0f, actionDuration);
        }
    }

    public struct SkillCtx
    {
        public PlayerController Caster;
        public PlayerController Target;
        public int Slot;

        public SkillCtx(PlayerController caster, PlayerController target, int slot)
        {
            Caster = caster;
            Target = target;
            Slot = slot;
        }
    }
}
