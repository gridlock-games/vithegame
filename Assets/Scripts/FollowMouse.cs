using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    public GameObject TargetObject;

    private void Update()
    {
        if (TargetObject != null)
        {
            transform.position = TargetObject.transform.position;
        }
    }
}