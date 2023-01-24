namespace NJG.PUN
{
    using GameCreator.Core;
    using GameCreator.Variables;
    using Photon.Pun;
    using Photon.Realtime;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("")]
    public class IgniterOnPhotonInstantiate : Igniter, IPunInstantiateMagicCallback
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Photon Instantiate";
        public new static bool REQUIRES_COLLIDER = false;
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public new static string COMMENT = "Instantiation Data needs to match the same amount/type of variables from the instantiate action";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        private bool executed = false;

        [Space]
        [VariableFilter(Variable.DataType.GameObject)]
        public VariableProperty storeOwner = new VariableProperty(Variable.VarType.GlobalVariable);
        
        public PhotonReceiveData instantiationData = new PhotonReceiveData();

        protected new void OnValidate()
        {
            executed = false;
#if UNITY_EDITOR
            hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
        }

        public void ManualExecute(GameObject invoker, PhotonMessageInfo info)
        {
            if (!executed)
            {
                Execute(info);
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            Execute(info);
        }

        private void Execute(PhotonMessageInfo info)
        {
            var invoker = (GameObject) info.Sender.TagObject;
            instantiationData.FromObject(info.photonView.gameObject, info.photonView.InstantiationData);
            storeOwner?.Set(invoker, gameObject);
            ExecuteTrigger(invoker);
            executed = true;
        }
    }
}