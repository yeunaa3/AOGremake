using System;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class Status : MonoBehaviour
    {
        public float StunRemaining { get; private set; }
        public float SilenceRemaining { get; private set; }
        public bool IsStunned => StunRemaining > 0f;
        public bool IsSilenced => SilenceRemaining > 0f;
        public bool CanMove => !IsStunned;
        public bool CanBasicAttack => !IsStunned;
        public bool CanUseSpecialActions => !IsStunned && !IsSilenced;

        public event Action StatusChanged;
        public event Action StunStarted;
        public event Action StunEnded;
        public event Action SilenceStarted;
        public event Action SilenceEnded;

        private void Update()
        {
            bool wasStunned = IsStunned;
            bool wasSilenced = IsSilenced;

            StunRemaining = Mathf.Max(0f, StunRemaining - Time.deltaTime);
            SilenceRemaining = Mathf.Max(0f, SilenceRemaining - Time.deltaTime);

            if (wasStunned && !IsStunned)
            {
                StunEnded?.Invoke();
                StatusChanged?.Invoke();
            }

            if (wasSilenced && !IsSilenced)
            {
                SilenceEnded?.Invoke();
                StatusChanged?.Invoke();
            }
        }

        public void ApplyStun(float duration)
        {
            if (duration <= 0f) return;
            bool wasStunned = IsStunned;
            StunRemaining = Mathf.Max(StunRemaining, duration);
            if (!wasStunned) StunStarted?.Invoke();
            StatusChanged?.Invoke();
        }

        public void ApplySilence(float duration)
        {
            if (duration <= 0f) return;
            bool wasSilenced = IsSilenced;
            SilenceRemaining = Mathf.Max(SilenceRemaining, duration);
            if (!wasSilenced) SilenceStarted?.Invoke();
            StatusChanged?.Invoke();
        }

        public void ClearStun()
        {
            if (!IsStunned) return;
            StunRemaining = 0f;
            StunEnded?.Invoke();
            StatusChanged?.Invoke();
        }

        public void ClearSilence()
        {
            if (!IsSilenced) return;
            SilenceRemaining = 0f;
            SilenceEnded?.Invoke();
            StatusChanged?.Invoke();
        }

        public void ResetStatuses()
        {
            bool hadStun = IsStunned;
            bool hadSilence = IsSilenced;
            StunRemaining = 0f;
            SilenceRemaining = 0f;
            if (hadStun) StunEnded?.Invoke();
            if (hadSilence) SilenceEnded?.Invoke();
            StatusChanged?.Invoke();
        }
    }
}
