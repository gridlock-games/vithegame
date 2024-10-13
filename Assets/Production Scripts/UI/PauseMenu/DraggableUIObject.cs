using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vi.UI
{
    public class DraggableUIObject : MonoBehaviour, IDragHandler
    {
        private Canvas canvas;
        private RectTransform rectTransform;
        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>().rootCanvas;
            rectTransform = (RectTransform)transform;
        }

        private UIModificationMenu UIModificationMenu;
        public void Initialize(UIModificationMenu UIModificationMenu)
        {
            this.UIModificationMenu = UIModificationMenu;
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rectTransform.parent,
                eventData.position, canvas.worldCamera, out Vector2 localPoint);
            rectTransform.localPosition = localPoint;

            Vector3[] worldBounds = new Vector3[4];
            ((RectTransform)canvas.transform).GetWorldCorners(worldBounds);

            Vector3 newPosition = rectTransform.position;
            newPosition.x = Mathf.Clamp(newPosition.x,
                worldBounds[0].x,
                worldBounds[3].x - 50 * transform.lossyScale.x);

            newPosition.y = Mathf.Clamp(newPosition.y,
                worldBounds[0].y,
                worldBounds[2].y - 50 * transform.lossyScale.y);

            transform.position = newPosition;

            UIModificationMenu.OnDraggableUIObject(this);
        }
    }
}