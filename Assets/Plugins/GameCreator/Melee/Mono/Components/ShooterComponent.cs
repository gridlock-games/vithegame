namespace GameCreator.Melee
{
    using UnityEngine;
    using Unity.Netcode;

    public class ShooterComponent : MonoBehaviour
    {
        private static readonly Color GIZMOS_DEFAULT_COLOR = Color.yellow;

        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private Vector3 projectilePositionOffset;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Vector3 aimOffset;

        public void Shoot(CharacterMelee attacker, MeleeClip meleeClip, Vector3 projectileSpeed)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("ShooterComponent.Shoot() should only be called on the server"); return; }

            GameObject projectileInstance = Instantiate(projectilePrefab.gameObject, projectileSpawnPoint.position + projectileSpawnPoint.rotation * projectilePositionOffset, projectileSpawnPoint.rotation);
            Projectile proj = projectileInstance.GetComponent<Projectile>();
            proj.Initialize(attacker, meleeClip, projectileSpeed);
            proj.NetworkObject.Spawn();
        }

        public Transform GetProjectileSpawnPoint() { return projectileSpawnPoint; }

        public Quaternion GetAimOffset() { return Quaternion.Euler(aimOffset); }

        private void OnDrawGizmos()
        {
            Gizmos.color = GIZMOS_DEFAULT_COLOR;
            if (projectileSpawnPoint)
            {
                Gizmos.DrawWireCube(projectileSpawnPoint.position, Vector3.one * 0.05f);
            }
            Gizmos.DrawLine(projectileSpawnPoint.position, projectileSpawnPoint.position + projectileSpawnPoint.forward * 20);
        }
    }
}