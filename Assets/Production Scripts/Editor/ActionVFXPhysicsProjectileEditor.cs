using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core;

namespace Vi.Editor
{
    [CustomEditor(typeof(ActionVFXPhysicsProjectile))]
    [CanEditMultipleObjects]
    public class ActionVFXPhysicsProjectileEditor : ActionVFXEditor
    {
        private SerializedProperty spProjectileForce;
        private SerializedProperty spTimeToActivateGravity;

        private new void OnEnable()
        {
            base.OnEnable();
            spProjectileForce = serializedObject.FindProperty("projectileForce");
            spTimeToActivateGravity = serializedObject.FindProperty("timeToActivateGravity");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(spProjectileForce);
            EditorGUILayout.PropertyField(spTimeToActivateGravity);
        }
    }
}