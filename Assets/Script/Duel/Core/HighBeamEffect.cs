using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Hiệu ứng Cột Sáng Trắng (White Lighting Beam / Pillar of Light):
    /// - 1 cột sáng dài từ người nhân vật vút cao qua khỏi màn hình Main Camera (màu trắng rực rỡ).
    /// - Kết hợp cột laser ánh sáng trung tâm (Solid White Beam) và hệ thống hạt phát sáng (Particles, Sparkles, Ground Burst).
    /// </summary>
    public sealed class HighBeamEffect : MonoBehaviour
    {
        [Header("1. Kích Thước Cột Sáng (Beam Size)")]
        [Tooltip("Chiều cao cột sáng vút thẳng lên trời (vượt qua đỉnh màn hình camera). Khuyên dùng: 20 - 35")]
        [SerializeField, Range(10f, 50f)] private float beamHeight = 25f;

        [Tooltip("Độ to của lõi sáng trắng ở chính giữa. Càng to thì cột sáng càng dày đặc. Khuyên dùng: 0.4 - 1.5")]
        [SerializeField, Range(0.1f, 3.0f)] private float coreWidth = 0.65f;

        [Tooltip("Độ to của vầng hào quang phát sáng bao quanh lõi. Nên gấp đôi hoặc gấp ba Core Width để phát sáng đẹp. Khuyên dùng: 1.0 - 3.5")]
        [SerializeField, Range(0.2f, 6.0f)] private float glowWidth = 1.60f;

        [Header("2. Thời Gian Tồn Tại (Timing - Giây)")]
        [Tooltip("Thời gian bùng nổ lóe sáng cực nhanh lúc mới xuất hiện (giây). Mặc định: 0.06")]
        [SerializeField, Range(0.01f, 0.3f)] private float introDuration = 0.06f;

        [Tooltip("Thời gian giữ sáng rực rỡ nhất (giây). Muốn cột sáng đứng lâu hơn trên màn hình thì kéo tăng số này lên (ví dụ: 0.3 - 0.5s).")]
        [SerializeField, Range(0.05f, 2.0f)] private float holdDuration = 0.16f;

        [Tooltip("Thời gian mờ dần và biến mất (giây). Mặc định: 0.40s")]
        [SerializeField, Range(0.1f, 1.5f)] private float fadeDuration = 0.40f;

        [Header("3. Màu Sắc & Hiển Thị (Color & Layer)")]
        [Tooltip("Màu sắc hào quang của cột sáng (Mặc định: Trắng. Có thể đổi sang Vàng, Xanh, Đỏ, Tím...)")]
        [SerializeField] private Color beamColor = Color.white;

        [Tooltip("Material hiệu ứng phát sáng. Bỏ trống sẽ tự động nạp M_HighBeam.")]
        [SerializeField] private Material beamMaterial;

        [Tooltip("Độ ưu tiên hiển thị (Sorting Order). Mặc định 25 để nổi bật phía trên nhân vật.")]
        [SerializeField] private int sortingOrder = 25;

        private LineRenderer coreLine;
        private LineRenderer glowLine;
        private ParticleSystem sparklesPs;
        private ParticleSystem burstPs;

        private float elapsed;
        private bool isFading;

        private void Awake()
        {
            ResolveMaterial();
            SetupCoreBeam();
            SetupGlowBeam();
            SetupSparkles();
            SetupGroundBurst();
        }

        private void Start()
        {
            if (sparklesPs != null) sparklesPs.Play();
            if (burstPs != null) burstPs.Play();
        }

        private void ResolveMaterial()
        {
            if (beamMaterial != null) return;

            var rend = GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                beamMaterial = rend.sharedMaterial;
                return;
            }

            beamMaterial = Resources.Load<Material>("M_HighBeam");
#if UNITY_EDITOR
            if (beamMaterial == null)
            {
                beamMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Effect/M_HighBeam.mat");
            }
#endif
        }

        private void SetupCoreBeam()
        {
            coreLine = GetComponent<LineRenderer>();
            if (coreLine == null) coreLine = gameObject.AddComponent<LineRenderer>();

            coreLine.useWorldSpace = false;
            coreLine.positionCount = 2;
            coreLine.SetPosition(0, Vector3.zero);
            coreLine.SetPosition(1, new Vector3(0f, beamHeight, 0f));
            coreLine.startWidth = coreWidth;
            coreLine.endWidth = coreWidth * 0.95f;
            coreLine.material = beamMaterial;
            coreLine.textureMode = LineTextureMode.Stretch;
            coreLine.alignment = LineAlignment.TransformZ;
            coreLine.sortingOrder = sortingOrder + 1;

            // Gradient thuần trắng sáng chói
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.85f), new GradientAlphaKey(0f, 1f) }
            );
            coreLine.colorGradient = grad;
        }

        private void SetupGlowBeam()
        {
            Transform existing = transform.Find("GlowBeam");
            GameObject glowObj = existing != null ? existing.gameObject : new GameObject("GlowBeam");
            glowObj.transform.SetParent(transform, false);

            glowLine = glowObj.GetComponent<LineRenderer>();
            if (glowLine == null) glowLine = glowObj.AddComponent<LineRenderer>();

            glowLine.useWorldSpace = false;
            glowLine.positionCount = 2;
            glowLine.SetPosition(0, Vector3.zero);
            glowLine.SetPosition(1, new Vector3(0f, beamHeight, 0f));
            glowLine.startWidth = glowWidth;
            glowLine.endWidth = glowWidth * 0.95f;
            glowLine.material = beamMaterial;
            glowLine.textureMode = LineTextureMode.Stretch;
            glowLine.alignment = LineAlignment.TransformZ;
            glowLine.sortingOrder = sortingOrder;

            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(beamColor, 0f), new GradientColorKey(beamColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.65f, 0f), new GradientAlphaKey(0.55f, 0.85f), new GradientAlphaKey(0f, 1f) }
            );
            glowLine.colorGradient = grad;
        }

        private void SetupSparkles()
        {
            Transform existing = transform.Find("Sparkles");
            GameObject spObj = existing != null ? existing.gameObject : new GameObject("Sparkles");
            spObj.transform.SetParent(transform, false);

            sparklesPs = spObj.GetComponent<ParticleSystem>();
            if (sparklesPs == null) sparklesPs = spObj.AddComponent<ParticleSystem>();

            var main = sparklesPs.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = introDuration + holdDuration + 0.1f;
            main.startLifetime = 0.55f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Phát hạt dọc theo thân cột sáng từ mặt đất lên cao
            var shape = sparklesPs.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, beamHeight * 0.45f, 0f);
            shape.scale = new Vector3(coreWidth * 1.6f, beamHeight * 0.9f, 0.2f);

            var emission = sparklesPs.emission;
            emission.enabled = true;
            emission.rateOverTime = 60f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var rend = spObj.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.renderMode = ParticleSystemRenderMode.Billboard;
                rend.sortingOrder = sortingOrder + 2;
                if (beamMaterial != null) rend.material = beamMaterial;
            }
        }

        private void SetupGroundBurst()
        {
            Transform existing = transform.Find("GroundBurst");
            GameObject burstObj = existing != null ? existing.gameObject : new GameObject("GroundBurst");
            burstObj.transform.SetParent(transform, false);

            burstPs = burstObj.GetComponent<ParticleSystem>();
            if (burstPs == null) burstPs = burstObj.AddComponent<ParticleSystem>();

            var main = burstPs.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.15f;
            main.startLifetime = 0.28f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = burstPs.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            var emission = burstPs.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            var rend = burstObj.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.renderMode = ParticleSystemRenderMode.Billboard;
                rend.sortingOrder = sortingOrder + 3;
                if (beamMaterial != null) rend.material = beamMaterial;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (elapsed < introDuration)
            {
                // 1. Bùng sáng & mở rộng cột sáng tức thì
                float t = Mathf.Clamp01(elapsed / introDuration);
                float widthFactor = Mathf.Lerp(0.3f, 1.15f, t);
                SetBeamWidths(widthFactor);
                SetBeamAlpha(1f);
            }
            else if (elapsed < introDuration + holdDuration)
            {
                // 2. Giữ nguyên độ sáng cực đại (có độ rung rực rỡ nhẹ)
                float pulse = 1f + Mathf.Sin((elapsed - introDuration) * 35f) * 0.05f;
                SetBeamWidths(pulse);
                SetBeamAlpha(1f);
            }
            else
            {
                // 3. Mờ dần và thu nhỏ biến mất từ từ
                float fadeElapsed = elapsed - (introDuration + holdDuration);
                float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
                float alpha = Mathf.SmoothStep(1f, 0f, t);
                float widthFactor = Mathf.Lerp(1f, 0.4f, t);

                SetBeamWidths(widthFactor);
                SetBeamAlpha(alpha);

                if (fadeElapsed >= fadeDuration)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void SetBeamWidths(float factor)
        {
            if (coreLine != null)
            {
                coreLine.startWidth = coreWidth * factor;
                coreLine.endWidth = coreWidth * factor * 0.95f;
            }
            if (glowLine != null)
            {
                glowLine.startWidth = glowWidth * factor;
                glowLine.endWidth = glowWidth * factor * 0.95f;
            }
        }

        private void SetBeamAlpha(float alpha)
        {
            if (coreLine != null)
            {
                Gradient g = new Gradient();
                g.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha * 0.9f, 0.85f), new GradientAlphaKey(0f, 1f) }
                );
                coreLine.colorGradient = g;
            }

            if (glowLine != null)
            {
                Gradient g = new Gradient();
                g.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(beamColor, 0f), new GradientColorKey(beamColor, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(alpha * 0.65f, 0f), new GradientAlphaKey(alpha * 0.55f, 0.85f), new GradientAlphaKey(0f, 1f) }
                );
                glowLine.colorGradient = g;
            }
        }
    }
}
