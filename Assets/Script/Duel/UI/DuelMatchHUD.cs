using TMPro;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class DuelMatchHUD : MonoBehaviour
    {
        [SerializeField] private GameController matchManager;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private DuelCharacter localPlayer;

        private void OnEnable()
        {
            if (matchManager == null) return;
            matchManager.StateChanged += HandleStateChanged;
            matchManager.CountdownChanged += HandleCountdownChanged;
            matchManager.MatchTimeChanged += HandleTimeChanged;
            matchManager.MatchFinished += HandleMatchFinished;
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        private void OnDisable()
        {
            if (matchManager == null) return;
            matchManager.StateChanged -= HandleStateChanged;
            matchManager.CountdownChanged -= HandleCountdownChanged;
            matchManager.MatchTimeChanged -= HandleTimeChanged;
            matchManager.MatchFinished -= HandleMatchFinished;
        }

        private void HandleStateChanged(DuelMatchState state)
        {
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(state == DuelMatchState.Countdown);
            }
        }

        private void HandleCountdownChanged(float remaining)
        {
            if (countdownText != null) countdownText.text = Mathf.CeilToInt(remaining).ToString();
        }

        private void HandleTimeChanged(float remaining)
        {
            if (timerText == null) return;
            int seconds = Mathf.CeilToInt(remaining);
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private void HandleMatchFinished(DuelCharacter winner)
        {
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultText == null) return;

            if (winner == null) resultText.text = "HÒA";
            else if (winner == localPlayer) resultText.text = "CHIẾN THẮNG";
            else resultText.text = "THẤT BẠI";
        }
    }
}
