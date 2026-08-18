using UnityEngine;
using UnityEngine.EventSystems;

namespace AOG.Duel
{
    public enum DuelMobileButtonAction
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

    public sealed class DuelMobileButtonRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private DuelMobileInput input;
        [SerializeField] private DuelMobileButtonAction action;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (input == null) return;

            switch (action)
            {
                case DuelMobileButtonAction.MoveLeft: input.SetLeftHeld(true); break;
                case DuelMobileButtonAction.MoveRight: input.SetRightHeld(true); break;
                case DuelMobileButtonAction.Dash: input.PressDash(); break;
                case DuelMobileButtonAction.Shield: input.PressShield(); break;
                case DuelMobileButtonAction.Skill1: input.PressSkill(0); break;
                case DuelMobileButtonAction.Skill2: input.PressSkill(1); break;
                case DuelMobileButtonAction.Skill3: input.PressSkill(2); break;
                case DuelMobileButtonAction.Skill4: input.PressSkill(3); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (input == null) return;
            if (action == DuelMobileButtonAction.MoveLeft) input.SetLeftHeld(false);
            if (action == DuelMobileButtonAction.MoveRight) input.SetRightHeld(false);
        }
    }
}
