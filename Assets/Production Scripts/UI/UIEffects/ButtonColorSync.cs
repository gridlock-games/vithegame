using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class ButtonColorSync : MonoBehaviour
    {
        [SerializeField] private Image imageToSyncFrom;
        [SerializeField] private Image[] imagesToSync;

        private void Update()
        {
            foreach (Image image in imagesToSync)
            {
                image.color = imageToSyncFrom.color;
            }
        }
    }
}