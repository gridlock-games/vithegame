using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragableUIObject : MonoBehaviour , IDragHandler
{
  [SerializeField] Transform draggableObject;

  public void Awake()
  {
    draggableObject = this.GetComponent<Transform>();
  }
  public void OnDrag(PointerEventData eventData)
  {
    Debug.Log("Touching");
    draggableObject.position = eventData.position;
  }


}
