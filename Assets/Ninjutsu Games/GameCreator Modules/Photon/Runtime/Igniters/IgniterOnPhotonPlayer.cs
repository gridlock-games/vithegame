namespace NJG.PUN
{
    using GameCreator.Core;
    using GameCreator.Variables;
    using Photon.Pun;
    using Photon.Realtime;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("")]
    public class IgniterOnPhotonPlayer : Igniter
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Player Instantiate";
        public new static bool REQUIRES_COLLIDER = false;
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public new static string COMMENT = "Instantiation Data needs to match the same amount/type of variables from the instantiate action";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        public bool localPlayer = true;

        [Space]
        [VariableFilter(Variable.DataType.GameObject)]
        public VariableProperty storeOwner = new VariableProperty(Variable.VarType.GlobalVariable);
        
        public PhotonReceiveData instantiationData = new PhotonReceiveData();

        new private void OnEnable()
        {
#if UNITY_EDITOR
            hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
            CharacterNetwork.OnPlayerInstantiated.AddListener(OnPlayerInstantiated);
        }

        private void OnDisable()
        {
            CharacterNetwork.OnPlayerInstantiated.RemoveListener(OnPlayerInstantiated);
        }

        private void OnPlayerInstantiated(CharacterNetwork character, PhotonMessageInfo info)
        {
            if(localPlayer && !info.photonView.IsMine) return;
            
            var invoker = (GameObject) info.Sender.TagObject;
            instantiationData.FromObject(info.photonView.gameObject, info.photonView.InstantiationData);
            storeOwner?.Set(invoker, gameObject);
            ExecuteTrigger(invoker);
        }
    }
}