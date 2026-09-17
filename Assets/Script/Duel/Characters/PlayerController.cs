using System.Collections;
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
        private InputSource input;
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
        private ArrowEffect nextArrowEffect;
        private int nextEffectPower;
        private float nextEffectDuration;

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

        private void Awake()
        {
            input = GetComponent<InputSource>();
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
            if (!Alive || input == null)
            {
                move.Input(0f);
                anim.Values(false, stats.Stunned, stats.Silenced, action.ShieldOn, stats.ClipSpeed);
                return;
            }

            InputCmd cmd = input.ReadCommand();
            float direction = CanMove ? cmd.Move : 0f;
            move.Input(direction);
            if (move.Moving) action.CancelAttack();
            anim.Face(Mathf.Abs(direction) > 0.05f ? direction : Face);
            anim.Values(move.Moving, stats.Stunned, stats.Silenced, action.ShieldOn, stats.ClipSpeed);

            if (!CanSkill) return;
            if (cmd.DashPressed) { action.Dash(direction == 0f ? Face : direction); return; }
            if (cmd.ShieldPressed) { action.Shield(); return; }
            if (cmd.TryGetSkillSlot(out int slot)) action.Skill(slot);
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
            nextArrowEffect = ArrowEffect.None;
            nextEffectPower = 0;
            nextEffectDuration = 0f;
            ShowArrow(false);
        }

        public void Play(bool on)
        {
            playing = on && stats.Alive;
            move.Match(playing);
            ShowArrow(playing);
            if (!playing) action.Stop();
        }

        public void Shoot()
        {
            if (stats.ArrowSprite == null || Enemy == null || arrowPoint == null) return;
            Fire(Enemy, 0f);
        }

        public void Volley(PlayerController target, int count, float delay, float arcStep)
        {
            if (target != null) StartCoroutine(VolleyRoutine(target, count, delay, arcStep));
        }

        public void PowerNextArrow(ArrowEffect effect, int power, float duration)
        {
            nextArrowEffect = effect;
            nextEffectPower = power;
            nextEffectDuration = duration;
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

        private IEnumerator VolleyRoutine(PlayerController target, int count, float delay, float arcStep)
        {
            for (int i = 0; i < count; i++)
            {
                if (!Alive || target == null || !target.Stats.Alive) yield break;
                Fire(target, (i - (count - 1) * .5f) * arcStep);
                if (i < count - 1) yield return new WaitForSeconds(delay);
            }
        }

        private void Fire(PlayerController target, float arc)
        {
            Arrow arrow = pool == null ? null : pool.Spawn();
            if (arrow == null) return;

            arrow.Launch(this, arrowPoint.position, target.Aim, arc, stats.Damage, stats.ArrowSprite,
                nextArrowEffect, nextEffectPower, nextEffectDuration);
            nextArrowEffect = ArrowEffect.None;
            nextEffectPower = 0;
            nextEffectDuration = 0f;
        }

        private Transform Find(string name)
        {
            foreach (Transform item in GetComponentsInChildren<Transform>(true))
                if (string.Equals(item.name, name, System.StringComparison.OrdinalIgnoreCase)) return item;
            return null;
        }
    }
}
