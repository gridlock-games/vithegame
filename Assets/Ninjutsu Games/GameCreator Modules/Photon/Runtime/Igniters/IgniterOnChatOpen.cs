using System.Collections;
using System.Collections.Generic;
using NJG.PUN;
using NJG.PUN.UI;
using Photon.Realtime;
using UnityEngine;

namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using GameCreator.Core.Hooks;
    using ExitGames.Client.Photon;
    using Photon.Pun;

    [AddComponentMenu("")]
    public class IgniterOnChatOpen : Igniter
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Chat Open";
        public new static bool REQUIRES_COLLIDER = false;
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        protected override void Awake()
        {
            base.Awake();
            RoomChat.Instance.onOpen.AddListener(OnChatOpen);
        }

        private void OnChatOpen()
        {
            ExecuteTrigger(gameObject);
        }
    }
}