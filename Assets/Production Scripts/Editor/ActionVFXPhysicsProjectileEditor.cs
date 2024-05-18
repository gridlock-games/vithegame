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
        private SerializedProperty spKillDistance;
        private SerializedProperty spVFXToPlayOnDestroy;

        private new void OnEnable()
        {
            base.OnEnable();
            spProjectileForce = serializedObject.FindProperty("projectileForce");
            spTimeToActivateGravity = serializedObject.FindProperty("timeToActivateGravity");
            spKillDistance = serializedObject.FindProperty("killDistance");
            spVFXToPlayOnDestroy = serializedObject.FindProperty("VFXToPlayOnDestroy");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(spProjectileForce);
            EditorGUILayout.PropertyField(spTimeToActivateGravity);
            EditorGUILayout.PropertyField(spKillDistance);
            EditorGUILayout.PropertyField(spVFXToPlayOnDestroy);
            serializedObject.ApplyModifiedProperties();
        }
    }
}