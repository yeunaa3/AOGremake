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
        [SerializeField, Min(0f)] private float countdownDuration = 4f;
        [SerializeField, Min(0f)] private float matchDuration = 90f;

        [Header("Audio (Nhạc Trận Đấu & Âm Thanh Thắng / Thua)")]
        [SerializeField] private AudioSource bgmSource;
        [Tooltip("Nhạc nền trận đấu Map 1 (BGM_PowerWar_Angel)")]
        [SerializeField] private AudioClip map1Bgm;
        [Tooltip("Âm lượng nhạc nền Map 1")]
        [SerializeField, Range(0f, 1f)] private float map1BgmVolume = 0.5f;

        [Tooltip("Nhạc nền trận đấu Map 2 (BGM_PowerWar_Devil)")]
        [SerializeField] private AudioClip map2Bgm;
        [Tooltip("Âm lượng nhạc nền Map 2")]
        [SerializeField, Range(0f, 1f)] private float map2BgmVolume = 0.5f;

        [Tooltip("Âm thanh khi người chơi 1 chiến thắng (BGM_Win)")]
        [SerializeField] private AudioClip winSfx;
        [Tooltip("Âm lượng âm thanh chiến thắng")]
        [SerializeField, Range(0f, 1f)] private float winSfxVolume = 0.8f;

        [Tooltip("Âm thanh khi người chơi 1 thất bại (BGM_Lose)")]
        [SerializeField] private AudioClip loseSfx;
        [Tooltip("Âm lượng âm thanh thất bại")]
        [SerializeField, Range(0f, 1f)] private float loseSfxVolume = 0.8f;

        private Coroutine countdownRoutine;

        public GameState State { get; private set; } = GameState.Waiting;
        public PlayerController PlayerOne => playerOne;
        public PlayerController PlayerTwo => playerTwo;
        public float CountdownRemaining { get; private set; }
        public float MatchTimeRemaining { get; private set; }
        public PlayerController Winner { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action<float> CountdownChanged;
        public event Action<float> MatchTimeChanged;
        public event Action<PlayerController> MatchFinished;

        private void Start()
        {
            if (GetComponent<CountdownDisplayUI>() == null && FindFirstObjectByType<CountdownDisplayUI>() == null)
            {
                gameObject.AddComponent<CountdownDisplayUI>();
            }

            if (GetComponent<MatchTimerDisplayUI>() == null && FindFirstObjectByType<MatchTimerDisplayUI>() == null)
            {
                gameObject.AddComponent<MatchTimerDisplayUI>();
            }

            if (GetComponent<MatchEndPresentationUI>() == null && FindFirstObjectByType<MatchEndPresentationUI>() == null)
            {
                gameObject.AddComponent<MatchEndPresentationUI>();
            }

            SetupAudio();

            if (startAutomatically)
            {
                StartMatch();
            }
            else ConfigureParticipants();
        }

        private void SetupAudio()
        {
            MenuMusicManager.StopMusic();
            if (bgmSource == null)
            {
                bgmSource = GetComponent<AudioSource>();
                if (bgmSource == null)
                {
                    bgmSource = gameObject.AddComponent<AudioSource>();
                    bgmSource.playOnAwake = false;
                }
            }

#if UNITY_EDITOR
            if (map1Bgm == null)
                map1Bgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_PowerWar_Angel.ogg");
            if (map2Bgm == null)
                map2Bgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_PowerWar_Devil.ogg");
            if (winSfx == null)
                winSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_Win.ogg");
            if (loseSfx == null)
                loseSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_Lose.ogg");
#endif

            PlayMatchBgm();
        }

        public void PlayMatchBgm()
        {
            if (bgmSource == null) return;

            string sceneName = SceneManager.GetActiveScene().name;
            AudioClip clipToPlay = map1Bgm;
            float vol = map1BgmVolume;

            if (sceneName.IndexOf("Map2", StringComparison.OrdinalIgnoreCase) >= 0 ||
                sceneName.IndexOf("Devil", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                clipToPlay = map2Bgm;
                vol = map2BgmVolume;
            }

            if (clipToPlay != null)
            {
                bgmSource.clip = clipToPlay;
                bgmSource.volume = vol;
                bgmSource.loop = true;
                bgmSource.Play();
            }
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
            SummonPool.Instance.ReturnAll();
            ConfigureParticipants();
            playerOne.ResetPlayer();
            playerTwo.ResetPlayer();
            PlaceAtSpawn(playerOne, playerSpawn);
            PlaceAtSpawn(playerTwo, enemySpawn);
            Physics2D.SyncTransforms();
            playerOne.LookAtEnemy();
            playerTwo.LookAtEnemy();
            Winner = null;
            MatchTimeRemaining = matchDuration;
            MatchTimeChanged?.Invoke(MatchTimeRemaining);

            if (bgmSource != null && !bgmSource.isPlaying)
            {
                PlayMatchBgm();
            }

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

            if (playerOne != null) playerOne.LookAtEnemy();
            if (playerTwo != null) playerTwo.LookAtEnemy();

            while (CountdownRemaining > 0f)
            {
                yield return null;
                CountdownRemaining = Mathf.Max(0f, CountdownRemaining - Time.unscaledDeltaTime);
                CountdownChanged?.Invoke(CountdownRemaining);
                if (playerOne != null) playerOne.LookAtEnemy();
                if (playerTwo != null) playerTwo.LookAtEnemy();
            }

            countdownRoutine = null;
            if (playerOne != null) playerOne.LookAtEnemy();
            if (playerTwo != null) playerTwo.LookAtEnemy();
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
            SetState(GameState.Finished);
            playerOne.Play(false);
            playerTwo.Play(false);
            projectilePool?.ReturnAll();
            SummonPool.Instance.ReturnAll();

            if (playerOne != null) playerOne.LookAtEnemy();
            if (playerTwo != null) playerTwo.LookAtEnemy();

            if (bgmSource != null)
            {
                bgmSource.Stop();
            }

            // Phát âm thanh Thắng / Thua tương ứng với người chơi 1
            AudioClip finishClip = (winner == playerOne) ? winSfx : loseSfx;
            float finishVol = (winner == playerOne) ? winSfxVolume : loseSfxVolume;
            if (finishClip != null && bgmSource != null)
            {
                bgmSource.PlayOneShot(finishClip, finishVol);
            }

            if (winner != null)
            {
                PlayerController loser = winner == playerOne ? playerTwo : playerOne;
                winner.LookAtEnemy();
                winner.Win();
                if (loser != null)
                {
                    loser.LookAtEnemy();
                    if (loser.Stats.Alive)
                        loser.Die();
                    else
                        loser.Anim.Trigger("Die");
                }
            }
            else
            {
                if (playerOne != null)
                {
                    playerOne.LookAtEnemy();
                    if (playerOne.Stats.Alive) playerOne.Die();
                }
                if (playerTwo != null)
                {
                    playerTwo.LookAtEnemy();
                    if (playerTwo.Stats.Alive) playerTwo.Die();
                }
            }

            MatchFinished?.Invoke(winner);
        }

        private void ConfigureParticipants()
        {
            playerHealthBar?.Bind(playerOne != null ? playerOne.GetComponent<PlayerStats>() : null);
            enemyHealthBar?.Bind(playerTwo != null ? playerTwo.GetComponent<PlayerStats>() : null);
            if (playerOne == null || playerTwo == null) return;

            if (PlayerLoadoutManager.Instance != null)
            {
                PlayerLoadoutManager.Instance.ApplyToPlayer(playerOne);
            }

            if (CharacterSkinManager.Instance != null)
            {
                CharacterSkinManager.Instance.ApplyCurrentSkin(playerOne.gameObject);
            }

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
