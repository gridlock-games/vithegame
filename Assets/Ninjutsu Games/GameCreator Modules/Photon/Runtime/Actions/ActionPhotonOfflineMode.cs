namespace GameCreator.Core
{
    using UnityEngine;
    using NJG.PUN;
    using Variables;
    using Photon.Pun;
    using Photon.Realtime;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ActionPhotonOfflineMode : IAction
	{
        public BoolProperty offline = new BoolProperty(true);

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
	        PhotonNetwork.OfflineMode = offline.GetValue(target);
            return true;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Offline Mode";
		private const string NODE_TITLE = "Set Offline Mode {0}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spTarget;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
            return string.Format(NODE_TITLE, offline);
        }

		protected override void OnEnableEditorChild ()
		{
			spTarget = serializedObject.FindProperty("offline");
        }

		protected override void OnDisableEditorChild ()
		{
			spTarget = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(spTarget);

            serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
