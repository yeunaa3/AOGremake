using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace AOG.Duel
{
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(Gear))]
    [RequireComponent(typeof(PlayerMove))]
    [RequireComponent(typeof(PlayerAction))]
    [RequireComponent(typeof(AutoShoot))]
    [RequireComponent(typeof(PlayerAnim))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private int team;
        private InputSource[] inputs;
        private PlayerStats stats;
        private PlayerMove move;
        private PlayerAction action;
        private AutoShoot autoShoot;
        private PlayerAnim anim;
        private ArrowPool pool;
        private GameManager game;
        private Transform arrowHoldPoint;
        private Transform arrowSpawnPoint;
        private Transform aimPoint;
        private Transform summonPoint;
        private SpriteRenderer bowVisual;
        private readonly List<SpriteRenderer> heldArrows = new List<SpriteRenderer>();
        [Header("Tên đang cầm (tạo bằng code)")]
        [FormerlySerializedAs("heldArrowScale")]
        [Tooltip("Hệ số chỉnh thêm sau khi code đã tự khớp kích thước với prefab tên bay. Để (1, 1) nếu không cần chỉnh.")]
        [SerializeField] private Vector2 heldArrowScaleMultiplier = Vector2.one;
        [SerializeField] private int heldArrowSortingOrder = 4;
        private bool playing;
        private int poweredArrowCount;
        private ArrowBuffData arrowBuff;
        private int arrowBuffSkillSlot = -1;

        public int Team => team;
        public PlayerController Enemy { get; private set; }
        public PlayerStats Stats => stats;
        public PlayerAction Action => action;
        public bool Alive => playing && stats.Alive;
        public bool CanMove => Alive && stats.CanMove;
        public bool CanAttack => Alive && stats.CanAttack;
        public bool CanSkill => Alive && stats.CanUseSkill;
        public float Face => Enemy == null ? (team == 0 ? 1f : -1f) : Mathf.Sign(Enemy.transform.position.x - transform.position.x);
        public Vector2 Aim => aimPoint == null ? transform.position : aimPoint.position;
        public bool IsAirborne => move.IsAirborne;

        private void Awake()
        {
            inputs = GetComponents<InputSource>();
            stats = GetComponent<PlayerStats>();
            move = GetComponent<PlayerMove>();
            action = GetComponent<PlayerAction>();
            autoShoot = GetComponent<AutoShoot>();
            anim = GetComponent<PlayerAnim>();
            arrowHoldPoint = Find("arrowholdpoint");
            arrowSpawnPoint = Find("arrowspawnpoint");
            // Prefab cũ chưa tách điểm vẫn hoạt động được.
            if (arrowHoldPoint == null) arrowHoldPoint = arrowSpawnPoint;
            aimPoint = Find("aimtarget");
            summonPoint = Find("summonpoint");
            Transform bow = Find("bow");
            bowVisual = bow == null ? null : bow.GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (!Alive || inputs == null || inputs.Length == 0)
            {
                move.Input(0f);
                anim.Values(false, stats.Stunned, stats.Frozen, stats.Silenced, action.ShieldOn, stats.ClipSpeed);
                return;
            }

            InputCmd cmd = default;
            foreach (InputSource source in inputs)
            {
                if (source == null || !source.isActiveAndEnabled) continue;
                InputCmd next = source.ReadCommand();
                if (Mathf.Abs(next.Move) > Mathf.Abs(cmd.Move)) cmd.Move = next.Move;
                cmd.DashPressed |= next.DashPressed;
                cmd.ShieldPressed |= next.ShieldPressed;
                cmd.Skill1Pressed |= next.Skill1Pressed;
                cmd.Skill2Pressed |= next.Skill2Pressed;
                cmd.Skill3Pressed |= next.Skill3Pressed;
                cmd.Skill4Pressed |= next.Skill4Pressed;
            }
            bool usingSkill = action.State == ActionState.UsingSkill;
            float direction = CanMove && !usingSkill ? cmd.Move : 0f;
            move.Input(direction);
            if (move.Moving) action.CancelAttack();
            // Trong lúc dùng skill luôn nhìn Enemy; phím A/D không được đổi hướng.
            anim.Face(usingSkill ? Face : Mathf.Abs(direction) > 0.05f ? direction : Face);
            anim.Values(move.Moving, stats.Stunned, stats.Frozen, stats.Silenced, action.ShieldOn, stats.ClipSpeed);

            if (!CanSkill) return;
            if (cmd.DashPressed) { action.Dash(direction == 0f ? Face : direction); return; }
            if (cmd.ShieldPressed) { action.Shield(); return; }
            if (cmd.TryGetSkillSlot(out int slot)) action.Skill(slot, direction == 0f ? Face : direction);
        }

        public void Setup(GameManager manager, PlayerController enemy, int id, ArrowPool arrowPool)
        {
            game = manager; Enemy = enemy; team = id; pool = arrowPool;
        }

        public void ResetPlayer()
        {
            playing = false;
            move.Match(false);
            action.ResetAction();
            autoShoot.ResetAttack();
            stats.ResetAll();
            poweredArrowCount = 0;
            arrowBuff = null;
            arrowBuffSkillSlot = -1;
        }

        public void Play(bool on)
        {
            playing = on && stats.Alive;
            move.Match(playing);
            if (playing) action.StartPassives();
            if (!playing) action.Stop();
        }

        public void Shoot()
        {
            if (stats.ArrowSprite == null || Enemy == null || arrowSpawnPoint == null) return;
            bool powered = poweredArrowCount > 0;
            float damageMultiplier = powered ? arrowBuff.damageMultiplier : 1f;
            bool fired = Fire(arrowSpawnPoint.position, Enemy.Aim, ProjectilePath.NormalArc, 0f,
                Mathf.RoundToInt(stats.Damage * damageMultiplier), stats.ArrowSprite,
                powered ? arrowBuff.speedMultiplier : 1f,
                powered ? arrowBuff.effect : ArrowEffect.None,
                powered ? arrowBuff.effectPower : 0,
                powered ? arrowBuff.effectDuration : 0f,
                powered ? arrowBuff.lifeSteal : 0f,
                powered ? arrowBuff.worldEffectPrefab : null,
                powered && arrowBuff.resetCooldownOnHit ? arrowBuffSkillSlot : -1);

            if (fired && powered)
            {
                poweredArrowCount--;
                if (poweredArrowCount == 0)
                {
                    arrowBuff = null;
                    arrowBuffSkillSlot = -1;
                }
            }
        }

        public void ApplyArrowBuff(ArrowBuffData buff, int skillSlot)
        {
            if (buff == null) return;
            arrowBuff = buff;
            arrowBuffSkillSlot = skillSlot;
            poweredArrowCount = Mathf.Max(1, buff.arrowCount);
        }

        public void StartSkillMove(float direction, float speed)
        {
            move.Lock(true);
            move.Dash(direction, speed);
        }

        public void ShootSkill(ShotData shot, int skillSlot)
        {
            if (shot == null || Enemy == null || (shot.onlyHitAirborne && !Enemy.IsAirborne)) return;
            int count = Mathf.Max(1, shot.count);
            int damage = Mathf.RoundToInt(stats.Damage * Mathf.Max(0f, shot.damageMultiplier));
            Sprite sprite = stats.ArrowSprite;
            float first = -(count - 1) * shot.spacing * 0.5f;

            if (shot.path == ProjectilePath.Beam)
            {
                int dealt = Enemy.Stats.TakeDamage(new DamageInfo(this, damage, false, Enemy.Aim,
                    shot.effect, shot.effectPower, shot.effectDuration));
                if (dealt > 0 && shot.lifeSteal > 0f) stats.Heal(Mathf.RoundToInt(dealt * shot.lifeSteal));
                if (dealt > 0 && shot.resetCooldownOnHit) action.ResetSkillCooldown(skillSlot);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                float spread = first + i * shot.spacing;
                Vector2 target = Enemy.Aim;
                Vector2 start = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position;
                if (shot.spawnNearTarget || shot.path == ProjectilePath.Falling)
                {
                    start = Enemy.Aim + new Vector2(shot.spawnOffset.x * Face, shot.spawnOffset.y);
                }
                else
                {
                    start += new Vector2(shot.spawnOffset.x * Face, shot.spawnOffset.y);
                }

                if (shot.formation == ShotFormation.Fan)
                {
                    target += Vector2.up * spread;
                }
                else if (shot.formation == ShotFormation.ParallelHorizontal)
                {
                    start += Vector2.right * spread;
                    target += Vector2.right * spread;
                }
                else
                {
                    start += Vector2.up * spread;
                    target += Vector2.up * spread;
                }

                Fire(start, target, shot.path, shot.arc, damage, sprite, shot.speedMultiplier,
                    shot.effect, shot.effectPower, shot.effectDuration, shot.lifeSteal,
                    shot.worldEffectPrefab, shot.resetCooldownOnHit ? skillSlot : -1);
            }
        }

        // Dùng cho các skill bắn một mũi tên từ cung và có hiệu ứng riêng.
        public bool ShootArrow(ProjectilePath path, float damageMultiplier, float speedMultiplier,
            ArrowEffect effect, int effectPower, float effectDuration)
        {
            if (Enemy == null || arrowSpawnPoint == null || stats.ArrowSprite == null) return false;
            return Fire(arrowSpawnPoint.position, Enemy.Aim, path, 0f,
                Mathf.RoundToInt(stats.Damage * Mathf.Max(0f, damageMultiplier)),
                stats.ArrowSprite, Mathf.Max(.01f, speedMultiplier), effect,
                effectPower, effectDuration, 0f, null, -1);
        }

        public void SpawnSkillObject(GameObject prefab, bool nearTarget, Vector2 offset, int count, float spacing)
        {
            if (prefab == null) return;
            Vector2 center = nearTarget && Enemy != null ? Enemy.transform.position : transform.position;
            center += new Vector2(offset.x * Face, offset.y);
            float first = -(Mathf.Max(1, count) - 1) * spacing * 0.5f;
            for (int i = 0; i < Mathf.Max(1, count); i++)
            {
                GameObject created = Instantiate(prefab, center + Vector2.right * (first + i * spacing), Quaternion.identity);
                SkillObject skillObject = created.GetComponent<SkillObject>();
                if (skillObject != null) skillObject.Setup(this);
            }
        }

        // Hệ spawn mới: prefab tự quyết định hành vi; Skill chỉ chọn điểm xuất hiện và số lượng.
        public void Spawn(SpawnData data, int skillSlot)
        {
            if (data == null || data.prefab == null || Enemy == null) return;

            Vector2 start;
            if (data.spawnPoint == SpawnPoint.AboveEnemy)
                start = Enemy.summonPoint != null ? Enemy.summonPoint.position : Enemy.transform.position + Vector3.up * 3f;
            else if (data.spawnPoint == SpawnPoint.AbovePlayer)
                start = summonPoint != null ? summonPoint.position : transform.position + Vector3.up * 3f;
            else
                start = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position;
            start += new Vector2(data.offset.x * Face, data.offset.y);

            int count = Mathf.Max(1, data.count);
            Arrow projectilePrefab = data.prefab.GetComponent<Arrow>();
            if (projectilePrefab != null)
            {
                for (int i = 0; i < count; i++)
                {
                    float targetY = count == 1 ? 0f : Mathf.Lerp(-data.targetRadius, data.targetRadius, i / (count - 1f));
                    Arrow projectile = pool == null ? null : pool.Spawn(projectilePrefab);
                    if (projectile == null) continue;
                    Sprite sprite = stats.ArrowSprite;
                    projectile.Launch(this, start, Enemy.Aim + Vector2.up * targetY,
                        data.path, data.arc,
                        Mathf.RoundToInt(stats.Damage * data.damageMultiplier), sprite,
                        data.speedMultiplier,
                        data.hasEffect ? data.effect : ArrowEffect.None,
                        data.effectPower, data.effectDuration,
                        shotResetCooldownSlot: -1);
                }
                return;
            }

            float first = -(count - 1) * data.spacing * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector2 position = start + Vector2.right * (first + i * data.spacing);
                GameObject created = Instantiate(data.prefab, position, Quaternion.identity);
                SkillObject skillObject = created.GetComponent<SkillObject>();
                if (skillObject != null) skillObject.Setup(this);
            }
        }

        public void ShowHeldArrows(int amount, float fanAngle)
        {
            HideHeldArrows();
            if (arrowHoldPoint == null) return;

            Sprite sprite = stats.ArrowSprite;
            if (sprite == null) return;

            int count = Mathf.Max(1, amount);
            Vector3 projectileScale = pool == null ? Vector3.one : pool.DefaultProjectileScale;
            Vector3 parentScale = arrowHoldPoint.lossyScale;
            Vector3 matchingLocalScale = new Vector3(
                projectileScale.x / Mathf.Max(.0001f, Mathf.Abs(parentScale.x)),
                projectileScale.y / Mathf.Max(.0001f, Mathf.Abs(parentScale.y)),
                1f);
            while (heldArrows.Count < count)
            {
                GameObject item = new GameObject("HeldArrow_" + heldArrows.Count);
                item.transform.SetParent(arrowHoldPoint, false);
                heldArrows.Add(item.AddComponent<SpriteRenderer>());
            }

            for (int i = 0; i < count; i++)
            {
                SpriteRenderer item = heldArrows[i];
                float angle = count == 1 ? 0f : Mathf.Lerp(-fanAngle * .5f, fanAngle * .5f, i / (count - 1f));
                item.sprite = sprite;
                item.sortingLayerID = bowVisual != null ? bowVisual.sortingLayerID : 0;
                item.sortingOrder = heldArrowSortingOrder;
                item.transform.localPosition = Vector3.zero;
                item.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                item.transform.localScale = new Vector3(
                    matchingLocalScale.x * heldArrowScaleMultiplier.x,
                    matchingLocalScale.y * heldArrowScaleMultiplier.y,
                    1f);
                item.gameObject.SetActive(true);
            }
        }

        public void HideHeldArrows()
        {
            foreach (SpriteRenderer item in heldArrows)
                if (item != null) item.gameObject.SetActive(false);
        }

        public void Hit() => anim.Trigger("Hit");

        public void Die()
        {
            playing = false;
            move.Match(false);
            action.Die();
            game?.NotifyCharacterDied(this);
        }

        private bool Fire(Vector2 start, Vector2 target, ProjectilePath path, float arc,
            int damage, Sprite sprite, float speedMultiplier, ArrowEffect effect,
            int effectPower, float effectDuration, float lifeSteal,
            GameObject worldEffect, int resetCooldownSlot)
        {
            Arrow arrow = pool == null ? null : pool.Spawn();
            if (arrow == null || sprite == null) return false;

            arrow.Launch(this, start, target, path, arc, damage, sprite, speedMultiplier,
                effect, effectPower, effectDuration, lifeSteal, worldEffect, resetCooldownSlot);
            return true;
        }

        private Transform Find(string name)
        {
            foreach (Transform item in GetComponentsInChildren<Transform>(true))
                if (string.Equals(item.name, name, System.StringComparison.OrdinalIgnoreCase)) return item;
            return null;
        }
    }
}
