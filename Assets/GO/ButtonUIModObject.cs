using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonUIModObject : MonoBehaviour
{
  string buttonID;
  bool active;

  void MoveButtonPosition(Vector2 buttonPosition, float buttonSize)
  {
    RectTransform rt = this.GetComponent<RectTransform>();
    rt.position = buttonPosition;
    rt.localScale = new Vector2(buttonSize, buttonSize);
  }
}
