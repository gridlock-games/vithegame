using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core.Weapons
{
    public class Quiver : MonoBehaviour
    {
        [SerializeField] private Renderer[] projectileRenderers;

        public void Initialize(LoadoutManager loadoutManager, Weapon weapon)
        {
            this.loadoutManager = loadoutManager;
            shooterWeapon = weapon;
        }

        private void OnDisable()
        {
            lastAmmoCount = -1;
            loadoutManager = null;
            shooterWeapon = null;
        }

        private LoadoutManager loadoutManager;
        private Weapon shooterWeapon;
        private int lastAmmoCount = -1;
        private void Update()
        {
            if (!loadoutManager) { return; }
            if (!shooterWeapon) { return; }
            if (!loadoutManager.IsSpawned) { return; }

            int ammoCount = loadoutManager.GetAmmoCount(shooterWeapon);
            if (lastAmmoCount != ammoCount)
            {
                for (int i = 0; i < projectileRenderers.Length; i++)
                {
                    projectileRenderers[i].forceRenderingOff = i >= ammoCount;
                }
                lastAmmoCount = ammoCount;
            }
        }
    }
}