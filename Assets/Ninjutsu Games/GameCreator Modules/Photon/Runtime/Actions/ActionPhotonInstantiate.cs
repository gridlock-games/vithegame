using NJG.PUN.BulkSync;

namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Core;
    using NJG.PUN;
    using Characters;
    using Variables;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonInstantiate : IAction
    {
        public enum InstantiateType
        {
            PlayerObject,
            RoomObject
        }
        public enum InstantiationMethod
        {
            Normal,
            BulkBatch
        }
        public InstantiationMethod method = InstantiationMethod.Normal;
        public InstantiateType instantiate = InstantiateType.PlayerObject;
        public TargetPrefabId prefab = new TargetPrefabId();
        public TargetPosition initLocation;
        public IntProperty group = new IntProperty();

        [Space, VariableFilter(Variable.DataType.GameObject)]
        public VariableProperty assignToVariable = new VariableProperty(
            Variable.VarType.GlobalVariable
        );
        
        public PhotonSendData instantiationData = new PhotonSendData();

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            /*if (method == InstantiationMethod.BulkBatch)
            {
                System.Action instantiate = () =>
                {
                    BulkBatchSync.Instance.Manager.Instantiate(prefab.GetPrefabId(),
                        initLocation.GetPosition(target),
                        initLocation.GetRotation(target), (byte) group.GetValue(target),
                        instantiationData.ToArray(target));
                };
                BulkBatchSync inst = BulkBatchSync.Instance;
                if (!inst)
                {
                    BulkBatchSync.GetBatchSync();
                    BulkBatchSync.OnReady += () =>
                    {
                        instantiate.Invoke();
                    };
                }
                else
                {
                    instantiate.Invoke();
                }
            }
            else*/
            {
                GameObject instance;
                if (instantiate == InstantiateType.PlayerObject)
                {
                    instance = PhotonNetwork.Instantiate(prefab.GetPrefabId(), initLocation.GetPosition(target),
                        initLocation.GetRotation(target), (byte) group.GetValue(target),
                        instantiationData.ToArray(target));
                }
                else
                {
                    instance = PhotonNetwork.InstantiateRoomObject(prefab.GetPrefabId(),
                        initLocation.GetPosition(target),
                        initLocation.GetRotation(target), (byte) group.GetValue(target),
                        instantiationData.ToArray(target));
                }

                assignToVariable.Set(instance, target);
            }
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Photon Instantiate";
        private const string NODE_TITLE = "Photon Instantiate {0}: {1}";
        private const string NO_PHOTON_VIEW = "Cannot Instantiate a prefab without PhotonView component.";
        private const string NO_CHARACTER = "This Character doesn't have CharacterNetwork component attached.";
        private const string NOT_FOUND = "This Prefab needs to be added to the Photon Prefab Database or into a Resources folder.";
        private const string DEFAULT_PREFABS = "Cannot use default prefabs. Lets create a copy of it.";
        private const string NEEDS_OBSERVED = "CharacterNetwork component needs to be added to photonView observed list.";
        private const string FIX = "Fix It";
        private const string RESOURCES = "Resources";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spMethod;
        private SerializedProperty spInstantiate;
        private SerializedProperty spPrefab;
        private SerializedProperty spPosition;
        private SerializedProperty spGroup;        
        private SerializedProperty spVar;
        private SerializedProperty spData;
        private bool toggle;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, instantiate, prefab);
        }

        protected override void OnEnableEditorChild()
        {
            spMethod = serializedObject.FindProperty("method");
            spInstantiate = serializedObject.FindProperty("instantiate");
            spPrefab = serializedObject.FindProperty("prefab");
            spPosition = serializedObject.FindProperty("initLocation");
            spGroup = serializedObject.FindProperty("group");
            spVar = serializedObject.FindProperty("assignToVariable");
            spData = serializedObject.FindProperty("instantiationData");
        }

        protected override void OnDisableEditorChild()
        {
            spMethod = null;
            spInstantiate = null;
            spPrefab = null;
            spPosition = null;
            spGroup = null;
            spVar = null;
            spData = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // EditorGUILayout.PropertyField(spMethod);
            EditorGUILayout.PropertyField(spInstantiate);
            EditorGUILayout.PropertyField(spPrefab);

            if(prefab != null && prefab.target == TargetPrefabId.Target.GameObject && 
                prefab.gameObject.optionIndex == BaseProperty<GameObject>.OPTION.Value
                && prefab.gameObject != null && prefab.gameObject.value)
            {
                GameObject go = prefab.gameObject.GetValue(null);
                if (go)
                {
                    PhotonView pview = go.GetPhotonView();
                    CharacterNetwork chnet = go.GetComponent<CharacterNetwork>();
                    Character character = go.GetComponent<Character>();

                    bool hasPrefab = DatabasePhoton.Load().prefabs.Contains(go) || AssetDatabase.GetAssetPath(go).Contains(RESOURCES); //Resources.Load(go.name) != null || 

                    EditorGUILayout.BeginVertical();

                    string message = string.Empty;

                    if (DatabasePhoton.IsDefaultPrefab(go))
                    {
                        message = DEFAULT_PREFABS;
                    }
                    else if (!hasPrefab)
                    {
                        message = NOT_FOUND;
                    }
                    else if (pview == null)
                    {
                        message = NO_PHOTON_VIEW;
                    }
                    else if (chnet == null && character != null)
                    {
                        message = NO_CHARACTER;
                    }
                    else if (chnet != null && pview != null && !pview.ObservedComponents.Contains(chnet))
                    {
                        message = NEEDS_OBSERVED;
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        EditorGUILayout.HelpBox(message, MessageType.Warning, false);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(EditorGUIUtility.labelWidth + 4);
                        if (GUILayout.Button(FIX, EditorStyles.miniButton))
                        {
                            GameObject newPrefab = null;
                            DatabasePhoton.FixPrefab(go, out newPrefab);
                            if (newPrefab != null)
                            {
                                prefab.gameObject.value = newPrefab;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(spPosition);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(spGroup);
            // EditorGUILayout.Space();
            EditorGUILayout.PropertyField(spVar);
            // EditorGUILayout.Space();
            
            // EditorGUI.indentLevel++;
            // toggle = EditorGUILayout.Foldout(toggle, "Instantiation Data", true);
            // EditorGUI.indentLevel--;
            // if(toggle)
            // {
            //     EditorGUILayout.HelpBox("The On Photon Instantiate trigger needs to match the same amount/type of variables", MessageType.Warning);
            //
            //     EditorGUILayout.BeginVertical("ShurikenEffectBg", GUILayout.MinHeight(50));
                EditorGUILayout.PropertyField(spData);
            //     EditorGUILayout.EndVertical();
            // }

            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
