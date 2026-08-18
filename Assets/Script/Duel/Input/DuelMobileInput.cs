using UnityEngine;

namespace AOG.Duel
{
    public sealed class DuelMobileInput : DuelInputSource
    {
        private bool leftHeld;
        private bool rightHeld;
        private bool dashPressed;
        private bool shieldPressed;
        private readonly bool[] skillPressed = new bool[4];

        public void SetLeftHeld(bool held) => leftHeld = held;
        public void SetRightHeld(bool held) => rightHeld = held;
        public void PressDash() => dashPressed = true;
        public void PressShield() => shieldPressed = true;

        public void PressSkill(int slot)
        {
            if (slot >= 0 && slot < skillPressed.Length)
            {
                skillPressed[slot] = true;
            }
        }

        public override DuelPlayerCommand ReadCommand()
        {
            var command = new DuelPlayerCommand
            {
                Move = (rightHeld ? 1f : 0f) - (leftHeld ? 1f : 0f),
                DashPressed = dashPressed,
                ShieldPressed = shieldPressed,
                Skill1Pressed = skillPressed[0],
                Skill2Pressed = skillPressed[1],
                Skill3Pressed = skillPressed[2],
                Skill4Pressed = skillPressed[3]
            };

            dashPressed = false;
            shieldPressed = false;
            for (int i = 0; i < skillPressed.Length; i++)
            {
                skillPressed[i] = false;
            }

            return command;
        }
    }
}
