namespace GameCreator.Melee
{
    using UnityEngine;

    public class ShooterComponent : MonoBehaviour
    {
        private static readonly Color GIZMOS_DEFAULT_COLOR = Color.yellow;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private GameObject projectilePrefab;

        public void Shoot()
        {
            Debug.Log("Shoot at " + Time.time);
            GameObject projectileInstance = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
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