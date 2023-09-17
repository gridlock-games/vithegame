namespace GameCreator.Melee
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using GameCreator.Camera;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;
    using Unity.Netcode;
    using GameCreator.Variables;


    public class ShooterComponent : MonoBehaviour
    {

        public enum ProjectilePath
        {
            arc,
            line
        }

        private static readonly Color GIZMOS_DEFAULT_COLOR = Color.yellow;
        private static readonly Color GIZMOS_ACTIVE_COLOR = Color.red;
        [SerializeField] private Vector3 muzzlePosition = new Vector3(0.3f, 0.3f, 1.5f);
        public Vector3 boxCenter = new Vector3(0, 0, 0.75f);
        [SerializeField] private ProjectilePath projectilePath = ProjectilePath.line;

        private void OnDrawGizmos()
        {
            Gizmos.color = GIZMOS_DEFAULT_COLOR;

            Vector3 center = transform.TransformPoint(this.boxCenter);
            Matrix4x4 gizmosMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawWireCube(this.boxCenter, this.muzzlePosition);
            Gizmos.matrix = gizmosMatrix;
        }
    }
}