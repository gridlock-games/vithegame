using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Vi.UI
{
    public class ImageOnDragData : MonoBehaviour, IDragHandler
    {
        public UnityAction<Vector2> OnDragEvent;

        public void OnDrag(PointerEventData eventData)
        {
            if (OnDragEvent != null) { OnDragEvent.Invoke(eventData.delta); }
        }
    }
}