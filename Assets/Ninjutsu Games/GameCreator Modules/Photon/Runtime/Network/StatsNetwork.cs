namespace NJG.PUN
{
    using System.Collections.Generic;
    using ExitGames.Client.Photon;
    using GameCreator.Core;
    using Photon.Pun;
    using Photon.Realtime;

#if PHOTON_STATS
    using GameCreator.Stats;
#endif
    using UnityEngine;
    using System;

#if PHOTON_RPG
    using NJG.RPG;
#endif

#if PHOTON_STATS
    [AddComponentMenu(""), RequireComponent(typeof(Stats), typeof(PhotonView))]
#else
    [AddComponentMenu(""), RequireComponent(typeof(PhotonView))]
#endif
    public class StatsNetwork : MonoBehaviourPun, IInRoomCallbacks//, IPunObservable
    {
        private const string SYNC_STATS = "SyncStats";
        private const string RPC_STAT = "StatRPC";
        //private const string RPC_ATT = "AttRPC";

        //public static Dictionary<string, ActionsNetwork> REGISTER = new Dictionary<string, ActionsNetwork>();

        /*[System.Serializable]
        public class ActionRPC
        {
            public enum TargetType
            {
                Targets,
                PhotonPlayer
            }
            public Actions actions;
            public RpcTarget targets = RpcTarget.Others;
            public Player targetPlayer;
        }*/

        [System.Serializable]
        public struct NetworkStat
        { 
            public string name;
            public string id;
            public bool sync;
        }

        // PROPERTIES: ----------------------------------------------------------------------------

        public NetworkStat[] networkAttributes = new NetworkStat[] { };
        public NetworkStat[] networkStats = new NetworkStat[] { };

#if PHOTON_STATS
        private Dictionary<string, float> cachedStats = new Dictionary<string, float>();
        private List<string> cachedStatsSync = new List<string>();
        private Dictionary<int, string> cachedStatsIndex = new Dictionary<int, string>();
        private Dictionary<string, int> cachedStatsKeys = new Dictionary<string, int>();

        private Dictionary<string, float> cachedAttributes = new Dictionary<string, float>();
        private List<string> cachedAttributesSync = new List<string>();
        private Dictionary<int, string> cachedAttributesIndex = new Dictionary<int, string>();
        private Dictionary<string, int> cachedAttributesKeys = new Dictionary<string, int>();
        private Stats stats;

        private bool registered;
        private float lastChange;
        private float refreshRate = 0.05f;
        private static bool initialized;
        public Hashtable attributesChanged = new Hashtable();
#endif

#if PHOTON_RPG
        private Actor actor;
#endif


        // CONSTRUCTORS: --------------------------------------------------------------------------

        private void OnEnable()
        {
#if PHOTON_STATS
#if UNITY_EDITOR
            HideStuff();
            SetupPhotonView();
#endif
            if (PhotonNetwork.UseRpcMonoBehaviourCache)
            {
                photonView.RefreshRpcMonoBehaviourCache();
            }

            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.AddCallbackTarget(this);

            if (!stats) stats = GetComponent<Stats>();
            stats.AddOnChangeAttr(OnChangedAttribute);
            stats.AddOnChangeStat(OnChangedStat);
#endif
        }
        
        private void OnDisable()
        {
#if PHOTON_STATS
            /*if (photonView && photonView.IsMine && Application.isPlaying && PhotonNetwork.InRoom)
            {
                PhotonNetwork.RemoveRPCs(photonView);
            }*/

            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.RemoveCallbackTarget(this);

            if (!stats) stats = GetComponent<Stats>();
            stats.RemoveOnChangeAttr(OnChangedAttribute);
            stats.RemoveOnChangeStat(OnChangedStat);
#endif
        }

#if UNITY_EDITOR
        private void HideStuff()
        {
            //this.hideFlags = HideFlags.None;
            this.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }

        private void OnValidate()
        {
            HideStuff();
        }
#endif
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
#if PHOTON_STATS
            if (photonView.IsMine && gameObject.activeSelf)
            {
                //Debug.LogWarning("OnPhotonPlayerConnected player: " + newPlayer + " / stats: " + JsonUtility.ToJson(stats.GetSaveData()));
                photonView.RPC(SYNC_STATS, newPlayer, JsonUtility.ToJson(stats.GetSaveData()));
            }
#endif
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            //RegisterEvents();
        }

#if PHOTON_STATS

        private void Awake()
        {
            if (!Application.isPlaying) return;

#if UNITY_EDITOR
            HideStuff();
#endif
#if PHOTON_RPG
            actor = GetComponent<Actor>();
#endif
            //RegisterEvents();

            if (!stats) stats = GetComponent<Stats>();
            
        }

        private void Start()
        {
            RequireInit();
        }

        private void RequireInit()
        {
            if (!initialized || cachedAttributesIndex.Count == 0 || cachedStats.Count == 0)
            {
                if (stats.runtimeAttrsData == null) return;

                for(int i = 0, imax = networkAttributes.Length; i<imax; i++)
                {
                    if (networkAttributes[i].sync)
                    {
                        cachedAttributesSync.Add(networkAttributes[i].name);
                    }
                }

                if (!stats) stats = GetComponent<Stats>();

                //int i = 0;
                foreach (var s in stats.runtimeAttrsData)
                {
                    if (!cachedAttributes.ContainsKey(s.Key))
                    {
                        cachedAttributes.Add(s.Key, s.Value.value);
                    }
                    if (!cachedAttributesIndex.ContainsKey(s.Value.index))
                    {
                        cachedAttributesIndex.Add(s.Value.index, s.Value.attrAsset.attribute.uniqueName);
                    }
                    if (!cachedAttributesKeys.ContainsKey(s.Value.attrAsset.attribute.uniqueName))
                    {
                        //Debug.Log("CachedKeyName " + s.Value.attrAsset.attribute.uniqueName+" / "+ s.Value.attrAsset.attribute.shortName);
                        cachedAttributesKeys.Add(s.Value.attrAsset.attribute.uniqueName, s.Value.index);
                    }
                    //i++;
                }

                for (int i = 0, imax = networkStats.Length; i < imax; i++)
                {
                    if (networkStats[i].sync)
                    {
                        cachedStatsSync.Add(networkStats[i].name);
                    }
                }

                foreach (var s in stats.runtimeStatsData)
                {
                    if (!cachedStats.ContainsKey(s.Value.statAsset.stat.uniqueName))
                    {
                        cachedStats.Add(s.Value.statAsset.stat.uniqueName, s.Value.statAsset.stat.baseValue);
                    }
                    if (!cachedStatsIndex.ContainsKey(s.Value.index))
                    {
                        cachedStatsIndex.Add(s.Value.index, s.Value.statAsset.stat.uniqueName);
                    }
                    if (!cachedStatsKeys.ContainsKey(s.Value.statAsset.stat.uniqueName))
                    {
                        cachedStatsKeys.Add(s.Value.statAsset.stat.uniqueName, s.Value.index);
                    }
                }

                //.Log("Setup " + cachedAttributesKeys.ToStringFull()+" // "+ cachedAttributesIndex.ToStringFull());
                initialized = true;
            }
        }

        private void LateUpdate()
        {
            if(Time.time >= lastChange && attributesChanged.Count > 0)
            {
                attributesChanged.Clear();
            }
        }

#if UNITY_EDITOR
        public void SetupPhotonView()
        {
            /*if (photonView.ObservedComponents == null) photonView.ObservedComponents = new List<Component>();

            CharacterNetwork ch = GetComponent<CharacterNetwork>();

            if (ch == null && !photonView.ObservedComponents.Contains(this))
            {
                if (photonView.ObservedComponents.Count > 0 && photonView.ObservedComponents[0] == null)
                {
                    photonView.ObservedComponents[0] = this;
                }
                else photonView.ObservedComponents.Add(this);
            }*/
        }
#endif

        /*private void RegisterEvents()
        {
            if (!registered)
            {
                stats.AddOnChangeAttr(OnChangedAttribute);
                stats.AddOnChangeStat(OnChangedStat);
                registered = true;
            }
        }*/

        private void OnChangedStat(Stats.EventArgs ev)
        {
            if (!PhotonNetwork.InRoom || !photonView.IsMine || !gameObject.activeSelf) return;

            if (string.IsNullOrEmpty(ev.name))
            {
                return;
            }

            if (!cachedStatsSync.Contains(ev.name))
            {
                return;
            }

            RequireInit();

            //Debug.Log("OnChangedStat " + ev.name + " / " + cachedStats.Count+ " / cachedStats: " + cachedStats.ToStringFull());

            float current = this.stats.GetStat(ev.name);
            float last = cachedStats[ev.name];
            bool changed = false;

            //if (cachedStats.TryGetValue(ev.name, out last))
            //{
                if (!Mathf.Approximately(current, last))
                {
                cachedStats[ev.name] = current;
                changed = true;
                }
            /*}
            else
            {
                cachedStats.Add(ev.name, current);
                cachedStatsIndex.Add(ev.name);
                changed = true;
            }*/

            int index = cachedStatsKeys[ev.name];// cachedStatsIndex.IndexOf(ev.name);

            if (changed && gameObject.activeInHierarchy)
            {
                photonView.RPC(RPC_STAT, RpcTarget.Others, index, current);
            }
        }

        private void OnChangedAttribute(Stats.EventArgs ev)
        {
            if (PhotonNetwork.InRoom && photonView.IsMine && gameObject.activeSelf)
            {
                //Debug.Log("OnChangedAttribute " + ev.name, gameObject);
                if (string.IsNullOrEmpty(ev.name))
                {
                    return;
                }

                if (!cachedAttributesSync.Contains(ev.name))
                {
                    return;
                }

                RequireInit();

                float current = this.stats.GetAttrValue(ev.name);
                float last = -1;
                bool changed = false;

                if (cachedAttributes.TryGetValue(ev.name, out last))
                {
                    if (!Mathf.Approximately(current, last))
                    {
                        changed = true;
                    }
                }
                else
                {
                    //cachedAttributesIndex.Add(ev.name);
                    cachedAttributes.Add(ev.name, current);
                    changed = true;
                }
                //Debug.Log("OnChangedAttribute " + ev.name +" / current: " + current);
                int index = cachedAttributesKeys[ev.name];

#if PHOTON_RPG
                if (changed && gameObject.activeSelf && (actor ? !actor.objectDestroyer.InProgress : true))
#else
                if (changed && gameObject.activeSelf)
#endif

                {
                    if (attributesChanged.ContainsKey(index))
                    {
                        attributesChanged[index] = (int)current;
                    }
                    else
                    {
                        attributesChanged.Add(index, (int)current);
                    }
                    //Debug.Log("Changed  index: " + index + " name: " + ev.name +" / attributesChanged: " + attributesChanged.ToStringFull());
                    //photonView.RPC(RPC_ATT, RpcTarget.Others, index, current);
                    //PhotonNetwork.SendAllOutgoingCommands();
                    lastChange = Time.time + refreshRate;
                }
            }
        }

        /*private void OnChangeAttribute()
        {
            photonView.RPC(SYNC_STATS, RpcTarget.Others, JsonUtility.ToJson(stats.GetSaveData()));
        }*/

        [PunRPC]
        public void StatRPC(int statIndex, float statValue)
        {
            RequireInit();
            /*if (statIndex >= cachedStatsIndex.Count)
            {
                Debug.LogWarning("StatRPC statIndex: " + statIndex + " of "+ cachedStatsIndex.Count+" = " + statValue);
                return;
            }*/

            //Debug.LogWarning("StatRPC att: " + stat + " = " + statValue);
            stats.SetStatBase(cachedStatsIndex[statIndex], statValue);
        }

        /*[PunRPC]
        public void AttRPC(int attIndex, float attValue)
        {
            if (attIndex >= cachedAttributesIndex.Count)
            {
                Debug.LogWarning("AttRPC attIndex: " + attIndex + " of " + cachedStatsIndex.Count + " = " + attValue);
                return;
            }

            //Debug.LogWarning("AttRPC att: " + att+" = "+ attValue);
            stats.SetAttrValue(cachedAttributesIndex[attIndex], attValue);
        }*/

        [PunRPC]
        public void SyncStats(string statsJson)
        {
            RequireInit();
            var data = JsonUtility.FromJson<Stats.SerialData>(statsJson);
            //Debug.LogWarning("SPRPC statsJson: " + actor.IsReady+ " / "+ data);
            stats.OnLoad(data);
        }

        /*public bool SendUpdate(PhotonStream stream)
        {
            if (attributesChanged.Count > 0)
            {
                stream.SendNext(attributesChanged);
                attributesChanged.Clear();
                return true;
            }
            return false;
        }*/

        public void ReceiveUpdate(Hashtable tbl)
        {
            RequireInit();
            //Debug.Log("2 stream.Count " + stream.Count + " reading " + stream.IsReading + " / " + stream.ToArray().Length);

            //Debug.Log("ReceiveStats: " + initialized+" ==== "+tbl.ToStringFull()+ " cachedAttributesIndex "+ cachedAttributesIndex.Count);
            if (tbl != null && cachedAttributesIndex.Count > 0)
            {
                var enumerator = tbl.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    //Debug.Log((int)enumerator.Current.Key+" / "+ enumerator.Current.Value+" from "+cachedAttributesIndex.Count);
                    //Debug.Log(cachedAttributesIndex[(int)enumerator.Current.Key]+" = "+ (int)enumerator.Current.Value);
                    stats.SetAttrValue(cachedAttributesIndex[(int)enumerator.Current.Key], (int)enumerator.Current.Value, true);
                }
            }
        }

        /*public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                if(attributesChanged.Count > 0)
                {
                    Debug.Log("Stats Write " + attributesChanged.ToStringFull());
                    stream.SendNext(attributesChanged);
                    //attributesChanged.Clear();
                    lastChange = Time.time + refreshRate;
                }
            }
            else
            {
                if(stream.Count > 0)
                {
                    Debug.Log("2 stream.Count " + stream.Count+" reading "+ stream.IsReading+" / "+stream.ToArray().Length);
                    ReceiveUpdate((Hashtable)stream.ReceiveNext());
                }
            }
        }*/
#endif
            }

        }
