using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Vi.ScriptableObjects;

namespace Vi.Player
{
    [RequireComponent(typeof(Animator))]
    public class AnimationHandler : NetworkBehaviour
    {
        private NetworkVariable<FixedString32Bytes> currentActionStateName = new NetworkVariable<FixedString32Bytes>("Empty");

        public void PlayAction(ActionClip actionClip)
        {
            currentActionStateName.Value = actionClip.name;
        }

        public override void OnNetworkSpawn()
        {
            currentActionStateName.OnValueChanged += OnCurrentActionStateNameChange;
        }

        public override void OnNetworkDespawn()
        {
            currentActionStateName.OnValueChanged -= OnCurrentActionStateNameChange;
        }

        private void OnCurrentActionStateNameChange(FixedString32Bytes prev, FixedString32Bytes current)
        {
            animator.Play(current.ToString(), animator.GetLayerIndex("Actions"));
        }

        Animator animator;

        private void Start()
        {
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(currentActionStateName.Value.ToString()))
            {
                Debug.LogError(Time.time + " actions layer state does not match network state name, don't play animations except from the animation handler script!");
            }
        }

        private Vector3 networkRootMotion;
        public Vector3 ApplyNetworkRootMotion()
        {
            Vector3 _ = networkRootMotion;
            networkRootMotion = Vector3.zero;
            return _;
        }

        private Vector3 localRootMotion;
        public Vector3 ApplyLocalRootMotion()
        {
            Vector3 _ = localRootMotion;
            localRootMotion = Vector3.zero;
            return _;
        }

        private void OnAnimatorMove()
        {
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            {
                networkRootMotion += animator.deltaPosition;
                localRootMotion += animator.deltaPosition;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            
        }
    }
}