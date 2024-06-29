using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core;

namespace Vi.Editor
{
    [CustomEditor(typeof(ActionVFXPhysicsProjectile))]
    [CanEditMultipleObjects]
    public class ActionVFXPhysicsProjectileEditor : GameInteractiveActionVFXEditor
    {
        private SerializedProperty spProjectileForce;
        private SerializedProperty spTimeToActivateGravity;
        private SerializedProperty spKillDistance;

        private new void OnEnable()
        {
            base.OnEnable();
            spProjectileForce = serializedObject.FindProperty("projectileForce");
            spTimeToActivateGravity = serializedObject.FindProperty("timeToActivateGravity");
            spKillDistance = serializedObject.FindProperty("killDistance");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("Action VFX Physics Projectile", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spProjectileForce);
            EditorGUILayout.PropertyField(spTimeToActivateGravity);
            EditorGUILayout.PropertyField(spKillDistance);
            serializedObject.ApplyModifiedProperties();
        }
    }
}