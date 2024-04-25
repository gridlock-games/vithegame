using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vi.UI
{
    public class DraggableUIObject : MonoBehaviour, IDragHandler
    {
        private RectTransform limits;
        public void Initialize(RectTransform limits)
        {
            this.limits = limits;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 newPosition = eventData.position;
            newPosition.x = Mathf.Clamp(newPosition.x, 0, Screen.width - 50);
            newPosition.y = Mathf.Clamp(newPosition.y, 0, Screen.height - 50);
            transform.position = newPosition;
        }
    }
}