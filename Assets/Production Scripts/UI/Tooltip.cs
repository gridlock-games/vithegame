using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField] private GameObject tooltipPrefab;

        private Canvas parentCanvas;

        private void Start()
        {
            parentCanvas = GetComponentInParent<Canvas>(true);
        }
    }
}