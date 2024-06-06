using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.UI
{
  public class UISetting_Menu : MonoBehaviour
  {

    [SerializeField] private UIModificationMenu UILayoutSetting;
    // Start is called before the first frame update
    public void OpenUILayout()
    {
      GameObject _settings = Instantiate(UILayoutSetting.gameObject);
    }

    // Update is called once per frame
    void Update()
    {

    }
  }
}