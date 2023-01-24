using GameCreator.Core;
using GameCreator.Variables;
using NJG;
using Photon.Pun;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace NJG.PUN
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LocalVariables))]
    public class CustomLocalVariablesEditor : LocalVariablesEditor
    {
        public class Section
        {
            private const string ICONS_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/";
            private const string KEY_STATE = "network-localvars-section-{0}";

            private const float ANIM_BOOL_SPEED = 3.0f;

            // PROPERTIES: ------------------------------------------------------------------------

            public GUIContent name;
            public AnimBool state;

            // INITIALIZERS: ----------------------------------------------------------------------

            public Section(string name, string icon, UnityAction repaint, string overridePath = "")
            {
                this.name = new GUIContent(
                    string.Format(" {0}", name),
                    this.GetTexture(icon, overridePath)
                );

                this.state = new AnimBool(this.GetState());
                this.state.speed = ANIM_BOOL_SPEED;
                this.state.valueChanged.AddListener(repaint);
            }

            // PUBLIC METHODS: --------------------------------------------------------------------

            public void PaintSection()
            {
                GUIStyle buttonStyle = (this.state.target
                    ? CoreGUIStyles.GetToggleButtonNormalOn()
                    : CoreGUIStyles.GetToggleButtonNormalOff()
                );

                if (GUILayout.Button(this.name, buttonStyle))
                {
                    this.state.target = !this.state.target;
                    string key = string.Format(KEY_STATE, this.name.text.GetHashCode());
                    EditorPrefs.SetBool(key, this.state.target);
                }
            }

            // PRIVATE METHODS: -------------------------------------------------------------------

            private bool GetState()
            {
                string key = string.Format(KEY_STATE, this.name.text.GetHashCode());
                return EditorPrefs.GetBool(key, false);
            }

            private Texture2D GetTexture(string icon, string overridePath = "")
            {
                string path = Path.Combine(string.IsNullOrEmpty(overridePath) ? ICONS_PATH : overridePath, icon);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        private GUIContent GUI_SYNC = new GUIContent("Sync Variables", "Adds a network component to sync these actions.");
        private string GUI_FORBID_TYPES = "Null, GameObject, Sprite or Texture2D types cannot be sync and will be ignored.";

        private bool hasComponent = false;
        private LocalVariablesNetwork.VarRPC varRPC;
        private int index = -1;
        private bool initialized = false;
        private LocalVariablesNetwork network;
        private SerializedObject serializedNetwork;
        private LocalVariables localVars;
        private Section section;
        private SerializedProperty spActions;
        private SerializedObject actionSerializedObject;
        private bool hasForbiddenTypes;

        /*private void CheckTypes()
        {
            foreach (var v in varRPC.variables.references)
            {
                Variable.DataType varType = (Variable.DataType)v.variable.type;
                if (varType == Variable.DataType.Null ||
                varType == Variable.DataType.GameObject ||
                varType == Variable.DataType.Sprite ||
                varType == Variable.DataType.Texture2D)
                {
                    hasForbiddenTypes = true;
                }
                else
                {
                    hasForbiddenTypes = false;
                }
            }
        }*/

        public override void OnInspectorGUI()
        {
            if (serializedObject == null) return;

            base.OnInspectorGUI();
            localVars = target as LocalVariables;

            if (localVars.transform.root.gameObject.GetComponent<NetworkItem>())
            {
                return;
            }

            if (this.section == null)
            {
                this.section = new Section("Network Settings", "ActionNetwork.png", this.Repaint);
            }

            if (!initialized)
            {
                network = localVars.transform.root.gameObject.GetComponent<LocalVariablesNetwork>();

                //if(actionsNetwork != null) actionsRPC = ArrayUtility.Find(actionsNetwork.actions, p => p.actions == actions);
                if (network != null)
                {
                    serializedNetwork = new SerializedObject(network);
                    varRPC = network.localVars.Find(p => p.variables == localVars);
                    index = network.localVars.IndexOf(varRPC);
                }
                hasComponent = varRPC != null;
                initialized = true;
                //CheckTypes();
            }

            bool hasChanged = false;

            this.section.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.section.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUI.BeginChangeCheck();
                    hasComponent = EditorGUILayout.Toggle(GUI_SYNC, hasComponent);
                    hasChanged = EditorGUI.EndChangeCheck();

                    if (varRPC != null)
                    {
                        /*if (hasChanged)
                        {
                            CheckTypes();
                        }
                        if (hasForbiddenTypes)
                        {*/
                            EditorGUILayout.HelpBox(GUI_FORBID_TYPES, MessageType.Info);
                        //}
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            if (hasChanged)
            {
                if (varRPC != null && network)
                {
                    Debug.Log("Removed LocalVariables");
                    network.localVars.Remove(varRPC);
                    if (network.localVars.Count == 0)
                    {
                        PhotonView pv = network.photonView;

                        DestroyImmediate(network, true);

                        /*if (!pv.GetComponent<CharacterNetwork>() ||
                            !pv.GetComponent<StatsNetwork>() ||
                            !pv.GetComponent<ActionsNetwork>())
                        {
                            DestroyImmediate(pv, true);
                        }*/
                    }

                    varRPC = null;
                    network = null;

                    initialized = false;
                }
                else
                {
                    network = localVars.transform.root.gameObject.GetComponent<LocalVariablesNetwork>() ?? localVars.transform.root.gameObject.AddComponent<LocalVariablesNetwork>();

                    SerializedObject serializedObject = new UnityEditor.SerializedObject(network);

                    varRPC = new LocalVariablesNetwork.VarRPC() { variables = localVars };
                    network.localVars.Add(varRPC);

                    Debug.LogFormat("Added LocalVariables: {0}", varRPC.variables.gameObject.name);

                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();

                    hasComponent = true;
                }
                EditorUtility.SetDirty(localVars.gameObject);
            }
            EditorGUILayout.Space();
        }

        /*private void OnDestroy()
        {
            if (actionsRPC != null && actionsNetwork != null)
            {
                Debug.Log("OnDestroy");
                actionsNetwork.actions.Remove(actionsRPC);
                //ArrayUtility.Remove(ref actionsNetwork.actions, actionsRPC);
            }
        }*/
    }
}