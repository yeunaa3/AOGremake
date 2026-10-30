using System.Collections;

using TMPro;

using UnityEngine;

using UnityEngine.InputSystem;

using UnityEngine.SceneManagement;

using UnityEngine.UI;



namespace AOG.Duel

{

    /// <summary>

    /// Xử lý hiệu ứng khi trận đấu kết thúc (thắng / thua / hòa):

    /// 1. Làm mờ 30% nền đằng sau (overlay đen 30% alpha).

    /// 2. Phóng to 2 character và di chuyển mượt mà ra 2 bên màn hình (đối mặt nhau).

    /// 3. Hiển thị chữ "PRESS ANY BUTTON TO RETURN TO MENU" (hoặc nhấn bất kỳ để về menu).

    /// 4. Nhấn phím bất kỳ / chuột / chạm màn hình để quay về màn hình Menu.

    /// </summary>

    public sealed class MatchEndPresentationUI : MonoBehaviour

    {

        [Header("Game Manager")]

        [SerializeField] private GameManager matchManager;



        [Header("Hiệu ứng Mờ Nền")]

        [Tooltip("Độ mờ tối nền đằng sau (mặc định 30% = 0.3f)")]

        [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.3f;

        [Tooltip("Thời gian chuyển mờ nền (giây)")]

        [SerializeField] private float dimFadeDuration = 0.5f;



        [Header("Hiệu ứng 2 Nhân Vật")]

        [Tooltip("Tọa độ đích bên trái cho Player 1")]

        [SerializeField] private Vector3 leftCharacterTargetPos = new Vector3(-5.2f, -1.0f, 0f);

        [Tooltip("Tọa độ đích bên phải cho Player 2 / Địch")]

        [SerializeField] private Vector3 rightCharacterTargetPos = new Vector3(5.2f, -1.0f, 0f);

        [Tooltip("Tỉ lệ phóng to của 2 nhân vật (phóng to gấp 2 lần nữa)")]

        [SerializeField] private float characterEnlargeScale = 3.5f;

        [Tooltip("Thời gian nhân vật di chuyển và phóng to (giây)")]

        [SerializeField] private float characterMoveDuration = 0.8f;



        [Header("Thời gian chờ trước khi cho phép bấm")]

        [Tooltip("Tránh người chơi bấm nhầm nút lúc vừa dứt điểm")]

        [SerializeField] private float inputDelay = 0.6f;



        private GameObject dimOverlayObj;

        private Canvas targetCanvas;

        private RectTransform resultContainer;

        private TMP_Text titleText;

        private TMP_Text promptText;

        private bool isFinished;

        private bool canExit;

        private float finishTime;



        private void Awake()

        {

            if (matchManager == null)

            {

                matchManager = GetComponent<GameManager>() ?? FindFirstObjectByType<GameManager>();

            }



            Canvas canvas = FindFirstObjectByType<Canvas>();

            if (canvas != null)

            {

                Transform existing = canvas.transform.Find("MatchEndResultUI");

                if (existing != null)

                {

                    existing.gameObject.SetActive(false);

                }

            }

        }



        private void OnEnable()

        {

            if (matchManager != null)

            {

                matchManager.MatchFinished -= HandleMatchFinished;

                matchManager.MatchFinished += HandleMatchFinished;

            }

        }



        private void OnDisable()

        {

            if (matchManager != null)

            {

                matchManager.MatchFinished -= HandleMatchFinished;

            }

        }



        private void HandleMatchFinished(PlayerController winner)

        {

            if (isFinished) return;

            isFinished = true;

            finishTime = Time.time;



            PlayerController p1 = matchManager != null ? matchManager.PlayerOne : null;

            PlayerController p2 = matchManager != null ? matchManager.PlayerTwo : null;



            if (p1 == null || p2 == null)

            {

                PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

                if (players.Length > 0 && p1 == null) p1 = players[0];

                if (players.Length > 1 && p2 == null) p2 = players[1];

            }



            StartCoroutine(MatchEndSequence(winner, p1, p2));

        }



        private IEnumerator MatchEndSequence(PlayerController winner, PlayerController p1, PlayerController p2)
        {
            // 1. Ẩn toàn bộ HUD gameplay không cần thiết (Thanh Skill, Nút di chuyển, Timer, Thanh máu...)
            HideGameplayHUD();

            // 2. Kích hoạt và giữ nguyên state animation Win cho người thắng và Die/Lose cho người thua
            if (winner != null)
            {
                winner.LookAtEnemy();
                winner.Anim?.Trigger("Win");
                PlayerController loser = (winner == p1) ? p2 : p1;
                if (loser != null)
                {
                    loser.LookAtEnemy();
                    loser.Anim?.Trigger("Die");
                }
            }
            else
            {
                if (p1 != null) { p1.LookAtEnemy(); p1.Anim?.Trigger("Die"); }
                if (p2 != null) { p2.LookAtEnemy(); p2.Anim?.Trigger("Die"); }
            }

            // 3. Tạo màn mờ 30% nền đằng sau
            CreateDimOverlay();

            // 4. Chuẩn bị 2 nhân vật: tăng sorting order để nổi lên trên màn mờ
            SetupCharacterSorting(p1);
            SetupCharacterSorting(p2);

            // 5. Di chuyển mượt mà và phóng to 2 nhân vật ra 2 bên màn hình
            Coroutine moveRoutine = StartCoroutine(AnimateCharacters(p1, p2));

            // 6. Hiển thị UI kết quả và dòng chữ "Press Any Button"
            BuildResultUI(winner, p1);

            yield return moveRoutine;

            // Chờ một khoảng nhỏ tránh bấm nhầm
            yield return new WaitForSeconds(inputDelay);
            canExit = true;

            // Hiệu ứng nhấp nháy cho chữ "Press Any Button"
            StartCoroutine(PulsePromptText());
        }



        private void HideGameplayHUD()
        {
            // Ẩn Timer bộ số nếu có component
            MatchTimerDisplayUI timer = FindFirstObjectByType<MatchTimerDisplayUI>();
            if (timer != null)
            {
                Transform tContainer = timer.transform.Find("MatchTimerDigitsContainer");
                if (tContainer != null) tContainer.gameObject.SetActive(false);
            }

            // Ẩn toàn bộ UI trên Canvas (Thanh Skill, Nút di chuyển, HealthBars, Countdown, Timer...)
            // Chỉ giữ lại MatchEndResultUI
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                for (int i = 0; i < canvas.transform.childCount; i++)
                {
                    Transform child = canvas.transform.GetChild(i);
                    if (child.name != "MatchEndResultUI" && child.name != "MatchEndDimOverlay")
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            // Đảm bảo toàn bộ CooldownUI cũng ẩn
            CooldownUI[] cooldowns = FindObjectsByType<CooldownUI>(FindObjectsSortMode.None);
            for (int i = 0; i < cooldowns.Length; i++)
            {
                if (cooldowns[i] != null) cooldowns[i].gameObject.SetActive(false);
            }
        }



        private void CreateDimOverlay()

        {

            if (dimOverlayObj != null) return;



            dimOverlayObj = new GameObject("MatchEndDimOverlay");

            dimOverlayObj.transform.position = new Vector3(0f, 0f, 0f);



            SpriteRenderer sr = dimOverlayObj.AddComponent<SpriteRenderer>();

            Texture2D tex = Texture2D.whiteTexture;

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1f);

            sr.color = new Color(0f, 0f, 0f, 0f); // Bắt đầu từ 0

            sr.sortingOrder = 10; // Phía trên nền và đất (thường 0 đến 3), nhưng phía dưới nhân vật (100)

            dimOverlayObj.transform.localScale = new Vector3(35f, 25f, 1f);



            StartCoroutine(FadeDimOverlay(sr));

        }



        private IEnumerator FadeDimOverlay(SpriteRenderer sr)

        {

            float elapsed = 0f;

            while (elapsed < dimFadeDuration)

            {

                elapsed += Time.deltaTime;

                float alpha = Mathf.Lerp(0f, dimAlpha, elapsed / dimFadeDuration);

                if (sr != null) sr.color = new Color(0f, 0f, 0f, alpha);

                yield return null;

            }

            if (sr != null) sr.color = new Color(0f, 0f, 0f, dimAlpha);

        }



        private void SetupCharacterSorting(PlayerController p)

        {

            if (p == null) return;



            // Ngắt vật lý rơi tự do để nhân vật đứng cố định tại vị trí kết thúc

            Rigidbody2D rb = p.GetComponent<Rigidbody2D>();

            if (rb != null)

            {

                rb.linearVelocity = Vector2.zero;

                rb.bodyType = RigidbodyType2D.Kinematic;

            }



            // Tắt collider để tránh va chạm với map

            Collider2D[] cols = p.GetComponentsInChildren<Collider2D>();

            for (int i = 0; i < cols.Length; i++)

            {

                if (cols[i] != null) cols[i].enabled = false;

            }



            // Đưa các bộ phận sprite lên sortingOrder 100+ để nổi bật phía trên nền mờ 30%

            SpriteRenderer[] renderers = p.GetComponentsInChildren<SpriteRenderer>(true);

            for (int i = 0; i < renderers.Length; i++)

            {

                if (renderers[i] != null)

                {

                    renderers[i].sortingOrder += 100;

                }

            }

        }



        private IEnumerator AnimateCharacters(PlayerController p1, PlayerController p2)
        {
            Vector3 startPos1 = p1 != null ? p1.transform.position : Vector3.zero;
            Vector3 startPos2 = p2 != null ? p2.transform.position : Vector3.zero;

            Vector3 baseScale1 = p1 != null
                ? new Vector3(Mathf.Abs(p1.transform.localScale.x), Mathf.Abs(p1.transform.localScale.y), 1f)
                : Vector3.one;
            Vector3 baseScale2 = p2 != null
                ? new Vector3(Mathf.Abs(p2.transform.localScale.x), Mathf.Abs(p2.transform.localScale.y), 1f)
                : Vector3.one;

            // Cả hai nhân vật đều giữ localScale dương (phóng to đều 2 chiều).
            // Hướng mặt đối diện nhau hoàn toàn do Visual (Anim.Face) đảm nhiệm:
            // p1 ở bên trái màn hình nhìn sang phải (+1).
            // p2 ở bên phải màn hình nhìn sang trái (-1).
            Vector3 targetScale = new Vector3(Mathf.Abs(characterEnlargeScale), Mathf.Abs(characterEnlargeScale), 1f);

            if (p1 != null && p1.Anim != null) p1.Anim.Face(1f);
            if (p2 != null && p2.Anim != null) p2.Anim.Face(-1f);

            float elapsed = 0f;
            while (elapsed < characterMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / characterMoveDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (p1 != null)
                {
                    p1.transform.position = Vector3.Lerp(startPos1, leftCharacterTargetPos, smoothT);
                    p1.transform.localScale = Vector3.Lerp(baseScale1, targetScale, smoothT);
                    p1.Anim?.Face(1f);
                }

                if (p2 != null)
                {
                    p2.transform.position = Vector3.Lerp(startPos2, rightCharacterTargetPos, smoothT);
                    p2.transform.localScale = Vector3.Lerp(baseScale2, targetScale, smoothT);
                    p2.Anim?.Face(-1f);
                }

                yield return null;
            }

            if (p1 != null)
            {
                p1.transform.position = leftCharacterTargetPos;
                p1.transform.localScale = targetScale;
                p1.Anim?.Face(1f);
            }

            if (p2 != null)
            {
                p2.transform.position = rightCharacterTargetPos;
                p2.transform.localScale = targetScale;
                p2.Anim?.Face(-1f);
            }
        }



        private void BuildResultUI(PlayerController winner, PlayerController p1)

        {

            targetCanvas = FindFirstObjectByType<Canvas>();

            if (targetCanvas == null) return;



            Transform existing = targetCanvas.transform.Find("MatchEndResultUI");

            if (existing != null)

            {

                resultContainer = existing.GetComponent<RectTransform>();

                resultContainer.gameObject.SetActive(true);

                titleText = existing.Find("TitleText")?.GetComponent<TextMeshProUGUI>();

                promptText = existing.Find("PromptText")?.GetComponent<TextMeshProUGUI>();

            }

            else

            {

                GameObject container = new GameObject("MatchEndResultUI", typeof(RectTransform));

                container.transform.SetParent(targetCanvas.transform, false);

                resultContainer = container.GetComponent<RectTransform>();

                resultContainer.anchorMin = Vector2.zero;

                resultContainer.anchorMax = Vector2.one;

                resultContainer.offsetMin = Vector2.zero;

                resultContainer.offsetMax = Vector2.zero;



                // 1. Tiêu đề Thắng / Thua (VICTORY / DEFEAT)

                GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));

                titleObj.transform.SetParent(resultContainer, false);

                RectTransform titleRt = titleObj.GetComponent<RectTransform>();

                titleRt.anchorMin = new Vector2(0.5f, 0.5f);

                titleRt.anchorMax = new Vector2(0.5f, 0.5f);

                titleRt.pivot = new Vector2(0.5f, 0.5f);

                titleRt.anchoredPosition = new Vector2(0f, 220f);

                titleRt.sizeDelta = new Vector2(900f, 120f);



                titleText = titleObj.AddComponent<TextMeshProUGUI>();

                titleText.alignment = TextAlignmentOptions.Center;

                titleText.fontSize = 68f;

                titleText.fontStyle = FontStyles.Bold;



                // 2. Dòng chữ hướng dẫn: "PRESS ANY BUTTON TO RETURN TO MENU"

                GameObject promptObj = new GameObject("PromptText", typeof(RectTransform));

                promptObj.transform.SetParent(resultContainer, false);

                RectTransform promptRt = promptObj.GetComponent<RectTransform>();

                promptRt.anchorMin = new Vector2(0.5f, 0.5f);

                promptRt.anchorMax = new Vector2(0.5f, 0.5f);

                promptRt.pivot = new Vector2(0.5f, 0.5f);

                promptRt.anchoredPosition = new Vector2(0f, -320f);

                promptRt.sizeDelta = new Vector2(800f, 60f);



                promptText = promptObj.AddComponent<TextMeshProUGUI>();

                promptText.alignment = TextAlignmentOptions.Center;

                promptText.fontSize = 28f;

                promptText.text = "PRESS ANY BUTTON TO RETURN TO MENU";

                promptText.color = new Color(1f, 1f, 1f, 0.9f);

            }



