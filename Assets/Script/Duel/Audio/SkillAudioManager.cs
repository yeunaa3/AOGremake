using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Quản lý âm thanh cho toàn bộ kỹ năng và chiến đấu in-game:
    /// - Kỹ năng: Lướt (Dash), Địa chấn (Earthquake), Hồi máu (Heal), Tên lửa (Flame), Tên băng (Ice),
    ///            Tên độc (Poison), Sét (Lightning), Đặt bẫy (Mine), Nổ bẫy (Boom), Độc trúng (Poison Hit), Triệu hồi (Summon).
    /// - Tên & Đòn đánh thường: Bắn tên thường (Ingame_ArrowDrop2.ogg), Nhân vật bị bắn trúng (Ingame_Player_Hit.ogg).
    /// Mỗi file âm thanh đều có thanh trượt âm lượng riêng biệt trong Inspector.
    /// </summary>
    [AddComponentMenu("AOG/Audio/Skill Audio Manager")]
    public sealed class SkillAudioManager : MonoBehaviour
    {
        public static SkillAudioManager Instance { get; private set; }

        [Header("1. Đòn Đánh Thường & Bị Bắn Trúng (Arrow & Hit)")]
        [Tooltip("Âm thanh khi bắn tên thường (Assets/Audio/Arrow/Ingame_ArrowDrop2.ogg)")]
        [SerializeField] private AudioClip normalArrowShootSound;
        [Tooltip("Âm lượng bắn tên thường")]
        [SerializeField, Range(0f, 1f)] private float normalArrowShootVolume = 0.8f;

        [Tooltip("Âm thanh khi nhân vật bị bắn trúng (Assets/Audio/Arrow/Ingame_Player_Hit.ogg)")]
        [SerializeField] private AudioClip playerHitSound;
        [Tooltip("Âm lượng khi nhân vật bị bắn trúng")]
        [SerializeField, Range(0f, 1f)] private float playerHitVolume = 0.85f;

        [Header("2. Lướt (Dash)")]
        [Tooltip("Âm thanh kỹ năng lướt (DashShot)")]
        [SerializeField] private AudioClip dashSound;
        [Tooltip("Âm lượng âm thanh lướt")]
        [SerializeField, Range(0f, 1f)] private float dashVolume = 0.8f;

        [Header("3. Địa Chấn / Nhảy (Earthquake)")]
        [Tooltip("Âm thanh kỹ năng nhảy / địa chấn (JumpShot)")]
        [SerializeField] private AudioClip earthquakeSound;
        [Tooltip("Âm lượng âm thanh nhảy / địa chấn")]
        [SerializeField, Range(0f, 1f)] private float earthquakeVolume = 0.8f;

        [Header("4. Hồi Máu (Heal - Tất cả chiêu hồi máu, mỗi đợt 1 lần)")]
        [Tooltip("Âm thanh dùng cho mọi chiêu hồi máu (Skill_Heal, Skill_Rest, HealingTotem,...)")]
        [SerializeField] private AudioClip healSound;
        [Tooltip("Âm lượng âm thanh hồi máu")]
        [SerializeField, Range(0f, 1f)] private float healVolume = 0.8f;

        [Header("5. Tên Lửa / Thiêu Đốt (Flame)")]
        [Tooltip("Âm thanh khi bắn chiêu lửa (Flame)")]
        [SerializeField] private AudioClip flameAttackSound;
        [Tooltip("Âm lượng tên lửa")]
        [SerializeField, Range(0f, 1f)] private float flameAttackVolume = 0.8f;

        [Header("6. Tên Băng / Đóng Băng (Ice)")]
        [Tooltip("Âm thanh khi bắn chiêu băng (FreezeArrow / IceShot)")]
        [SerializeField] private AudioClip iceAttackSound;
        [Tooltip("Âm lượng tên băng")]
        [SerializeField, Range(0f, 1f)] private float iceAttackVolume = 0.8f;

        [Header("7. Tên Độc (Poison)")]
        [Tooltip("Âm thanh khi bắn chiêu độc (PoisonArrow)")]
        [SerializeField] private AudioClip poisonAttackSound;
        [Tooltip("Âm lượng tên độc")]
        [SerializeField, Range(0f, 1f)] private float poisonAttackVolume = 0.8f;

        [Header("8. Sấm Sét (Lightning)")]
        [Tooltip("Âm thanh khi kích hoạt / bắn chiêu sét")]
        [SerializeField] private AudioClip lightningAttackSound;
        [Tooltip("Âm lượng đòn đánh sét")]
        [SerializeField, Range(0f, 1f)] private float lightningAttackVolume = 0.8f;

        [Header("9. Đặt / Thả Bẫy (Mine / Trap)")]
        [Tooltip("Âm thanh khi phóng / thả bẫy (TrapBow, Mine)")]
        [SerializeField] private AudioClip mineAttackSound;
        [Tooltip("Âm lượng thả bẫy")]
        [SerializeField, Range(0f, 1f)] private float mineAttackVolume = 0.8f;

        [Header("10. Nổ / Bẫy Sập (Boom)")]
        [Tooltip("Âm thanh nổ của bẫy, bom khi kích hoạt")]
        [SerializeField] private AudioClip boomSound;
        [Tooltip("Âm lượng vụ nổ")]
        [SerializeField, Range(0f, 1f)] private float boomVolume = 0.85f;

        [Header("11. Độc Trúng Đích (Poison Hit)")]
        [Tooltip("Âm thanh khi tên độc găm trúng mục tiêu")]
        [SerializeField] private AudioClip poisonHitSound;
        [Tooltip("Âm lượng độc trúng")]
        [SerializeField, Range(0f, 1f)] private float poisonHitVolume = 0.8f;

        [Header("12. Triệu Hồi Vật Thể (Wall / Summon)")]
        [Tooltip("Âm thanh khi triệu hồi vật cản / trụ / bia / cầu gai (Totem, Decoy, Turret, Boulder, Wall)")]
        [SerializeField] private AudioClip summonSound;
        [Tooltip("Âm lượng triệu hồi")]
        [SerializeField, Range(0f, 1f)] private float summonVolume = 0.8f;

        private AudioSource audioSource;
        private float lastHealSoundTime;
        private float lastHitSoundTime;
        private const float HealDebounceSeconds = 0.08f;
        private const float HitDebounceSeconds = 0.05f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("SkillAudioManager");
                Instance = go.AddComponent<SkillAudioManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D Sound

            LoadDefaultClipsIfMissing();
        }

        private void Reset()
        {
            LoadDefaultClipsIfMissing();
        }

        private void LoadDefaultClipsIfMissing()
        {
#if UNITY_EDITOR
            if (normalArrowShootSound == null)
                normalArrowShootSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Arrow/Ingame_ArrowDrop2.ogg");
            if (playerHitSound == null)
                playerHitSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Arrow/Ingame_Player_Hit.ogg");

            if (dashSound == null)
                dashSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Effect_Dash.ogg");
            if (earthquakeSound == null)
                earthquakeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Effect_Earthquake.ogg");
            if (healSound == null)
                healSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Effect_Heal.ogg");
            if (flameAttackSound == null)
                flameAttackSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Player_AttackFlame.ogg");
            if (iceAttackSound == null)
                iceAttackSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Player_AttackIce.ogg");
            if (poisonAttackSound == null)
                poisonAttackSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Player_AttackPoison.ogg");
            if (lightningAttackSound == null)
                lightningAttackSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Player_AttackLightning.ogg");
            if (mineAttackSound == null)
                mineAttackSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Player_AttackMine.ogg");
            if (boomSound == null)
                boomSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Effect_Boom.ogg");
            if (poisonHitSound == null)
                poisonHitSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Effect_Poison_Hit.ogg");
            if (summonSound == null)
                summonSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Skill/Ingame_Effect_WallSummon.ogg");
#endif
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        /// <summary>
        /// Phát âm thanh khi bắn mũi tên thường.
        /// </summary>
        public static void PlayNormalArrowShoot()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.normalArrowShootSound, Instance.normalArrowShootVolume);
        }

        /// <summary>
        /// Phát âm thanh khi nhân vật bị bắn trúng.
        /// </summary>
        public static void PlayPlayerHit()
        {
            if (Instance == null) return;
            if (Time.unscaledTime - Instance.lastHitSoundTime < HitDebounceSeconds) return;
            Instance.lastHitSoundTime = Time.unscaledTime;
            Instance.PlayClip(Instance.playerHitSound, Instance.playerHitVolume);
        }

        public static void PlayDash()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.dashSound, Instance.dashVolume);
        }

        public static void PlayEarthquake()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.earthquakeSound, Instance.earthquakeVolume);
        }

        /// <summary>
        /// Phát âm thanh khi hồi 1 đợt máu. Có khử trùng lặp (debounce) để khi hồi nhiều mục tiêu trong 1 nhịp không bị chát tiếng.
        /// </summary>
        public static void PlayHeal()
        {
            if (Instance == null) return;
            if (Time.unscaledTime - Instance.lastHealSoundTime < HealDebounceSeconds) return;
            Instance.lastHealSoundTime = Time.unscaledTime;
            Instance.PlayClip(Instance.healSound, Instance.healVolume);
        }

        public static void PlayFlame()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.flameAttackSound, Instance.flameAttackVolume);
        }

        public static void PlayIce()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.iceAttackSound, Instance.iceAttackVolume);
        }

        public static void PlayPoison()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.poisonAttackSound, Instance.poisonAttackVolume);
        }

        public static void PlayLightning()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.lightningAttackSound, Instance.lightningAttackVolume);
        }

        public static void PlayMine()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.mineAttackSound, Instance.mineAttackVolume);
        }

        public static void PlayBoom()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.boomSound, Instance.boomVolume);
        }

        public static void PlayPoisonHit()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.poisonHitSound, Instance.poisonHitVolume);
        }

        public static void PlaySummon()
        {
            if (Instance == null) return;
            Instance.PlayClip(Instance.summonSound, Instance.summonVolume);
        }

        /// <summary>
        /// Tự động nhận diện và phát âm thanh phù hợp cho kỹ năng bắn ra dựa theo dữ liệu hiệu ứng và tên chiêu.
        /// </summary>
        public static void PlaySkillShot(SkillData skill, ArrowEffect effect = ArrowEffect.None)
        {
            if (Instance == null) return;

            if (effect == ArrowEffect.Burn)
            {
                PlayFlame();
                return;
            }
            if (effect == ArrowEffect.Freeze)
            {
                PlayIce();
                return;
            }
            if (effect == ArrowEffect.Poison)
            {
                PlayPoison();
                return;
            }

            if (skill != null)
            {
                string id = (skill.SkillId ?? string.Empty).ToLowerInvariant();
                string name = (skill.DisplayName ?? skill.name ?? string.Empty).ToLowerInvariant();

                if (id.Contains("flame") || id.Contains("fire") || name.Contains("flame") || name.Contains("lửa"))
                {
                    PlayFlame();
                    return;
                }
                if (id.Contains("ice") || id.Contains("freeze") || name.Contains("ice") || name.Contains("băng"))
                {
                    PlayIce();
                    return;
                }
                if (id.Contains("poison") || name.Contains("poison") || name.Contains("độc"))
                {
                    PlayPoison();
                    return;
                }
                if (id.Contains("lightning") || id.Contains("thunder") || name.Contains("lightning") || name.Contains("sét"))
                {
                    PlayLightning();
                    return;
                }
                if (id.Contains("mine") || id.Contains("trap") || name.Contains("mine") || name.Contains("bẫy"))
                {
                    PlayMine();
                    return;
                }
            }
        }
    }
}
