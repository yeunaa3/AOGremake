using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Summon Shot", fileName = "SummonShotPassive")]
    public sealed class SummonShotPassive : BowPassive
    {
        [Tooltip("Số phát bắn tích lũy để triệu hồi prefab.")]
        [SerializeField, Min(1)] private int shotsRequired = 5;
        [Tooltip("Prefab được triệu hồi khi tích đủ phát bắn.")]
        [SerializeField] private GameObject prefabToSummon;
        [Tooltip("Triệu hồi tại vị trí chân đối thủ (true) hay chân người bắn (false).")]
        [SerializeField] private bool summonAtTarget = true;
        [Tooltip("Độ lệch vị trí xuất hiện so với mục tiêu.")]
        [SerializeField] private Vector2 spawnOffset = Vector2.zero;

        public int ShotsRequired => shotsRequired;
        public GameObject PrefabToSummon => prefabToSummon;

        public override bool HasChargeRelease => true;
        public override string SpecialAnimationTrigger => "High";

        public override void ExecuteSpecialRelease(PlayerController player, BowRuntimeState state)
        {
            SummonPrefab(player, state);
            if (state != null)
            {
                state.shotCount = 0;
                state.specialSkillReady = false;
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
            if (player == null || prefabToSummon == null || state == null) return;

            if (state.shotCount < shotsRequired)
            {
                state.shotCount++;
                if (state.shotCount >= shotsRequired)
                {
                    state.specialSkillReady = true;
                }
            }
        }

        public void SummonPrefab(PlayerController player, BowRuntimeState state)
        {
            Vector3 basePos = (summonAtTarget && player.Enemy != null)
                ? player.Enemy.transform.position
                : player.transform.position;
            Vector3 spawnPos = basePos + (Vector3)spawnOffset;

            GameObject obj = SummonPool.Instance != null
                ? SummonPool.Instance.Spawn(prefabToSummon, spawnPos, Quaternion.identity)
                : Object.Instantiate(prefabToSummon, spawnPos, Quaternion.identity);

            if (obj != null)
            {
                SkillObject skillObj = obj.GetComponent<SkillObject>();
                if (skillObj != null)
                {
                    skillObj.Setup(player);
                }

                TrapObject trapObj = obj.GetComponent<TrapObject>();
                if (trapObj != null)
                {
                    trapObj.Setup(player);
                }

                if (state != null)
                {
                    state.RegisterSummon(obj);
                }
            }

            SkillAudioManager.PlaySummon();
        }

        public override float GetProgress(BowRuntimeState state)
        {
            if (state == null) return 0f;
            if (state.specialSkillReady)
            {
                return 1f;
            }
            return shotsRequired > 0 ? Mathf.Clamp01((float)state.shotCount / shotsRequired) : 1f;
        }
    }
}
