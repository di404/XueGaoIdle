using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace XueGao
{
    public class JuicyFeedbacks : MonoBehaviour
    {
        [SerializeField] private MMF_Player biteFeedbacks;
        [SerializeField] private MMF_Player completeFeedbacks;
        [SerializeField] private MMF_Player prizeFeedbacks;
        [SerializeField] private Transform iceCreamTarget;
        [SerializeField] private Transform stickTarget;
        [SerializeField] private CanvasGroup prizePanel;

        private Vector3 iceCreamBaseScale = Vector3.one;
        private Vector3 stickBaseScale = Vector3.one;

        private void Awake()
        {
            if (iceCreamTarget != null)
            {
                iceCreamBaseScale = iceCreamTarget.localScale;
            }

            if (stickTarget != null)
            {
                stickBaseScale = stickTarget.localScale;
            }
        }

        public void PlayBite()
        {
            biteFeedbacks?.PlayFeedbacks();
            if (iceCreamTarget != null)
            {
                StopCoroutine(nameof(PunchScale));
                StartCoroutine(PunchScale(iceCreamTarget, iceCreamBaseScale, 1.08f, 0.1f));
            }
        }

        public void PlayComplete()
        {
            completeFeedbacks?.PlayFeedbacks();
            if (stickTarget != null)
            {
                stickTarget.localScale = Vector3.zero;
                StartCoroutine(PunchScale(stickTarget, stickBaseScale, 1.25f, 0.28f));
            }
        }

        public void PlayPrize(bool isWin)
        {
            prizeFeedbacks?.PlayFeedbacks();
            if (prizePanel != null)
            {
                StartCoroutine(FadePrizePanel(isWin ? 1f : 0.75f));
            }
        }

        private IEnumerator PunchScale(Transform target, Vector3 baseScale, float peak, float duration)
        {
            float halfDuration = duration * 0.5f;
            for (float t = 0f; t < halfDuration; t += Time.deltaTime)
            {
                float ratio = t / halfDuration;
                target.localScale = Vector3.Lerp(baseScale, baseScale * peak, ratio);
                yield return null;
            }

            for (float t = 0f; t < halfDuration; t += Time.deltaTime)
            {
                float ratio = t / halfDuration;
                target.localScale = Vector3.Lerp(baseScale * peak, baseScale, ratio);
                yield return null;
            }

            target.localScale = baseScale;
        }

        private IEnumerator FadePrizePanel(float peakAlpha)
        {
            prizePanel.alpha = 0f;
            prizePanel.gameObject.SetActive(true);
            for (float t = 0f; t < 0.16f; t += Time.deltaTime)
            {
                prizePanel.alpha = Mathf.Lerp(0f, peakAlpha, t / 0.16f);
                yield return null;
            }

            prizePanel.alpha = peakAlpha;
        }
    }
}
