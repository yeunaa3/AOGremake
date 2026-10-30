using TMPro;
using UnityEngine;

namespace AOG.Duel
{
    // Hỗ trợ hiển thị chữ trên các nút bấm 2D (Sprite Button) bằng TextMeshPro thay vì dùng ảnh tĩnh
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshPro))]
    public class WorldButtonLabel : MonoBehaviour
    {
        [Header("Nội Dung Chữ")]
        [SerializeField] private string text = "BUTTON";

        [Header("Định Dạng")]
        [SerializeField] private float fontSize = 2.4f;
        [SerializeField] private Color fontColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField] private int sortingOrder = 5;
        [SerializeField] private bool bold = true;

        [Header("Viền Đen (Outline)")]
        [SerializeField] private bool enableOutline = true;
        [SerializeField] private Color outlineColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        [SerializeField, Range(0f, 1f)] private float outlineThickness = 0.22f;

        private TextMeshPro tmp;

        public string Text
        {
            get => text;
            set
            {
                text = value;
                Apply();
            }
        }

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            if (tmp == null) tmp = GetComponent<TextMeshPro>();
            if (tmp == null) return;

            if (tmp.font == null)
            {
                var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (font != null) tmp.font = font;
            }

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = fontColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = sortingOrder;

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = sortingOrder;
            }

            if (enableOutline && tmp.fontMaterial != null)
            {
                tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
                tmp.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
                tmp.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineThickness);
            }
        }
    }
}
