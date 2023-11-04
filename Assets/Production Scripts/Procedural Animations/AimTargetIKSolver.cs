using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.ProceduralAnimations
{
    public class AimTargetIKSolver : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0, 0, 10);

        private void Update()
        {
            if (!Camera.main) { return; }

            transform.position = Camera.main.transform.position + Camera.main.transform.rotation * offset;
        }
    }
}