using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "Skill/Time Stop", fileName = "NewTimeStopSkill")]
    public sealed class TimeStopSkill : SkillData
    {
        [Tooltip("Thời gian làm chậm các mũi tên đang bay (giây, mặc định 5s).")]
        [SerializeField, Min(0.5f)] private float timeSlowDuration = 5f;
        [Tooltip("Hệ số nhân tốc độ mũi tên trong thời gian hiệu lực (0.1 = giảm 10 lần).")]
        [SerializeField, Range(0.01f, 1f)] private float slowMultiplier = 0.1f;
        [Tooltip("Hiệu ứng VFX khi kích hoạt (tùy chọn).")]
        [SerializeField] private GameObject vfxPrefab;

        public override SkillType Type => SkillType.PlayerOnly;
        public float TimeSlowDuration => timeSlowDuration;
        public float SlowMultiplier => slowMultiplier;

        public override void Use(SkillCtx context)
        {
            if (context.Caster == null || !context.Caster.Alive) return;

            if (vfxPrefab != null)
            {
                Instantiate(vfxPrefab, context.Caster.transform.position, Quaternion.identity, context.Caster.transform);
            }

            context.Caster.StartCoroutine(RoutineTimeStop(context.Caster, timeSlowDuration, slowMultiplier));
        }

        private static IEnumerator RoutineTimeStop(PlayerController caster, float dur, float speedMult)
        {
            float elapsed = 0f;
            while (elapsed < dur)
            {
                if (caster == null || !caster.Alive) yield break;

                if (caster.Pool != null)
                {
                    foreach (Arrow arrow in caster.Pool.ActiveArrows)
                    {
                        if (arrow != null && arrow.Owner == caster && arrow.IsFlying && !arrow.IsStuckInGround)
                        {
                            arrow.TimeScale = speedMult;
                        }
                    }
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            if (caster != null && caster.Pool != null)
            {
                foreach (Arrow arrow in caster.Pool.ActiveArrows)
                {
                    if (arrow != null && arrow.Owner == caster)
                    {
                        arrow.TimeScale = 1.0f;
                    }
                }
            }
        }
    }
}
