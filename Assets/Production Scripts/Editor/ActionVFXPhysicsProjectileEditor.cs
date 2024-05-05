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

        private new void OnEnable()
        {
            base.OnEnable();
            spProjectileForce = serializedObject.FindProperty("projectileForce");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(spProjectileForce);
        }
    }
}