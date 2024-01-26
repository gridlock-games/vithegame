using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    [RequireComponent(typeof(Image))]
    public class IrregularButton : MonoBehaviour
    {
        [SerializeField] private float alphaThreshold = 0.1f;

        private void Start()
        {
            GetComponent<Image>().alphaHitTestMinimumThreshold = alphaThreshold;
        }
    }
}