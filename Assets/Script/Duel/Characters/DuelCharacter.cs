using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class DuelCharacter : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int teamId;

        [Header("References")]
        [SerializeField] private DuelInputSource inputSource;
        [SerializeField] private DuelCharacterMotor motor;
        [SerializeField] private DuelCharacterActionController actionController;
        [SerializeField] private DuelAutoAttackController autoAttackController;
        [SerializeField] private DuelHealth health;
        [SerializeField] private DuelCharacterAnimator animationController;
        [SerializeField] private ProjectilePool projectilePool;

        [Header("Combat points")]
        [SerializeField] private Transform arrowSpawnPoint;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private GameObject loadedArrowVisual;
        [SerializeField] private DuelProjectileDefinition basicArrow;

        private GameController matchManager;
        private bool matchActive;

        public int TeamId => teamId;
        public DuelCharacter Opponent { get; private set; }
        public DuelCharacterMotor Motor => motor;
        public DuelCharacterActionController ActionController => actionController;
        public DuelHealth Health => health;
        public DuelCharacterAnimator AnimationController => animationController;
        public bool CanReceiveInput => matchActive && health != null && health.IsAlive;
        public Vector2 AimTargetPosition => aimTarget != null ? aimTarget.position : transform.position;
        public float FacingDirection
        {
            get
            {
                if (Opponent == null) return teamId == 0 ? 1f : -1f;
                return Mathf.Sign(Opponent.transform.position.x - transform.position.x);
            }
        }

        private void Awake()
        {
            if (motor == null) motor = GetComponent<DuelCharacterMotor>();
            if (actionController == null) actionController = GetComponent<DuelCharacterActionController>();
            if (autoAttackController == null) autoAttackController = GetComponent<DuelAutoAttackController>();
            if (health == null) health = GetComponent<DuelHealth>();
            if (animationController == null) animationController = GetComponent<DuelCharacterAnimator>();
            if (inputSource == null) inputSource = GetComponent<DuelInputSource>();
            SetLoadedArrowVisible(false);
        }

        private void Update()
        {
            animationController.SetFacing(FacingDirection);

            if (!CanReceiveInput || inputSource == null)
            {
                motor.SetMovementInput(0f);
                animationController.SetMoving(false);
                return;
            }

            DuelPlayerCommand command = inputSource.ReadCommand();
            motor.SetMovementInput(command.Move);
            animationController.SetMoving(Mathf.Abs(command.Move) > 0.05f || motor.IsDashing);

            // Chỉ hành động đầu tiên trong frame được xét; controller tiếp tục bảo đảm khóa độc quyền.
            if (command.DashPressed)
            {
                actionController.TryDash(command.Move, FacingDirection);
                return;
            }

            if (command.ShieldPressed)
            {
                actionController.TryShield();
                return;
            }

            if (command.TryGetSkillSlot(out int slot))
            {
                actionController.TrySkill(slot);
            }
        }

        public void ConfigureForMatch(
            GameController manager,
            DuelCharacter opponent,
            int assignedTeamId,
            ProjectilePool sharedProjectilePool)
        {
            matchManager = manager;
            Opponent = opponent;
            teamId = assignedTeamId;
            if (sharedProjectilePool != null) projectilePool = sharedProjectilePool;
        }

        public void PrepareForMatch()
        {
            matchActive = false;
            motor.SetGameplayMovementAllowed(false);
            actionController.ResetActions();
            autoAttackController.ResetAttack();
            health.ResetHealth();
            SetLoadedArrowVisible(false);
        }

        public void SetMatchActive(bool active)
        {
            matchActive = active && health.IsAlive;
            motor.SetGameplayMovementAllowed(matchActive);
            if (!matchActive)
            {
                actionController.StopForMatchEnd();
                SetLoadedArrowVisible(false);
            }
        }

        public void FireBasicArrow()
        {
            if (basicArrow != null && Opponent != null && health.IsAlive)
            {
                SpawnArrow(basicArrow, Opponent, 0f);
            }
        }

        public void FireVolley(
            DuelProjectileDefinition projectile,
            DuelCharacter target,
            int projectileCount,
            float delayBetweenProjectiles,
            float arcHeightStep)
        {
            if (projectile == null || target == null) return;
            StartCoroutine(FireVolleyRoutine(
                projectile,
                target,
                Mathf.Max(1, projectileCount),
                Mathf.Max(0f, delayBetweenProjectiles),
                arcHeightStep));
        }

        public void SetLoadedArrowVisible(bool visible)
        {
            if (loadedArrowVisual != null)
            {
                loadedArrowVisual.SetActive(visible);
            }
        }

        public void HandleDeath()
        {
            matchActive = false;
            SetLoadedArrowVisible(false);
            motor.SetGameplayMovementAllowed(false);
            actionController.MarkDead();
            matchManager?.NotifyCharacterDied(this);
        }

        private IEnumerator FireVolleyRoutine(
            DuelProjectileDefinition projectile,
            DuelCharacter target,
            int count,
            float delay,
            float arcHeightStep)
        {
            float center = (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                if (!CanReceiveInput || target == null || !target.Health.IsAlive)
                {
                    yield break;
                }

                float arcOffset = (i - center) * arcHeightStep;
                SpawnArrow(projectile, target, arcOffset);

                if (delay > 0f && i < count - 1)
                {
                    yield return new WaitForSeconds(delay);
                }
            }
        }

        private void SpawnArrow(
            DuelProjectileDefinition definition,
            DuelCharacter target,
            float arcHeightOffset)
        {
            if (arrowSpawnPoint == null || definition.ProjectilePrefab == null) return;

            ArrowProjectile projectile = projectilePool != null
                ? projectilePool.Spawn(definition)
                : Instantiate(definition.ProjectilePrefab);

            if (projectile == null) return;

            Vector2 start = arrowSpawnPoint.position;
            Vector2 targetSnapshot = target.AimTargetPosition;
            projectile.Launch(this, definition, start, targetSnapshot, arcHeightOffset, null);
        }
    }
}
