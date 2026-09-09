using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Player), typeof(PlayerMove), typeof(PlayerAction))]
    public sealed class AutoShoot : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float idleDelay = 0.2f;
        private Player player;
        private PlayerMove move;
        private PlayerAction action;
        private float idleTimer;
        private float cooldown;

        private void Awake()
        {
            player = GetComponent<Player>();
            move = GetComponent<PlayerMove>();
            action = GetComponent<PlayerAction>();
        }

        private void Update()
        {
            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);
            if (!player.CanBasicAttack || move.IsMoving || !action.IsReady)
            {
                idleTimer = 0f;
                return;
            }
            idleTimer += Time.deltaTime;
            if (idleTimer < idleDelay || cooldown > 0f) return;
            if (action.TryAutoAttack())
            {
                cooldown = player.Stats.AttackInterval;
                idleTimer = 0f;
            }
        }
        public void ResetAttack() { idleTimer = 0f; cooldown = 0f; }
    }
}