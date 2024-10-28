using UnityEngine;
using System.Collections;
using Vi.Utility;

namespace Vi.ProceduralAnimations
{
    // This component should be attached to the upper lid
    public class EyelidAnimator : MonoBehaviour
    {
        [SerializeField] private AnimationCurve blinkCurve;
        [SerializeField] private Transform[] eyelidBones;
        [SerializeField] private Vector3[] closedLocalPositions;

        private void OnValidate()
        {
            if (eyelidBones.Length != closedLocalPositions.Length)
            {
                Debug.LogError("Eyelid bones and closed local positions must be the same length");
            }
        }

        private Vector3[] openLocalPositions;
        private void Awake()
        {
            openLocalPositions = new Vector3[eyelidBones.Length];
            for (int i = 0; i < eyelidBones.Length; i++)
            {
                openLocalPositions[i] = eyelidBones[i].localPosition;
            }
        }

        private void OnEnable()
        {
            StartCoroutine(Blink());
        }

        private const float blinkFrequency = 1.5f;
        private const float blinkSpeed = 5;
        private IEnumerator Blink()
        {
            while (true)
            {
                yield return new WaitForSeconds(blinkFrequency + Random.Range(-1f, 2));

                // Close eyelid
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime * (blinkSpeed + Random.Range(-3, 4f));
                    for (int i = 0; i < eyelidBones.Length; i++)
                    {
                        eyelidBones[i].localPosition = Vector3.Lerp(eyelidBones[i].localPosition, closedLocalPositions[i], blinkCurve.EvaluateNormalizedTime(t));
                    }
                    yield return null;
                }

                // Wait at bottom
                yield return new WaitForSeconds(Random.Range(0, 0.2f));

                // Open eyelid
                t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime * (blinkSpeed + Random.Range(-3, 4f));
                    for (int i = 0; i < eyelidBones.Length; i++)
                    {
                        eyelidBones[i].localPosition = Vector3.Lerp(eyelidBones[i].localPosition, openLocalPositions[i], blinkCurve.EvaluateNormalizedTime(t));
                    }
                    yield return null;
                }
            }
        }
    }
}