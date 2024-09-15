using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core;
using Vi.Core.VFX;

namespace Vi.Editor
{
    [CustomEditor(typeof(ActionVFXPhysicsProjectile))]
    [CanEditMultipleObjects]
    public class ActionVFXPhysicsProjectileEditor : GameInteractiveActionVFXEditor
    {
        private SerializedProperty spProjectileForce;
        private SerializedProperty spTimeToActivateGravity;
        private SerializedProperty spKillDistance;

        private SerializedProperty spWhooshNearbySound;

        private new void OnEnable()
        {
            base.OnEnable();
            spProjectileForce = serializedObject.FindProperty("projectileForce");
            spTimeToActivateGravity = serializedObject.FindProperty("timeToActivateGravity");
            spKillDistance = serializedObject.FindProperty("killDistance");
            spWhooshNearbySound = serializedObject.FindProperty("whooshNearbySound");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("Action VFX Physics Projectile", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spProjectileForce);
            EditorGUILayout.PropertyField(spTimeToActivateGravity);
            EditorGUILayout.PropertyField(spKillDistance);
            EditorGUILayout.PropertyField(spWhooshNearbySound);
            serializedObject.ApplyModifiedProperties();
        }
    }
}