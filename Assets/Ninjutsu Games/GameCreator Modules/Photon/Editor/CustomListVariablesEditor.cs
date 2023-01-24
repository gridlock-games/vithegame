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
    [CustomEditor(typeof(ListVariables))]
    public class CustomListVariablesEditor : ListVariablesEditor
    {
        public class Section
        {
            private const string ICONS_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/";
            private const string KEY_STATE = "network-listvars-section-{0}";

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
        private GUIContent GUI_INSTATIATE = new GUIContent("Network Instantiation", "Adds all gameobjects from this list " +
            "into the Photon network instantiation list allowing them to be used with Photon Instantiate Action.");
        
        private GUIContent GUI_LIST = new GUIContent("Player List", "Adds all players into this list. The list will be updated when a player leaves or join.");

        private string TXT_INSTANTIATE =
            "Adds all gameobjects from this list into the Photon network instantiation list allowing them to be used with Photon Instantiate Action.";
        
        private string TXT_LIST =
            "Adds all players into this list. The list will be updated when a player leaves or join.";
        
        private string GUI_FORBID_TYPES = "Null, GameObject, Sprite or Texture2D types cannot be sync and will be ignored.";
        private string GUI_FORBID_INSTANTIATE = "This option can only be used with GameObject type.";

        private bool hasComponent = false;
        private bool useNetworkInstantiation = false;
        private bool usePlayerList = false;
        private bool syncVariables = false;
        private ListVariablesNetwork.ListRPC listRPC;
        private int index = -1;
        private bool initialized = false;
        private ListVariablesNetwork network;
        private SerializedObject serializedNetwork;
        private ListVariables listVars;
        private Section section;
        private SerializedProperty spActions;
        private SerializedObject actionSerializedObject;
        private bool hasForbiddenTypes;
        private SerializedProperty spType;
        protected override void OnEnable()
        {
            base.OnEnable();

            listVars = target as ListVariables;
            this.spType = this.serializedObject.FindProperty("type");
        }

        private bool CanInstantiate()
        {
            Variable.DataType type = (Variable.DataType)this.spType.enumValueIndex;
            return type == Variable.DataType.GameObject;
        }

        private bool CanSync()
        {
            Variable.DataType type = (Variable.DataType)this.spType.enumValueIndex;
            return type != Variable.DataType.GameObject && type != Variable.DataType.Texture2D && 
                type != Variable.DataType.Sprite && type != Variable.DataType.Null;
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null) return;

            base.OnInspectorGUI();
            listVars = target as ListVariables;

            if (listVars.transform.root.gameObject.GetComponent<NetworkItem>())
            {
                return;
            }

            if (this.section == null)
            {
                this.section = new Section("Network Settings", "ActionNetwork.png", this.Repaint);
            }

            if (!initialized)
            {
                network = listVars.transform.root.gameObject.GetComponent<ListVariablesNetwork>();

                //if(actionsNetwork != null) actionsRPC = ArrayUtility.Find(actionsNetwork.actions, p => p.actions == actions);
                if (network != null)
                {
                    serializedNetwork = new SerializedObject(network);
                    listRPC = network.listVars.Find(p => p.variables == listVars);
                    index = network.listVars.IndexOf(listRPC);
                }
                hasComponent = listRPC != null;
                syncVariables = listRPC != null && listRPC.syncVariables;
                useNetworkInstantiation = listRPC != null && listRPC.useNetworkInstantiation && !listRPC.usePlayerList;
                usePlayerList = listRPC != null && listRPC.usePlayerList && !listRPC.useNetworkInstantiation;
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

                    EditorGUI.BeginDisabledGroup(!CanInstantiate());
                    //EditorGUIUtility.labelWidth = 120;
                    useNetworkInstantiation = EditorGUILayout.Toggle(GUI_INSTATIATE, useNetworkInstantiation);
                    if (useNetworkInstantiation)
                    {
                        EditorGUILayout.HelpBox(TXT_INSTANTIATE, MessageType.Info);
                    }
                    usePlayerList = EditorGUILayout.Toggle(GUI_LIST, usePlayerList);
                    if (usePlayerList)
                    {
                        EditorGUILayout.HelpBox(TXT_LIST, MessageType.Info);
                    }
                    EditorGUI.EndDisabledGroup();
                    
                    hasChanged = EditorGUI.EndChangeCheck();
                    if (!CanInstantiate())
                    {
                        useNetworkInstantiation = false;
                        EditorGUILayout.HelpBox(GUI_FORBID_INSTANTIATE, MessageType.Warning);
                    }
                    /*else
                    {
                        if(useNetworkInstantiation) hasComponent = false;
                    }*/

                    /*EditorGUI.BeginDisabledGroup(!CanSync());
                    syncVariables = EditorGUILayout.Toggle(GUI_SYNC, syncVariables);
                    

                    EditorGUI.EndDisabledGroup();*/

                    if (useNetworkInstantiation || usePlayerList) syncVariables = false;

                    if (listRPC != null && !CanInstantiate())
                    {
                        EditorGUILayout.HelpBox(GUI_FORBID_TYPES, MessageType.Info);
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            if (hasChanged)
            {
                if ((listRPC != null) && network)
                {
                    Debug.Log("Removed ListVariablesNetwork");
                    network.listVars.Remove(listRPC);
                    if (network.listVars.Count == 0 && (!useNetworkInstantiation && !usePlayerList))
                    {
                        PhotonView pv = network.photonView;

                        DestroyImmediate(network, true);
                    }

                    listRPC = null;
                    network = null;

                    initialized = false;
                }
                else
                {
                    network = listVars.transform.root.gameObject.GetComponent<ListVariablesNetwork>() ?? listVars.transform.root.gameObject.AddComponent<ListVariablesNetwork>();

                    SerializedObject serializedObject = new UnityEditor.SerializedObject(network);

                    listRPC = new ListVariablesNetwork.ListRPC() { variables = listVars };
                    listRPC.useNetworkInstantiation = useNetworkInstantiation;
                    listRPC.usePlayerList = usePlayerList;
                    listRPC.syncVariables = syncVariables;
                    network.listVars.Add(listRPC);

                    Debug.LogFormat("Added ListVariablesNetwork: {0}", listRPC.variables.gameObject.name);

                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();

                    hasComponent = true;
                }
                EditorUtility.SetDirty(listVars.gameObject);
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