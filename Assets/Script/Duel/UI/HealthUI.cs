using UnityEngine;
using UnityEngine.UI;

namespace AOG.Duel
{
    public sealed class HealthUI : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [SerializeField] private Slider slider;
        private void Awake() { if (slider == null) slider = GetComponent<Slider>(); }
        private void OnEnable()
        {
            if (stats == null) return;
            stats.HpChanged += UpdateBar;
            UpdateBar(stats.Hp, stats.MaxHp);
        }
        private void OnDisable() { if (stats != null) stats.HpChanged -= UpdateBar; }
        private void UpdateBar(int hp, int max)
        {
            if (slider == null) return;
            slider.minValue = 0; slider.maxValue = max; slider.value = hp;
        }
    }
}