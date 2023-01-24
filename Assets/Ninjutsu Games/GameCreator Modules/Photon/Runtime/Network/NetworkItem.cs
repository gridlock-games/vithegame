using GameCreator.Core;
using GameCreator.Core.Hooks;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NJG.PUN
{
    [RequireComponent(typeof(PhotonView))]
    public class NetworkItem : Trigger, IPunObservable
    {
#if UNITY_EDITOR
        public bool initialized = false;
#endif
        /// <summary>Cache field for the PhotonView on this GameObject.</summary>
        private PhotonView pvCache = null;

        /// <summary>A cached reference to a PhotonView on this GameObject.</summary>
        /// <remarks>
        /// If you intend to work with a PhotonView in a script, it's usually easier to write this.photonView.
        ///
        /// If you intend to remove the PhotonView component from the GameObject but keep this Photon.MonoBehaviour,
        /// avoid this reference or modify this code to use PhotonView.Get(obj) instead.
        /// </remarks>
        public PhotonView photonView
        {
            get
            {
                if (pvCache == null)
                {
                    pvCache = PhotonView.Get(this);
                }
                return pvCache;
            }
        }

        ///<summary>Enables you to define a timeout when the picked up item should re-spawn at the same place it was before.</summary>
        /// <remarks>
        /// Set in Inspector per GameObject! The value in code is just the default.
        ///
        /// If you don't want an item to respawn, set SecondsBeforeRespawn == 0.
        /// If an item does not respawn, it could be consumed or carried around and dropped somewhere else.
        ///
        /// A respawning item should stick to a fixed position. It should not be observed at all (in any PhotonView).
        /// It can only be consumed and can't be dropped somewhere else (cause that would double the item).
        ///
        /// This script uses PunRespawn() as RPC and as method that gets called by Invoke() after a timeout.
        /// No matter if the item respawns timed or by Drop, that method makes sure (temporary) owner and other status-values
        /// are being re-set.
        /// </remarks>
        [Range(0, 100)] public float secondsBeforeRespawn = 2;

        public bool destroyOnPickup;
        
        /// <summary>If the pickup item is currently yours. Interesting in OnPickedUp(PickupItem item).</summary>
        public bool PickupIsMine;

        /// <summary>If this client sent a pickup. To avoid sending multiple pickup requests before reply is there.</summary>
        public bool SentPickup;

        /// <summary>Timestamp when to respawn the item (compared to PhotonNetwork.time). </summary>
        public double timeOfRespawn;    // needed when we want to update new players when a PickupItem respawns

        /// <summary></summary>
        public int ViewID { get { return this.photonView.ViewID; } }

        //public double TimeUntileRespawn { get { return timeUntilRespawn; } }

        public static HashSet<NetworkItem> DisabledPickupItems = new HashSet<NetworkItem>();

        public IActionsList onStartActions;
        public IActionsList onPickUpMine;
        public IActionsList onPickUpOthers;
        public IActionsList onPickUpEveryone;

        private const string PUN_RESPAWN = "PunRespawn";
        private const string PUN_PICKUP = "PunPickup";

        public double timeUntilRespawn;


        private void Awake()
        {
#if UNITY_EDITOR
            HideStuff();
#endif
        }

#if UNITY_EDITOR
        private void HideStuff()
        {
            if (onStartActions != null) onStartActions.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            if (onPickUpMine != null) onPickUpMine.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            if (onPickUpOthers != null) onPickUpOthers.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            if (onPickUpEveryone != null) onPickUpEveryone.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }
#endif

        private void Start()
        {
            if (onStartActions != null) onStartActions.Execute(gameObject, null);
        }

        #region Game Creator Overrides

        public override void Execute(GameObject target, params object[] parameters)
        {            
            if (this.minDistance && HookPlayer.Instance != null)
            {
                float distance = Vector3.Distance(HookPlayer.Instance.transform.position, transform.position);
                if (distance > this.minDistanceToPlayer) return;
            }

            //Debug.Log("2 NetworkItem Execute " + target, gameObject);

            PhotonView otherpv = target.GetComponent<PhotonView>();
            if (otherpv != null && otherpv.IsMine || !PhotonNetwork.InRoom || PhotonNetwork.OfflineMode)
            {
                //Debug.Log("OnTriggerEnter() calls Pickup().");
                this.Pickup();
            }
        }

        public override void Execute()
        {
            //Debug.Log("1 NetworkItem Execute ", gameObject);
            Execute(gameObject);
        }

        #endregion

        #region Photon Stuff

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // read the description in SecondsBeforeRespawn

            if (stream.IsWriting && secondsBeforeRespawn <= 0)
            {
                stream.SendNext(this.gameObject.transform.position);
            }
            else
            {
                // this will directly apply the last received position for this PickupItem. No smoothing. Usually not needed though.
                Vector3 lastIncomingPos = (Vector3)stream.ReceiveNext();
                this.gameObject.transform.position = lastIncomingPos;
            }
        }

        public void Pickup()
        {
            if (this.SentPickup)
            {
                // skip sending more pickups until the original pickup-RPC got back to this client
                return;
            }

            this.SentPickup = true;
            this.photonView.RPC(PUN_PICKUP, RpcTarget.AllViaServer);
        }

        /// <summary>Makes use of RPC PunRespawn to drop an item (sent through server for all).</summary>
        public void Drop()
        {
            if (this.PickupIsMine)
            {
                this.photonView.RPC(PUN_RESPAWN, RpcTarget.AllViaServer);
            }
        }

        /// <summary>Makes use of RPC PunRespawn to drop an item (sent through server for all).</summary>
        public void Drop(Vector3 newPosition)
        {
            if (this.PickupIsMine)
            {
                this.photonView.RPC(PUN_RESPAWN, RpcTarget.AllViaServer, newPosition);
            }
        }

        [PunRPC]
        public void PunPickup(PhotonMessageInfo msgInfo)
        {
            // when this client's RPC gets executed, this client no longer waits for a sent pickup and can try again
            if (msgInfo.Sender.IsLocal) this.SentPickup = false;


            // In this solution, picked up items are disabled. They can't be picked up again this way, etc.
            // You could check "active" first, if you're not interested in failed pickup-attempts.
            if (!this.gameObject.activeInHierarchy)
            {
                // optional logging:
                //Debug.Log("Ignored PUN RPC, cause item is inactive. " + this.gameObject + " SecondsBeforeRespawn: " + secondsBeforeRespawn + " TimeOfRespawn: " + this.timeOfRespawn + " respawn in future: " + (timeOfRespawn > PhotonNetwork.time));
                return;     // makes this RPC being ignored
            }


            // if the RPC isn't ignored by now, this is a successful pickup. this might be "my" pickup and we should do a callback
            this.PickupIsMine = msgInfo.Sender.IsLocal;

            //Debug.Log("Invoker " + gameObject + " / " + msgInfo.photonView.gameObject + " / " + msgInfo.sender.TagObject);

            if (onPickUpEveryone != null) onPickUpEveryone.Execute((msgInfo.Sender.TagObject as GameObject) ?? msgInfo.photonView.gameObject, null);

            if (msgInfo.Sender.IsLocal)
            {
                if (onPickUpMine != null) onPickUpMine.Execute((msgInfo.Sender.TagObject as GameObject) ?? msgInfo.photonView.gameObject, null);
            }
            else
            {
                if (onPickUpOthers != null) onPickUpOthers.Execute((msgInfo.Sender.TagObject as GameObject) ?? msgInfo.photonView.gameObject, null);
            }


            // setup a respawn (or none, if the item has to be dropped)
            if (secondsBeforeRespawn <= 0)
            {
                this.PickedUp();    // item doesn't auto-respawn. must be dropped
            }
            else
            {
                // how long it is until this item respanws, depends on the pickup time and the respawn time
                double timeSinceRpcCall = (PhotonNetwork.Time - msgInfo.SentServerTime);
                timeUntilRespawn = secondsBeforeRespawn - timeSinceRpcCall;

                //Debug.Log("msg timestamp: " + msgInfo.timestamp + " time until respawn: " + timeUntilRespawn);

                if (timeUntilRespawn > 0)
                {
                    this.PickedUp();
                }
            }
        }

        internal void PickedUp()
        {
            // this script simply disables the GO for a while until it respawns.

            /*if (destroyOnPickup)
            {

                return;
            }*/

            this.gameObject.SetActive(false);
            NetworkItem.DisabledPickupItems.Add(this);
            this.timeOfRespawn = 0;

            if (timeUntilRespawn > 0)
            {
                this.timeOfRespawn = PhotonNetwork.Time + timeUntilRespawn;
                Invoke(PUN_RESPAWN, (float)timeUntilRespawn);
            }
        }


        [PunRPC]
        internal void PunRespawn(Vector3 pos)
        {
            //Debug.Log("PunRespawn with Position.");
            this.PunRespawn();
            this.gameObject.transform.position = pos;
        }

        [PunRPC]
        internal void PunRespawn()
        {
#if DEBUG
            // debugging: in some cases, the respawn is "late". it's unclear why! just be aware of this.
            //double timeDiffToRespawnTime = PhotonNetwork.time - this.timeOfRespawn;
            //if (timeDiffToRespawnTime > 0.1f) Debug.LogWarning("Spawn time is wrong by: " + timeDiffToRespawnTime + " (this is not an error. you just need to be aware of this.)");
#endif


            // if this is called from another thread, we might want to do this in OnEnable() instead of here (depends on Invoke's implementation)
            NetworkItem.DisabledPickupItems.Remove(this);
            this.timeOfRespawn = 0;
            this.PickupIsMine = false;

            if (this.gameObject != null)
            {
                this.gameObject.SetActive(true);
            }
        }

        #endregion
    }
}
