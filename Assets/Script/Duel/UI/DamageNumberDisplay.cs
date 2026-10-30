using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Hiển thị số sát thương nổi (Floating Damage Number) trên đầu nhân vật (cả địch lẫn ta).
    /// Tự động biến mất sau 0.5s và sử dụng bộ số sprite TimerDigits.
    /// </summary>
    public sealed class DamageNumberDisplay : MonoBehaviour
    {
        private static readonly List<DamageNumberDisplay> pool = new List<DamageNumberDisplay>();
        private static Sprite[] cachedSprites;

        [Header("Cấu hình hiển thị")]
        [Tooltip("Độ cao nổi phía trên đầu nhân vật")]
        [SerializeField] private float heightAboveHead = 1.8f;
        [Tooltip("Thời gian tồn tại trước khi biến mất (chuẩn 0.5s)")]
        [SerializeField] private float displayDuration = 0.5f;
        [Tooltip("Tốc độ bay nhẹ lên trên")]
        [SerializeField] private float upwardSpeed = 0.8f;
        [Tooltip("Kích thước chữ số")]
        [SerializeField] private float digitScale = 0.35f;
        [Tooltip("Khoảng cách giữa các chữ số")]
        [SerializeField] private float digitSpacing = 0.32f;
        [Tooltip("Thứ tự render (đảm bảo luôn nổi trên nhân vật và chiêu thức)")]
        [SerializeField] private int sortingOrder = 200;

        private readonly List<SpriteRenderer> digitRenderers = new List<SpriteRenderer>();
        private Coroutine activeRoutine;

        public static void Show(Vector3 worldPos, int damage, Color? tintColor = null)
        {
            if (damage <= 0) return;

            DamageNumberDisplay instance = GetOrCreateInstance();
            instance.Display(worldPos, damage, tintColor ?? Color.white);
        }

        private static DamageNumberDisplay GetOrCreateInstance()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !pool[i].gameObject.activeInHierarchy)
                {
                    return pool[i];
                }
            }

            GameObject go = new GameObject("DamageNumberPopup");
            DamageNumberDisplay display = go.AddComponent<DamageNumberDisplay>();
            pool.Add(display);
            return display;
        }

        private static void EnsureSpritesLoaded()
        {
            if (cachedSprites != null && cachedSprites.Length == 10) return;

            cachedSprites = new Sprite[10];
            for (int i = 0; i < 10; i++)
            {
#if UNITY_EDITOR
                cachedSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/TimerDigits/Digit_{i}.png");
#endif
                if (cachedSprites[i] == null)
                {
                    cachedSprites[i] = Resources.Load<Sprite>($"TimerDigits/Digit_{i}");
                }
            }
        }

        private void Display(Vector3 worldPos, int damage, Color color)
        {
            EnsureSpritesLoaded();
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }

            gameObject.SetActive(true);

            // Đặt vị trí trên đầu nhân vật với một chút ngẫu nhiên ngang nhẹ để không bị che lấp khi trúng nhiều hit
            float randomX = Random.Range(-0.15f, 0.15f);
            transform.position = worldPos + new Vector3(randomX, heightAboveHead, 0f);

            string text = damage.ToString();
            int count = text.Length;

            // Đảm bảo đủ SpriteRenderer con
            while (digitRenderers.Count < count)
            {
                GameObject child = new GameObject($"Digit_{digitRenderers.Count}");
                child.transform.SetParent(transform, false);
                SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
                sr.sortingOrder = sortingOrder;
                digitRenderers.Add(sr);
            }

            float totalWidth = (count - 1) * digitSpacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < digitRenderers.Count; i++)
            {
                SpriteRenderer sr = digitRenderers[i];
                if (i < count)
                {
                    sr.gameObject.SetActive(true);
                    int digit = text[i] - '0';
                    if (digit >= 0 && digit <= 9 && cachedSprites != null && cachedSprites[digit] != null)
                    {
                        sr.sprite = cachedSprites[digit];
                    }
                    sr.color = color;
                    sr.transform.localScale = new Vector3(digitScale, digitScale, 1f);
                    sr.transform.localPosition = new Vector3(startX + i * digitSpacing, 0f, 0f);
                }
                else
                {
                    sr.gameObject.SetActive(false);
                }
            }

            activeRoutine = StartCoroutine(AnimateAndFade());
        }

        private IEnumerator AnimateAndFade()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < displayDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / displayDuration);

                // Bay nhẹ lên trên
                transform.position = startPos + Vector3.up * (upwardSpeed * elapsed);

                // Mờ dần ở 0.15s cuối cùng
                float alpha = 1f;
                float fadeStart = displayDuration - 0.15f;
                if (elapsed > fadeStart)
                {
                    alpha = Mathf.Clamp01((displayDuration - elapsed) / 0.15f);
                }

                for (int i = 0; i < digitRenderers.Count; i++)
                {
                    if (digitRenderers[i].gameObject.activeSelf)
                    {
                        Color c = digitRenderers[i].color;
                        c.a = alpha;
                        digitRenderers[i].color = c;
                    }
                }

                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
