namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ConditionPhotonRoomState : ICondition
	{
		public enum Operation
		{
			IsOpen,
			IsVisible,
			IsOffline
		}
		public Operation operation = Operation.IsOpen;
		public bool isPositive = true;

		// EXECUTABLE: ----------------------------------------------------------------------------

		public override bool Check(GameObject target)
		{
			if (PhotonNetwork.InRoom)
			{
				switch (operation)
				{
					case Operation.IsOpen: return PhotonNetwork.CurrentRoom.IsOpen == isPositive;
					case Operation.IsVisible: return PhotonNetwork.CurrentRoom.IsVisible == isPositive;
					case Operation.IsOffline: return PhotonNetwork.CurrentRoom.IsOffline == isPositive;
				}
			}
            return false;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Room State";
		private const string NODE_TITLE = "Roomt State {0} is {1}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spOperation;
        private SerializedProperty spSatisfied;

		// INSPECTOR METHODS: ---------------------------------------------------------------------

		public override string GetNodeTitle()
		{
            return string.Format(NODE_TITLE, this.operation, this.isPositive);
		}

		protected override void OnEnableEditorChild ()
		{
			this.spOperation = this.serializedObject.FindProperty("operation");
			this.spSatisfied = this.serializedObject.FindProperty("isPositive");
		}

		protected override void OnDisableEditorChild ()
		{
			this.spSatisfied = null;
			this.spOperation = null;
		}

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

			EditorGUILayout.PropertyField(this.spOperation);
			EditorGUILayout.PropertyField(this.spSatisfied);

			this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
