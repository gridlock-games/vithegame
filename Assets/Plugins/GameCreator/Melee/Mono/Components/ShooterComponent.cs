namespace GameCreator.Melee
{
    using UnityEngine;
    using Unity.Netcode;

    public class ShooterComponent : MonoBehaviour
    {
        private static readonly Color GIZMOS_DEFAULT_COLOR = Color.yellow;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private GameObject projectilePrefab;

        public void Shoot(CharacterMelee attacker, MeleeClip meleeClip)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("ShooterComponent.Shoot() should only be called on the server"); return; }

            GameObject projectileInstance = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            projectileInstance.GetComponent<Projectile>().Initialize(attacker, meleeClip);
            projectileInstance.GetComponent<NetworkObject>().Spawn();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = GIZMOS_DEFAULT_COLOR;
            if (projectileSpawnPoint)
            {
                Gizmos.DrawWireCube(projectileSpawnPoint.position, Vector3.one * 0.05f);
            }
            Gizmos.DrawLine(projectileSpawnPoint.position, projectileSpawnPoint.forward * 20);
        }
    }
}