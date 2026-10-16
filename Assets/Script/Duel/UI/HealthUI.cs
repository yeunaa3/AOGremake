using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AOG.Duel
{
    public sealed class HealthUI : MonoBehaviour
    {
        [Header("Stats & Binding")]
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerController character;

        [Header("Health Bar")]
        [SerializeField] private Slider slider;
        [SerializeField] private Image fill;
        [SerializeField] private float fillMaxWidth = 376f;
        [SerializeField] private TMP_Text hpText;

        [Header("Profile / Skills")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image bowIcon;
        [SerializeField] private Image[] skillIcons;
        [SerializeField] private Image[] rarityFrames;
        [SerializeField] private SkillFrames framesConfig;

        [Header("Bow Passive Ring")]
        [SerializeField] private Image passiveRingBase;
        [SerializeField] private Image passiveRingFill;
        [SerializeField] private Sprite playerRingSprite;
        [SerializeField] private Sprite enemyRingSprite;

        private PlayerStats subscribedStats;
        private Gear subscribedGear;

        private void Awake()
        {
            if (slider == null) slider = GetComponent<Slider>();
        }

        private void Start()
        {
            if (stats != null)
            {
                UpdateBar(stats.Hp, stats.MaxHp);
                RefreshProfile();
            }
        }

        private void Update()
        {
            UpdatePassiveRing();
        }

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
                character = null;
                UpdateBar(0, 0);
                RefreshProfile();
                return;
            }

            character = stats.GetComponent<PlayerController>();

            if (isActiveAndEnabled)
            {
                subscribedStats = stats;
                subscribedStats.HpChanged += UpdateBar;

                if (character != null && character.Gear != null)
                {
                    subscribedGear = character.Gear;
                    subscribedGear.BowChanged += OnBowChanged;
                }
            }
            UpdateBar(stats.Hp, stats.MaxHp);
            RefreshProfile();
        }

        private void Unsubscribe()
        {
            if (subscribedStats != null) subscribedStats.HpChanged -= UpdateBar;
            subscribedStats = null;

            if (subscribedGear != null) subscribedGear.BowChanged -= OnBowChanged;
            subscribedGear = null;
        }

        private void OnBowChanged(BowData bow)
        {
            UpdateBowVisual(bow);
            UpdatePassiveRing();
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
                if (fill.sprite != null && fillMaxWidth > 0f)
                {
                    Vector2 size = fill.rectTransform.sizeDelta;
                    size.x = fillMaxWidth * ratio;
                    fill.rectTransform.sizeDelta = size;
                    fill.enabled = ratio > 0.001f;
                }
                else if (fill.sprite == null)
                {
                    Vector3 scale = fill.rectTransform.localScale;
                    scale.x = ratio;
                    fill.rectTransform.localScale = scale;
                }
            }
            if (hpText != null) hpText.SetText("{0}", hp);
        }

        public void RefreshProfile()
        {
            if (character == null && stats != null)
            {
                character = stats.GetComponent<PlayerController>();
            }

            if (character != null)
            {
                if (nameText != null)
                {
                    nameText.SetText(character.Team == 0 ? "Player" : "Enemy");
                }

                if (character.Gear != null)
                {
                    UpdateBowVisual(character.Gear.EquippedBow);
                }
                else if (bowIcon != null)
                {
                    bowIcon.enabled = false;
                }

                UpdatePassiveRing();

                if (skillIcons != null && skillIcons.Length > 0 && character.Action != null)
                {
                    for (int i = 0; i < skillIcons.Length; i++)
                    {
                        Image iconImg = skillIcons[i];
                        Image frameImg = (rarityFrames != null && i < rarityFrames.Length) ? rarityFrames[i] : null;

                        SkillData skill = character.Action.GetEquippedSkill(i);
                        if (skill != null && skill.Icon != null)
                        {
                            if (iconImg != null)
                            {
                                iconImg.sprite = skill.Icon;
                                iconImg.enabled = true;
                            }
                            if (frameImg != null)
                            {
                                if (framesConfig != null)
                                {
                                    frameImg.sprite = framesConfig.Get(skill.Rarity);
                                }
                                frameImg.enabled = frameImg.sprite != null;
                            }
                        }
                        else
                        {
                            if (iconImg != null) iconImg.enabled = false;
                            if (frameImg != null) frameImg.enabled = false;
                        }
                    }
                }
            }
            else
            {
                if (bowIcon != null) bowIcon.enabled = false;
                if (passiveRingFill != null) passiveRingFill.enabled = false;
                if (skillIcons != null)
                {
                    for (int i = 0; i < skillIcons.Length; i++)
                    {
                        if (skillIcons[i] != null) skillIcons[i].enabled = false;
                        if (rarityFrames != null && i < rarityFrames.Length && rarityFrames[i] != null)
                            rarityFrames[i].enabled = false;
                    }
                }
            }
        }

        private void UpdatePassiveRing()
        {
            if (character == null && stats != null)
            {
                character = stats.GetComponent<PlayerController>();
            }

            if (character == null || character.Gear == null) return;

            Sprite teamRing = character.Team == 0 ? playerRingSprite : enemyRingSprite;
            if (passiveRingFill != null)
            {
                if (teamRing != null && passiveRingFill.sprite != teamRing)
                {
                    passiveRingFill.sprite = teamRing;
                }

                BowData bow = character.Gear.EquippedBow;
                if (bow != null && bow.Passive != null)
                {
                    float progress = bow.Passive.GetProgress(character.Gear.BowState);
                    passiveRingFill.fillAmount = progress;
                    passiveRingFill.enabled = progress > 0.001f;
                }
                else
                {
                    passiveRingFill.fillAmount = 1f;
                    passiveRingFill.enabled = true;
                }
            }

            if (passiveRingBase != null)
            {
                passiveRingBase.enabled = true;
            }
        }

        private void UpdateBowVisual(BowData bow)
        {
            if (bowIcon == null) return;
            if (bow != null)
            {
                Sprite s = bow.Icon != null ? bow.Icon : bow.BowSprite;
                bowIcon.sprite = s;
                bowIcon.enabled = s != null;
            }
            else
            {
                bowIcon.enabled = false;
            }
        }
    }
}
