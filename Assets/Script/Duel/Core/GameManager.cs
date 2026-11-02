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

            // Phân biệt chế độ PVP (2 người chơi trên cùng máy) và PVE (đấu với Máy)
            if (GameModeManager.IsPVP)
            {
                ConfigureLocalPVP(playerOne, playerTwo);
            }
            else
            {
                ConfigureEnemyLoadout(playerTwo);
            }

            ConfigureHUD(GameModeManager.IsPVP);
        }

        
        private void ConfigureHUD(bool isPvp)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Nút di chuyển (MoveButton) ở góc dưới bên trái
            Transform moveBtnTransform = canvas.transform.Find("MoveButton");
            if (moveBtnTransform == null)
            {
                GameObject mbObj = GameObject.Find("MoveButton");
                if (mbObj != null) moveBtnTransform = mbObj.transform;
            }

            if (moveBtnTransform != null)
            {
                // Ở chế độ PVP: ẩn nút di chuyển để nhường chỗ cho thanh Skill Bar của Player 1
                // Ở chế độ PVE: hiển thị nút di chuyển như bình thường
                moveBtnTransform.gameObject.SetActive(!isPvp);
            }

            // 2. Thanh Skill Bar của Player 1
            Transform p1SkillBarTransform = canvas.transform.Find("Skill Bar");
            if (p1SkillBarTransform == null)
            {
                GameObject sbObj = GameObject.Find("Skill Bar");
                if (sbObj != null) p1SkillBarTransform = sbObj.transform;
            }

            if (p1SkillBarTransform == null) return;

            RectTransform p1Rt = p1SkillBarTransform.GetComponent<RectTransform>();
            if (p1Rt != null)
            {
                if (isPvp)
                {
                    // Chuyển sang bên trái màn hình (đối xứng với Player 2)
                    p1Rt.anchoredPosition = new Vector2(-540f, -416f);
                }
                else
                {
                    // Vị trí mặc định ở bên phải màn hình trong PVE
                    p1Rt.anchoredPosition = new Vector2(500f, -416f);
                }
            }

            // Bind CooldownUI và Input cho Player 1
            foreach (var cd in p1SkillBarTransform.GetComponentsInChildren<CooldownUI>(true))
            {
                cd.BindCharacter(playerOne);
            }
            KeyInput p1Input = playerOne != null ? playerOne.GetComponent<KeyInput>() : null;
            TouchInput p1Touch = playerOne != null ? playerOne.GetComponent<TouchInput>() : null;
            foreach (var mb in p1SkillBarTransform.GetComponentsInChildren<MobileButton>(true))
            {
                mb.BindInputs(p1Input, p1Touch);
            }

            // 3. Thanh Skill Bar của Player 2 trong PVP
            Transform p2SkillBarTransform = canvas.transform.Find("Skill Bar P2");
            if (isPvp)
            {
                GameObject p2SkillBarObj;
                if (p2SkillBarTransform == null)
                {
                    p2SkillBarObj = Instantiate(p1SkillBarTransform.gameObject, canvas.transform);
                    p2SkillBarObj.name = "Skill Bar P2";
                    p2SkillBarTransform = p2SkillBarObj.transform;
                }
                else
                {
                    p2SkillBarObj = p2SkillBarTransform.gameObject;
                }

                p2SkillBarObj.SetActive(true);

                RectTransform p2Rt = p2SkillBarObj.GetComponent<RectTransform>();
                if (p2Rt != null)
                {
                    p2Rt.anchoredPosition = new Vector2(540f, -416f);
                }

                // Gán toàn bộ CooldownUI cho Player 2
                foreach (var cd in p2SkillBarObj.GetComponentsInChildren<CooldownUI>(true))
                {
                    cd.BindCharacter(playerTwo);
                }

                // Gán input phản hồi của Player 2
                KeyInput p2Input = playerTwo != null ? playerTwo.GetComponent<KeyInput>() : null;
                TouchInput p2Touch = playerTwo != null ? playerTwo.GetComponent<TouchInput>() : null;
                foreach (var mb in p2SkillBarObj.GetComponentsInChildren<MobileButton>(true))
                {
                    mb.BindInputs(p2Input, p2Touch);
                }
            }
            else
            {
                if (p2SkillBarTransform != null)
                {
                    p2SkillBarTransform.gameObject.SetActive(false);
                }
            }
        }

        private void ConfigureLocalPVP(PlayerController p1, PlayerController p2)
        {
            if (p2 == null) return;

            // 1. Tắt Bot AI
            Bot bot = p2.GetComponent<Bot>();
            if (bot != null) bot.enabled = false;

            // 2. Kích hoạt KeyInput cho Người chơi 2 (PlayerIndex = 1)
            KeyInput p2Input = p2.GetComponent<KeyInput>();
            if (p2Input == null) p2Input = p2.gameObject.AddComponent<KeyInput>();
            p2Input.enabled = true;
            p2Input.PlayerIndex = 1;
            p2Input.ReloadKeybindings();
            p2.RefreshInputs();

            // Đảm bảo Người chơi 1 dùng PlayerIndex = 0
            if (p1 != null)
            {
                KeyInput p1Input = p1.GetComponent<KeyInput>();
                if (p1Input != null)
                {
                    p1Input.PlayerIndex = 0;
                    p1Input.ReloadKeybindings();
                }
            }

            // 3. Thiết lập trang bị cho Player 2 theo tùy chọn P2LoadoutMode
            switch (GameModeManager.P2Mode)
            {
                case P2LoadoutMode.Random:
                    // Ngẫu nhiên: Random Cung và 5 chiêu thức (1 Common, 1 Rare, 1 Heroic, 2 Legendary) khác biệt mỗi trận
                    ConfigureEnemyLoadout(p2);
                    break;

                case P2LoadoutMode.UseSet2:
                    // Dùng Set II: Lấy từ Set kỹ năng II mà người chơi đã trang bị ở màn Kỹ năng
                    if (PlayerLoadoutManager.Instance != null)
                    {
                        PlayerLoadoutManager.Instance.ApplySetToPlayer(p2, 1);
                    }
                    break;

                case P2LoadoutMode.Mirror:
                    // Giống Player 1
                    if (PlayerLoadoutManager.Instance != null)
                    {
                        PlayerLoadoutManager.Instance.ApplyToPlayer(p2);
                    }
                    break;
            }

            // 4. Chọn skin khác biệt cho Player 2 để dễ phân biệt
            CharacterSkinDatabase skinDb = CharacterSkinManager.HasInstance && CharacterSkinManager.Instance.Database != null
                ? CharacterSkinManager.Instance.Database
                : Resources.Load<CharacterSkinDatabase>("CharacterSkinDatabase");

            if (skinDb != null && skinDb.Skins != null && skinDb.Skins.Count > 1)
            {
                int p1Index = CharacterSkinManager.HasInstance ? CharacterSkinManager.Instance.CurrentSkinIndex : 0;
                int p2Index = (p1Index + 1) % skinDb.Skins.Count;
                CharacterSkinData distinctSkin = skinDb.Skins[p2Index];
                if (distinctSkin != null) CharacterSkinManager.ApplySkin(p2.gameObject, distinctSkin);
            }

            Debug.Log("[GameManager] Đã thiết lập chế độ PVP Local 2 người chơi trên cùng máy tính!");
        }

        private void ConfigureEnemyLoadout(PlayerController enemy)
        {
            if (enemy == null) return;

            // 1. Random Skin
            CharacterSkinDatabase skinDb = CharacterSkinManager.HasInstance && CharacterSkinManager.Instance.Database != null
                ? CharacterSkinManager.Instance.Database
                : Resources.Load<CharacterSkinDatabase>("CharacterSkinDatabase");

            CharacterSkinData randomSkin = null;
            if (skinDb != null && skinDb.Skins != null && skinDb.Skins.Count > 0)
            {
                int randomSkinIdx = UnityEngine.Random.Range(0, skinDb.Skins.Count);
                randomSkin = skinDb.Skins[randomSkinIdx];
                CharacterSkinManager.ApplySkin(enemy.gameObject, randomSkin);
            }

            // 2. Random Bow
            LoadoutDatabase loadoutDb = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            if (loadoutDb == null && PlayerLoadoutManager.HasInstance)
            {
                loadoutDb = PlayerLoadoutManager.Instance.Database;
            }

            BowData randomBow = null;
            if (loadoutDb != null && loadoutDb.Bows != null && loadoutDb.Bows.Count > 0)
            {
                int randomBowIdx = UnityEngine.Random.Range(0, loadoutDb.Bows.Count);
                randomBow = loadoutDb.Bows[randomBowIdx];
                if (enemy.Gear != null)
                {
                    enemy.Gear.EquipBow(randomBow);
                }
            }

            // 3. Random Skills: 1 Common, 1 Rare, 1 Heroic, 2 Legendary
            if (loadoutDb != null && loadoutDb.Skills != null && loadoutDb.Skills.Count > 0)
            {
                var commonPool = new System.Collections.Generic.List<SkillData>();
                var rarePool = new System.Collections.Generic.List<SkillData>();
                var heroicPool = new System.Collections.Generic.List<SkillData>();
                var legendaryPool = new System.Collections.Generic.List<SkillData>();

                for (int i = 0; i < loadoutDb.Skills.Count; i++)
                {
                    SkillData s = loadoutDb.Skills[i];
                    if (s == null) continue;
                    switch (s.Rarity)
                    {
                        case SkillRarity.Common: commonPool.Add(s); break;
                        case SkillRarity.Rare: rarePool.Add(s); break;
                        case SkillRarity.Heroic: heroicPool.Add(s); break;
                        case SkillRarity.Legendary: legendaryPool.Add(s); break;
                    }
                }

                SkillData pickCommon = commonPool.Count > 0 ? commonPool[UnityEngine.Random.Range(0, commonPool.Count)] : null;
                SkillData pickRare = rarePool.Count > 0 ? rarePool[UnityEngine.Random.Range(0, rarePool.Count)] : null;
                SkillData pickHeroic = heroicPool.Count > 0 ? heroicPool[UnityEngine.Random.Range(0, heroicPool.Count)] : null;

                SkillData pickLegen1 = null;
                SkillData pickLegen2 = null;
                if (legendaryPool.Count > 0)
                {
                    int idx1 = UnityEngine.Random.Range(0, legendaryPool.Count);
                    pickLegen1 = legendaryPool[idx1];
                    if (legendaryPool.Count > 1)
                    {
                        int idx2 = UnityEngine.Random.Range(0, legendaryPool.Count - 1);
                        if (idx2 >= idx1) idx2++;
                        pickLegen2 = legendaryPool[idx2];
                    }
                    else
                    {
                        pickLegen2 = pickLegen1;
                    }
                }

                var selectedSkills = new System.Collections.Generic.List<SkillData>();
                if (pickCommon != null) selectedSkills.Add(pickCommon);
                if (pickRare != null) selectedSkills.Add(pickRare);
                if (pickHeroic != null) selectedSkills.Add(pickHeroic);
                if (pickLegen1 != null) selectedSkills.Add(pickLegen1);
                if (pickLegen2 != null) selectedSkills.Add(pickLegen2);

                // Trộn ngẫu nhiên vị trí giữa các ô kỹ năng
                for (int i = 0; i < selectedSkills.Count; i++)
                {
                    int r = UnityEngine.Random.Range(i, selectedSkills.Count);
                    SkillData temp = selectedSkills[i];
                    selectedSkills[i] = selectedSkills[r];
                    selectedSkills[r] = temp;
                }

                if (enemy.Action != null)
                {
                    enemy.Action.SetSkills(selectedSkills.ToArray());
                }

                Debug.Log($"[Enemy PVE Loadout] Skin: {(randomSkin != null ? randomSkin.displayName : "Default")}, " +
                          $"Bow: {(randomBow != null ? randomBow.DisplayName : "Default")}, " +
                          $"Skills: 1 Common ({pickCommon?.DisplayName}), 1 Rare ({pickRare?.DisplayName}), " +
                          $"1 Heroic ({pickHeroic?.DisplayName}), 2 Legendary ({pickLegen1?.DisplayName}, {pickLegen2?.DisplayName})");
            }
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
