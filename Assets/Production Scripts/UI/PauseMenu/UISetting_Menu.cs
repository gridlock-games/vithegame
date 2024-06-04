using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.UI
{
  public class UISetting_Menu : MonoBehaviour
  {

    [SerializeField] private GameObject UILayoutSetting;
    // Start is called before the first frame update
    void OpenUILayout()
    {
      Instantiate(UILayoutSetting);
    }

    // Update is called once per frame
    void Update()
    {

    }
  }
}