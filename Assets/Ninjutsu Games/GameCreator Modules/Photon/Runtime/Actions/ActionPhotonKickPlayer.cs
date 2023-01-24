namespace GameCreator.Core
{
    using UnityEngine;
    using NJG.PUN;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;

#endif

    [AddComponentMenu("")]
	public class ActionPhotonKickPlayer : IAction
	{
        public TargetPhotonPlayer player = new TargetPhotonPlayer();

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                return PhotonNetwork.CloseConnection(player.GetPhotonPlayer(target));
            }

            return true;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Kick Player";
		private const string NODE_TITLE = "Kick {0} from room";

		// PROPERTIES: ----------------------------------------------------------------------------

		private SerializedProperty spPlayer;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
			return string.Format(NODE_TITLE, this.player);
		}

		protected override void OnEnableEditorChild ()
		{
            this.spPlayer = this.serializedObject.FindProperty("player");
        }

		protected override void OnDisableEditorChild ()
		{
			this.spPlayer = null;
        }

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spPlayer);
            EditorGUILayout.HelpBox("Only the Master Client can do this.", MessageType.None);

            this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
