using UnityEngine;

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
        private Transform arrowPoint;
        private Transform aimPoint;
        private GameObject loadedArrow;
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
            arrowPoint = Find("arrowspawnpoint");
            aimPoint = Find("aimtarget");
            Transform arrow = Find("arrowVisual");
            loadedArrow = arrow == null ? null : arrow.gameObject;
            ShowArrow(false);
        }

        private void Update()
        {
            if (!Alive || inputs == null || inputs.Length == 0)
            {
                move.Input(0f);
                anim.Values(false, stats.Stunned, stats.Silenced, action.ShieldOn, stats.ClipSpeed);
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
            float direction = CanMove ? cmd.Move : 0f;
            move.Input(direction);
            if (move.Moving) action.CancelAttack();
            anim.Face(Mathf.Abs(direction) > 0.05f ? direction : Face);
            anim.Values(move.Moving, stats.Stunned, stats.Silenced, action.ShieldOn, stats.ClipSpeed);

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
            ShowArrow(false);
        }

        public void Play(bool on)
        {
            playing = on && stats.Alive;
            move.Match(playing);
            ShowArrow(playing);
            if (playing) action.StartPassives();
            if (!playing) action.Stop();
        }

        public void Shoot()
        {
            if (stats.ArrowSprite == null || Enemy == null || arrowPoint == null) return;
            bool powered = poweredArrowCount > 0;
            float damageMultiplier = powered ? arrowBuff.damageMultiplier : 1f;
            bool fired = Fire(arrowPoint.position, Enemy.Aim, ProjectilePath.NormalArc, 0f,
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
                Vector2 start = arrowPoint != null ? arrowPoint.position : transform.position;
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

        public void ShowArrow(bool show)
        {
            if (loadedArrow != null) loadedArrow.SetActive(show);
        }

        public void Hit() => anim.Trigger("Hit");

        public void Die()
        {
            playing = false;
            move.Match(false);
            ShowArrow(false);
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
