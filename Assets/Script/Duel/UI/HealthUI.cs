using UnityEngine;
using UnityEngine.UI;

namespace AOG.Duel
{
    public sealed class HealthUI : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Slider slider;

        private void Awake()
        {
            if (slider == null) slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.HealthChanged += HandleHealthChanged;
            HandleHealthChanged(health.CurrentHealth, health.MaximumHealth);
        }

        private void OnDisable()
        {
            if (health != null) health.HealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int current, int maximum)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = maximum;
            slider.value = current;
        }
    }
}