            if (titleText != null)

            {

                if (winner == null)

                {

                    titleText.text = "DRAW";

                    titleText.color = new Color(0.9f, 0.9f, 0.9f, 1f);

                }

                else if (winner == p1)

                {

                    titleText.text = "VICTORY";

                    titleText.color = new Color(1f, 0.85f, 0.2f, 1f);

                }

                else

                {

                    titleText.text = "DEFEAT";

                    titleText.color = new Color(1f, 0.28f, 0.24f, 1f);

                }

            }



            if (promptText != null)

            {

                promptText.gameObject.SetActive(true);

            }

        }



        private IEnumerator PulsePromptText()

        {

            if (promptText == null) yield break;



            while (true)

            {

                float t = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;

                float alpha = Mathf.Lerp(0.35f, 1f, t);

                Color c = promptText.color;

                c.a = alpha;

                promptText.color = c;

                yield return null;

            }

        }



        private void Update()

        {

            if (!canExit) return;



            if (AnyInputDetected())

            {

                canExit = false;

                ButtonAudioManager.PlayClick();

                SceneManager.LoadScene("Menu");

            }

        }



        private bool AnyInputDetected()

        {

            // Bàn phím

            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;



            // Chuột

            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;



            // Cảm ứng điện thoại

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;



            // Tay cầm Gamepad

            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)) return true;



            return false;

        }

    }

}

