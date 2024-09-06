using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;

namespace Vi.Isolated
{
    public class KillAgentCollider : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (collision.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death)
                    networkCollider.CombatAgent.ProcessEnvironmentDamage(-networkCollider.CombatAgent.GetMaxHP(), null);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (collision.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death)
                    networkCollider.CombatAgent.ProcessEnvironmentDamage(-networkCollider.CombatAgent.GetMaxHP(), null);
            }
        }
    }
}