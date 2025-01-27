using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class TransitionController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup transitionGroup;
        [SerializeField] private Image topImage;
        [SerializeField] private Image bottomImage;

        public bool TransitionRunning { get; private set; }
        public bool TransitionPeakReached { get; private set; }

        private const float transitionSpeed = 3;
        private const float transitionPeakLimit = 540;
        private const float transitionValleyLimit = 1280;

        private void Start()
        {
            transitionGroup.alpha = 0;
            topImage.enabled = false;
            bottomImage.enabled = false;
        }

        public IEnumerator PlayTransition()
        {
            TransitionRunning = true;

            topImage.rectTransform.offsetMin = new Vector2(topImage.rectTransform.offsetMin.x, transitionValleyLimit);
            bottomImage.rectTransform.offsetMax = new Vector2(bottomImage.rectTransform.offsetMax.x, -transitionValleyLimit);

            topImage.enabled = true;
            bottomImage.enabled = true;

            transitionGroup.alpha = 1;

            float t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * transitionSpeed;
                t = Mathf.Clamp01(t);

                topImage.rectTransform.offsetMin = new Vector2(topImage.rectTransform.offsetMin.x, Mathf.Lerp(transitionValleyLimit, transitionPeakLimit, t));
                bottomImage.rectTransform.offsetMax = new Vector2(bottomImage.rectTransform.offsetMax.x, -Mathf.Lerp(transitionValleyLimit, transitionPeakLimit, t));
                yield return null;
            }

            TransitionPeakReached = true;
            yield return new WaitForSeconds(0.05f);
            yield return null;
            TransitionPeakReached = false;

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * transitionSpeed;
                t = Mathf.Clamp01(t);

                topImage.rectTransform.offsetMin = new Vector2(topImage.rectTransform.offsetMin.x, Mathf.Lerp(transitionPeakLimit, transitionValleyLimit, t));
                bottomImage.rectTransform.offsetMax = new Vector2(bottomImage.rectTransform.offsetMax.x, -Mathf.Lerp(transitionPeakLimit, transitionValleyLimit, t));
                yield return null;
            }

            transitionGroup.alpha = 0;

            topImage.enabled = false;
            bottomImage.enabled = false;

            TransitionRunning = false;
        }
    }
}