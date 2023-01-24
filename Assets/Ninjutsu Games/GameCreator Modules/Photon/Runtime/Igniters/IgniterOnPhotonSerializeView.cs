#if PHOTON_UNITY_NETWORKING

namespace NJG.PUN
{
    using GameCreator.Core;
    using Photon.Pun;
    using UnityEngine;

    [AddComponentMenu("")]
    public class IgniterOnPhotonSerializeView : Igniter, IPunObservable
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Serialize View";
        public new static bool REQUIRES_COLLIDER = false;
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        public enum SerializationType
        {
            Write,
            Read
        }
        public SerializationType serialization = SerializationType.Write;
        public TargetPhotonView target;

        private PhotonView photonView;

        new private void OnEnable()
        {
#if UNITY_EDITOR
            this.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif

            if (!photonView)
            {
                photonView = target.GetView(gameObject);
            }

            if (!photonView)
            {
                Debug.LogWarningFormat("[OnPhotonSerializeView] Couldn't find any PhotonView on {0}", target);
                return;
            }

            if (!photonView.ObservedComponents.Contains(this))
            {
                photonView.ObservedComponents.Add(this);
            }

            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if ((stream.IsWriting && serialization == SerializationType.Write) || 
                (stream.IsReading && serialization == SerializationType.Read))
            {
                ExecuteTrigger(gameObject);
            }
        }
    }
}

#endif