using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreator.Characters;

namespace LightPat.ProceduralAnimations
{
    public class AimTargetIKSolver : MonoBehaviour
    {
        private void Update()
        {
            CharacterHandIK handIK = GetComponentInParent<CharacterHandIK>();

            if (!handIK) return;

            transform.position = handIK.aimPoint;
        }
    }
}