using NJG.PUN;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;

#if PHOTON_UNITY_NETWORKING
namespace GameCreator.Core
{
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ConditionPhotonPlayerNumber : ICondition
	{
        public enum Operation
        {
            Equal,
            NotEqual,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual
        }

        public TargetPhotonPlayer target = new TargetPhotonPlayer { target = TargetPhotonPlayer.Target.Player };
        public Operation comparisson = Operation.Equal;
		public int playerNumber = 1;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool Check(GameObject target)
		{
            if (PhotonNetwork.InRoom)
            {
                Player player = this.target.GetPhotonPlayer(target);
                int playerNumber = player.GetPlayerNumber();// player == null ? -1 : System.Array.IndexOf(NetworkManager.Instance.Players, player) + 1;
                switch (comparisson)
                {
                    case Operation.Equal: return playerNumber == this.playerNumber;
                    case Operation.NotEqual: return playerNumber != this.playerNumber;
                    case Operation.Greater: return playerNumber > this.playerNumber;
                    case Operation.GreaterOrEqual: return playerNumber >= this.playerNumber;
                    case Operation.Less: return playerNumber < this.playerNumber;
                    case Operation.LessOrEqual: return playerNumber <= this.playerNumber;
                }
            }

			return false;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Player Number";
		private const string NODE_TITLE = "{3} Number is {0} {1} {2}";

		// PROPERTIES: ----------------------------------------------------------------------------

		private SerializedProperty spOperation;
		private SerializedProperty spValue;
		private SerializedProperty spTarget;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
            string mid = "than";
            if (comparisson == Operation.Equal || comparisson == Operation.NotEqual) mid = "to";
			return string.Format(NODE_TITLE, comparisson, mid, playerNumber, target);
		}

		protected override void OnEnableEditorChild ()
		{
			spTarget = serializedObject.FindProperty("target");
			spOperation = serializedObject.FindProperty("comparisson");
            spValue = serializedObject.FindProperty("playerNumber");
        }

		protected override void OnDisableEditorChild ()
		{
			spTarget = null;
			spOperation = null;
            spValue = null;
        }

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
            
            EditorGUILayout.PropertyField(spTarget);
            EditorGUILayout.PropertyField(spOperation);
            EditorGUILayout.PropertyField(spValue);
            //this.spValue.intValue = Mathf.Max(1, this.spValue.intValue);

            serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
#endif