namespace GameCreator.Core
{
    using UnityEngine;
    using Variables;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;

#endif

    [AddComponentMenu("")]
	public class ConditionPhotonPlayerCount : ICondition
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
        public Operation comparisson = Operation.Equal;
        public IntProperty count = new IntProperty();

		// EXECUTABLE: ----------------------------------------------------------------------------

		public override bool Check(GameObject target)
		{
            if (PhotonNetwork.InRoom)
            {
                int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
                switch (comparisson)
                {
                    case Operation.Equal: return playerCount == count.GetValue(target);
                    case Operation.NotEqual: return playerCount != count.GetValue(target);
                    case Operation.Greater: return playerCount > count.GetValue(target);
                    case Operation.GreaterOrEqual: return playerCount >= count.GetValue(target);
                    case Operation.Less: return playerCount < count.GetValue(target);
                    case Operation.LessOrEqual: return playerCount <= count.GetValue(target);
                }
            }

            return false;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Player Count";
		private const string NODE_TITLE = "Player Count is {0} {1} {2}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spOperation;
        private SerializedProperty spValue;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
            string mid = "than";
            if (comparisson == Operation.Equal || comparisson == Operation.NotEqual) mid = "to";
            return string.Format(NODE_TITLE, comparisson, mid, count);
        }

        protected override void OnEnableEditorChild()
        {
            spOperation = serializedObject.FindProperty("comparisson");
            spValue = serializedObject.FindProperty("count");
        }

        protected override void OnDisableEditorChild()
        {
            spOperation = null;
            spValue = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spOperation);
            EditorGUILayout.PropertyField(spValue);

            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
