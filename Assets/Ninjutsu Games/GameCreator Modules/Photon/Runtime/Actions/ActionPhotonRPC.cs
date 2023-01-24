using GameCreator.Variables;
using NJG.PUN;
using Photon.Pun;
using UnityEngine;

namespace GameCreator.Core
{
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonRPC : IAction
    {
        public enum TargetType
        {
            RpcTarget,
            PhotonPlayer
        }

        public TargetType targetType = TargetType.RpcTarget;
        public string rpcName;
        [Tooltip("Who will receive this RPC.")]
        public RpcTarget targets = RpcTarget.Others;
        [Tooltip("If true this RPC will only be send by the MasterClient.")]
        public TargetPhotonPlayer targetPlayer;
        public bool onlyMasterClient;
        
        public bool secureRPC;
        public bool encrypt;
        
        public PhotonSendData sendData = new PhotonSendData();

        public PhotonView photonView
        {
            get
            {
                if (!pview) pview = GetComponentInParent<PhotonView>();
                if (!pview) pview = PhotonView.Get(this);

                return pview;
            }
        }
        private PhotonView pview;


        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index, params object[] parameters)
        {
            // Debug.LogWarningFormat("ActionPhotonRPC CanExecute: {0} Invoker: {1} root: {2} rpcName: {3} data: {4}", 
            //     ((!onlyMasterClient) || (onlyMasterClient && PhotonNetwork.IsMasterClient)), 
            //     target, transform.root, rpcName, sendData);
            
            if ((!onlyMasterClient) || (onlyMasterClient && PhotonNetwork.IsMasterClient))
            {
                if(secureRPC)
                {
                    if(targetType == TargetType.RpcTarget)
                    {
                        photonView.RpcSecure(IgniterPhotonRPC.RPC_NAME, targets, encrypt, rpcName,
                            sendData.ToArray(photonView.gameObject));
                    }
                    else
                    {
                        photonView.RpcSecure(IgniterPhotonRPC.RPC_NAME, targetPlayer.GetPhotonPlayer(target), encrypt,
                            rpcName,
                            sendData.ToArray(photonView.gameObject));
                    }
                }
                else
                {
                    if(targetType == TargetType.RpcTarget)
                    {
                        photonView.RPC(IgniterPhotonRPC.RPC_NAME, targets, rpcName, 
                            sendData.ToArray(photonView.gameObject));
                    }
                    else
                    {
                        photonView.RPC(IgniterPhotonRPC.RPC_NAME, targetPlayer.GetPhotonPlayer(target), rpcName,
                            sendData.ToArray(photonView.gameObject));
                    }
                    
                }
            }
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Photon RPC";
        private const string NODE_TITLE = "Send Photon RPC to {0}";
        private const string FIX = "Fix It";

        private static readonly GUIContent ONLY_MASTER = new GUIContent("Only MasterClient","If On only master client can send this message.");
        private static readonly GUIContent TARGET_OTHERS = new GUIContent("Targets", "Sends the RPC to everyone else. This client does not execute the RPC. Player who join later will not execute this RPC.");

        private const string PHOTONVIEW = "This component requires a PhotonView in order to work.";
        private const string All = "Sends the RPC to everyone else and executes it immediately on this client. Player who join later will not execute this RPC.";
        private const string Others = "Sends the RPC to everyone else. This client does not execute the RPC. Player who join later will not execute this RPC.";
        private const string MasterClient = "Sends the RPC to MasterClient only. Careful: The MasterClient might disconnect before it executes the RPC and that might cause dropped RPCs.";
        private const string AllBuffered = "Sends the RPC to everyone else and executes it immediately on this client. New players get the RPC when they join as it's buffered (until this client leaves).";
        private const string OthersBuffered = "Sends the RPC to everyone. This client does not execute the RPC. New players get the RPC when they join as it's buffered (until this client leaves).";
        private const string AllViaServer = "Sends the RPC to everyone (including this client) through the server.\nThe server's order of sending the RPCs is the same on all clients.";
        private const string AllBufferedViaServer = "Sends the RPC to everyone (including this client) through the server and buffers it for players joining later.\nThe server's order of sending the RPCs is the same on all clients.";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spTargetType;
        private SerializedProperty spTargetPlayer;
        private SerializedProperty spSecure;
        private SerializedProperty spEncrypt;
        private SerializedProperty spTargets;
        private SerializedProperty spRPC;
        private SerializedProperty spMaster;
        private SerializedProperty spData;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, targets);
        }

        protected override void OnEnableEditorChild()
        {
            spTargetType = serializedObject.FindProperty("targetType");
            spTargetPlayer = serializedObject.FindProperty("targetPlayer");
            spSecure = serializedObject.FindProperty("secureRPC");
            spEncrypt = serializedObject.FindProperty("encrypt");
            spRPC = serializedObject.FindProperty("rpcName");
            spTargets = serializedObject.FindProperty("targets");
            spMaster = serializedObject.FindProperty("onlyMasterClient");
            spData = serializedObject.FindProperty("sendData");
        }

        protected override void OnDisableEditorChild()
        {
            spTargetType = null;
            spTargetPlayer = null;
            spSecure = null;
            spEncrypt = null;
            spTargets = null;
            spRPC = null;
            spMaster = null;
            spData = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spRPC);
            EditorGUILayout.PropertyField(spTargetType);
            
            if(photonView == null)
            {
                if (GUILayout.Button(FIX, EditorStyles.miniButton))
                {
                    gameObject.AddComponent<PhotonView>();
                }
                EditorGUILayout.HelpBox(PHOTONVIEW, MessageType.Error);

                EditorGUILayout.Space();
            }
            if(targetType == TargetType.RpcTarget)
            {
                EditorGUILayout.PropertyField(spTargets, TARGET_OTHERS);
                if (targets == RpcTarget.All)
                {
                    EditorGUILayout.HelpBox(All, MessageType.Info, false);
                }
                else if (targets == RpcTarget.AllBuffered)
                {
                    EditorGUILayout.HelpBox(AllBuffered, MessageType.Info, false);
                }
                else if (targets == RpcTarget.AllBufferedViaServer)
                {
                    EditorGUILayout.HelpBox(AllBufferedViaServer, MessageType.Info, false);
                }
                else if (targets == RpcTarget.AllViaServer)
                {
                    EditorGUILayout.HelpBox(AllViaServer, MessageType.Info, false);
                }
                else if (targets == RpcTarget.MasterClient)
                {
                    EditorGUILayout.HelpBox(MasterClient, MessageType.Info, false);
                }
                else if (targets == RpcTarget.Others)
                {
                    EditorGUILayout.HelpBox(Others, MessageType.Info, false);
                }
                else if (targets == RpcTarget.OthersBuffered)
                {
                    EditorGUILayout.HelpBox(OthersBuffered, MessageType.Info, false);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(spTargetPlayer);
            }
            // EditorGUILayout.Space();
            EditorGUILayout.PropertyField(spData);
            EditorGUILayout.PropertyField(spMaster, ONLY_MASTER);
            EditorGUILayout.PropertyField(spSecure);
            if(secureRPC)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(spEncrypt);
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
