using UnityEngine;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Melee;
using Unity.Netcode;


public class CharacterShooter : NetworkBehaviour
{

    public enum ProjectileType {
        Bullet,
        Grenade
    }
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ProjectileType projectileType = ProjectileType.Bullet;
    [SerializeField] private int clipSize;
    [SerializeField] private int magCapacity;
    [SerializeField] private float reloadTime;
    private CharacterMelee melee;

    private void Awake() {
        melee = GetComponentInParent<CharacterMelee>();
    }
}