using UnityEngine;
using Vi.Core.CombatAgents;
using Vi.Core.GameModeManagers;

namespace Vi.Core.Structures
{
    public class EssenceWarTotem : Structure
    {
        private EssenceWarManager essenceWarManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            essenceWarManager = GameModeManager.Singleton.GetComponent<EssenceWarManager>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (collision.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent.GetTeam() == GetTeam())
                {
                    if (essenceWarManager.TryGetBearerInstance(out Attributes bearer))
                    {
                        if (networkCollider.CombatAgent == bearer)
                        {
                            essenceWarManager.OnBearerReachedTotem(GetTeam());
                        }
                    }
                }
            }
        }
    }
}