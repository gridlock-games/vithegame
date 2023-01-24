namespace GameCreator.Core
{
    using UnityEngine;
    using GameCreator.Variables;
    using Photon.Pun;

    [AddComponentMenu("")]
    public class IgniterOnPhotonStatus : Igniter
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Connection State";
        public new static bool REQUIRES_COLLIDER = false;
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        private Photon.Realtime.ClientState lastState;

        public VariableProperty stateVariable = new VariableProperty();
        //public string localVariableName = "status";
        //private LocalVariables vars;

        new private void Awake()
        {
#if UNITY_EDITOR
            this.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
            //vars = GetComponent<LocalVariables>();
        }

        private void Update()
        {
            if(lastState != PhotonNetwork.NetworkClientState)
            {
                lastState = PhotonNetwork.NetworkClientState;
                if (stateVariable != null)
                {
                    if(stateVariable.variableType == VariableProperty.GetVarType.LocalVariable)
                    {
                        stateVariable.local.Set(lastState.ToString(), gameObject);
                    }
                    else
                    {
                        stateVariable.global.Set(lastState.ToString(), gameObject);
                    }
                    //variable.ToStringValue(gameObject)
                    //Variable var = vars.Get(localVariableName);
                    //if(var != null) var.Set(Variable.DataType.String, lastState.ToString());
                }
                ExecuteTrigger(gameObject);
            }
        }
    }
}