using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AOG.Duel
{
    public sealed class HealthUI : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [SerializeField] private Slider slider;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text hpText;
        private PlayerStats subscribedStats;
        private void Awake() { if (slider == null) slider = GetComponent<Slider>(); }
        // Start chạy sau Awake của PlayerStats, tránh hiển thị 0 máu lúc mới vào.
        private void Start() { if (stats != null) UpdateBar(stats.Hp, stats.MaxHp); }
        private void OnEnable()
        {
            Bind(stats);
        }
        private void OnDisable() => Unsubscribe();

        public void Bind(PlayerStats source)
        {
            Unsubscribe();
            stats = source;
            if (stats == null)
            {
                UpdateBar(0, 0);
                return;
            }
            if (isActiveAndEnabled)
            {
                subscribedStats = stats;
                subscribedStats.HpChanged += UpdateBar;
            }
            UpdateBar(stats.Hp, stats.MaxHp);
        }

        private void Unsubscribe()
        {
            if (subscribedStats != null) subscribedStats.HpChanged -= UpdateBar;
            subscribedStats = null;
        }
        private void UpdateBar(int hp, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
            if (slider != null)
            {
                slider.minValue = 0; slider.maxValue = max; slider.value = hp;
            }
            if (fill != null)
            {
                fill.fillAmount = ratio;
                // Image màu phẳng chưa gán sprite dùng scale, pivot đặt ở đầu thanh.
                if (fill.sprite == null)
                {
                    Vector3 scale = fill.rectTransform.localScale;
                    scale.x = ratio;
                    fill.rectTransform.localScale = scale;
                }
            }
            if (hpText != null) hpText.SetText("{0}", hp);
        }
    }
}
