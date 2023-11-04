using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Player
{
    public class ADSCameraController : MonoBehaviour
    {
        [SerializeField] private Transform spineBone;

        private void LateUpdate()
        {
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }
    }
}