using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerStats))]
    public sealed class PlayerMove : MonoBehaviour
    {
        private Rigidbody2D body;
        private PlayerStats stats;
        private float input;
        private bool matchOn;
        private bool canMove = true;
        private bool dashing;
        private float dashDir;
        private float dashSpeed;

        public bool Moving => dashing || Mathf.Abs(input) > 0.05f;
        public bool Dashing => dashing;

        private void Awake() { body = GetComponent<Rigidbody2D>(); stats = GetComponent<PlayerStats>(); }
        private void FixedUpdate()
        {
            float x = matchOn ? (dashing ? dashDir * dashSpeed : canMove ? input * stats.MoveSpeed : 0f) : 0f;
            body.linearVelocity = new Vector2(x, body.linearVelocity.y);
        }

        public void Input(float value) => input = Mathf.Clamp(value, -1f, 1f);
        public void Match(bool on) { matchOn = on; if (!on) Stop(); }
        public void Lock(bool value) => canMove = !value;
        public void Dash(float direction, float speed) { dashDir = Mathf.Sign(direction == 0 ? 1 : direction); dashSpeed = speed; dashing = true; }
        public void Stop() { input = 0f; dashing = false; }
    }
}