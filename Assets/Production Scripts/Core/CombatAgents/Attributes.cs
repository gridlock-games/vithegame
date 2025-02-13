using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Vi.Core.GameModeManagers;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.VFX;
using Vi.Core.Weapons;

namespace Vi.Core.CombatAgents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WeaponHandler))]
    public class Attributes : CombatAgent
    {
        private NetworkVariable<int> playerDataId = new NetworkVariable<int>();
        public int GetPlayerDataId() { return playerDataId.Value; }
        public void SetPlayerDataId(int id) { playerDataId.Value = id; name = PlayerDataManager.Singleton.GetPlayerData(id).character.name.ToString(); }
        public override PlayerDataManager.Team GetTeam() { return CachedPlayerData.team; }

        public override string GetName() { return CachedPlayerData.character.name.ToString(); }

        public override Weapon.ArmorType GetArmorType() { return AnimationHandler.GetArmorType(); }

        public override CharacterReference.RaceAndGender GetRaceAndGender() { return CachedPlayerData.character.raceAndGender; }

        public PlayerDataManager.PlayerData CachedPlayerData { get; private set; }

        public void SetCachedPlayerData(PlayerDataManager.PlayerData playerData)
        {
            if (playerData.id != GetPlayerDataId()) { Debug.LogError("Player data doesn't have the same id!"); return; }
            CachedPlayerData = playerData;
        }

        [SerializeField] private PooledObject teamIndicatorPrefab;
        private PooledObject teamIndicatorInstance;

        public override void OnNetworkSpawn()
        {
            SetCachedPlayerData(PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId()));
            base.OnNetworkSpawn();

            if (NetworkManager.Singleton.IsServer)
            {
                UpdateNetworkVisiblity();
            }

            StartCoroutine(AddPlayerObjectToPlayerDataManager());

            comboCounter.OnValueChanged += OnComboCounterChange;

            teamIndicatorInstance = ObjectPoolingManager.SpawnObject(teamIndicatorPrefab, transform);
            teamIndicatorInstance.transform.localPosition = new Vector3(0, 0.01f, 0);

            if (NetworkObject.IsPlayerObject)
            {
                if (!IsServer & !IsLocalPlayer)
                {
                    StartCoroutine(WebRequestManager.Singleton.CharacterManager.GetCharacterAttributes(CachedPlayerData.character._id.ToString()));
                }
            }
        }

        public void UpdateNetworkVisiblity()
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Attributes.UpdateNetworkVisibility() should only be called on the server!"); return; }
            if (!gameObject.activeInHierarchy) { return; }
            StartCoroutine(SetNetworkVisibilityAfterSpawn());
        }

        private IEnumerator SetNetworkVisibilityAfterSpawn()
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Attributes.SetNetworkVisibilityAfterSpawn() should only be called on the server!"); yield break; }
            yield return null;
            if (!IsSpawned) { yield return new WaitUntil(() => IsSpawned); }

            if (!NetworkObject.IsNetworkVisibleTo(OwnerClientId)) { NetworkObject.NetworkShow(OwnerClientId); }

            PlayerDataManager.PlayerData thisPlayerData = PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId());
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators())
            {
                ulong networkId = playerData.id >= 0 ? (ulong)playerData.id : 0;
                if (networkId == 0) { continue; }
                if (networkId == OwnerClientId) { continue; }

                if (playerData.channel == thisPlayerData.channel)
                {
                    if (!NetworkObject.IsNetworkVisibleTo(networkId))
                    {
                        NetworkObject.NetworkShow(networkId);
                    }
                }
                else
                {
                    if (NetworkObject.IsNetworkVisibleTo(networkId))
                    {
                        NetworkObject.NetworkHide(networkId);
                    }
                }
            }
        }

        private IEnumerator AddPlayerObjectToPlayerDataManager()
        {
            yield return new WaitUntil(() => IsSpawned);
            if (!(IsHost & IsLocalPlayer)) { yield return new WaitUntil(() => GetPlayerDataId() != (int)NetworkManager.ServerClientId); }
            PlayerDataManager.Singleton.AddPlayerObject(GetPlayerDataId(), this);

            //foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
            //{
            //    foreach (Collider col in NetworkCollider.Colliders)
            //    {
            //        foreach (Collider otherCol in attributes.NetworkCollider.Colliders)
            //        {
            //            Physics.IgnoreCollision(col, otherCol, true);
            //        }
            //    }
            //}
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            comboCounter.OnValueChanged -= OnComboCounterChange;

            PlayerDataManager.Singleton.RemovePlayerObject(GetPlayerDataId());

            ObjectPoolingManager.ReturnObjectToPool(ref teamIndicatorInstance);
        }

        private bool TryGetCharacterStats(out CharacterManager.CharacterStats characterStats)
        {
            if (WebRequestManager.Singleton.CharacterManager.TryGetCharacterStats(CachedPlayerData.character._id.ToString(), out CharacterManager.CharacterStats stats))
            {
                characterStats = stats;
                return true;
            }
            else
            {
                characterStats = new CharacterManager.CharacterStats();
                return false;
            }
        }

        public override float GetMaxHP()
        {
            if (TryGetCharacterStats(out CharacterManager.CharacterStats stats))
            {
                return stats.hp + SessionProgressionHandler.MaxHPBonus;
            }
            else
            {
                return base.GetMaxHP();
            }
        }

        public override float GetMaxStamina()
        {
            if (TryGetCharacterStats(out CharacterManager.CharacterStats stats))
            {
                return stats.stamina + SessionProgressionHandler.MaxStaminaBonus;
            }
            else
            {
                return base.GetMaxStamina();
            }
        }

        public override float GetMaxPhysicalArmor()
        {
            if (TryGetCharacterStats(out CharacterManager.CharacterStats stats))
            {
                return stats.defense + SessionProgressionHandler.MaxArmorBonus;
            }
            else
            {
                return base.GetMaxPhysicalArmor();
            }
        }

        public override float GetMaxMagicalArmor()
        {
            if (TryGetCharacterStats(out CharacterManager.CharacterStats stats))
            {
                return stats.mdefense + SessionProgressionHandler.MaxArmorBonus;
            }
            else
            {
                return base.GetMaxMagicalArmor();
            }
        }

        [SerializeField] private AudioClip heartbeatSoundEffect;
        private const float heartbeatVolume = 1;
        private const float heartbeatHPPercentageThreshold = 0.1f;

        protected override void OnHPChanged(float prev, float current)
        {
            base.OnHPChanged(prev, current);
            if (IsLocalPlayer)
            {
                if (current / GetMaxHP() < heartbeatHPPercentageThreshold)
                {
                    if (!heartbeatSoundIsPlaying) { StartCoroutine(PlayHeartbeatSound()); }
                }
            }
        }

        private bool heartbeatSoundIsPlaying;
        private IEnumerator PlayHeartbeatSound()
        {
            heartbeatSoundIsPlaying = true;
            AudioSource audioSource = AudioManager.Singleton.Play2DClip(gameObject, heartbeatSoundEffect, heartbeatVolume);

            while (true)
            {
                if (!audioSource) { break; }
                if (!audioSource.isPlaying) { break; }
                if (GetAilment() == ActionClip.Ailment.Death) { break; }
                if (GetHP() / GetMaxHP() >= heartbeatHPPercentageThreshold) { break; }
                yield return null;
            }

            if (audioSource) { if (audioSource.isPlaying) { audioSource.Stop(); } }
            heartbeatSoundIsPlaying = false;
        }

        protected override void Awake()
        {
            base.Awake();
            SetCachedPlayerData(PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId()));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CachedPlayerData = default;

            lastComboCounterChangeTime = default;

            IsRespawning = false;
            isWaitingForSpawnPoint = false;
            respawnSelfCalledTime = default;
        }

        protected override float GetPhysicalAttack()
        {
            if (TryGetCharacterStats(out CharacterManager.CharacterStats characterStats))
            {
                switch (LoadoutManager.GetEquippedSlotType())
                {
                    case LoadoutManager.WeaponSlotType.Primary:
                        return characterStats.attack + characterStats.weaponABaseAtk;
                    case LoadoutManager.WeaponSlotType.Secondary:
                        return characterStats.attack + characterStats.weaponBBaseAtk;
                    default:
                        Debug.LogWarning("Unsure how to handle weapon slot type " + LoadoutManager.GetEquippedSlotType());
                        break;
                }
                return characterStats.attack;
            }
            else
            {
                return 0;
            }
        }

        protected override float GetMagicalAttack()
        {
            if (TryGetCharacterStats(out CharacterManager.CharacterStats characterStats))
            {
                switch (LoadoutManager.GetEquippedSlotType())
                {
                    case LoadoutManager.WeaponSlotType.Primary:
                        return characterStats.mattack + characterStats.weaponABaseAtk;
                    case LoadoutManager.WeaponSlotType.Secondary:
                        return characterStats.mattack + characterStats.weaponBBaseAtk;
                    default:
                        Debug.LogWarning("Unsure how to handle weapon slot type " + LoadoutManager.GetEquippedSlotType());
                        break;
                }
                return characterStats.mattack;
            }
            else
            {
                return 0;
            }
        }

        public override bool ProcessMeleeHit(CombatAgent attacker, NetworkObject attackingNetworkObject, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessMeleeHit() should only be called on the server!"); return false; }
            HitResult hitResult = ProcessHit(true, attacker, attackingNetworkObject, attack, impactPosition, hitSourcePosition, runtimeWeapon.GetHitCounter(), runtimeWeapon);
            
            return CastHitResultToBoolean(hitResult);
        }

        public override bool ProcessProjectileHit(CombatAgent attacker, NetworkObject attackingNetworkObject, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessProjectileHit() should only be called on the server!"); return false; }
            HitResult hitResult = ProcessHit(false, attacker, attackingNetworkObject, attack, impactPosition, hitSourcePosition, hitCounter, runtimeWeapon, damageMultiplier);
            
            return CastHitResultToBoolean(hitResult);
        }

        public void AddHitToComboCounter() { comboCounter.Value++; }

        private const float comboCounterResetTime = 3;

        private NetworkVariable<int> comboCounter = new NetworkVariable<int>();
        private float lastComboCounterChangeTime;

        private void OnComboCounterChange(int prev, int current)
        {
            lastComboCounterChangeTime = Time.time;
        }

        public int GetComboCounter() { return comboCounter.Value; }

        public void ResetComboCounter()
        {
            if (!IsServer) { Debug.LogError("Reset combo counter should only be called on the server!"); return; }
            comboCounter.Value = 0;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (Time.time - lastComboCounterChangeTime >= comboCounterResetTime) { comboCounter.Value = 0; }
        }

        public void StopRespawnSelfCoroutine()
        {
            if (respawnCoroutine != null)
            {
                IsRespawning = false;
                StopCoroutine(respawnCoroutine);
            }
        }

        protected override void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current)
        {
            base.OnAilmentChanged(prev, current);
            if (current == ActionClip.Ailment.Death)
            {
                respawnCoroutine = StartCoroutine(RespawnSelf());
            }
            else if (prev == ActionClip.Ailment.Death)
            {
                if (respawnCoroutine != null)
                {
                    IsRespawning = false;
                    StopCoroutine(respawnCoroutine);
                }
            }
        }

        public float GetRespawnTime() { return Mathf.Clamp(GameModeManager.Singleton.GetRespawnTime() - (Time.time - respawnSelfCalledTime), 0, GameModeManager.Singleton.GetRespawnTime()); }
        public float GetRespawnTimeAsPercentage()
        {
            if (GetRespawnTime() <= 5)
            {
                if (GameModeManager.Singleton.GetRespawnTime() <= 5)
                {
                    return 1 - (GetRespawnTime() / GameModeManager.Singleton.GetRespawnTime());
                }
                else
                {
                    return 1 - (GetRespawnTime() / 5);
                }
            }
            else
            {
                return 0;
            }
        }

        public bool IsRespawning { get; private set; }
        [HideInInspector] public bool isWaitingForSpawnPoint;
        private Coroutine respawnCoroutine;
        private float respawnSelfCalledTime;
        private IEnumerator RespawnSelf()
        {
            if (!GameModeManager.Singleton) { yield break; }
            if (GameModeManager.Singleton.GetRespawnTime() <= 0) { yield break; }
            IsRespawning = true;
            respawnSelfCalledTime = Time.time;
            yield return new WaitForSeconds(GameModeManager.Singleton.GetRespawnTime());
            if (IsServer & !GameModeManager.Singleton.IsGameOver())
            {
                yield return PlayerDataManager.Singleton.RespawnPlayer(this);
            }
            yield return new WaitUntil(() => ailment.Value != ActionClip.Ailment.Death);
            IsRespawning = false;
        }
    }
}