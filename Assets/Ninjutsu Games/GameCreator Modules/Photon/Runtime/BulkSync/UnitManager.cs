using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace NJG.PUN.BulkSync
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(BulkBatchSync))]
    public class UnitManager : MonoBehaviour
    {

        // [SerializeField,Range(0,200)] private int m_unitAmount = 50;
        // [SerializeField] private UnitBase m_unit;
        private PhotonView m_view;
        private BulkBatchSync m_bulkBatchSync;
        // private ObjectPool<UnitBase> m_pool = new ObjectPool<UnitBase>();
        private List<UnitBase> objs = new List<UnitBase>();
        
        public PhotonView View => m_view == null ? m_view = GetComponent<PhotonView>() : m_view;


        private void Awake()
        {
            m_bulkBatchSync = GetComponent<BulkBatchSync>();
        }

        private void Start()
        {
            TransferOwnership();

            m_bulkBatchSync.GameObjectStateChanged += OnUnitStateChanged;
            // CreateUnit(m_unitAmount);
        }

        private void TransferOwnership()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                View.TransferOwnership(PhotonNetwork.MasterClient);
            }
        }
        
        public void Instantiate(string prefab, Vector3 position, Quaternion rotation, byte group = 0, object[] data = null)
        {
            // UnitBase unit = prefab.GetComponent<UnitBase>();
            // if(!unit) unit = prefab.AddComponent
            NJGPhotonPool pool = PhotonNetwork.PrefabPool as NJGPhotonPool;
            UnitBase unit = pool.Instantiate(prefab, position, rotation).GetComponent<UnitBase>();
            unit.Setup(View, this);
            objs.Add(unit);
            // if(!m_pool.Initialized) m_pool.Initialize(20, pool.ResourceCache.GetComponent<UnitBase>(), this.transform);

            /*if (CurrentRoom == null)
            {
                Debug.LogError("Can not Instantiate before the client joined/created a room.");
                return null;
            }

            if (LocalPlayer.IsMasterClient)
            {
                Pun.InstantiateParameters netParams = new InstantiateParameters(prefabName, position, rotation, group, data, currentLevelPrefix, null, LocalPlayer, ServerTimestamp);
                return NetworkInstantiate(netParams, true);
            }*/

        }

        /*private void CreateUnit(int count)
        {
            m_pool.Initialize(count, m_unit, this.transform);
            foreach (var unit in m_pool.Pool)
            {
                unit.GetComponent<UnitBase>().Setup(m_view, this);
            }
        }

        public void ActivateUnit(Vector3 spawnPos)
        {
            if (m_pool.TryPop(out var unit))
            {
                unit.transform.position = spawnPos;
                unit.gameObject.SetActive(true);
            }
        }*/

        private void Update()
        {
            //TODO simulate the movement but do not apply it, also the masterclient should receive the batches for better results between clients
            if (View.IsMine)
            {
                // m_bulkBatchSync.CreateBatches(m_pool.Pool);
                m_bulkBatchSync.CreateBatches(objs);
            }

            else
            {
                // m_bulkBatchSync.Sync(m_pool.Pool);
                m_bulkBatchSync.Sync(objs);
            }
        }

        private void OnUnitStateChanged(int index, bool active)
        {
            // m_pool.Pool[index].gameObject.SetActive(active);
            objs[index].gameObject.SetActive(active);
        }

        public void SendStateChange(UnitBase unit)
        {
            if (!View.IsMine)
            {
                // var idx = m_pool.Pool.FindIndex(x => x == unit);
                var idx = objs.FindIndex(x => x == unit);
                m_bulkBatchSync.SendStateChange(idx);
            }
        }

        public void Recycle(UnitBase unit)
        {
            // m_pool.Push(unit);
            // objs.Push(unit);
        }

        private void OnDisable()
        {
            m_bulkBatchSync.GameObjectStateChanged -= OnUnitStateChanged;
        }
    }
}