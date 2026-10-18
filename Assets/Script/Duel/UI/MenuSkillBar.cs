using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    // Hiển thị thanh 5 skill mà người chơi đã chọn tại màn hình Menu/Lobby.
    public sealed class MenuSkillBar : MonoBehaviour
    {
        [Header("Loadout Dữ Liệu")]
        [Tooltip("SkillLoadout chứa 5 kỹ năng được trang bị (ví dụ Skills1.asset).")]
        [SerializeField] private SkillLoadout loadout;

        [Header("5 Ô Hiển Thị Kỹ Năng")]
        [Tooltip("5 SpriteRenderer hiển thị icon của 5 skill trong ô đen trung tâm.")]
        [SerializeField] private SpriteRenderer[] slotRenderers = new SpriteRenderer[5];

        [Header("Kỹ Năng Mặc Định (Nếu Không Gán Loadout)")]
        [SerializeField] private Sprite[] defaultSkillIcons = new Sprite[5];

        public SkillLoadout Loadout => loadout;

        private void OnEnable()
        {
            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.SkillChanged += HandleSkillChanged;
                PlayerLoadoutManager.Instance.LoadoutChanged += HandleLoadoutChanged;
            }
            RefreshSlots();
        }

        private void OnDisable()
        {
            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.SkillChanged -= HandleSkillChanged;
                PlayerLoadoutManager.Instance.LoadoutChanged -= HandleLoadoutChanged;
            }
        }

        private void Start()
        {
            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.SkillChanged -= HandleSkillChanged;
                PlayerLoadoutManager.Instance.SkillChanged += HandleSkillChanged;
                PlayerLoadoutManager.Instance.LoadoutChanged -= HandleLoadoutChanged;
                PlayerLoadoutManager.Instance.LoadoutChanged += HandleLoadoutChanged;
            }
            RefreshSlots();
        }

        private void HandleSkillChanged(int slot, SkillData skill)
        {
            RefreshSlots();
        }

        private void HandleLoadoutChanged()
        {
            RefreshSlots();
        }

        public void SetLoadout(SkillLoadout newLoadout)
        {
            loadout = newLoadout;
            RefreshSlots();
        }

        public void SetSkillIcon(int slotIndex, Sprite icon)
        {
            if (slotIndex >= 0 && slotIndex < slotRenderers.Length && slotRenderers[slotIndex] != null)
            {
                slotRenderers[slotIndex].sprite = icon;
                slotRenderers[slotIndex].enabled = icon != null;
            }
        }

        public void RefreshSlots()
        {
            if (slotRenderers == null || slotRenderers.Length == 0) return;

            for (int i = 0; i < slotRenderers.Length; i++)
            {
                SpriteRenderer sr = slotRenderers[i];
                if (sr == null) continue;

                Sprite iconToDisplay = null;
                SkillData skill = null;

                if (PlayerLoadoutManager.HasInstance)
                {
                    skill = PlayerLoadoutManager.Instance.GetEquippedSkill(i);
                }

                if (skill == null && loadout != null)
                {
                    skill = loadout.GetSkill(i);
                }

                if (skill != null && skill.Icon != null)
                {
                    iconToDisplay = skill.Icon;
                }
                else if (defaultSkillIcons != null && i < defaultSkillIcons.Length && defaultSkillIcons[i] != null)
                {
                    iconToDisplay = defaultSkillIcons[i];
                }
                else if (sr.sprite != null)
                {
                    iconToDisplay = sr.sprite;
                }

                if (iconToDisplay != null)
                {
                    sr.sprite = iconToDisplay;
                    sr.enabled = true;
                }
            }
        }
    }
}
