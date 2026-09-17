using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(PlayerController), typeof(PlayerMove), typeof(PlayerAction))]
    public sealed class AutoShoot : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float waitBeforeShoot = .2f;
        private PlayerController player;
        private PlayerMove move;
        private PlayerAction action;
        private float wait;

        private void Awake() { player = GetComponent<PlayerController>(); move = GetComponent<PlayerMove>(); action = GetComponent<PlayerAction>(); }
        private void Update()
        {
            if (!player.CanAttack || move.Moving || !action.Ready) { wait = 0f; return; }
            wait += Time.deltaTime;
            if (wait < waitBeforeShoot) return;
            if (action.Attack()) wait = 0f;
        }
        public void ResetAttack() => wait = 0f;
    }
}
