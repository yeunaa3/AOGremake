using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AOG.Duel
{
    public enum DuelActionSlot
    {
        Dash,
        Shield,
        Skill1,
        Skill2,
        Skill3,
        Skill4
    }

    public sealed class CooldownUI : MonoBehaviour
    {
        [SerializeField] private PlayerController character;
        [SerializeField] private DuelActionSlot slot;
        [SerializeField] private Image cooldownFill;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private Button button;
        [Header("Skill visual")]
        [SerializeField] private Image skillIcon;
        [SerializeField] private Image rarityFrame;
        [SerializeField] private SkillLoadout loadout;
        [SerializeField] private SkillFrames frames;
        private SkillData shownSkill;

        private void Start() => RefreshSkillVisual();

        private void Update()
        {
            RefreshSkillVisual();
            if (character == null || character.Action == null) return;

            GetCooldown(out float remaining, out float duration, out bool configured);
            float normalized = duration > 0f ? remaining / duration : 0f;
            bool coolingDown = remaining > 0.05f;

            if (cooldownFill != null)
            {
                cooldownFill.enabled = coolingDown;
                cooldownFill.fillAmount = coolingDown ? Mathf.Clamp01(normalized) : 0f;

                Color color = cooldownFill.color;
                color.a = 0.5f;
                cooldownFill.color = color;
            }
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(coolingDown);
                cooldownText.text = coolingDown ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
            }

            if (button != null)
            {
                button.interactable = configured
                    && character.CanSkill
                    && (character.Action.Ready || character.Action.State == ActionState.AutoAttacking)
                    && remaining <= 0f;
            }
        }

        private void RefreshSkillVisual()
        {
            if (slot < DuelActionSlot.Skill1) return;
            int skillSlot = (int)slot - (int)DuelActionSlot.Skill1;
            SkillData skill = character != null && character.Action != null
                ? character.Action.GetSkill(skillSlot)
                : loadout != null ? loadout.GetSkill(skillSlot) : null;
            if (shownSkill == skill) return;
            shownSkill = skill;

            if (skillIcon != null)
            {
                skillIcon.sprite = skill != null ? skill.Icon : null;
                skillIcon.enabled = skill != null && skill.Icon != null;
            }
            if (rarityFrame != null)
            {
                rarityFrame.sprite = skill != null && frames != null ? frames.Get(skill.Rarity) : null;
                rarityFrame.enabled = rarityFrame.sprite != null;
            }
        }

        public void SelectSkill(SkillData skill)
        {
            if (slot < DuelActionSlot.Skill1 || loadout == null) return;
            int skillSlot = (int)slot - (int)DuelActionSlot.Skill1;
            loadout.SetSkill(skillSlot, skill);
            shownSkill = null;
            RefreshSkillVisual();
        }

        private void GetCooldown(out float remaining, out float duration, out bool configured)
        {
            PlayerAction actions = character.Action;
            configured = true;

            switch (slot)
            {
                case DuelActionSlot.Dash:
                    remaining = actions.DashCooldownRemaining;
                    duration = actions.DashCooldownDuration;
                    return;
                case DuelActionSlot.Shield:
                    remaining = actions.ShieldCooldownRemaining;
                    duration = actions.ShieldCooldownDuration;
                    return;
            }

            int skillSlot = (int)slot - (int)DuelActionSlot.Skill1;
            SkillData skill = actions.GetSkill(skillSlot);
            configured = skill != null && skill.Kind != SkillKind.Passive;
            remaining = actions.GetSkillCooldownRemaining(skillSlot);
            duration = skill != null ? skill.Cooldown : 0f;
        }
    }

}
