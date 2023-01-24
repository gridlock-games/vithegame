using NJG.PUN.UI;

namespace GameCreator.Core
{
    using UnityEngine;
#if UNITY_EDITOR

#endif

    [AddComponentMenu("")]
    public class ConditionIsChatOpen : ICondition
    {
        public bool isOpen = false;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool Check(GameObject target)
        {
            return RoomChat.IsOpen() == isOpen;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public static new string NAME = "Photon/Is Chat Open";
        private const string NODE_TITLE = "Is Chat {0}Open";

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, isOpen ? string.Empty : "NOT ");
        }

#endif
    }
}
