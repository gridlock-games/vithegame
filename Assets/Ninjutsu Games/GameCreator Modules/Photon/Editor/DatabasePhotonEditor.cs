namespace NJG.PUN
{
    using ExitGames.Client.Photon;
    using GameCreator.Core;
    using GameCreator.ModuleManager;
    using Photon.Pun;
    using Photon.Realtime;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    [CustomEditor(typeof(DatabasePhoton))]
    public class DatabasePhotonEditor : IDatabaseEditor
    {
        private const string SETTINGS_TITLE = "Photon Unity Network Settings";
        private const string ICONS_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/";
        private const string KEY_SIDEBAR_INDEX = "photon-tab-index";
        private const string SEARCHBOX_NAME = "searchbox";

        private const string TITLE = "{0} Module";
        private const string VERSION = "Version {0}";

        private static readonly GUIContent[] TAB_NAMES = new GUIContent[]
        {
            new GUIContent("Info"),
            new GUIContent("Prefabs"),
            new GUIContent("Attachments"),
            new GUIContent("Settings")
        };

        // PROPERTIES: -------------------------------------------------------------------------------------------------

        private int tabIndex = 0;

        private GUIStyle searchFieldStyle;
        private GUIStyle searchCloseOnStyle;
        private GUIStyle searchCloseOffStyle;

        public string searchText = "";
        public bool searchFocus = true;

        private DatabasePhoton inst;
        private ReorderableList prefabList;
        private ReorderableList attachmentList;
        private Texture2D statusIcon;

        private ModuleManifest module;
        //private SerializedProperty spMessageTarget;
        private string lastSearch;
        private SerializedProperty spMonoCache;

        bool isMaster = false;

        int lastMsgIn = 0;
        int lastMsgOut = 0;
        int lastMsgTotal = 0;

        int lastBytesCommand_In = 0;
        int lastBytesCommand_Out = 0;

        float lastSecTime = 0f;

        int perSecMsgIn = 0;
        int perSecMsgOut = 0;
        int perSecMsgTotal = 0;

        float perSecBytesCommand_Greater = 0;
        float perSecBytesCommand_In = 0;
        float perSecBytesCommand_Out = 0;
        private int lastOutgoingCount;
        private int lastIncomingCount;

        // INITIALIZE: -------------------------------------------------------------------------------------------------

        private void OnEnable()
        {
            inst = target as DatabasePhoton;
            if (inst == null) return;

            prefabList = new ReorderableList(GetList(inst.prefabs), typeof(GameObject), true, true, false, true);
            prefabList.drawHeaderCallback += rect => GUI.Label(rect, new GUIContent("Photon Instantiate Prefabs"));
            prefabList.onAddCallback += l =>
            {
                prefabList.list.Add(null);
            };
            prefabList.onSelectCallback += (ReorderableList list) =>
            {
                var prefab = list.list[list.index] as GameObject;
                if (prefab) EditorGUIUtility.PingObject(prefab.gameObject);
            };
            prefabList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;

                if (index >= inst.prefabs.Count) return;

                Rect nameRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                var prefab = inst.prefabs[index];
                EditorGUI.LabelField(nameRect, prefab != null ? prefab.name : "(undefined)");
            };

            attachmentList = new ReorderableList(GetList(inst.attachments), typeof(GameObject), true, true, true, true);
            attachmentList.drawHeaderCallback += rect => GUI.Label(rect, new GUIContent("Attachments"));
            attachmentList.onSelectCallback += (ReorderableList list) =>
            {
                var prefab = list.list[list.index] as GameObject;
                if (prefab) EditorGUIUtility.PingObject(prefab.gameObject);
            };
            attachmentList.onAddCallback += l =>
            {
                attachmentList.list.Add(null);
            };
            attachmentList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;

                if (index >= inst.attachments.Count) return;

                Rect nameRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                inst.attachments[index] = (GameObject)EditorGUI.ObjectField(nameRect, inst.attachments[index], typeof(GameObject), false);
            };

            this.tabIndex = EditorPrefs.GetInt(KEY_SIDEBAR_INDEX, 0);
            this.module = ModuleManager.GetModuleManifest("com.ninjutsugames.modules.photon");

            //this.spMessageTarget = serializedObject.FindProperty("messageTarget");
            this.spMonoCache = serializedObject.FindProperty("monobehaviourCache");
        }

        private List<GameObject> GetList(List<GameObject> defaultList)
        {
            searchText = searchText.ToLower();
            List<GameObject> suggestions = defaultList;

            if (!string.IsNullOrEmpty(searchText))
            {
                suggestions = new List<GameObject>();
                for (int i = 0; i < defaultList.Count; ++i)
                {
                    if (defaultList[i].name.ToLower().Contains(searchText))
                    {
                        suggestions.Add(defaultList[i]);
                    }
                }
            }

            return suggestions;
        }

        // OVERRIDE METHODS: -------------------------------------------------------------------------------------------

        public override string GetDocumentationURL()
        {
            return "https://njg.gitbook.io/gc-modules/photon-network/photon-network";
        }

        public override string GetName()
        {
            return "Photon";
        }

        public override bool CanBeDecoupled()
        {
            return true;
        }

        // GUI METHODS: ------------------------------------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            inst = target as DatabasePhoton;
            this.OnPreferencesWindowGUI();
        }

        public override void OnPreferencesWindowGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.LabelField(string.Format(TITLE, GetName()), EditorStyles.boldLabel);
            if (module != null) EditorGUILayout.HelpBox(string.Format(VERSION, module.module.version), MessageType.None);

            int prevTabIndex = this.tabIndex;
            this.tabIndex = GUILayout.Toolbar(this.tabIndex, TAB_NAMES);
            if(prevTabIndex != this.tabIndex)
            {
                EditorPrefs.SetInt(KEY_SIDEBAR_INDEX, this.tabIndex);
                prevTabIndex = this.tabIndex;
                this.ResetSearch();
            }

            switch (tabIndex)
            {
                case 0: this.PaintDebug(); break;
                case 1: this.PaintPrefabs(); break;
                case 2: this.PaintAttachments(); break;                
                case 3: this.PaintSettings(); break;
            }

            this.serializedObject.ApplyModifiedProperties();
        }

        private void PaintPrefabs()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();

            EditorGUILayout.HelpBox("Prefabs on this list will be added automatically to the Photon prefab cache list.\n" +
                "Keeps references to GameObjects for frequent instantiation (out of memory instead of loading the Resources).", MessageType.Info);

            this.PaintSearch();

            if(lastSearch != searchText)
            {
                prefabList.list = GetList(inst.prefabs);
                lastSearch = searchText;
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            DropAreaGUI();
            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.BeginVertical();
            prefabList.DoLayoutList();
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            /*int itemsCatalogueSize = this.spItems.arraySize;
            if (itemsCatalogueSize == 0)
            {
                EditorGUILayout.HelpBox(MSG_EMPTY_CATALOGUE, MessageType.Info);
            }
            */
        }

        private void PaintAttachments()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();

            EditorGUILayout.HelpBox("To be able to sync Character attachments you need to register them in this list.", MessageType.Info);

            this.PaintSearch();

            if (lastSearch != searchText)
            {
                attachmentList.list = GetList(inst.attachments);
                lastSearch = searchText;
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            DropAreaGUI(false);
            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.BeginVertical();
            attachmentList.DoLayoutList();
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void PaintDebug()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("You need to be playing Unity, connected to photon and inside a room in order to see anything here.", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField("Photon Status", EditorStyles.boldLabel, GUILayout.MinWidth(20));            
            EditorGUILayout.LabelField("Ping", EditorStyles.boldLabel, GUILayout.MinWidth(20));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            statusIcon = EditorIconUtils.GetStatusIcon(PhotonNetwork.NetworkClientState);
            EditorGUILayout.LabelField(new GUIContent(PhotonNetwork.NetworkClientState.ToString(), statusIcon), GUILayout.MinWidth(20));
            EditorGUILayout.LabelField(PhotonNetwork.InRoom ? PhotonNetwork.GetPing().ToString() : "0", GUILayout.MinWidth(20));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);
            if (!PhotonNetwork.InRoom) EditorGUILayout.HelpBox("Not in a room", MessageType.None);
            int masterId = PhotonNetwork.MasterClient == null ? -1 : PhotonNetwork.MasterClient.ActorNumber;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(string.Format("{0:00} Nickname: {1}{2}{3} Properties: {4}", player.ActorNumber, string.IsNullOrEmpty(player.NickName) ? "Undefined" : player.NickName, 
                    player.IsInactive ? " (inactive)" : "", (player.IsLocal ? " - [You] -" : "") + (masterId == player.ActorNumber ? " [Master]" : ""), player.CustomProperties.ToStringFull()));

                EditorGUI.BeginDisabledGroup(!PhotonNetwork.IsMasterClient);
                if (GUILayout.Button("Kick", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    PhotonNetwork.CloseConnection(player);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Network Statistics", EditorStyles.boldLabel);
            if (PhotonNetwork.InRoom)
            {
                TrafficStatsGameLevel gls = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsGameLevel;
                long elapsedMs = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsElapsedMs / 1000;

                int averageOut = gls.TotalOutgoingMessageCount - lastOutgoingCount;
                int averageIn = gls.TotalIncomingMessageCount - lastIncomingCount;

                if (elapsedMs == 0)
                {
                    elapsedMs = 1;
                }

                if (elapsedMs == 1)
                {
                    averageOut = gls.TotalOutgoingMessageCount - lastOutgoingCount;
                    averageIn = gls.TotalIncomingMessageCount - lastIncomingCount;

                    lastOutgoingCount = gls.TotalOutgoingMessageCount;
                    lastIncomingCount = gls.TotalIncomingMessageCount;
                }

                float newTime = Time.time;
                if (newTime - lastSecTime > 1f)
                {
                    isMaster = PhotonNetwork.IsMasterClient;

                    perSecMsgIn = gls.TotalIncomingMessageCount - lastMsgIn;
                    perSecMsgOut = gls.TotalOutgoingMessageCount - lastMsgOut;
                    perSecMsgTotal = gls.TotalMessageCount - lastMsgTotal;

                    perSecBytesCommand_In = (PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsIncoming.TotalCommandBytes - lastBytesCommand_In) / 1024f;
                    perSecBytesCommand_Out = (PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsOutgoing.TotalCommandBytes - lastBytesCommand_Out) / 1024f;

                    lastMsgIn = gls.TotalIncomingMessageCount;
                    lastMsgOut = gls.TotalOutgoingMessageCount;
                    lastMsgTotal = gls.TotalMessageCount;

                    lastBytesCommand_In = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsIncoming.TotalCommandBytes;
                    lastBytesCommand_Out = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsOutgoing.TotalCommandBytes;

                    lastSecTime = newTime;
                }

                perSecBytesCommand_Greater = Mathf.Max(perSecBytesCommand_In, perSecBytesCommand_Out);

                string total =
                    $"Total Messages: Out {gls.TotalOutgoingMessageCount,4} | In {gls.TotalIncomingMessageCount,4} | Sum {gls.TotalMessageCount,4}";
                //string elapsedTime = string.Format("{0}sec average:", elapsedMs);
                //string average = string.Format("<b>Messages Per Sec:</b> Out {0,4} | In {1,4} | Sum {2,4}", gls.TotalOutgoingMessageCount / elapsedMs, gls.TotalIncomingMessageCount / elapsedMs, gls.TotalMessageCount / elapsedMs);
                string average = $"Messages Per Sec: Out {averageOut,4} | In {averageIn,4}";
                string bytesSummary =
                    $"Usage: In {perSecBytesCommand_In:F2}kB/sec | Out {perSecBytesCommand_Out:F2}kB/sec";
                string isMasterSummary = "Master: " + isMaster;
                //string elapsedTime = string.Format("{0} sec average:", elapsedMs);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(isMasterSummary);
                EditorGUILayout.LabelField(bytesSummary);
                EditorGUILayout.LabelField(total);
                EditorGUILayout.LabelField(average);
                EditorGUI.indentLevel--;
                //PhotonNetwork.NetworkStatisticsEnabled = EditorGUILayout.Toggle("Enabled", PhotonNetwork.NetworkStatisticsEnabled);
                //EditorGUILayout.LabelField(PhotonNetwork.InRoom ? PhotonNetwork.NetworkStatisticsToString() : string.Empty);
            }
            else EditorGUILayout.HelpBox("Not in a room", MessageType.None);
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Room Information", EditorStyles.boldLabel);
            if (!PhotonNetwork.InRoom) EditorGUILayout.HelpBox("Not in a room", MessageType.None);
            else
            {
                EditorGUI.indentLevel++;
                if (PhotonNetwork.InRoom)
                {
                    EditorGUILayout.LabelField($"Name: {PhotonNetwork.CurrentRoom.Name}");
                    EditorGUILayout.LabelField($"Visible: {PhotonNetwork.CurrentRoom.IsVisible}");
                    EditorGUILayout.LabelField($"Open: {PhotonNetwork.CurrentRoom.IsOpen}");
                    EditorGUILayout.LabelField($"Is Offline: {PhotonNetwork.CurrentRoom.IsOffline}");
                    EditorGUILayout.LabelField(
                        $"Max Players: {(PhotonNetwork.CurrentRoom.MaxPlayers == 0 ? "Unlimited" : PhotonNetwork.CurrentRoom.MaxPlayers.ToString())}");

                    EditorGUILayout.LabelField("Custom Properties:");
                    EditorGUI.indentLevel++;
                    int index = 0;
                    foreach (var p in PhotonNetwork.CurrentRoom.CustomProperties)
                    {
                        EditorGUILayout.LabelField($"[{index}] {p.Key} : {p.Value}");
                        index++;
                    }
                    EditorGUI.indentLevel--;
                    
                    EditorGUILayout.LabelField("Lobby Properties:");
                    EditorGUI.indentLevel++;
                    index = 0;
                    foreach (var p in PhotonNetwork.CurrentRoom.PropertiesListedInLobby)
                    {
                        EditorGUILayout.LabelField($"[{index}] {p}");
                        index++;
                    }
                    EditorGUI.indentLevel--;
                }
                //SupportClass.DictionaryToString(origin, false)
                //EditorGUILayout.LabelField(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.ToStringFull() : string.Empty);
                EditorGUI.indentLevel--;
            }
            /*EditorGUILayout.LabelField("Room Properties", EditorStyles.boldLabel);
            if (PhotonNetwork.inRoom)
            {
                foreach (var prop in PhotonNetwork.room.CustomProperties)
                {
                    EditorGUILayout.LabelField(prop.Key + " = " + prop.Value);
                }
            }*/
            EditorGUILayout.EndVertical();
        }

        /*public void TrafficStatsWindow(int windowID)
        {
            bool statsToLog = false;
            TrafficStatsGameLevel gls = PhotonNetwork.networkingPeer.TrafficStatsGameLevel;
            long elapsedMs = PhotonNetwork.networkingPeer.TrafficStatsElapsedMs / 1000;
            if (elapsedMs == 0)
            {
                elapsedMs = 1;
            }

            GUILayout.BeginHorizontal();
            this.buttonsOn = GUILayout.Toggle(this.buttonsOn, "buttons");
            this.healthStatsVisible = GUILayout.Toggle(this.healthStatsVisible, "health");
            this.trafficStatsOn = GUILayout.Toggle(this.trafficStatsOn, "traffic");
            GUILayout.EndHorizontal();

            string total = string.Format("Out {0,4} | In {1,4} | Sum {2,4}", gls.TotalOutgoingMessageCount, gls.TotalIncomingMessageCount, gls.TotalMessageCount);
            string elapsedTime = string.Format("{0}sec average:", elapsedMs);
            string average = string.Format("Out {0,4} | In {1,4} | Sum {2,4}", gls.TotalOutgoingMessageCount / elapsedMs, gls.TotalIncomingMessageCount / elapsedMs, gls.TotalMessageCount / elapsedMs);
            GUILayout.Label(total);
            GUILayout.Label(elapsedTime);
            GUILayout.Label(average);

            if (this.buttonsOn)
            {
                GUILayout.BeginHorizontal();
                this.statsOn = GUILayout.Toggle(this.statsOn, "stats on");
                if (GUILayout.Button("Reset"))
                {
                    PhotonNetwork.networkingPeer.TrafficStatsReset();
                    PhotonNetwork.networkingPeer.TrafficStatsEnabled = true;
                }
                statsToLog = GUILayout.Button("To Log");
                GUILayout.EndHorizontal();
            }

            string trafficStatsIn = string.Empty;
            string trafficStatsOut = string.Empty;
            if (this.trafficStatsOn)
            {
                GUILayout.Box("Traffic Stats");
                trafficStatsIn = "Incoming: \n" + PhotonNetwork.networkingPeer.TrafficStatsIncoming.ToString();
                trafficStatsOut = "Outgoing: \n" + PhotonNetwork.networkingPeer.TrafficStatsOutgoing.ToString();
                GUILayout.Label(trafficStatsIn);
                GUILayout.Label(trafficStatsOut);
            }

            string healthStats = string.Empty;
            if (this.healthStatsVisible)
            {
                GUILayout.Box("Health Stats");
                healthStats = string.Format(
                    "ping: {6}[+/-{7}]ms resent:{8} \n\nmax ms between\nsend: {0,4} \ndispatch: {1,4} \n\nlongest dispatch for: \nev({3}):{2,3}ms \nop({5}):{4,3}ms",
                    gls.LongestDeltaBetweenSending,
                    gls.LongestDeltaBetweenDispatching,
                    gls.LongestEventCallback,
                    gls.LongestEventCallbackCode,
                    gls.LongestOpResponseCallback,
                    gls.LongestOpResponseCallbackOpCode,
                    PhotonNetwork.networkingPeer.RoundTripTime,
                    PhotonNetwork.networkingPeer.RoundTripTimeVariance,
                    PhotonNetwork.networkingPeer.ResentReliableCommands);
                GUILayout.Label(healthStats);
            }

            if (statsToLog)
            {
                string complete = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}", total, elapsedTime, average, trafficStatsIn, trafficStatsOut, healthStats);
                Debug.Log(complete);
            }

            // if anything was clicked, the height of this window is likely changed. reduce it to be layouted again next frame
            if (GUI.changed)
            {
                this.statsRect.height = 100;
            }

            GUI.DragWindow();
        }*/

        private void PaintSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUIUtility.labelWidth = 200;
            EditorGUILayout.LabelField(SETTINGS_TITLE, EditorStyles.boldLabel);            

            /*EditorGUILayout.PropertyField(spMessageTarget, new GUIContent("Send MonoMessage Target"));

            if (spMessageTarget.boolValue)
            {
                EditorGUILayout.HelpBox("Defines which classes can contain PUN Callback implementations.\nThis provides the option to optimize your runtime for speed.\n" +
                "The more specific this Type is, the fewer classes will be checked with reflection for callback methods.", MessageType.Info);
            }*/

            EditorGUILayout.PropertyField(spMonoCache, new GUIContent("Use Rpc MonoBehaviour Cache"));

            if (spMonoCache.boolValue)
            {
                EditorGUILayout.HelpBox("While enabled, the MonoBehaviours on which we call RPCs are cached, avoiding costly GetComponents<MonoBehaviour>() calls.\n" +
                    "RPCs are called on the MonoBehaviours of a target PhotonView. Those have to be found via GetComponents.\nWhen set this to true, the list of MonoBehaviours gets cached in each PhotonView.\n" +
                    "You can use photonView.RefreshRpcMonoBehaviourCache() to manually refresh a PhotonView's", MessageType.Info);
            }

            EditorGUILayout.Space();           

            EditorGUILayout.PropertyField(serializedObject.FindProperty("sendRate"), new GUIContent("Send Rate",
                "Defines how many times per second PhotonNetwork should send a package. If you change this, do not forget to also change sendRateOnSerialize"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("sendRateOnSerialize"), new GUIContent("Send Rate On Serialize",
                "Defines how many times per second OnPhotonSerialize should be called on PhotonViews."));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("updatePing"), new GUIContent("Publish Ping", "Sends Player's ping to the server so other players can see it."));

            EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty("updatePing").boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("updatePingEvery"), new GUIContent("Update Every", "How often we update players ping."));
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("switchMasterClient"), new GUIContent("Switch Master", "If ON we will switch Master Client based on settings below." +
                " If master has too much lag or errors.\nThis will run only if you are currently the Master Client."));

            EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty("switchMasterClient").boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lagCheck"), new GUIContent("Update Every", "How often we check to swith Master Client."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lagThreshold"), new GUIContent("Lag Threshold", "How much lag the Master Client can have until is goign to be switch."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("switchMasterErrors"), new GUIContent("Error Limit", "How much errors can happen before swith the Master Client."));
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();

            /*if(GUILayout.Button("Clear Player Prefs"))
            {
                PlayerPrefs.DeleteAll();
                EditorPrefs.DeleteAll();
            }*/
        }

        // PRIVATE METHODS: --------------------------------------------------------------------------------------------

        private void DropAreaGUI(bool photonPrefab = true)
        {
            UnityEngine.Event evt = UnityEngine.Event.current;
            Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            Rect text_area = drop_area;

            GUI.Box(drop_area, "", "ShurikenEffectBg");

            GUIStyle style = EditorStyles.centeredGreyMiniLabel;

            if (!InternalEditorUtility.HasPro())
            {
                style.normal.textColor = Color.white;
            }

            EditorGUI.LabelField(text_area, "To Add a new prefab just drag and drop it here.", style);
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                    {
                        return;
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is GameObject)
                            {
                                if (PrefabUtility.GetPrefabAssetType(draggedObject) == PrefabAssetType.Regular ||
                                    PrefabUtility.GetPrefabAssetType(draggedObject) == PrefabAssetType.Variant) //!photonPrefab || (photonPrefab
                                {
                                    GameObject go = draggedObject as GameObject;
                                    if (photonPrefab && DatabasePhoton.IsDefaultPrefab(go))
                                    {
                                        bool result = EditorUtility.DisplayDialog("Error", "Cannot use default prefabs.\nDo you want to create a copy?", "Ok", "Cancel");
                                        if(result)
                                        {
                                            go = DatabasePhoton.CreatePrefabCopy(go);
                                        }
                                        else
                                        {
                                            return;
                                        }
                                    }
                                    if (go == null) return;

                                    if(photonPrefab && go.GetComponent<PhotonView>() == null)
                                    {
                                        EditorUtility.DisplayDialog("Error", string.Format("This prefab needs to have a Photon View.", PrefabUtility.GetPrefabAssetType(draggedObject)), "Ok");
                                        return;
                                    }

                                    serializedObject.Update();

                                    if (photonPrefab)
                                    {
                                        
                                        if (!inst.prefabs.Contains(go))
                                        {
                                            serializedObject.FindProperty("prefabs").AddToObjectArray(go);
                                            //inst.prefabs.Add(go);
                                            AssetDatabase.SaveAssets();
                                        }
                                    }
                                    else
                                    {
                                        if (!inst.attachments.Contains(go))
                                        {
                                            serializedObject.FindProperty("attachments").AddToObjectArray(go);
                                            //inst.attachments.Add(go);
                                            AssetDatabase.SaveAssets();
                                        }
                                    }
                                    
                                    serializedObject.ApplyModifiedProperties();
                                    
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Error",
                                        $"Only Prefabs are allowed. Type: {PrefabUtility.GetPrefabAssetType(draggedObject)}", "Ok");
                                }
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error",
                                    $"Only GameObjects are allowed. Type: {draggedObject}", "Ok");
                            }
                        }

                        if(GUI.changed) EditorUtility.SetDirty(inst);
                    }
                    break;
            }
        }

        // PRIVATE METHODS: --------------------------------------------------------------------------------------------

        private void PaintSearch()
        {
            if (this.searchFieldStyle == null) this.searchFieldStyle = new GUIStyle(GUI.skin.FindStyle("SearchTextField"));
            if (this.searchCloseOnStyle == null) this.searchCloseOnStyle = new GUIStyle(GUI.skin.FindStyle("SearchCancelButton"));
            if (this.searchCloseOffStyle == null) this.searchCloseOffStyle = new GUIStyle(GUI.skin.FindStyle("SearchCancelButtonEmpty"));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5f);

            GUI.SetNextControlName(SEARCHBOX_NAME);
            this.searchText = EditorGUILayout.TextField(this.searchText, this.searchFieldStyle);

            if (this.searchFocus)
            {
                EditorGUI.FocusTextInControl(SEARCHBOX_NAME);
                this.searchFocus = false;
            }

            GUIStyle style = (string.IsNullOrEmpty(this.searchText)
                ? this.searchCloseOffStyle
                : this.searchCloseOnStyle
            );

            if (GUILayout.Button("", style))
            {
                this.ResetSearch();
            }

            GUILayout.Space(5f);
            EditorGUILayout.EndHorizontal();
        }

        private void ResetSearch()
        {
            this.searchText = "";
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.keyboardControl = 0;
            this.searchFocus = true;
        }
    }
}