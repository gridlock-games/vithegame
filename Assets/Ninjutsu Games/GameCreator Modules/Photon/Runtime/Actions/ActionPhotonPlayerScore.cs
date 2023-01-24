namespace GameCreator.Core
{
    using UnityEngine;
    using GameCreator.Variables;
    using Photon.Pun.UtilityScripts;
    using Photon.Pun;
    using NJG.PUN;
    using Photon.Realtime;

#if UNITY_EDITOR
    using UnityEditor;
    
#endif

    [AddComponentMenu("")]
	public class ActionPhotonPlayerScore : IAction
	{
        public enum SetType
        {
            Add,
            Set
        }

        public TargetPhotonPlayer target = new TargetPhotonPlayer() { target = TargetPhotonPlayer.Target.Player };
        public SetType operation = SetType.Add;
        public IntProperty amount = new IntProperty(1);

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            Player player = this.target.GetPhotonPlayer(target);

            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("Cannot add score. You need to be connected to Photon and inside a room.");
                return true;
            }

            if (PhotonNetwork.InRoom && player != null)
            {
                switch (this.operation)
                {
                    case SetType.Add: player.AddScore(this.amount.GetValue(target)); break;
                    case SetType.Set: player.SetScore(this.amount.GetValue(target)); break;
                }
            }
            
            return true;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Player Score";
		private const string NODE_TITLE = "{0} {1} Score to {2}";

		// PROPERTIES: ----------------------------------------------------------------------------

		private SerializedProperty spOperation;
		private SerializedProperty spAmount;
		private SerializedProperty spTarget;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
			return string.Format(NODE_TITLE, this.operation, this.amount, this.target);
		}

		protected override void OnEnableEditorChild ()
		{
			this.spTarget = this.serializedObject.FindProperty("target");
			this.spOperation = this.serializedObject.FindProperty("operation");
            this.spAmount = this.serializedObject.FindProperty("amount");
        }

		protected override void OnDisableEditorChild ()
		{
			this.spTarget = null;
			this.spOperation = null;
            this.spAmount = null;
        }

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

			EditorGUILayout.PropertyField(this.spTarget);
			EditorGUILayout.PropertyField(this.spOperation);
            EditorGUILayout.PropertyField(this.spAmount);

            this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
