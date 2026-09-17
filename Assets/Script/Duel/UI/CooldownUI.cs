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

        private void Update()
        {
            if (character == null || character.Action == null) return;

            GetCooldown(out float remaining, out float duration, out bool configured);
            float normalized = duration > 0f ? remaining / duration : 0f;

            if (cooldownFill != null) cooldownFill.fillAmount = normalized;
            if (cooldownText != null)
            {
                cooldownText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
            }

            if (button != null)
            {
                button.interactable = configured
                    && character.CanSkill
                    && character.Action.Ready
                    && remaining <= 0f;
            }
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
            configured = skill != null;
            remaining = actions.GetSkillCooldownRemaining(skillSlot);
            duration = skill != null ? skill.Cooldown : 0f;
        }
    }
}
