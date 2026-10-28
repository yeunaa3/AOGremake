using System;
using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Gắn trên prefab hiệu ứng để tự động phát animation và trả về Pool sau khi xong,
    /// loại bỏ hoàn toàn hiện tượng chớp lại frame đầu trước khi biến mất.
    /// </summary>
    public sealed class PooledEffect : MonoBehaviour
    {
        [Tooltip("Thời gian hiển thị hiệu ứng (giây). Nếu <= 0 sẽ tự đo độ dài AnimationClip.")]
        [SerializeField] private float duration = 0f;

        private Animator anim;
        private SpriteRenderer sr;
        private Coroutine playRoutine;
        private Action<GameObject> onReturnToPool;
        private int defaultStateHash = 0;

        public float Duration { get => duration; set => duration = value; }

        private void Awake()
        {
            anim = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();

            if (anim != null)
            {
                AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
                defaultStateHash = state.shortNameHash;
                if (duration <= 0f && state.length > 0.05f)
                {
                    duration = state.length;
                }
            }
        }

        public void Play(Action<GameObject> returnCallback)
        {
            onReturnToPool = returnCallback;

            if (playRoutine != null) StopCoroutine(playRoutine);
            playRoutine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            if (sr != null)
            {
                sr.enabled = true;
            }

            if (anim != null)
            {
                if (defaultStateHash == 0)
                {
                    AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
                    defaultStateHash = state.shortNameHash;
                }

                if (defaultStateHash != 0)
                {
                    anim.Play(defaultStateHash, 0, 0f);
                }
                else
                {
                    anim.Rebind();
                }

                anim.SetTrigger("Play");
                anim.Update(0f);
            }

            // Tính thời gian của clip
            float clipLength = duration;
            if (clipLength <= 0f && anim != null)
            {
                AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
                clipLength = state.length > 0.05f ? state.length : 0.5f;
            }

            // Chờ hết thời gian animation
            yield return new WaitForSeconds(clipLength);

            // Tắt SpriteRenderer NGAY LẬP TỨC để triệt tiêu hiện tượng chớp frame đầu
            if (sr != null)
            {
                sr.sprite = null;
                sr.enabled = false;
            }

            playRoutine = null;

            if (onReturnToPool != null)
            {
                onReturnToPool.Invoke(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }
            if (sr != null)
            {
                sr.sprite = null;
                sr.enabled = false;
            }
        }
    }
}
