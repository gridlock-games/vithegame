#if PHOTON_UNITY_NETWORKING
namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun.UtilityScripts;
    using Photon.Pun;
    using NJG.PUN;
    using Photon.Realtime;

#if UNITY_EDITOR
    using UnityEditor;    
#endif

    [AddComponentMenu("")]
	public class ConditionPhotonPlayerScore : ICondition
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

        public TargetPhotonPlayer target = new TargetPhotonPlayer() { target = TargetPhotonPlayer.Target.Player };
        public Operation comparisson = Operation.Equal;
		public int scoreValue = 0;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool Check(GameObject target)
		{
            if (PhotonNetwork.InRoom)
            {
                Player player = this.target.GetPhotonPlayer(target);
                int score = player == null ? 0 : player.GetScore();
                switch (comparisson)
                {
                    case Operation.Equal: return score == this.scoreValue;
                    case Operation.NotEqual: return score != this.scoreValue;
                    case Operation.Greater: return score > this.scoreValue;
                    case Operation.GreaterOrEqual: return score >= this.scoreValue;
                    case Operation.Less: return score < this.scoreValue;
                    case Operation.LessOrEqual: return score <= this.scoreValue;
                }
            }

			return false;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Player Score";
		private const string NODE_TITLE = "{3} Score is {0} {1} {2}";

		// PROPERTIES: ----------------------------------------------------------------------------

		private SerializedProperty spTarget;
		private SerializedProperty spOperation;
        private SerializedProperty spValue;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
            string mid = "than";
            if (comparisson == Operation.Equal || comparisson == Operation.NotEqual) mid = "to";
			return string.Format(NODE_TITLE, this.comparisson, mid, this.scoreValue, this.target);
		}

		protected override void OnEnableEditorChild ()
		{
			this.spTarget = this.serializedObject.FindProperty("target");
			this.spOperation = this.serializedObject.FindProperty("comparisson");
            this.spValue = this.serializedObject.FindProperty("scoreValue");
        }

		protected override void OnDisableEditorChild ()
		{
			this.spTarget = null;
			this.spOperation = null;
            this.spValue = null;
        }

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();
            
            EditorGUILayout.PropertyField(this.spTarget);
            EditorGUILayout.PropertyField(this.spOperation);
            EditorGUILayout.PropertyField(this.spValue);

            this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
#endif