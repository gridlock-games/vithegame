using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace NJG.PUN
{

    /// <summary>Finds out which PickupItems are not spawned at the moment and send this to new players.</summary>
    /// <remarks>Attach this component to a single GameObject in the scene, not to all PickupItems.</remarks>
    [RequireComponent(typeof(PhotonView))]
    public class NetworkItemSyncer : MonoBehaviourPunCallbacks
    {
        public bool IsWaitingForPickupInit;
        private const float TimeDeltaToIgnore = 0.2f;

        private const string ASK_PICKUP = "AskForPickupItemSpawnTimes";
        private const string REQUEST_ITEMS = "RequestForPickupItems";
        private const string INIT = "PickupItemInit";

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                this.SendPickedUpItems(newPlayer);
            }
        }

        public override void OnJoinedRoom()
        {
            //Debug.Log("Joined Room. isMasterClient: " + PhotonNetwork.isMasterClient + " id: " + PhotonNetwork.LocalPlayer.ID);
            // this client joined the room. let's see if there are players and if someone has to inform us about pickups
            this.IsWaitingForPickupInit = !PhotonNetwork.IsMasterClient;

            if (NetworkManager.Instance.Players.Length >= 2)
            {
                this.Invoke(ASK_PICKUP, 2.0f);
            }
        }

        public void AskForPickupItemSpawnTimes()
        {
            if (this.IsWaitingForPickupInit)
            {
                if (NetworkManager.Instance.Players.Length < 2)
                {
                    Debug.Log("Cant ask anyone else for PickupItem spawn times.");
                    this.IsWaitingForPickupInit = false;
                    return;
                }


                // find a another player (than the master, who likely is gone) to ask for the PickupItem spawn times
                Player nextPlayer = PhotonNetwork.MasterClient.GetNext();
                if (nextPlayer == null || nextPlayer.Equals(PhotonNetwork.LocalPlayer))
                {
                    nextPlayer = PhotonNetwork.LocalPlayer.GetNext();
                    //Debug.Log("This player is the Master's next. Asking this client's 'next' player: " + ((nextPlayer != null) ? nextPlayer.ToStringFull() : ""));
                }

                if (nextPlayer != null && !nextPlayer.Equals(PhotonNetwork.LocalPlayer))
                {
                    this.photonView.RPC(REQUEST_ITEMS, nextPlayer);

                    // you could restart this invoke and try to find another player after 4 seconds. but after a while it doesnt make a difference anymore
                    //this.Invoke(ASK_PICKUP, 2.0f);
                }
                else
                {
                    Debug.Log("No player left to ask");
                    this.IsWaitingForPickupInit = false;
                }
            }
        }

        /*[PunRPC]
        [Obsolete("Use RequestForPickupItems(PhotonMessageInfo msgInfo) with corrected typing instead.")]
        public void RequestForPickupTimes(PhotonMessageInfo msgInfo)
        {
            RequestForPickupItems(msgInfo);
        }*/

        [PunRPC]
        public void RequestForPickupItems(PhotonMessageInfo msgInfo)
        {
            if (msgInfo.Sender == null)
            {
                Debug.LogError("Unknown player asked for PickupItems");
                return;
            }

            SendPickedUpItems(msgInfo.Sender);
        }

        /// <summary>Summarizes all PickupItem ids and spawn times for new players. Calls RPC "PickupItemInit".</summary>
        /// <param name="targetPlayer">The player to send the pickup times to. It's a targetted RPC.</param>
        private void SendPickedUpItems(Player targetPlayer)
        {
            if (targetPlayer == null)
            {
                Debug.LogWarning("Cant send PickupItem spawn times to unknown targetPlayer.");
                return;
            }

            double now = PhotonNetwork.Time;
            double soon = now + TimeDeltaToIgnore;


            NetworkItem[] items = new NetworkItem[NetworkItem.DisabledPickupItems.Count];
            NetworkItem.DisabledPickupItems.CopyTo(items);

            List<float> valuesToSend = new List<float>(items.Length * 2);
            for (int i = 0; i < items.Length; i++)
            {
                NetworkItem pi = items[i];
                if (pi.secondsBeforeRespawn <= 0)
                {
                    valuesToSend.Add(pi.ViewID);
                    valuesToSend.Add((float)0.0f);
                }
                else
                {
                    double timeUntilRespawn = pi.timeOfRespawn - PhotonNetwork.Time;
                    if (pi.timeOfRespawn > soon)
                    {
                        // the respawn of this item is not "immediately", so we include it in the message "these items are not active" for the new player
                        //Debug.Log(pi.ViewID + " respawn: " + pi.timeOfRespawn + " timeUntilRespawn: " + timeUntilRespawn + " (now: " + PhotonNetwork.time + ")");
                        valuesToSend.Add(pi.ViewID);
                        valuesToSend.Add((float)timeUntilRespawn);
                    }
                }
            }

            //Debug.Log("Sent count: " + valuesToSend.Count + " now: " + now);
            this.photonView.RPC(INIT, targetPlayer, PhotonNetwork.Time, valuesToSend.ToArray());
        }


        [PunRPC]
        public void PickupItemInit(double timeBase, float[] inactivePickupsAndTimes)
        {
            this.IsWaitingForPickupInit = false;

            // if there are no inactive pickups, the sender will send a list of 0 items. this is not a problem...
            for (int i = 0; i < inactivePickupsAndTimes.Length / 2; i++)
            {
                int arrayIndex = i * 2;
                int viewIdOfPickup = (int)inactivePickupsAndTimes[arrayIndex];
                float timeUntilRespawnBasedOnTimeBase = inactivePickupsAndTimes[arrayIndex + 1];


                PhotonView view = PhotonView.Find(viewIdOfPickup);
                NetworkItem pi = view.GetComponent<NetworkItem>();

                if (timeUntilRespawnBasedOnTimeBase <= 0)
                {
                    pi.PickedUp();
                }
                else
                {
                    double timeOfRespawn = timeUntilRespawnBasedOnTimeBase + timeBase;

                    //Debug.Log(view.viewID + " respawn: " + timeOfRespawn + " timeUntilRespawnBasedOnTimeBase:" + timeUntilRespawnBasedOnTimeBase + " SecondsBeforeRespawn: " + pi.secondsBeforeRespawn);
                    double timeBeforeRespawn = timeOfRespawn - PhotonNetwork.Time;
                    if (timeUntilRespawnBasedOnTimeBase <= 0)
                    {
                        timeBeforeRespawn = 0.0f;
                    }
                    pi.timeUntilRespawn = (float)timeBeforeRespawn;
                    pi.PickedUp();
                }
            }
        }
    }
}