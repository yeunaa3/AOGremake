using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Shield Shot", fileName = "ShieldShotPassive")]
    public sealed class ShieldShotPassive : BowPassive
    {
        [Tooltip("Số phát bắn tích lũy để triệu hồi khiên.")]
        [SerializeField, Min(1)] private int shotsRequired = 6;
        [Tooltip("Prefab của vật thể khiên che chắn (ShieldBarrier).")]
        [SerializeField] private GameObject shieldBarrierPrefab;
        [Tooltip("Thời gian tồn tại của khiên kể từ lần đầu tiên nhận dame (giây, mặc định 5s).")]
        [SerializeField, Min(1f)] private float immortalDuration = 5f;

        [Header("Tùy chọn Vị trí & Góc quay khiên")]
        [Tooltip("Nếu tích chọn, sẽ dùng giá trị Front / Height Offset & Rotation Angle tại đây thay vì lấy từ Prefab khiên.")]
        [SerializeField] private bool overrideTransformConfig = false;
        [Tooltip("Khoảng cách phía trước mặt nhân vật (mặc định 0.85).")]
        [SerializeField] private float frontOffset = 0.85f;
        [Tooltip("Độ cao lệch so với nhân vật (mặc định 0.35).")]
        [SerializeField] private float heightOffset = 0.35f;
        [Tooltip("Góc nghiêng của khiên (độ, trục Z, mặc định 0).")]
        [SerializeField] private float rotationAngle = 0f;

        public int ShotsRequired => shotsRequired;
        public GameObject ShieldBarrierPrefab => shieldBarrierPrefab;
        public float ImmortalDuration => immortalDuration;
        public bool OverrideTransformConfig => overrideTransformConfig;
        public float FrontOffset => frontOffset;
        public float HeightOffset => heightOffset;
        public float RotationAngle => rotationAngle;

        public override bool HasChargeRelease => true;
        public override string SpecialAnimationTrigger => "High";

        public override void ExecuteSpecialRelease(PlayerController player, BowRuntimeState state)
        {
            SummonShieldBarrier(player, state);
            if (state != null)
            {
                state.shotCount = 0;
                state.specialSkillReady = false;
            }
        }

        public override void OnEquip(PlayerController player, BowRuntimeState state)
        {
            if (state != null)
            {
                state.shotCount = 0;
            }
        }

        public override void OnUnequip(PlayerController player, BowRuntimeState state)
        {
            if (state != null)
            {
                for (int i = 0; i < state.activeSummons.Count; i++)
                {
                    if (state.activeSummons[i] != null)
                    {
                        if (SummonPool.Instance != null) SummonPool.Instance.Return(state.activeSummons[i]);
                        else Destroy(state.activeSummons[i]);
                    }
                }
                state.Reset();
            }
        }

        public override void OnUpdate(PlayerController player, BowRuntimeState state, float deltaTime)
        {
        }

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isNormalShot)
        {
            if (player == null || !player.Alive || state == null) return;

            if (state.shotCount < shotsRequired)
            {
                state.shotCount++;
                if (state.shotCount >= shotsRequired)
                {
                    state.specialSkillReady = true;
                }
            }
        }

        public void SummonShieldBarrier(PlayerController player, BowRuntimeState state)
        {
            if (shieldBarrierPrefab == null || player == null) return;

            ShieldBarrierObject prefabBarrier = shieldBarrierPrefab.GetComponent<ShieldBarrierObject>();
            float targetFrontOffset = overrideTransformConfig ? frontOffset : (prefabBarrier != null ? prefabBarrier.FrontOffset : 0.85f);
            float targetHeightOffset = overrideTransformConfig ? heightOffset : (prefabBarrier != null ? prefabBarrier.HeightOffset : 0.35f);
            float targetRotationAngle = overrideTransformConfig ? rotationAngle : (prefabBarrier != null ? prefabBarrier.RotationAngle : 0f);

            float faceSign = player.Face >= 0f ? 1f : -1f;
            Vector3 spawnPosition = player.transform.position + new Vector3(faceSign * targetFrontOffset, targetHeightOffset, 0f);
            Quaternion spawnRotation = Quaternion.Euler(0f, 0f, faceSign * targetRotationAngle);

            GameObject spawnedObject = SummonPool.Instance != null
                ? SummonPool.Instance.Spawn(shieldBarrierPrefab, spawnPosition, spawnRotation)
                : Object.Instantiate(shieldBarrierPrefab, spawnPosition, spawnRotation);

            if (spawnedObject != null)
            {
                ShieldBarrierObject barrierObject = spawnedObject.GetComponent<ShieldBarrierObject>();
                if (barrierObject != null)
                {
                    if (overrideTransformConfig)
                    {
                        barrierObject.SetTransformConfig(targetFrontOffset, targetHeightOffset, targetRotationAngle);
                    }
                    barrierObject.Setup(player);
                }
                else
                {
                    SkillObject skillObject = spawnedObject.GetComponent<SkillObject>();
                    skillObject?.Setup(player);
                }

                if (state != null)
                {
                    state.RegisterSummon(spawnedObject);
                }
            }

            SkillAudioManager.PlaySummon();
        }

        private ShieldBarrierObject GetActiveShield(BowRuntimeState state)
        {
            if (state == null) return null;
            for (int i = state.activeSummons.Count - 1; i >= 0; i--)
            {
                GameObject obj = state.activeSummons[i];
                if (obj == null || !obj.activeInHierarchy)
                {
                    state.activeSummons.RemoveAt(i);
                    continue;
                }
                ShieldBarrierObject s = obj.GetComponent<ShieldBarrierObject>();
                if (s != null)
                {
                    if (s.IsDead)
                    {
                        state.activeSummons.RemoveAt(i);
                        continue;
                    }
                    return s;
                }
            }
            return null;
        }

        public override float GetProgress(BowRuntimeState state)
        {
            if (state == null) return 0f;
            if (state.specialSkillReady) return 1f;
            return shotsRequired > 0 ? Mathf.Clamp01((float)state.shotCount / shotsRequired) : 1f;
        }
    }
}
