using System;
using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skill Loadout", fileName = "PlayerSkillLoadout")]
    public sealed class SkillLoadout : ScriptableObject
    {
        [SerializeField] private SkillData[] skills = new SkillData[5];

        public SkillData GetSkill(int slot)
        {
            return slot >= 0 && slot < skills.Length ? skills[slot] : null;
        }

        public void SetSkill(int slot, SkillData skill)
        {
            if (slot >= 0 && slot < skills.Length) skills[slot] = skill;
        }

        private void OnValidate()
        {
            if (skills == null || skills.Length != 5) Array.Resize(ref skills, 5);
        }
    }
}
