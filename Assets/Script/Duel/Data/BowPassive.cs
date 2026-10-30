using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class BowRuntimeState
    {
        public int shotCount;
        public float timer;
        public bool specialSkillReady;
        public readonly List<Arrow> activeArrows = new List<Arrow>();
        public readonly List<GameObject> activeSummons = new List<GameObject>();

        public void Reset()
        {
            shotCount = 0;
            timer = 0f;
            specialSkillReady = false;
            activeArrows.Clear();
            activeSummons.Clear();
        }

        public bool HasActiveSummons()
        {
            for (int i = activeSummons.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeSummons[i];
                if (obj == null || !obj.activeInHierarchy)
                {
                    activeSummons.RemoveAt(i);
                    continue;
                }

                SkillObject so = obj.GetComponent<SkillObject>();
                if (so != null && so.IsDead)
                {
                    activeSummons.RemoveAt(i);
                    continue;
                }
            }
            return activeSummons.Count > 0;
        }

        public void RegisterSummon(GameObject obj)
        {
            if (obj != null && !activeSummons.Contains(obj))
            {
                activeSummons.Add(obj);
            }
        }
    }

    public abstract class BowPassive : ScriptableObject
    {
        [Header("Passive Information")]
        [SerializeField] private string passiveName;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        public string PassiveName => passiveName;
        public string Description => description;

        /// <summary>
        /// Cho biết nội tại này có cơ chế tích lũy phát bắn và giải phóng khi đứng im bắn thường hay không.
        /// </summary>
        public virtual bool HasChargeRelease => false;

        /// <summary>
        /// Trigger animation khi giải phóng nội tại (Cung băng dùng "Mid", các cung khác dùng "High").
        /// </summary>
        public virtual string SpecialAnimationTrigger => "High";

        /// <summary>
        /// Kỹ năng đặc biệt đi kèm nếu có (ví dụ FreezeArrow của cung băng).
        /// </summary>
        public virtual SkillData SpecialSkill => null;

        /// <summary>
        /// Thực thi giải phóng nội tại (triệu hồi prefab, bắn mũi tên đặc biệt...) tại thời điểm phát bắn.
        /// </summary>
        public virtual void ExecuteSpecialRelease(PlayerController player, BowRuntimeState state) { }

        public virtual void OnEquip(PlayerController player, BowRuntimeState state) { }
        public virtual void OnUnequip(PlayerController player, BowRuntimeState state) { }
        public virtual void OnUpdate(PlayerController player, BowRuntimeState state, float deltaTime) { }
        public virtual void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isNormalShot) { }
        public virtual void OnArrowHit(PlayerController player, BowRuntimeState state, Arrow arrow, PlayerController target, int damageDealt) { }
        public virtual void OnSkillUsed(PlayerController player, BowRuntimeState state, SkillData skill, int slot, float direction) { }
        public virtual void OnSkillEnded(PlayerController player, BowRuntimeState state, SkillData skill) { }
        public virtual float GetProgress(BowRuntimeState state) => 1f;
    }
}
