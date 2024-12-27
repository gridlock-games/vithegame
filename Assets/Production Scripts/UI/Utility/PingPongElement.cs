using UnityEngine;

namespace Vi.UI
{
    public class PingPongElement : MonoBehaviour
    {
        [SerializeField] private Vector2 pingPongLength;
        [SerializeField] private float pingPongSpeed = 1;

        private RectTransform rectTransform;
        private Vector2 originalAnchoredPosition;
        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            pingPongTime = 0;
        }

        private float pingPongTime;
        private void Update()
        {
            float xOffset = Mathf.Approximately(pingPongLength.x, 0) ? 0 : Mathf.PingPong(pingPongTime, Mathf.Abs(pingPongLength.x));
            float yOffset = Mathf.Approximately(pingPongLength.y, 0) ? 0 : Mathf.PingPong(pingPongTime, Mathf.Abs(pingPongLength.y));

            if (pingPongLength.x < 0) { xOffset *= -1; }
            if (pingPongLength.y < 0) { yOffset *= -1; }

            rectTransform.anchoredPosition = new Vector2(originalAnchoredPosition.x + xOffset,
                originalAnchoredPosition.y + yOffset);

            pingPongTime += Time.deltaTime * pingPongSpeed;
        }
    }
}