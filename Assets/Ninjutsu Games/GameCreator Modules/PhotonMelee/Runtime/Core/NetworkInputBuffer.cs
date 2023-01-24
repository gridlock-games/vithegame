using GameCreator.Melee;
using Photon.Pun;

namespace NJG.PUN.Melee
{
    using UnityEngine;
#if PHOTON_MELEE
    public class NetworkInputBuffer : InputBuffer
    {
        
        //public float timeWindow;

        // CONSTRUCTOR: ---------------------------------------------------------------------------

        public NetworkInputBuffer(float timeWindow) : base(timeWindow)
        {
            this.timeWindow = timeWindow;

            this.inputTime = -100f;
            this.key = CharacterMelee.ActionKey.A;
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public override void AddInput(CharacterMelee.ActionKey key)
        {
            // Debug.LogFormat("NIB AddInput key: {0}", key);
            this.key = key;
            this.inputTime = (float)PhotonNetwork.Time;
        }
        
        public void AddInput(CharacterMelee.ActionKey key, float time)
        {
            // Debug.LogFormat("NIB AddInput key: {0} time: {1}", key, time);

            this.key = key;
            this.inputTime = time;
        }

        public override bool HasInput()
        {
            // Debug.LogFormat("NIB HasInput: {0}",(PhotonNetwork.Time - this.inputTime <= this.timeWindow));

            if (this.inputTime <= 0f) return false;
            return PhotonNetwork.Time - this.inputTime <= this.timeWindow;
        }

        /*public CharacterMelee.ActionKey GetInput()
        {
            return this.key;
        }

        public void ConsumeInput()
        {
            this.inputTime = -100f;
        }*/
    }
#endif
}