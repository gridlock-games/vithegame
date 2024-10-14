using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Player
{
    public class UIDeadZoneElement : MonoBehaviour
    {
        public Canvas RootCanvas { get; private set; }
        private void Awake()
        {
            RootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        }
    }
}