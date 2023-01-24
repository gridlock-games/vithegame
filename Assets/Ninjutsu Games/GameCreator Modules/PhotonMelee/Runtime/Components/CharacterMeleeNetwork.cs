using System.Collections;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Core;
using GameCreator.Melee;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace NJG.PUN.Melee
{
    [RequireComponent(typeof(PhotonView))]
    public class CharacterMeleeNetwork : CharacterMelee
#if PHOTON_MELEE
        , IPunObservable, IInRoomCallbacks
#endif
    {
#if PHOTON_MELEE
        public bool debug;

        /// <summary>Cache field for the PhotonView on this GameObject.</summary>
        private PhotonView pvCache;

        private MeleeShield lastShield;
        private bool networkInitialized;

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
                if (!pvCache)
                {
                    pvCache = PhotonView.Get(this);
                }

                return pvCache;
            }
        }

        protected override void Awake()
        {
            Character = GetComponent<Character>();
            CharacterAnimator = GetComponent<CharacterAnimator>();
            inputBuffer = new NetworkInputBuffer(INPUT_BUFFER_TIME);
        }

        private void Start()
        {
            EventBlock += OnBlock;
            EventStagger += OnStaggered;
            EventDrawWeapon += OnDrawWeapon;
            //EventSheatheWeapon += OnSheatheWeapon;
            // += OnAttack;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            //HideStuff();
            SetupPhotonView();
#endif
            if (!Application.isPlaying) return;

            if (PhotonNetwork.UseRpcMonoBehaviourCache)
            {
                photonView.RefreshRpcMonoBehaviourCache();
            }

            PhotonNetwork.AddCallbackTarget(this);
        }

        protected void OnDisable()
        {
            EventBlock -= OnBlock;
            EventStagger -= OnStaggered;
            EventDrawWeapon -= OnDrawWeapon;
            //EventSheatheWeapon -= OnSheatheWeapon;
            //EventAttack -= OnAttack;

            if (!Application.isPlaying) return;
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        protected override void Update()
        {
            base.Update();
            
            
            /*this.UpdatePoise();
            this.UpdateDefense();

            if (this.comboSystem != null)
            {
                this.comboSystem.Update();

                if (photonView.IsMine && this.CanAttack() && this.inputBuffer.HasInput())
                {
                    ActionKey key = this.inputBuffer.GetInput();
                    MeleeClip meleeClip = this.comboSystem.Select(key);

                    if (meleeClip)
                    {
                        this.inputBuffer.ConsumeInput();

                        this.currentMeleeClip = meleeClip;
                        this.targetsEvaluated = new HashSet<int>();

                        this.currentMeleeClip.Play(this);
                        
                        
                        if (debug) Debug.LogWarningFormat("OnAttack clp: {0} isMine: {1}", meleeClip, photonView.IsMine);

                        photonView.RPC(nameof(NAttack), RpcTarget.Others, MeleeRegistry.Instance.clips.IndexOf(meleeClip));
                        
                        if (this.EventAttack != null) this.EventAttack.Invoke(meleeClip);
                    }
                }
            }*/
            
            if(!networkInitialized) return;
            
            // Debug.LogWarningFormat(gameObject, "comboSystem: {0} go: {1}", comboSystem, gameObject);

            if (photonView.IsMine && !(currentShield is null) && currentShield != lastShield)
            {
                lastShield = currentShield;
                photonView.RPC(nameof(NChangeShield), RpcTarget.Others,
                    MeleeRegistry.Instance.shields.IndexOf(currentShield));
            }
        }

        protected override float GetTime()
        {
            return (float)PhotonNetwork.Time;
        }

#if UNITY_EDITOR
        public void SetupPhotonView()
        {
            if (!photonView)
            {
                pvCache = gameObject.GetPhotonView();
                if (!pvCache) pvCache = gameObject.AddComponent<PhotonView>();
            }

            if (photonView.ObservedComponents == null) photonView.ObservedComponents = new List<Component>();

            if (photonView.Synchronization == ViewSynchronization.Off)
                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;

            if (photonView.ObservedComponents.Count > 0)
            {
                for (int i = 0; i < photonView.ObservedComponents.Count; i++)
                {
                    if (photonView.ObservedComponents[i] == null)
                    {
                        photonView.ObservedComponents[i] = this;
                        break;
                    }
                }
            }

            if (!photonView.ObservedComponents.Contains(this))
            {
                photonView.ObservedComponents.Add(this);
            }
        }
#endif

        #region Melee Events
        
        public override void Execute(ActionKey actionKey)
        {
            if (!currentWeapon) return;
            if (!CanAttack()) return;

            StopBlocking();
            (inputBuffer as NetworkInputBuffer).AddInput(actionKey);
            
            if (debug) Debug.LogWarningFormat("Execute Attack key: {0} isMine: {1}", actionKey, photonView.IsMine);
            
            photonView.RPC(nameof(NExecute), RpcTarget.Others, (int)actionKey);
        }

        private void OnAttack(MeleeClip clip)
        {
            if (debug) Debug.LogWarningFormat("OnAttack clp: {0} isMine: {1}", clip, photonView.IsMine);

            if (photonView.IsMine)
            {
                photonView.RPC(nameof(NAttack), RpcTarget.Others, MeleeRegistry.Instance.clips.IndexOf(clip));
            }
        }

        private void OnSheatheWeapon(MeleeWeapon weapon)
        {
            if (debug) Debug.LogWarningFormat("OnSheatheWeapon weapon: {0} isMine: {1}", weapon, photonView.IsMine);

            if (photonView.IsMine)
            {
                photonView.RPC(nameof(NSheatheWeapon), RpcTarget.Others);
            }
        }

        private void OnDrawWeapon(MeleeWeapon weapon)
        {
            if (debug) Debug.LogWarningFormat("OnDrawWeapon weapon: {0} isMine: {1}", weapon, photonView.IsMine);

            if (photonView.IsMine)
            {
                photonView.RPC(nameof(NDrawWeapon), RpcTarget.Others,
                    MeleeRegistry.Instance.weapons.IndexOf(weapon));
            }
        }

        private void OnStaggered(float duration)
        {
            if (debug) Debug.LogWarningFormat("OnStaggered duration: {0} isMine: {1}", duration, photonView.IsMine);

            if (photonView.IsMine)
            {
                photonView.RPC(nameof(NStaggered), RpcTarget.Others, duration);
            }
        }

        private void OnBlock(bool isBlocking)
        {
            if (debug) Debug.LogWarningFormat("OnStaggered isBlocking: {0} isMine: {1}", isBlocking, photonView.IsMine);

            if (photonView.IsMine)
            {
                if (isBlocking) photonView.RPC(nameof(NStartBlock), RpcTarget.Others);
                else photonView.RPC(nameof(NStopBlock), RpcTarget.Others);
            }
        }
        
        private IEnumerator NetSheathe()
        {
            // if (this.Character.characterLocomotion.isBusy) yield break;
            // if (!this.CanAttack()) yield break;
            // if (this.IsAttacking) yield break;

            ReleaseTargetFocus();

            WaitForSeconds wait = new WaitForSeconds(0f);
            if (currentWeapon != null)
            {
                if (currentWeapon.characterState != null)
                {
                    CharacterState currentState = CharacterAnimator.GetState(MeleeWeapon.LAYER_STANCE);
                    if (currentState != null)
                    {
                        float time = ResetState(currentState, MeleeWeapon.LAYER_STANCE);
                        wait = new WaitForSeconds(time);
                    }
                }

                PlayAudio(currentWeapon.audioSheathe);
            }

            Character.characterLocomotion.isBusy = true;
            IsSheathing = true;

            yield return wait;

            if (EventSheatheWeapon != null) EventSheatheWeapon.Invoke(currentWeapon);
            if (modelWeapon != null) Destroy(modelWeapon);
            if (modelShield != null) Destroy(modelShield);

            OnSheatheWeapon();

            yield return wait;

            IsSheathing = false;

            previousWeapon = currentWeapon;
            previousShield = currentShield;

            currentWeapon = null;
            currentShield = null;

            comboSystem = null;

            Character.characterLocomotion.isBusy = false;
        }

        private IEnumerator NetDraw(MeleeWeapon weapon, MeleeShield shield = null)
        {
            // if (this.Character.characterLocomotion.isBusy) yield break;
            // if (this.IsAttacking) yield break;
            // if (!this.CanAttack()) yield break;

            yield return NetSheathe();

            if (weapon != null)
            {
                currentWeapon = weapon;
                EquipShield(shield != null ? shield : weapon.defaultShield);

                comboSystem = new ComboSystem(this, weapon.combos);

                WaitForSeconds wait = new WaitForSeconds(0f);

                if (currentWeapon.characterState != null)
                {
                    CharacterState state = currentWeapon.characterState;
                    float time = ChangeState(
                        currentWeapon.characterState,
                        currentWeapon.characterMask,
                        MeleeWeapon.LAYER_STANCE
                    );

                    if (state.enterClip != null) wait = new WaitForSeconds(time);
                }

                PlayAudio(currentWeapon.audioDraw);

                Character.characterLocomotion.isBusy = true;
                IsDrawing = true;

                yield return wait;

                if (EventDrawWeapon != null) EventDrawWeapon.Invoke(currentWeapon);

                modelWeapon = currentWeapon.EquipWeapon(CharacterAnimator);
                Blade = modelWeapon.GetComponentInChildren<BladeComponent>();
                if (Blade != null) Blade.Setup(this);

                OnDrawWeapon();

                yield return wait;

                IsDrawing = false;
                Character.characterLocomotion.isBusy = false;
            }
        }

        #endregion

        #region RPCs

        [PunRPC]
        private void NExecute(int networkKey, PhotonMessageInfo info)
        {
            ActionKey actionKey = (ActionKey) networkKey;
            StopBlocking();
            ((NetworkInputBuffer) inputBuffer).AddInput(actionKey, (float)info.SentServerTime);
            
            if (debug) Debug.LogWarningFormat("Network Execute Attack key: {0} sender: {1}", actionKey, info.Sender);
        }

        [PunRPC]
        private void NAttack(int clipIndex, PhotonMessageInfo info)
        {
            if (debug) Debug.LogWarningFormat("Network Attack sender: {0} clipIndex: {1}", info.Sender, clipIndex);
            currentMeleeClip = MeleeRegistry.Instance.clips[clipIndex];
            targetsEvaluated = new HashSet<int>();
            currentMeleeClip.Play(this);
        }

        [PunRPC]
        private void NChangeShield(int shieldIndex, PhotonMessageInfo info)
        {
            if (debug) Debug.LogWarningFormat("Network ChangeShield sender: {0} shieldIndex: {1}", info.Sender, shieldIndex);
            EquipShield(MeleeRegistry.Instance.shields[shieldIndex]);
        }

        [PunRPC]
        private void NSheatheWeapon(PhotonMessageInfo info)
        {
            if (debug) Debug.LogWarningFormat("Network SheatheWeapon sender: {0}", info.Sender);

            CoroutinesManager.Instance.StartCoroutine(NetSheathe());
        }

        [PunRPC]
        private void NDrawWeapon(int weaponIndex, PhotonMessageInfo info)
        {
            if (debug) Debug.LogWarningFormat("Network DrawWeapon sender: {0} weaponIndex: {1}", info.Sender, weaponIndex);
            CoroutinesManager.Instance.StartCoroutine(NetDraw(
                MeleeRegistry.Instance.weapons[weaponIndex]
            ));
        }

        [PunRPC]
        private void NStaggered(float duration, PhotonMessageInfo info)
        {
            if (debug) Debug.LogWarningFormat("Network Staggered sender: {0} duration: {1}", info.Sender, duration);
            if (comboSystem == null)
            {
                Debug.LogWarningFormat("Error receiving Stagger posture on: {0}", info.Sender);
                return;
            }
            // SetPosture(MeleeClip.Posture.Stagger, duration);
            if (!IsStaggered)
            {
                comboSystem.Stop();
                // if (EventStagger != null) EventStagger.Invoke();
            }

            isStaggered = true;
            staggerEndtime = GetTime() + duration;
        }

        [PunRPC]
        private void NStartBlock(PhotonMessageInfo info)
        {
            if (debug) Debug.LogWarningFormat("Network StartBlock sender: {0}", info.Sender);
            StartBlocking();
        }

        [PunRPC]
        private void NStopBlock(PhotonMessageInfo info)
        {
            if (debug) Debug.LogWarningFormat("Network StopBlock sender: {0}", info.Sender);
            StopBlocking();
        }

        [PunRPC]
        private void SyncCharacterMelee(int weaponIndex, int shieldIndex, PhotonMessageInfo info)
        {
            if (debug) Debug.LogWarningFormat("SyncCharacterMelee weapon: {0} shield: {1} isMine: {2} sender: {3}", 
                weaponIndex, shieldIndex, photonView.IsMine, info.Sender);
            if(weaponIndex != -1)
            {
                CoroutinesManager.Instance.StartCoroutine(NetDraw(
                    MeleeRegistry.Instance.weapons[weaponIndex]
                ));
            }
            
            if(shieldIndex != -1)
            {
                EquipShield(MeleeRegistry.Instance.shields[shieldIndex]);
            }

            networkInitialized = true;
        }

        #endregion

        #region Network Events

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(Poise);
                stream.SendNext(Defense);
                // if(currentShield != null) 
                // stream.SendNext(IsAttacking);
                // stream.SendNext(IsBlocking);
                // stream.SendNext(IsDrawing);
                // stream.SendNext(IsInvincible);
                // stream.SendNext(IsSheathing);
                // stream.SendNext(IsStaggered);
            }
            else
            {
                Poise = Mathf.Clamp((float) stream.ReceiveNext(), 0f, maxPoise.GetValue(gameObject));
                Defense = Mathf.Clamp((float) stream.ReceiveNext(), 0f, currentShield ? currentShield.maxDefense.GetValue(gameObject) : 0);
                //SetDefense((float) stream.ReceiveNext());
                //SetPoise((float) stream.ReceiveNext());

                // bool isAttacking = (bool)stream.ReceiveNext();
                //
                // if (!IsAttacking)
                // {
                //     
                // }
            }
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (debug) Debug.LogWarningFormat("OnPlayerEnteredRoom weapon: {0} shield: {1} player: {2}", 
                MeleeRegistry.Instance.weapons.IndexOf(currentWeapon), MeleeRegistry.Instance.shields.IndexOf(currentShield), newPlayer);
            
            if (photonView.IsMine)
            {
                photonView.RPC(nameof(SyncCharacterMelee), newPlayer,
                    MeleeRegistry.Instance.weapons.IndexOf(currentWeapon),
                    MeleeRegistry.Instance.shields.IndexOf(currentShield));
            }
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
        }

        #endregion

        #endif
    }
}