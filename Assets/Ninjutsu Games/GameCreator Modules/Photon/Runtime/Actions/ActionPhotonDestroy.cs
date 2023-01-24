#if PHOTON_UNITY_NETWORKING
namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ActionPhotonDestroy : IAction
	{
        public TargetGameObject target = new TargetGameObject();
        public bool transferOwnership = false;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            var go = this.target.GetGameObject(target);

            // Don't remove the GO if it doesn't have any PhotonView
            PhotonView[] views = go.GetComponentsInChildren<PhotonView>(true);
            if (views == null || views.Length <= 0)
            {
                Debug.LogError("Failed to 'network-remove' GameObject because has no PhotonView components: " + go);
                return false;
            }

            if (views[0].IsMine || PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(go);
            }
            else if(transferOwnership)
            {
                views[0].TransferOwnership(PhotonNetwork.LocalPlayer);
                PhotonNetwork.Destroy(go);
            }
            else
            {
                Debug.LogError("Cannot Destroy! You don't have the rights to destroy this: " + go);
            }
            
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Photon Destroy";
		private const string NODE_TITLE = "Photon Destroy {0}";

		// PROPERTIES: ----------------------------------------------------------------------------

		private SerializedProperty spTarget;
		private SerializedProperty spTransfer;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
			return string.Format(NODE_TITLE, this.target);
		}

		protected override void OnEnableEditorChild ()
		{
			this.spTarget = this.serializedObject.FindProperty("target");
			this.spTransfer = this.serializedObject.FindProperty("transferOwnership");
        }

		protected override void OnDisableEditorChild ()
		{
			this.spTarget = null;
			this.spTransfer = null;
        }

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

			EditorGUILayout.PropertyField(this.spTarget);
			EditorGUILayout.PropertyField(this.spTransfer, new GUIContent("Force Destroy", "If TRUE and if you are not the owner of this object it will transfer the ownership to be able to destroy it"));

            this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
#endif