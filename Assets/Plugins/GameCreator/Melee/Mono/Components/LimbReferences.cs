using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreator.Melee
{
    public class LimbReferences : MonoBehaviour
    {
        public GameObject rightHand;
        public GameObject leftHand;

        [Header("Hand IK Settings")]
        public Vector3 rightHandAimForwardDir = new Vector3(0, 0, 1);
        public Vector3 rightHandAimUpDir = new Vector3(0, 1, 0);
    }
}