using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    // Menu đầu game và nhạc nền dùng chung cho cả Menu lẫn màn Duel.
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private Sprite logo;
        [SerializeField, Range(0f, 1f)] private float musicVolume = .35f;
        [SerializeField] private string battleSceneName = "Duel_battle";

        private static MainMenu current;
        private AudioSource musicSource;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle playStyle;
        private Texture2D normalButton;
        private Texture2D hoverButton;

        private void Awake()
        {
            if (current != null && current != this)
            {
                Destroy(gameObject);
                return;
            }

            current = this;
            DontDestroyOnLoad(gameObject);
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume;
            musicSource.playOnAwake = false;
            if (backgroundMusic != null) musicSource.Play();
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene") return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame))
                StartGame();
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene") return;
            CreateStyles();

            GUI.color = new Color(.08f, .22f, .32f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            float centerX = Screen.width * .5f;
            float titleY = Screen.height * .18f;
            DrawLogo(centerX, titleY, scale);
            GUI.Label(new Rect(centerX - 420 * scale, titleY + 245 * scale, 840 * scale, 50 * scale),
                "BOW DUEL", subtitleStyle);

            Rect playRect = new Rect(centerX - 170 * scale, Screen.height * .61f, 340 * scale, 76 * scale);
            if (GUI.Button(playRect, "START GAME", playStyle)) StartGame();

            GUI.Label(new Rect(centerX - 300 * scale, Screen.height * .78f, 600 * scale, 30 * scale),
                "Press ENTER or click START GAME", subtitleStyle);
        }

        private void DrawLogo(float centerX, float y, float scale)
        {
            if (logo == null || logo.texture == null)
            {
                GUI.Label(new Rect(centerX - 550 * scale, y, 1100 * scale, 120 * scale),
                    "ARCHER OF GOD", titleStyle);
                return;
            }

            float aspect = logo.rect.width / logo.rect.height;
            float height = 210 * scale;
            float width = height * aspect;
            Rect image = new Rect(centerX - width * .5f, y, width, height);
            Rect uv = new Rect(logo.rect.x / logo.texture.width, logo.rect.y / logo.texture.height,
                logo.rect.width / logo.texture.width, logo.rect.height / logo.texture.height);
            GUI.DrawTextureWithTexCoords(image, logo.texture, uv, true);
        }

        private void StartGame()
        {
            if (Application.CanStreamedLevelBeLoaded(battleSceneName))
                SceneManager.LoadScene(battleSceneName);
            else
                Debug.LogError("Chưa thêm Duel_battle vào Build Settings.", this);
        }

        private void CreateStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 74,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, .78f, .25f) }
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(.7f, .8f, .92f) }
            };
            playStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(.08f, .12f, .18f) },
                hover = { textColor = Color.white }
            };
            normalButton = MakeColorTexture(new Color(1f, .72f, .16f));
            hoverButton = MakeColorTexture(new Color(1f, .48f, .08f));
            playStyle.normal.background = normalButton;
            playStyle.hover.background = hoverButton;
            playStyle.active.background = hoverButton;
        }

        private static Texture2D MakeColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
