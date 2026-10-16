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

        public void Reset()
        {
            shotCount = 0;
            timer = 0f;
            specialSkillReady = false;
            activeArrows.Clear();
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

        public virtual void OnEquip(PlayerController player, BowRuntimeState state) { }
        public virtual void OnUnequip(PlayerController player, BowRuntimeState state) { }
        public virtual void OnUpdate(PlayerController player, BowRuntimeState state, float deltaTime) { }
        public virtual void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isSecondaryShot) { }
        public virtual void OnArrowHit(PlayerController player, BowRuntimeState state, Arrow arrow, PlayerController target, int damageDealt) { }
        public virtual float GetProgress(BowRuntimeState state) => 1f;
    }
}
