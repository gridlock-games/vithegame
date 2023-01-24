namespace NJG.PUN
{
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.AI;
    using UnityEditor;
    using UnityEditor.AnimatedValues;
    using GameCreator.Core;
    using GameCreator.Characters;
    using Photon.Pun;
    using System;

    [CustomEditor(typeof(CharacterNetwork), true)]
    public class CharacterNetworkEditor : Editor
    {
        // CONSTANTS: --------------------------------------------------------------------------------------------------

        private const string PROP_TELEPORT = "teleportIfDistance";
        //private const string PROP_ROTATION = "rotationDampening";
        private const string PROP_ATTACHMENTS = "syncAttachments";
        private const string PROP_CULLING = "networkCulling";
        private const string PROP_LOCOMOTION = "locomotion";
        private readonly GUIContent GUI_LAST_POS = new GUIContent("Spawn at previous position", "If you are rejoining to the same room as previously connected it will spawn at the last known location.");

        // PROPERTIES: -------------------------------------------------------------------------------------------------

        protected CharacterNetwork character;

        protected SerializedProperty spTeleportDistance;
        //protected SerializedProperty spRotationDamp;
        protected SerializedProperty spSyncAttachments;
        protected SerializedProperty spCulling;
        protected SerializedProperty spLocomotion;
        private PhotonView photonView;

        // INITIALIZERS: -----------------------------------------------------------------------------------------------

        protected void OnEnable()
        {
            if (target == null) return;

            character = (CharacterNetwork)target;
            photonView = character.GetComponent<PhotonView>();

            spTeleportDistance = serializedObject.FindProperty(PROP_TELEPORT);
            //this.spRotationDamp = serializedObject.FindProperty(PROP_ROTATION);
            spSyncAttachments = serializedObject.FindProperty(PROP_ATTACHMENTS);
            spCulling = serializedObject.FindProperty(PROP_CULLING);
            spLocomotion = serializedObject.FindProperty(PROP_LOCOMOTION);

            if (target != null) target.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }

        protected void OnDisable()
        {
            character = null;
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };


        static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        // INSPECTOR GUI: ----------------------------------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            //EditorGUILayout.Space();

            PaintInspector();

            //EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }
        
        public void PaintInspector()
        {
            PaintCharacterBasicProperties();
            character.CheckObservables();
        }

        private void PaintCharacterBasicProperties()
        {
            EditorGUILayout.Space();

            if (PhotonNetwork.InRoom)
            {
                EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());
                EditorGUILayout.LabelField(string.Format("Lag: {0}", character.Lag));
                EditorGUILayout.LabelField(string.Format("Last Recieved Update: {0}", character.LastReceivedUpdate));
                //EditorGUILayout.LabelField(string.Format("KB Sent: {0}", character.photonView.))
                EditorGUILayout.EndVertical();
            }
            
            //EditorGUILayout.Space();
            EditorGUILayout.LabelField("Locomotion Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            //EditorGUILayout.PropertyField(this.spLocomotion.FindPropertyRelative("useCompression"));
            // EditorGUILayout.PropertyField(spLocomotion.FindPropertyRelative("spawnAtLastRoomPosition"), GUI_LAST_POS);
            EditorGUILayout.PropertyField(spLocomotion.FindPropertyRelative("syncPosition"));
            //EditorGUILayout.PropertyField(this.spLocomotion.FindPropertyRelative("syncDirection"));
            EditorGUI.indentLevel++;
            if (character.locomotion.syncPosition) EditorGUILayout.PropertyField(spTeleportDistance);
            EditorGUI.indentLevel--;
            //if (!character.isNPC) EditorGUILayout.PropertyField(this.spLocomotion.FindPropertyRelative("syncRotation"));
            //if (character.locomotion.syncRotation) EditorGUILayout.PropertyField(this.spRotationDamp);
            EditorGUILayout.PropertyField(spLocomotion.FindPropertyRelative("syncCanRun"));
            EditorGUILayout.PropertyField(spLocomotion.FindPropertyRelative("syncRunSpeed"));
            EditorGUILayout.PropertyField(spLocomotion.FindPropertyRelative("syncCanJump"));
            EditorGUILayout.PropertyField(spLocomotion.FindPropertyRelative("syncJump"));
            EditorGUILayout.PropertyField(spLocomotion.FindPropertyRelative("syncGravity"));
            EditorGUILayout.PropertyField(spLocomotion.FindPropertyRelative("syncAngularSpeed"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            /*public bool syncPosition = true;
            public bool syncDirection = true;
            public bool syncRotation = true;
            public bool syncRunSpeed = true;
            public bool syncCanRun = true;
            public bool syncCanJump = true;
            public bool syncJump = true;
            public bool syncAngularSpeed = true;
            public bool syncGravity = true;*/
            EditorGUILayout.PropertyField(spSyncAttachments);
            EditorGUILayout.PropertyField(spCulling);

            if (spCulling.boolValue)
            {
                if (Application.isPlaying)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());
                    string subscribedAndActiveCells = "Inside cells: ";
                    string subscribedCells = "Subscribed cells: ";

                    for (int index = 0; index < character.activeCells.Count; ++index)
                    {
                        if (index <= CharacterNetwork.CullArea.NumberOfSubdivisions)
                        {
                            subscribedAndActiveCells += character.activeCells[index] + " | ";
                        }

                        subscribedCells += character.activeCells[index] + " | ";
                    }
                    EditorGUILayout.LabelField("PhotonView Group: " + character.photonView.Group);
                    EditorGUILayout.LabelField(subscribedAndActiveCells);
                    EditorGUILayout.LabelField(subscribedCells);
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("You can only see Network Culling stats while playing and inside a room.", MessageType.Info);
                }
            }
        }
    }
}