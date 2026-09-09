using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Stats))]
    [RequireComponent(typeof(Status))]
    [RequireComponent(typeof(Gear))]
    [RequireComponent(typeof(PlayerMove))]
    [RequireComponent(typeof(PlayerAction))]
    [RequireComponent(typeof(AutoShoot))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerAnim))]
    public sealed class Player : MonoBehaviour
    {
        [SerializeField] private int teamId;

        private InputSource input;
        private PlayerMove move;
        private PlayerAction action;
        private AutoShoot autoShoot;
        private Health health;
        private PlayerAnim anim;
        private ArrowPool arrowPool;
        private Stats stats;
        private Status status;
        private Gear gear;
        private Transform arrowPoint;
        private Transform aimPoint;
        private GameObject loadedArrow;
        private GameManager game;
        private bool matchActive;

        public int TeamId => teamId;
        public Player Opponent { get; private set; }
        public PlayerMove Move => move;
        public PlayerAction ActionController => action;
        public Health Health => health;
        public PlayerAnim Anim => anim;
        public Stats Stats => stats;
        public Status Status => status;
        public Gear Gear => gear;
        public bool CanReceiveInput => matchActive && health.IsAlive;
        public bool CanMove => CanReceiveInput && status.CanMove;
        public bool CanBasicAttack => CanReceiveInput && status.CanBasicAttack;
        public bool CanUseSpecialActions => CanReceiveInput && status.CanUseSpecialActions;
        public Vector2 AimTargetPosition => aimPoint != null ? aimPoint.position : transform.position;
        public float FacingDirection => Opponent == null
            ? (teamId == 0 ? 1f : -1f)
            : Mathf.Sign(Opponent.transform.position.x - transform.position.x);

        private void Awake()
        {
            input = GetComponent<InputSource>();
            move = GetComponent<PlayerMove>();
            action = GetComponent<PlayerAction>();
            autoShoot = GetComponent<AutoShoot>();
            health = GetComponent<Health>();
            anim = GetComponent<PlayerAnim>();
            stats = GetComponent<Stats>();
            status = GetComponent<Status>();
            gear = GetComponent<Gear>();
            arrowPoint = FindChild("arrowspawnpoint");
            aimPoint = FindChild("aimtarget");
            Transform arrowVisual = FindChild("arrowVisual");
            loadedArrow = arrowVisual != null ? arrowVisual.gameObject : null;
            SetLoadedArrowVisible(false);
        }

        private void Update()
        {
            anim.SetFacing(FacingDirection);
            anim.SetAttackSpeed(stats.AttackAnimationSpeed);

            if (!CanReceiveInput || input == null)
            {
                move.SetMovementInput(0f);
                anim.SetMoving(false);
                return;
            }

            InputCmd command = input.ReadCommand();
            float permittedMove = CanMove ? command.Move : 0f;
            move.SetMovementInput(permittedMove);
            anim.SetMoving(Mathf.Abs(permittedMove) > 0.05f || move.IsDashing);

            if (!CanUseSpecialActions) return;
            if (command.DashPressed)
            {
                action.TryDash(command.Move, FacingDirection);
                return;
            }
            if (command.ShieldPressed)
            {
                action.TryShield();
                return;
            }
            if (command.TryGetSkillSlot(out int slot)) action.TrySkill(slot);
        }

        public void ConfigureForMatch(GameManager manager, Player opponent, int assignedTeamId, ArrowPool sharedArrowPool)
        {
            game = manager;
            Opponent = opponent;
            teamId = assignedTeamId;
            arrowPool = sharedArrowPool;
        }

        public void PrepareForMatch()
        {
            matchActive = false;
            move.SetGameplayMovementAllowed(false);
            action.ResetActions();
            autoShoot.ResetAttack();
            status.ResetStatuses();
            stats.ResetRuntimeModifiers();
            health.ResetHealth();
            SetLoadedArrowVisible(false);
        }

        public void SetMatchActive(bool active)
        {
            matchActive = active && health.IsAlive;
            move.SetGameplayMovementAllowed(matchActive);
            if (!matchActive)
            {
                action.StopForMatchEnd();
                SetLoadedArrowVisible(false);
            }
            else
            {
                SetLoadedArrowVisible(true);
            }
        }

        public void FireBasicArrow()
        {
            ArrowData arrowData = stats.BasicAttackProjectile;
            if (arrowData == null || Opponent == null || !health.IsAlive) return;
            SpawnArrow(arrowData, Opponent, 0f, stats.BasicAttackDamage);
        }

        public void FireVolley(ArrowData arrowData, Player target, int count, float delay, float arcStep)
        {
            if (arrowData == null || target == null) return;
            StartCoroutine(FireVolleyRoutine(arrowData, target, Mathf.Max(1, count), Mathf.Max(0f, delay), arcStep));
        }

        public void SetLoadedArrowVisible(bool visible)
        {
            if (loadedArrow != null) loadedArrow.SetActive(visible);
        }

        public void HandleDeath()
        {
            matchActive = false;
            SetLoadedArrowVisible(false);
            move.SetGameplayMovementAllowed(false);
            action.MarkDead();
            game?.NotifyCharacterDied(this);
        }

        private IEnumerator FireVolleyRoutine(ArrowData arrowData, Player target, int count, float delay, float arcStep)
        {
            float center = (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                if (!CanReceiveInput || target == null || !target.Health.IsAlive) yield break;
                SpawnArrow(arrowData, target, (i - center) * arcStep, arrowData.Damage);
                if (delay > 0f && i < count - 1) yield return new WaitForSeconds(delay);
            }
        }

        private void SpawnArrow(ArrowData data, Player target, float arcOffset, int damage)
        {
            if (arrowPoint == null || data.ProjectilePrefab == null) return;
            Arrow arrow = arrowPool != null ? arrowPool.Spawn(data) : Instantiate(data.ProjectilePrefab);
            if (arrow == null) return;
            arrow.Launch(this, data, arrowPoint.position, target.AimTargetPosition, arcOffset, damage, null);
        }

        private Transform FindChild(string childName)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase)) return child;
            }
            return null;
        }
    }
}