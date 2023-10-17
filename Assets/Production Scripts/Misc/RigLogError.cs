using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Vi.Misc
{
    public class RigLogError : MonoBehaviour
    {
        private void Update()
        {
            if (GetComponent<Rig>().weight != 0)
            {
                Debug.LogError(gameObject + " Rig weight not equal to 0!");
            }
        }
    }
}