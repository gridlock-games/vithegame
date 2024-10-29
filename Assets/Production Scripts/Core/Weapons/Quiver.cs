using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core.Weapons
{
    public class Quiver : MonoBehaviour
    {
        [SerializeField] private Renderer[] projectileRenderers;

        public void Initialize(CombatAgent combatAgent, Weapon weapon)
        {
            this.combatAgent = combatAgent;
            shooterWeapon = weapon;
        }

        private void OnDisable()
        {
            lastAmmoCount = -1;
            combatAgent = null;
            shooterWeapon = null;
        }

        private CombatAgent combatAgent;
        private Weapon shooterWeapon;
        private int lastAmmoCount = -1;
        private void Update()
        {
            if (!combatAgent) { return; }
            if (!shooterWeapon) { return; }
            if (!combatAgent.IsSpawned) { return; }

            int ammoCount = combatAgent.LoadoutManager.GetAmmoCount(shooterWeapon);
            if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.Reload)
            {
                if (combatAgent.AnimationHandler.IsActionClipPlaying(combatAgent.WeaponHandler.CurrentActionClip))
                {
                    ammoCount = combatAgent.WeaponHandler.GetMaxAmmoCount();
                }
            }

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