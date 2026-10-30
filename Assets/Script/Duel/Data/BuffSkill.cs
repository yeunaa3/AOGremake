using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "Skill/Buff", fileName = "NewBuffSkill")]
    public sealed class BuffSkill : SkillData
    {
        [SerializeField] private BuffData buff = new BuffData();

        public override SkillType Type => SkillType.PlayerOnly;
        public BuffData Buff => buff;

        public override void Begin(SkillCtx context)
        {
            if (context.Caster == null) return;
            int level = GetCasterLevel(context);
            int healPerSec = (level <= 1) ? buff.healPerSecond : GetHealAmount(buff.healPerSecond, level);

            if (buff.isRestSkill || healPerSec > 0)
            {
                context.Caster.StartRestHeal(healPerSec, buff.healInterval, buff.vfxPrefab);
            }
        }

        public override void Use(SkillCtx context)
        {
            if (context.Caster == null) return;
            int level = GetCasterLevel(context);
            BuffData activeBuff = (level <= 1) ? buff : CloneLeveledBuff(buff, level);

            if (activeBuff != null && activeBuff.heal > 0)
            {
                SkillAudioManager.PlayHeal();
            }
            context.Caster.Stats.ApplyBuff(activeBuff);
            if (activeBuff.vfxPrefab != null && !activeBuff.isRestSkill && activeBuff.healPerSecond == 0)
            {
                Instantiate(activeBuff.vfxPrefab, context.Caster.transform.position, Quaternion.identity, context.Caster.transform);
            }
        }

        private BuffData CloneLeveledBuff(BuffData src, int level)
        {
            if (src == null) return null;
            return new BuffData
            {
                heal = GetHealAmount(src.heal, level),
                duration = src.duration,
                moveMultiplier = src.moveMultiplier,
                dodgeChance = src.dodgeChance,
                projectileSpeedMultiplier = src.projectileSpeedMultiplier,
                invulnerable = src.invulnerable,
                cleanse = src.cleanse,
                statusImmune = src.statusImmune,
                isRestSkill = src.isRestSkill,
                healPerSecond = GetHealAmount(src.healPerSecond, level),
                healInterval = src.healInterval,
                vfxPrefab = src.vfxPrefab
            };
        }

        public override void End(SkillCtx context)
        {
            if (context.Caster == null) return;
            if (buff.isRestSkill || buff.healPerSecond > 0)
            {
                context.Caster.StopRestHeal();
            }
        }
    }
}
