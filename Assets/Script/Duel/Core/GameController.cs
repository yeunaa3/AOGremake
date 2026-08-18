using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AOG.Duel
{
    public sealed class GameController : MonoBehaviour
    {
        [Header("Participants")]
        [SerializeField] private DuelCharacter playerOne;
        [SerializeField] private DuelCharacter playerTwo;
        [SerializeField] private ProjectilePool projectilePool;

        [Header("Rules")]
        [SerializeField] private bool startAutomatically = true;
        [SerializeField, Min(0f)] private float countdownDuration = 3f;
        [SerializeField, Min(0f)] private float matchDuration = 90f;

        private Coroutine countdownRoutine;

        public DuelMatchState State { get; private set; } = DuelMatchState.Waiting;
        public float CountdownRemaining { get; private set; }
        public float MatchTimeRemaining { get; private set; }
        public DuelCharacter Winner { get; private set; }

        public event Action<DuelMatchState> StateChanged;
        public event Action<float> CountdownChanged;
        public event Action<float> MatchTimeChanged;
        public event Action<DuelCharacter> MatchFinished;

        private void Start()
        {
            ConfigureParticipants();
            if (startAutomatically)
            {
                StartMatch();
            }
        }

        private void Update()
        {
            if (State != DuelMatchState.Playing || matchDuration <= 0f) return;

            MatchTimeRemaining = Mathf.Max(0f, MatchTimeRemaining - Time.deltaTime);
            MatchTimeChanged?.Invoke(MatchTimeRemaining);
            if (MatchTimeRemaining <= 0f)
            {
                FinishByTimeLimit();
            }
        }

        public void StartMatch()
        {
            if (playerOne == null || playerTwo == null)
            {
                Debug.LogError("GameController cần đủ Player One và Player Two.", this);
                return;
            }

            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
            }

            projectilePool?.ReturnAll();
            ConfigureParticipants();
            playerOne.PrepareForMatch();
            playerTwo.PrepareForMatch();
            Winner = null;
            MatchTimeRemaining = matchDuration;
            MatchTimeChanged?.Invoke(MatchTimeRemaining);
            countdownRoutine = StartCoroutine(CountdownRoutine());
        }

        public void NotifyCharacterDied(DuelCharacter deadCharacter)
        {
            if (State != DuelMatchState.Playing) return;

            DuelCharacter winner = deadCharacter == playerOne ? playerTwo : playerOne;
            if (!playerOne.Health.IsAlive && !playerTwo.Health.IsAlive)
            {
                winner = null;
            }
            FinishMatch(winner);
        }

        public void ReloadCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private IEnumerator CountdownRoutine()
        {
            SetState(DuelMatchState.Countdown);
            CountdownRemaining = countdownDuration;
            CountdownChanged?.Invoke(CountdownRemaining);

            while (CountdownRemaining > 0f)
            {
                yield return null;
                CountdownRemaining = Mathf.Max(0f, CountdownRemaining - Time.unscaledDeltaTime);
                CountdownChanged?.Invoke(CountdownRemaining);
            }

            countdownRoutine = null;
            SetState(DuelMatchState.Playing);
            playerOne.SetMatchActive(true);
            playerTwo.SetMatchActive(true);
        }

        private void FinishByTimeLimit()
        {
            DuelCharacter winner = null;
            if (playerOne.Health.CurrentHealth > playerTwo.Health.CurrentHealth) winner = playerOne;
            if (playerTwo.Health.CurrentHealth > playerOne.Health.CurrentHealth) winner = playerTwo;
            FinishMatch(winner);
        }

        private void FinishMatch(DuelCharacter winner)
        {
            if (State == DuelMatchState.Finished) return;

            Winner = winner;
            playerOne.SetMatchActive(false);
            playerTwo.SetMatchActive(false);
            projectilePool?.ReturnAll();
            SetState(DuelMatchState.Finished);
            MatchFinished?.Invoke(winner);
        }

        private void ConfigureParticipants()
        {
            if (playerOne == null || playerTwo == null) return;
            playerOne.ConfigureForMatch(this, playerTwo, 0, projectilePool);
            playerTwo.ConfigureForMatch(this, playerOne, 1, projectilePool);
        }

        private void SetState(DuelMatchState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
