namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using GameCreator.Core;
    using GameCreator.Variables;
    using Photon.Pun;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ActionPhotonConnect : IAction
	{
        public enum ConnectType
        {
            UsingSettings,
            Region,
            BestCloudServer
        }

        public enum RegionCode
        {
            Asia,
            Australia,
            CanadaEast,
            Chinese,
            Europe,
            India,
            Japan,
            Russia,
            RussiaEast,
            SoutAmerica,
            SouthKorea,
            USAEast,
            USAWest
        }

        public ConnectType method = ConnectType.UsingSettings;
        public RegionCode region = RegionCode.USAEast;
        public StringProperty gameVersion = new StringProperty("0");

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index, params object[] parameters)
        {
            if(!string.IsNullOrEmpty(gameVersion.GetValue(target))) PhotonNetwork.GameVersion = gameVersion.GetValue(target);

            bool result = false;

            switch (method)
            {
                case ConnectType.UsingSettings: result = PhotonNetwork.ConnectUsingSettings(); break;
                case ConnectType.Region: result = PhotonNetwork.ConnectToRegion(GetRegionCode(region)); break;
                case ConnectType.BestCloudServer: result = PhotonNetwork.ConnectToBestCloudServer(); break;
            }
            return result;
        }

        private string GetRegionCode(RegionCode region)
        {
            switch (region)
            {
                case RegionCode.Asia: return "asia";
                case RegionCode.Australia: return "au";
                case RegionCode.CanadaEast: return "cae";
                case RegionCode.Chinese: return "cn";
                case RegionCode.Europe: return "eu";
                case RegionCode.India: return "in";
                case RegionCode.Japan: return "jp";
                case RegionCode.Russia: return "ru";
                case RegionCode.RussiaEast: return "rue";
                case RegionCode.SoutAmerica: return "sa";
                case RegionCode.SouthKorea: return "kr";
                case RegionCode.USAEast: return "us";
                case RegionCode.USAWest: return "usw";
            }

            return "us";
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Photon Connect";
		private const string NODE_TITLE = "Photon Connect UsingSettings with version: {0}";
		private const string NODE_TITLE2 = "Photon Connect to BestCloudServer with version: {0}";
		private const string NODE_TITLE3 = "Photon Connect to Region: '{0}' with version: {1}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spVersion;
		private SerializedProperty spType;
		private SerializedProperty spRegion;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
            string title = string.Format(NODE_TITLE, this.gameVersion);

            if (this.method == ConnectType.BestCloudServer)
            {
                title = string.Format(NODE_TITLE2, this.gameVersion);
            }
            else if (this.method == ConnectType.Region)
            {
                title = string.Format(NODE_TITLE3, this.region, this.gameVersion);
            }

            return title;
        }    

		protected override void OnEnableEditorChild ()
		{
			this.spType = this.serializedObject.FindProperty("method");
            this.spVersion = this.serializedObject.FindProperty("gameVersion");
            this.spRegion = this.serializedObject.FindProperty("region");
        }

		protected override void OnDisableEditorChild ()
		{
			this.spType = null;
			this.spVersion = null;
			this.spRegion = null;
        }

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

			EditorGUILayout.PropertyField(this.spType);
			if(method == ConnectType.Region) EditorGUILayout.PropertyField(this.spRegion);
            EditorGUILayout.PropertyField(this.spVersion);

            this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
