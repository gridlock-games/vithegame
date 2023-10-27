using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class WorldSpaceLabel : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float rotationSpeed = 15;
        [SerializeField] private float scalingSpeed = 8;
        [SerializeField] private float scalingDistanceDivisor = 500;
        [SerializeField] private float viewDistance = 20;

        [Header("Object Assignments")]
        [SerializeField] private Text nameDisplay;
        [SerializeField] private Image nameBackground;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private StatusImageReference statusImageReference;

        private Attributes attributes;
        private Renderer rendererToFollow;

        private void Start()
        {
            attributes = GetComponentInParent<Attributes>();

            transform.SetParent(null, true);

            Renderer[] renderers = attributes.GetComponentsInChildren<Renderer>();
            Vector3 highestPoint = renderers[0].bounds.center;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.GetType() != typeof(SkinnedMeshRenderer)) { continue; }

                if (renderer.bounds.center.y > highestPoint.y)
                {
                    rendererToFollow = renderer;
                    highestPoint = renderer.bounds.center;
                }
            }
        }

        private void LateUpdate()
        {
            nameDisplay.text = "Ailment: " + attributes.GetAilment().ToString();

            Vector3 localScaleTarget = Vector3.zero;
            if (Camera.main)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Camera.main.transform.position - transform.position), Time.deltaTime * rotationSpeed);

                float camDistance = Vector3.Distance(Camera.main.transform.position, transform.position);
                if (camDistance > viewDistance)
                {
                    Quaternion mouseOverRotation = Quaternion.LookRotation(Camera.main.transform.position - rendererToFollow.bounds.center);
                    float upAngle = Vector3.SignedAngle(Camera.main.transform.up, mouseOverRotation * Vector3.up, Camera.main.transform.right);
                    float horizontalAngle = Quaternion.Angle(Camera.main.transform.rotation, mouseOverRotation);
                    localScaleTarget = horizontalAngle > 178 & (upAngle > -4 & upAngle <= 0) ? Vector3.one * (camDistance / scalingDistanceDivisor) : Vector3.zero;
                }
                else
                {
                    localScaleTarget = Vector3.one * (camDistance / scalingDistanceDivisor);
                }

                Vector3 pos = rendererToFollow.bounds.center;
                pos.y += rendererToFollow.bounds.extents.y * transform.localScale.x * scalingDistanceDivisor;
                transform.position = pos;
            }
            else
            {
                Debug.LogWarning("Can't find a main camera for world space labels!");
            }
            transform.localScale = Vector3.Lerp(transform.localScale, localScaleTarget, Time.deltaTime * scalingSpeed);

            // Scale size of name background by size of text
            if (nameBackground.TryGetComponent(out RectTransform rectTransform))
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, nameDisplay.preferredWidth * nameDisplay.transform.localScale.x + 10);
            }

            healthFillImage.fillAmount = attributes.GetHP() / attributes.GetMaxHP();
        }
    }
}