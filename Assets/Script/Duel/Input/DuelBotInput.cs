using UnityEngine;

namespace AOG.Duel
{
    public sealed class DuelBotInput : DuelInputSource
    {
        [SerializeField, Min(0.1f)] private float decisionInterval = 0.5f;
        [SerializeField, Range(0f, 1f)] private float movementChance = 0.65f;
        [SerializeField, Range(0f, 1f)] private float actionChance = 0.35f;

        private float nextDecisionTime;
        private float currentMove;
        private DuelPlayerCommand pendingCommand;

        public override DuelPlayerCommand ReadCommand()
        {
            if (Time.time >= nextDecisionTime)
            {
                nextDecisionTime = Time.time + decisionInterval;
                MakeDecision();
            }

            DuelPlayerCommand result = pendingCommand;
            result.Move = currentMove;
            pendingCommand = default;
            return result;
        }

        private void MakeDecision()
        {
            currentMove = Random.value <= movementChance
                ? (Random.value < 0.5f ? -1f : 1f)
                : 0f;

            if (Random.value > actionChance)
            {
                return;
            }

            int action = Random.Range(0, 6);
            pendingCommand.DashPressed = action == 0;
            pendingCommand.ShieldPressed = action == 1;
            pendingCommand.Skill1Pressed = action == 2;
            pendingCommand.Skill2Pressed = action == 3;
            pendingCommand.Skill3Pressed = action == 4;
            pendingCommand.Skill4Pressed = action == 5;
        }
    }
}
