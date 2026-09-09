using UnityEngine;
using UnityEngine.EventSystems;

namespace AOG.Duel
{
    public enum MobileAction
    {
        MoveLeft,
        MoveRight,
        Dash,
        Shield,
        Skill1,
        Skill2,
        Skill3,
        Skill4
    }

    public sealed class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private TouchInput input;
        [SerializeField] private MobileAction action;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (input == null) return;

            switch (action)
            {
                case MobileAction.MoveLeft: input.SetLeftHeld(true); break;
                case MobileAction.MoveRight: input.SetRightHeld(true); break;
                case MobileAction.Dash: input.PressDash(); break;
                case MobileAction.Shield: input.PressShield(); break;
                case MobileAction.Skill1: input.PressSkill(0); break;
                case MobileAction.Skill2: input.PressSkill(1); break;
                case MobileAction.Skill3: input.PressSkill(2); break;
                case MobileAction.Skill4: input.PressSkill(3); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (input == null) return;
            if (action == MobileAction.MoveLeft) input.SetLeftHeld(false);
            if (action == MobileAction.MoveRight) input.SetRightHeld(false);
        }
    }
}
