using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AOG.Duel
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Participants")]
        [SerializeField] private PlayerController playerOne;
        [SerializeField] private PlayerController playerTwo;
        [SerializeField] private ArrowPool projectilePool;

        [Header("Vị trí bắt đầu trận")]
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private Transform enemySpawn;

        [Header("Thanh máu")]
        [SerializeField] private HealthUI playerHealthBar;
        [SerializeField] private HealthUI enemyHealthBar;

        [Header("Rules")]
        [SerializeField] private bool startAutomatically = true;
        [SerializeField, Min(0f)] private float countdownDuration = 3f;
        [SerializeField, Min(0f)] private float matchDuration = 90f;

        private Coroutine countdownRoutine;

        public GameState State { get; private set; } = GameState.Waiting;
        public float CountdownRemaining { get; private set; }
        public float MatchTimeRemaining { get; private set; }
        public PlayerController Winner { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action<float> CountdownChanged;
        public event Action<float> MatchTimeChanged;
        public event Action<PlayerController> MatchFinished;

        private void Start()
        {
            if (startAutomatically)
            {
                StartMatch();
            }
            else ConfigureParticipants();
        }

        private void Update()
        {
            if (State != GameState.Playing || matchDuration <= 0f) return;

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
                Debug.LogError("GameManager cần đủ PlayerController One và PlayerController Two.", this);
                return;
            }

            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
            }

            projectilePool?.ReturnAll();
            ConfigureParticipants();
            playerOne.ResetPlayer();
            playerTwo.ResetPlayer();
            PlaceAtSpawn(playerOne, playerSpawn);
            PlaceAtSpawn(playerTwo, enemySpawn);
            Physics2D.SyncTransforms();
            Winner = null;
            MatchTimeRemaining = matchDuration;
            MatchTimeChanged?.Invoke(MatchTimeRemaining);
            countdownRoutine = StartCoroutine(CountdownRoutine());
        }

        public void NotifyCharacterDied(PlayerController deadCharacter)
        {
            if (State != GameState.Playing) return;

            PlayerController winner = deadCharacter == playerOne ? playerTwo : playerOne;
            if (!playerOne.Stats.Alive && !playerTwo.Stats.Alive)
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
            SetState(GameState.Countdown);
            CountdownRemaining = countdownDuration;
            CountdownChanged?.Invoke(CountdownRemaining);

            while (CountdownRemaining > 0f)
            {
                yield return null;
                CountdownRemaining = Mathf.Max(0f, CountdownRemaining - Time.unscaledDeltaTime);
                CountdownChanged?.Invoke(CountdownRemaining);
            }

            countdownRoutine = null;
            SetState(GameState.Playing);
            playerOne.Play(true);
            playerTwo.Play(true);
        }

        private void FinishByTimeLimit()
        {
            PlayerController winner = null;
            if (playerOne.Stats.Hp > playerTwo.Stats.Hp) winner = playerOne;
            if (playerTwo.Stats.Hp > playerOne.Stats.Hp) winner = playerTwo;
            FinishMatch(winner);
        }

        private void FinishMatch(PlayerController winner)
        {
            if (State == GameState.Finished) return;

            Winner = winner;
            playerOne.Play(false);
            playerTwo.Play(false);
            projectilePool?.ReturnAll();
            SetState(GameState.Finished);
            MatchFinished?.Invoke(winner);
        }

        private void ConfigureParticipants()
        {
            playerHealthBar?.Bind(playerOne != null ? playerOne.GetComponent<PlayerStats>() : null);
            enemyHealthBar?.Bind(playerTwo != null ? playerTwo.GetComponent<PlayerStats>() : null);
            if (playerOne == null || playerTwo == null) return;
            playerOne.Setup(this, playerTwo, 0, projectilePool);
            playerTwo.Setup(this, playerOne, 1, projectilePool);
        }

        private static void PlaceAtSpawn(PlayerController character, Transform point)
        {
            if (character == null || point == null) return;
            character.transform.position = point.position;
            Rigidbody2D body = character.GetComponent<Rigidbody2D>();
            if (body == null) return;
            body.position = point.position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        private void SetState(GameState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
