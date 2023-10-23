using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;

namespace Vi.UI
{
    public class WorldSpaceLabel : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float rotationSpeed = 1;
        [SerializeField] private float scalingSpeed = 1;
        [SerializeField] private float viewDistance = 10;

        [Header("Object Assignments")]
        [SerializeField] private Text nameDisplay;
        [SerializeField] private Image nameBackground;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private GameObject spectatorHotKeyInstance;

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
            Vector3 pos = rendererToFollow.bounds.center;
            pos.y += rendererToFollow.bounds.extents.y * 10;
            transform.position = pos;
            transform.localScale = Vector3.one * 0.01f;

            if (Camera.main)
            {
                Quaternion targetRotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // Scale size of name background by size of text
            if (nameBackground.TryGetComponent(out RectTransform rectTransform))
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, nameDisplay.preferredWidth * nameDisplay.transform.localScale.x + 10);
            }

            healthFillImage.fillAmount = attributes.GetHP() / attributes.GetMaxHP();
        }
    }
}