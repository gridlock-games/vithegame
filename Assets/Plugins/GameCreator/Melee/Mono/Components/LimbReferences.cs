using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreator.Melee
{
    public class LimbReferences : MonoBehaviour
    {
        public GameObject rightHand;
        public GameObject leftHand;

        public Axis rightHandAimAxis;

        public enum Axis
        {
            X,
            Y,
            Z
        }
    }
}