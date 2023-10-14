namespace GameCreator.Melee
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Audio;
    using Unity.Netcode;
    using GameCreator.Core;
    using GameCreator.Characters;
    using GameCreator.Variables;
    using GameCreator.Pool;
    using static GameCreator.Melee.MeleeClip;
    using UnityEngine.SceneManagement;
    using System.Reflection;
    using GameCreator.Camera;
    using LightPat.Core;
    using MJM;

    [RequireComponent(typeof(Character))]
    [AddComponentMenu("Game Creator/Melee/Character Melee")]
    public class CharacterMelee : TargetMelee
    {
        public enum ActionKey
        {
            A, B, C,
            D, E, F
        }

        public enum HitResult
        {
            Ignore,
            ReceiveDamage,
            AttackBlock,
            PerfectBlock,
            PoiseBlock
        }

        private enum AttackDirection
        {
            Front,
            Left,
            Right,
            Back
        }

        private const float MIN_RAND_PITCH = 0.8f;
        private const float MAX_RAND_PITCH = 1.2f;

        private const float TRANSITION = 0.15f;
        protected const float INPUT_BUFFER_TIME = 0.35f;

        protected const float STUN_TIMEOUT_DURATION = 5.0f;

        private int KNOCK_UP_FOLLOWUP_LIMIT = 5;

        private const CharacterAnimation.Layer LAYER_DEFEND = CharacterAnimation.Layer.Layer3;

        private static readonly Vector3 PLANE = new Vector3(1, 0, 1);


        public NetworkVariable<bool> IsCastingAbility { get; private set; } = new NetworkVariable<bool>();

        private CameraMotorTypeAdventure adventureMotor = null;
        // PROPERTIES: ----------------------------------------------------------------------------

        public MeleeWeapon currentWeapon;
        public MeleeShield currentShield;

        public MeleeWeapon previousWeapon;
        public MeleeShield previousShield;

        protected ComboSystem comboSystem;
        protected InputBuffer inputBuffer;

        private NetworkVariable<float> Poise = new NetworkVariable<float>();
        private float poiseDelayCooldown;

        public NumberProperty delayPoise = new NumberProperty(1f);
        public NumberProperty maxPoise = new NumberProperty(5f);
        public NumberProperty poiseRecoveryRate = new NumberProperty(1f);


        public NumberProperty maxRage = new NumberProperty(100.0f);
        public NumberProperty rageRecoveryRate = new NumberProperty(1f);


        public float attackInterval = 0.10f;

        private NetworkVariable<float> Defense = new NetworkVariable<float>();
        private float defenseDelayCooldown;

        public bool IsDrawing { get; protected set; }
        public bool IsSheathing { get; protected set; }

        public bool IsAttacking { get; private set; }
        public NetworkVariable<bool> IsBlocking = new NetworkVariable<bool>();
        public bool HasFocusTarget { get; private set; }


        public bool IsGrabbing { get; private set; }
        public bool IsGrabbed { get; private set; }

        public bool IsStaggered => this.isStaggered && GetTime() <= this.staggerEndtime;
        public bool IsInvincible => this.isInvincible && GetTime() <= this.invincibilityEndTime;
        public bool IsUninterruptable => this.isUninterruptable && GetTime() <= this.uninterruptableEndTime;

        public Action<MeleeWeapon> EventDrawWeapon;
        public Action<MeleeWeapon> EventSheatheWeapon;
        public event Action<MeleeClip> EventAttack;
        // public event Action<MeleeClip> EventOnHitObstacle;
        public event Action<float> EventStagger;
        public event Action EventBreakDefense;
        public event Action<bool> EventBlock;
        public event Action<bool> EventFocus;

        // PRIVATE PROPERTIES: --------------------------------------------------------------------

        protected List<GameObject> modelWeapons;
        protected GameObject modelWeapon;
        protected GameObject modelShield;

        public MeleeClip currentMeleeClip;
        protected HashSet<int> targetsEvaluated;

        private float startBlockingTime = -100f;

        protected bool isStaggered;
        protected float staggerEndtime;

        private bool isInvincible;
        private float invincibilityEndTime;

        private bool isUninterruptable;
        private float uninterruptableEndTime;

        private float anim_ExecuterDuration = 0.0f;
        private float anim_ExecutedDuration = 0.0f;

        public AbilityManager abilityManager { get; private set; }
        private GlowRenderer glowRenderer;

        private MJMComboSystem mjmComboSystem;

        public bool isLunging = false;
        private static readonly Keyframe[] DEFAULT_KEY_MOVEMENT = {
            new Keyframe(0f, 0f),
            new Keyframe(1f, 0f)
        };
        public AnimationCurve movementForward = new AnimationCurve(DEFAULT_KEY_MOVEMENT);


        // ACCESSORS: -----------------------------------------------------------------------------

        public Character Character { get; protected set; }
        public CharacterAnimator CharacterAnimator { get; protected set; }
        public BladeComponent Blade { get; protected set; }

        public List<BladeComponent> Blades { get; protected set; }


        // INITIALIZERS: --------------------------------------------------------------------------

        private float originalRunSpeed;

        protected virtual void Awake()
        {
            this.Character = GetComponent<Character>();
            this.CharacterAnimator = GetComponent<CharacterAnimator>();
            this.inputBuffer = new InputBuffer(INPUT_BUFFER_TIME);
            abilityManager = GetComponentInParent<AbilityManager>();
            glowRenderer = GetComponentInChildren<GlowRenderer>();

            mjmComboSystem = GetComponentInChildren<MJMComboSystem>();

            originalRunSpeed = Character.characterLocomotion.runSpeed;
        }

        private void OnTransformChildrenChanged()
        {
            glowRenderer = GetComponentInChildren<GlowRenderer>();
        }

        // FOCUS TARGET: --------------------------------------------------------------------------

        public Transform characterCamera;
        public Vector3 boxCastHalfExtents = Vector3.one;
        public float boxCastDistance = 2;

        // UPDATE: --------------------------------------------------------------------------------

        protected virtual void Start()
        {
            if (IsOwner)
            {
                CameraMotor motor = CameraMotor.MAIN_MOTOR;
                if (motor != null)
                    adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;
            }
        }

        public ActionKey LastActionKey { get; private set; }

        private void Update()
        {
            if (IsServer)
            {
                this.UpdatePoise();
                this.UpdateDefense();
                this.UpdateRage();
            }

            // Adding check block to make sure melee animations are cancelled as soon ailment == dead
            if (this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.Dead)
            {
                this.StopAttack();
                this.CharacterAnimator.StopGesture(0f);
                this.currentMeleeClip = null;

                return;
            }

            if (this.comboSystem != null)
            {
                this.comboSystem.Update();

                if (!this.CanAttack()) return;
                if (this.Character.isCharacterDashing()) return;

                if (this.CanAttack() && this.inputBuffer.HasInput())
                {
                    ActionKey key = this.inputBuffer.GetInput();
                    LastActionKey = key;
                    MeleeClip meleeClip = this.comboSystem.Select(key);

                    if (meleeClip)
                    {
                        if (IsServer)
                        {
                            if (TryGetComponent(out CharacterShooter characterShooter))
                            {
                                characterShooter.ResetShootCount();
                                if (new List<ActionKey>() { ActionKey.A, ActionKey.B }.Contains(key))
                                {
                                    if (!characterShooter.IsAiming() | (characterShooter.enableReload & characterShooter.GetCurrentAmmo() <= 0))
                                    {
                                        if (inputBuffer.HasInput()) { inputBuffer.ConsumeInput(); }
                                        comboSystem.Stop();
                                        return;
                                    }
                                }
                            }

                            if (meleeClip.isHeavy) // Heavy Attack
                            {
                                if (Poise.Value <= 20)
                                {
                                    if (this.inputBuffer.HasInput())
                                    {
                                        this.inputBuffer.ConsumeInput();
                                    }
                                    this.comboSystem.Stop();
                                    return;
                                }
                                OnHeavyAttack();
                            }
                            else // Light Attack
                            {
                                OnLightAttack();
                            }
                        }

                        this.inputBuffer.ConsumeInput();
                        bool checkDash = this.Character.characterLocomotion.isDashing;

                        if (IsOwner)
                        {
                            FocusTarget(meleeClip);
                        }

                        this.currentMeleeClip = meleeClip;
                        this.targetsEvaluated = new HashSet<int>();

                        if (!this.currentMeleeClip.isSequence)
                        {
                            this.currentMeleeClip.PlayNetworked(this);

                            if (this.EventAttack != null) this.EventAttack.Invoke(meleeClip);
                        }
                        else if (this.currentMeleeClip.isSequence)
                        {
                            StartCoroutine(SequenceClipPlayHandler(currentMeleeClip));
                        }
                    }
                }
            }

            if (Time.time < slowEndTime | slowed.Value)
            {
                Character.characterLocomotion.runSpeed = slowAmount.Value;
                if (IsServer)
                    slowed.Value = true;
            }
            else
            {
                Character.characterLocomotion.runSpeed = IsBlocking.Value ? currentShield.defenseRunSpeed : overrideRunSpeed.Value;
                Character.characterLocomotion.canRun = IsBlocking.Value ? false : true;
                if (IsServer)
                    slowed.Value = false;
            }

            if (IsLocalPlayer)
            {
                if (IsBlocking.Value)
                {
                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        StopBlockingServerRpc();
                    }
                }
                else // not blocking
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        StartBlockingServerRpc();
                    }
                }

                //if (Input.GetKeyDown(KeyCode.LeftShift))
                //{
                //    if (IsBlocking.Value)
                //    {
                //        StopBlockingServerRpc();
                //    }
                //    else
                //    {
                //        StartBlockingServerRpc();
                //    }
                //}
            }

            if (IsServer)
            {
                if (Character.isCharacterDashing()) { IsBlocking.Value = false; }
            }
        }

        private IEnumerator SequenceClipPlayHandler(MeleeClip sequenceClipParent)
        {
            List<MeleeClip> sequenceChildren = sequenceClipParent.sequencedClips;

            // Reset attack time first
            this.comboSystem.StartAttackTime(false);

            foreach (MeleeClip clip in sequenceChildren)
            {
                this.currentMeleeClip = clip;
                this.comboSystem.StartAttackTime(true);
                clip.PlayNetworked(this);

                if (this.EventAttack != null) this.EventAttack.Invoke(clip);
                yield return new WaitForSeconds(clip.animationClip.length);
            }
        }

        public UnityEngine.Events.UnityEvent EventKnockedUpHitLimitReached;
        public UnityEngine.Events.UnityEvent EventOnHitObstacle;
        public NetworkVariable<int> knockedUpHitCount = new NetworkVariable<int>();

        public int hitCount { get; private set; }
        private float lastHitCountChangeTime;

        public void ResetHitCount()
        {
            hitCount = 0;
        }

        private void LateUpdate()
        {
            glowRenderer.RenderInvincible(IsInvincible);
            glowRenderer.RenderUninterruptable(IsUninterruptable);

            IsAttacking = false;

            if (this.Character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None)
            {
                this.isStaggered = true;
            }

            if (this.comboSystem != null)
            {
                int phase = this.comboSystem.GetCurrentPhase(this.currentMeleeClip);

                IsAttacking = phase >= 0f;

                // Only want hit registration on the owner
                if (!IsServer) { return; }

                if (this.Blades != null && this.Blades.Count > 0 && phase == 1)
                {
                    foreach (BladeComponent blade in this.Blades)
                    {
                        if (!this.currentMeleeClip.affectedBones.Contains(blade.weaponBone)) continue;
                        if (this.IsStaggered) return;

                        // This is what activates the blades
                        GameObject[] hits = blade.CaptureHits();

                        if (hitCount < currentMeleeClip.hitCount)
                        {
                            hitQueue.Enqueue(new MeleeHitQueueElement(this, blade.GetImpactPosition(), hits, comboSystem.GetCurrentClip() ? comboSystem.GetCurrentClip() : currentMeleeClip));
                            ProcessMeleeHitQueue();
                        }
                    }
                }

                if (TryGetComponent(out CharacterShooter characterShooter))
                {
                    //if ((phase > 0 & lastPhase <= 0) | (phase > 0 & newInputThisFrame))
                    if (phase > 0)
                    {
                        characterShooter.Shoot(comboSystem.GetCurrentClip() ? comboSystem.GetCurrentClip() : currentMeleeClip);
                    }
                }
            }
        }

        private static Queue<MeleeHitQueueElement> hitQueue = new Queue<MeleeHitQueueElement>();

        private struct MeleeHitQueueElement
        {
            public CharacterMelee attackerMelee;
            public Vector3 impactPosition;
            public GameObject[] hits;
            public MeleeClip attack;

            public MeleeHitQueueElement(CharacterMelee attackerMelee, Vector3 impactPosition, GameObject[] hits, MeleeClip attack)
            {
                this.attackerMelee = attackerMelee;
                this.impactPosition = impactPosition;
                this.hits = hits;
                this.attack = attack;
            }
        }

        private void ProcessMeleeHitQueue()
        {
            //Debug.Log(Time.time + " " + hitQueue.Count);
            // Wait for one frame for the hit queue to fill with all hits from the previous frame
            // Empty the hit queue and process the hits for each element
            while (hitQueue.Count > 0)
            {
                MeleeHitQueueElement queueElement = hitQueue.Dequeue();
                ProcessAttackedObjects(queueElement.attackerMelee, queueElement.impactPosition, queueElement.hits, queueElement.attack, false, 0);
            }
        }

        private void MarkHit() { StartCoroutine(ResetHitBool()); }

        private bool wasHit;
        private IEnumerator ResetHitBool()
        {
            wasHit = true;
            // Wait until we have stopped attacking to reset hit bool
            yield return new WaitUntil(() => !IsAttacking);
            wasHit = false;
        }

        public HitResult ProcessProjectileHit(CharacterMelee attackerMelee, CharacterMelee targetMelee, Vector3 impactPosition, MeleeClip attack, float healTeammatesPercentage)
        {
            List<HitResult> hitResults = ProcessAttackedObjects(attackerMelee, impactPosition, new GameObject[] { targetMelee.gameObject }, attack, true, healTeammatesPercentage);
            return hitResults.Count > 0 ? hitResults[0] : HitResult.Ignore;
        }

        public static bool CheckHitTeams(ulong attackerClientId, ulong targetClientId)
        {
            // False means they are teaammates, true means they are enemies
            if (ClientManager.Singleton.GetClientDataDictionary().ContainsKey(attackerClientId) & ClientManager.Singleton.GetClientDataDictionary().ContainsKey(targetClientId))
            {
                Team attackerMeleeTeam = ClientManager.Singleton.GetClient(attackerClientId).team;
                Team targetMeleeTeam = ClientManager.Singleton.GetClient(targetClientId).team;

                if (attackerMeleeTeam != Team.Competitor | targetMeleeTeam != Team.Competitor)
                {
                    // If the attacker's team is the same as the victim's team, do not register this hit
                    if (attackerMeleeTeam == targetMeleeTeam)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private List<HitResult> ProcessAttackedObjects(CharacterMelee melee, Vector3 impactPosition, GameObject[] hits, MeleeClip attack, bool projectileHit, float healTeammatesPercentage)
        {
            List<HitResult> hitResults = new List<HitResult>();

            if (SceneManager.GetActiveScene().name == "Hub") { return hitResults; }
            if (!attack.isAttack) { return hitResults; }

            // Repeat the action on each attacked object for a specific number of times
            // Perform the action on the attacked object
            foreach (GameObject hit in hits)
            {
                // Do something with the attacked object
                int hitInstanceID = hit.GetInstanceID();

                if (hit.transform.IsChildOf(transform)) continue;
                if (!projectileHit)
                {
                    if (melee.targetsEvaluated.Contains(hitInstanceID)) continue;
                }

                CharacterMelee targetMelee = hit.GetComponent<CharacterMelee>();
                if (!targetMelee) { continue; }

                if (ClientManager.Singleton)
                {
                    if (ClientManager.Singleton.GetClientDataDictionary().ContainsKey(melee.OwnerClientId) & ClientManager.Singleton.GetClientDataDictionary().ContainsKey(targetMelee.OwnerClientId))
                    {
                        Team attackerMeleeTeam = melee.NetworkObject.IsPlayerObject ? ClientManager.Singleton.GetClient(melee.OwnerClientId).team : Team.Environment;
                        Team targetMeleeTeam = targetMelee.NetworkObject.IsPlayerObject ? ClientManager.Singleton.GetClient(targetMelee.OwnerClientId).team : Team.Environment;

                        if (attackerMeleeTeam != Team.Competitor | targetMeleeTeam != Team.Competitor)
                        {
                            // If the attacker's team is the same as the victim's team, do not register this hit
                            if (attackerMeleeTeam == targetMeleeTeam)
                            {
                                targetMelee.AddHP(targetMelee.HP.Value * (healTeammatesPercentage / 100));
                                continue;
                            }
                        }
                    }
                }

                if (targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.WasGrabbed)
                {
                    if (targetMelee.Character.grabAssailant != melee.Character) { continue; }
                }

                if (targetMelee.IsInvincible) { continue; }
                if (targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.Dead) { continue; }

                // If this attacker melee has already been hit on this frame, ignore the all hits
                if (melee.wasHit) { return hitResults; }
                // If the attacker is dead, don't register their hits
                if (this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.Dead) { continue; }

                // Mark the target as hit, this prevents hit trading
                // If the target is interruptable, don't mark the hit
                // TO DO: This might be a problem when we have team fights
                if (!targetMelee.IsUninterruptable)
                    targetMelee.MarkHit();

                // This is for checking if we are hitting an environment object
                if (hit.CompareTag("Obstacle"))
                {
                    Vector3 position_attackWp = impactPosition;
                    Vector3 position_attacker = melee.transform.position;
                }

                // Increment hit count for attacks that can potentially hit a target many times
                // Want to wait to register a hit until a certain amount of time has passed
                if (attack.hitCount > 1)
                {
                    if (Time.time - melee.lastHitCountChangeTime < attack.multiHitRegDelay) { continue; }
                }

                melee.hitCount++;
                melee.lastHitCountChangeTime = Time.time;

                if (!projectileHit)
                    melee.targetsEvaluated.Add(hitInstanceID);

                if (attack && this.hitCount < attack.hitCount)
                {
                    if (!projectileHit)
                        melee.targetsEvaluated.Remove(hitInstanceID);
                }

                // Calculate hit result/HP damage
                float previousHP = targetMelee.HP.Value;
                melee.Rage.Value += 2;
                KeyValuePair<HitResult, MeleeClip> OnRecieveAttackResult = targetMelee.OnReceiveAttack(melee, attack, impactPosition);

                HitResult hitResult = OnRecieveAttackResult.Key;
                MeleeClip hitReaction = OnRecieveAttackResult.Value;


                // DO NOT REMOVE FOR DEBUGGING
                // if(hitReaction == null) {
                //     Debug.Log("No Hit Reaction");
                // }

                float damage = attack.baseDamage * melee.damageMultiplier.Value * targetMelee.damageReductionMultiplier.Value * targetMelee.damageReceivedMultiplier.Value;
                if (hitResult == HitResult.ReceiveDamage)
                {
                    if (mjmComboSystem)
                        mjmComboSystem.AddCount(1);
                    targetMelee.HP.Value -= damage;
                    targetMelee.Rage.Value += 1;
                    targetMelee.RenderHit();
                }
                else if (hitResult == HitResult.PoiseBlock)
                {
                    targetMelee.HP.Value -= damage * 0.7f;
                    targetMelee.Rage.Value += 1;
                    targetMelee.RenderBlock();
                }

                // Send messages for stats in NetworkPlayer script
                if (NetworkObject.IsPlayerObject) { SendMessage("OnDamageDealt", previousHP - targetMelee.HP.Value); }

                if (targetMelee.HP.Value <= 0 & previousHP > 0)
                {
                    // Death ailment
                    // MAKE SURE YOU CALL THIS BEFORE SENDING THE "OnDeath" MESSAGE IN ORDER TO MAKE SURE THAT THE GAME LOGIC MANAGER REGISTERS THE RESULT PROPERLY
                    targetMelee.Character.Die(melee.Character);

                    if (targetMelee.NetworkObject.IsPlayerObject) { targetMelee.SendMessage("OnDeath", melee); }
                    if (NetworkObject.IsPlayerObject) { SendMessage("OnKill", targetMelee); }
                }
                else // If we are not dead
                {
                    if (hitReaction != null) { hitReaction.PlayNetworked(targetMelee); }
                }

                // Add 1 to knocked up hit count if we are already knocked up
                if (targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp) { targetMelee.knockedUpHitCount.Value++; }

                // If we reach the knock up hit limit, perform a knockdown
                if (targetMelee.knockedUpHitCount.Value >= melee.KNOCK_UP_FOLLOWUP_LIMIT)
                {
                    targetMelee.knockedUpHitCount.Value = 0;
                    if (attack.attackType != AttackType.Knockdown)
                        targetMelee.Character.Knockdown(melee.Character, targetMelee.Character);
                    targetMelee.EventKnockedUpHitLimitReached.Invoke();
                }

                if (hitResult == HitResult.ReceiveDamage)
                {
                    // Making sure didDodgeCancelAilment is Reset everytime targetMelee is attacked
                    targetMelee.Character.didDodgeCancelAilment = false;
                    // Set Ailments here
                    switch (attack.attackType)
                    {
                        case AttackType.Stun:
                            targetMelee.Character.Stun(melee.Character, targetMelee.Character);
                            break;
                        case AttackType.Knockdown:
                            if (targetMelee.knockedUpHitCount.Value < melee.KNOCK_UP_FOLLOWUP_LIMIT)
                                targetMelee.Character.Knockdown(melee.Character, targetMelee.Character);
                            break;
                        case AttackType.Knockedup:
                            targetMelee.Character.Knockup(melee.Character, targetMelee.Character);
                            break;
                        case AttackType.Stagger:
                            targetMelee.Character.Stagger(melee.Character, targetMelee.Character);
                            break;
                        case AttackType.Followup:
                            if (targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp)
                            {
                                targetMelee.Character.Knockup(melee.Character, targetMelee.Character);
                            }
                            else
                            {
                                targetMelee.Character.Stagger(melee.Character, targetMelee.Character);
                            }
                            break;
                        case AttackType.None:
                        case AttackType.Pull:
                            if (targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned ||
                                targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered)
                            {
                                targetMelee.Character.CancelAilment(null);
                            }
                            break;
                    }
                }

                IgniterMeleeOnReceiveAttack[] triggers = targetMelee.GetComponentsInChildren<IgniterMeleeOnReceiveAttack>();

                bool hitSomething = triggers.Length > 0;
                if (hitSomething)
                {
                    for (int j = 0; j < triggers.Length; ++j)
                    {
                        triggers[j].OnReceiveAttack(melee, attack, hitResult);
                    }
                }

                if (hitSomething && attack != null && targetMelee != null)
                {
                    attack.ExecuteActionsOnHit(impactPosition, targetMelee.gameObject);
                }

                if (attack != null && attack.pushForce > float.Epsilon)
                {
                    Rigidbody[] rigidbodies = targetMelee.GetComponents<Rigidbody>();
                    for (int j = 0; j < rigidbodies.Length; ++j)
                    {
                        Vector3 direction = rigidbodies[j].transform.position - transform.position;
                        rigidbodies[j].AddForce(direction.normalized * attack.pushForce, ForceMode.Impulse);
                    }
                }

                hitResults.Add(hitResult);
            }
            return hitResults;
        }

        private void RenderHit()
        {
            if (!IsServer) { Debug.LogError("RenderHit() should only be called from the server"); return; }

            if (!IsClient)
                glowRenderer.RenderHit();


            RenderHitClientRpc();
        }

        [ClientRpc] private void RenderHitClientRpc() { glowRenderer.RenderHit(); }

        private void RenderBlock()
        {
            if (!IsServer) { Debug.LogError("RenderBlock() should only be called from the server"); return; }

            if (!IsClient)
                glowRenderer.RenderBlock();
            RenderBlockClientRpc();
        }

        [ClientRpc] private void RenderBlockClientRpc() { glowRenderer.RenderBlock(); }

        private void RenderUnInterruptable()
        {
            if (!IsServer) { Debug.LogError("RenderUninterruptable() should only be called from the server"); return; }

            if (!IsClient)
            {
                glowRenderer.RenderUninterruptable();
            }
            RenderUninterruptableClientRpc();
        }

        [ClientRpc] private void RenderUninterruptableClientRpc() { glowRenderer.RenderUninterruptable(); }

        public void PropogateMeleeClipChange(MeleeClip meleeClip)
        {
            if (!IsServer) { Debug.LogError("PropogateMeleeClipChange() should only be called from the server"); return; }

            PropogateMeleeClipChangeClientRpc(meleeClip.name);
        }

        private MeleeClip GetMeleeClipFromWeaponOrShieldByName(string clipName)
        {
            if (abilityManager)
            {
                foreach (Ability ablty in abilityManager.GetAbilityInstanceList())
                {
                    MeleeClip meleeClip = ablty.meleeClip;

                    if (meleeClip)
                    {
                        if (meleeClip.name == clipName) { return meleeClip; }
                    }
                }
            }

            IEnumerable<FieldInfo> propertyList = typeof(MeleeWeapon).GetFields();
            foreach (FieldInfo propertyInfo in propertyList)
            {
                if (propertyInfo.FieldType == typeof(MeleeClip))
                {
                    var meleeClipObject = propertyInfo.GetValue(currentWeapon);
                    MeleeClip meleeClip = (MeleeClip)meleeClipObject;

                    if (meleeClip)
                    {
                        if (meleeClip.name == clipName) { return meleeClip; }
                    }
                }
                else if (propertyInfo.FieldType == typeof(List<MeleeClip>))
                {
                    var meleeClipListObject = propertyInfo.GetValue(currentWeapon);
                    List<MeleeClip> meleeClipList = (List<MeleeClip>)meleeClipListObject;

                    foreach (MeleeClip meleeClip in meleeClipList)
                    {
                        if (meleeClip)
                        {
                            if (meleeClip.name == clipName) { return meleeClip; }
                        }
                    }
                }
                else if (propertyInfo.FieldType == typeof(List<Combo>))
                {
                    var comboListObject = propertyInfo.GetValue(currentWeapon);
                    List<Combo> comboList = (List<Combo>)comboListObject;

                    foreach (Combo combo in comboList)
                    {
                        MeleeClip meleeClip = combo.meleeClip;
                        if (meleeClip)
                        {
                            if (meleeClip.name == clipName) { return meleeClip; }
                        }
                    }
                }
                else if (propertyInfo.FieldType == typeof(Ability))
                {
                    var abilityObject = propertyInfo.GetValue(currentWeapon);
                    Ability ability = (Ability)abilityObject;

                    if (!ability) { continue; }

                    MeleeClip meleeClip = ability.meleeClip;
                    if (meleeClip)
                    {
                        if (meleeClip.name == clipName) { return meleeClip; }
                    }
                }
            }

            propertyList = typeof(MeleeShield).GetFields();

            foreach (FieldInfo propertyInfo in propertyList)
            {
                if (propertyInfo.FieldType == typeof(MeleeClip))
                {
                    var meleeClipObject = propertyInfo.GetValue(currentShield);
                    MeleeClip meleeClip = (MeleeClip)meleeClipObject;

                    if (meleeClip)
                    {
                        if (meleeClip.name == clipName) { return meleeClip; }
                    }
                }
                else if (propertyInfo.FieldType == typeof(List<MeleeClip>))
                {
                    var meleeClipListObject = propertyInfo.GetValue(currentShield);
                    List<MeleeClip> meleeClipList = (List<MeleeClip>)meleeClipListObject;

                    foreach (MeleeClip meleeClip in meleeClipList)
                    {
                        if (meleeClip)
                        {
                            if (meleeClip.name == clipName) { return meleeClip; }
                        }
                    }
                }
                else if (propertyInfo.FieldType == typeof(List<Combo>))
                {
                    var comboListObject = propertyInfo.GetValue(currentShield);
                    List<Combo> comboList = (List<Combo>)comboListObject;

                    foreach (Combo combo in comboList)
                    {
                        MeleeClip meleeClip = combo.meleeClip;
                        if (meleeClip)
                        {
                            if (meleeClip.name == clipName) { return meleeClip; }
                        }
                    }
                }
                else if (propertyInfo.FieldType == typeof(Ability))
                {
                    var abilityObject = propertyInfo.GetValue(currentShield);
                    Ability ability = (Ability)abilityObject;

                    if (!ability) { continue; }

                    MeleeClip meleeClip = ability.meleeClip;
                    if (meleeClip)
                    {
                        if (meleeClip.name == clipName) { return meleeClip; }
                    }
                }
            }

            Debug.LogError("Melee clip Not Found: " + clipName);
            return null;
        }

        [ClientRpc]
        private void PropogateMeleeClipChangeClientRpc(string clipName)
        {
            MeleeClip clip = GetMeleeClipFromWeaponOrShieldByName(clipName);
            if (clip)
            {
                clip.PlayLocally(this);
                if (clip.isAttack)
                {
                    currentMeleeClip = clip;
                    comboSystem.SetStateFromClip(clip);
                }
            }
            else
            {
                Debug.LogError("Clip Not Found: " + clipName);
            }
        }

        protected void UpdatePoise()
        {
            this.poiseDelayCooldown = Mathf.Max(0f, poiseDelayCooldown - Time.deltaTime);
            if (this.poiseDelayCooldown > float.Epsilon) return;

            this.Poise.Value += this.poiseRecoveryRate.GetValue(gameObject) * Time.deltaTime;
            this.Poise.Value = Mathf.Min(this.Poise.Value, this.maxPoise.GetValue(gameObject));
        }

        protected void UpdateRage()
        {
            this.poiseDelayCooldown = Mathf.Max(0f, poiseDelayCooldown - Time.deltaTime);
            if (this.poiseDelayCooldown > float.Epsilon) return;

            this.Rage.Value += this.rageRecoveryRate.GetValue(gameObject) * Time.deltaTime;
            this.Rage.Value = Mathf.Min(this.Rage.Value, this.maxRage.GetValue(gameObject));
        }

        protected void UpdateDefense()
        {
            if (IsBlocking.Value) return;
            if (!currentShield) return;

            defenseDelayCooldown = Mathf.Max(0f, defenseDelayCooldown - Time.deltaTime);
            if (defenseDelayCooldown > float.Epsilon) return;

            Defense.Value += currentShield.defenseRecoveryRate.GetValue(gameObject) * Time.deltaTime * defenseIncreaseMultiplier.Value;
            Defense.Value = Mathf.Min(Defense.Value, currentShield.maxDefense.GetValue(gameObject));
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public IEnumerator Sheathe()
        {
            if (this.Character.characterLocomotion.isBusy) yield break;
            if (!this.CanAttack()) yield break;
            if (this.IsAttacking) yield break;

            this.ReleaseTargetFocus();

            WaitForSeconds wait = new WaitForSeconds(0f);
            if (this.currentWeapon != null)
            {
                if (this.currentWeapon.characterState != null)
                {
                    CharacterState currentState = this.CharacterAnimator.GetState(MeleeWeapon.LAYER_STANCE);
                    if (currentState != null)
                    {
                        float time = this.ResetState(currentState, MeleeWeapon.LAYER_STANCE);
                        wait = new WaitForSeconds(time);
                    }
                }

                this.PlayAudio(this.currentWeapon.audioSheathe);
            }

            this.Character.characterLocomotion.isBusy = true;
            this.IsSheathing = true;

            yield return wait;

            if (this.EventSheatheWeapon != null) this.EventSheatheWeapon.Invoke(this.currentWeapon);
            // if (this.modelWeapon != null) Destroy(this.modelWeapon);
            if (this.modelWeapons != null) foreach (var model in modelWeapons) Destroy(model);
            if (this.modelShield != null) Destroy(this.modelShield);

            this.OnSheatheWeapon();

            yield return wait;

            this.IsSheathing = false;

            this.previousWeapon = this.currentWeapon;
            this.previousShield = this.currentShield;

            this.currentWeapon = null;
            this.currentShield = null;

            this.comboSystem = null;

            this.Character.characterLocomotion.isBusy = false;
        }

        public void DrawWeapon() => StartCoroutine(Draw(this.currentWeapon, currentShield));

        public IEnumerator Draw(MeleeWeapon weapon, MeleeShield shield = null)
        {
            if (this.Character.characterLocomotion.isBusy) yield break;
            if (this.IsAttacking) yield break;
            if (!this.CanAttack()) yield break;

            yield return this.Sheathe();

            if (weapon != null)
            {
                this.currentWeapon = weapon;
                this.EquipShield(shield != null ? shield : weapon.defaultShield);

                this.comboSystem = new ComboSystem(this, weapon.combos);

                WaitForSeconds wait = new WaitForSeconds(0f);

                if (this.currentWeapon.characterState != null)
                {
                    CharacterState state = this.currentWeapon.characterState;
                    float time = this.ChangeState(
                        this.currentWeapon.characterState,
                        this.currentWeapon.characterMask,
                        MeleeWeapon.LAYER_STANCE,
                        null
                    );

                    if (state.enterClip != null) wait = new WaitForSeconds(time);
                }

                this.PlayAudio(this.currentWeapon.audioDraw);

                this.Character.characterLocomotion.isBusy = true;
                this.IsDrawing = true;

                yield return wait;

                if (this.EventDrawWeapon != null) this.EventDrawWeapon.Invoke(this.currentWeapon);

                this.modelWeapons = this.currentWeapon.EquipNewWeapon(this.CharacterAnimator);
                this.Blades = new List<BladeComponent>();
                foreach (var model in modelWeapons)
                {
                    var blade = model.GetComponent<BladeComponent>();
                    Blades.Add(blade);
                    if (blade != null) blade.Setup(this);
                }

                // this.modelWeapon = this.currentWeapon.EquipWeapon(this.CharacterAnimator);
                // this.Blade = this.modelWeapon.GetComponentInChildren<BladeComponent>();
                // if (this.Blade != null) this.Blade.Setup(this);

                this.OnDrawWeapon();

                yield return wait;

                this.IsDrawing = false;
                this.Character.characterLocomotion.isBusy = false;
            }
        }

        public void EquipShield(MeleeShield shield)
        {
            if (shield == null) return;

            if (this.modelShield != null) Destroy(this.modelShield);

            this.modelShield = shield.EquipShield(this.CharacterAnimator);
            this.currentShield = shield;
        }

        public Ability GetActivatedAbility()
        {
            return abilityManager.GetActivatedAbility();
        }

        public float maxHealth = 100.0f;
        private NetworkVariable<float> HP = new NetworkVariable<float>();
        private NetworkVariable<float> Rage = new NetworkVariable<float>();

        public float GetHP() { return HP.Value; }

        public float GetRage() { return Rage.Value; }

        public float GetDefense() { return Defense.Value; }

        public float GetPoise() { return Poise.Value; }

        public void ResetHP()
        {
            if (!IsServer) { Debug.LogError("ResetHP() should only be called on the server"); return; }
            HP.Value = maxHealth;
        }

        public void ResetDefense()
        {
            if (!IsServer) { Debug.LogError("ResetDefense() should only be called on the server"); return; }
            Defense.Value = 0;
        }

        public void ResetPoise()
        {
            if (!IsServer) { Debug.LogError("ResetPoise() should only be called on the server"); return; }
            Poise.Value = 0;
        }

        public void AddHP(float value)
        {
            if (HP.Value + value > maxHealth)
            {
                HP.Value = maxHealth;
            }
            else
            {
                HP.Value += value;
            }
        }

        public void SetHP(float value)
        {
            HP.Value = value;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                HP.Value = maxHealth;
                overrideRunSpeed.Value = originalRunSpeed;
            }

            IsBlocking.OnValueChanged += OnIsBlockingChange;
            HP.OnValueChanged += OnHPChanged;
            rooted.OnValueChanged += OnRootedChange;
        }

        public override void OnNetworkDespawn()
        {
            IsBlocking.OnValueChanged -= OnIsBlockingChange;
            HP.OnValueChanged -= OnHPChanged;
            rooted.OnValueChanged -= OnRootedChange;
        }

        private void OnHPChanged(float prev, float current)
        {
            // DO NOT REMOVE FOR DEBUGGING
            // Debug.Log(this.gameObject.name + " HP CHANGE Prev: " + prev);
            // Debug.Log(this.gameObject.name + " HP CHANGE Curr: " + current);

            if (current < prev)
            {
                // Render hit is handled in LateUpdate() on server now
            }
            else if (current > prev)
            {
                glowRenderer.RenderHeal();
            }

            // Cancel death ailment if we get our health back
            if (IsServer)
            {
                if (prev <= 0 & current > 0)
                {
                    Character.CancelDeath();
                }
            }
        }

        void OnIsBlockingChange(bool prev, bool current)
        {
            CharacterAnimator.animator.SetBool("Blocking", current);
        }

        [ServerRpc]
        public void StartBlockingServerRpc()
        {
            if (this.Character.characterLocomotion.isBusy & !rooted.Value) return;

            if (this.IsDrawing) return;
            if (this.IsSheathing) return;
            if (this.IsStaggered) return;
            if (this.IsAttacking) return;

            if (this.currentShield == null) return;

            IsBlocking.Value = true;
        }

        [ServerRpc]
        public void StopBlockingServerRpc()
        {
            IsBlocking.Value = false;
        }

        public virtual void Execute(ActionKey actionKey)
        {
            if (!this.currentWeapon) return;
            if (!this.CanAttack()) return;

            if (IsOwner) this.StopBlockingServerRpc();
            this.inputBuffer.AddInput(actionKey);
        }

        public virtual void ExecuteAbility(ActionKey actionKey)
        {
            if (!this.currentWeapon) return;
            if (!this.CanAttack()) return;

            this.IsCastingAbility.Value = true;
            if (IsOwner) this.StopBlockingServerRpc();
            this.inputBuffer.AddInput(actionKey);
        }

        public void RevertAbilityCastingStatus()
        {
            if (!IsServer) { Debug.LogError("CharacterMelee.RevertAbilityCastingStatus() should only be called on the server"); return; }

            if (IsCastingAbility.Value)
            {
                IsCastingAbility.Value = false;
            }
        }

        public void StopAttack()
        {
            if (this != null && this.currentMeleeClip != null && this.currentMeleeClip.isAttack == true)
            {
                this.IsCastingAbility.Value = false;
                if (this.inputBuffer.HasInput())
                {
                    this.inputBuffer.ConsumeInput();
                }

                this.comboSystem.Stop();
                this.currentMeleeClip.Stop(this);

                if (this.Blades.Count > 0)
                {
                    foreach (var blade in this.Blades)
                    {
                        blade.EventAttackRecovery.Invoke();
                    }
                }
            }
        }

        public int GetCurrentPhase()
        {
            if (this.comboSystem == null) return -1;
            if (this.IsStaggered)
            {
                return -1;
            }

            int phase = this.currentMeleeClip != null ? this.comboSystem.GetCurrentPhase(this.currentMeleeClip) : -1;

            if (IsOwner && adventureMotor != null)
            {
                if (phase == 1 && this.currentMeleeClip.isOrbitLocked) { adventureMotor.allowOrbitInput = false; }
                if (IsUninterruptable && phase == 2) { SetUninterruptable(0f); }
                if (phase == 2 || phase <= 0) { adventureMotor.allowOrbitInput = true; }
            }

            return phase;
        }

        public void PlayAudio(AudioClip audioClip)
        {
            if (audioClip == null) return;

            Vector3 position = transform.position;
            if (this.Blade != null) position = this.Blade.transform.position;

            float pitch = UnityEngine.Random.Range(MIN_RAND_PITCH, MAX_RAND_PITCH);
            AudioMixerGroup soundMixer = DatabaseGeneral.Load().soundAudioMixer;

            AudioManager.Instance.PlaySound3D(
                audioClip, 0f, position, 1f, pitch,
                1.0f, soundMixer
            );
        }

        public void PlayAudioNoPitchModifier(AudioClip audioClip)
        {
            if (audioClip == null) return;

            Vector3 position = transform.position;
            if (this.Blade != null) position = this.Blade.transform.position;

            float pitch = 1.0f;
            AudioMixerGroup soundMixer = DatabaseGeneral.Load().soundAudioMixer;

            AudioManager.Instance.PlaySound3D(
                audioClip, 0f, position, 1f, pitch,
                1.0f, soundMixer
            );
        }

        public void SetPosture(Posture posture, float duration)
        {
            if (!this.IsStaggered && posture == Posture.Stagger)
            {
                this.comboSystem.Stop();
                if (EventStagger != null) EventStagger.Invoke(duration);
            }

            this.isStaggered = posture == Posture.Stagger;
            this.staggerEndtime = GetTime() + duration;
        }

        public void SetInvincibility(float duration)
        {
            this.isInvincible = true;
            this.invincibilityEndTime = GetTime() + duration;
        }

        public void SetUninterruptable(float duration)
        {
            this.isUninterruptable = true;
            this.uninterruptableEndTime = GetTime() + duration;
        }

        public void SetPoise(float value)
        {
            this.poiseDelayCooldown = this.delayPoise.GetValue(gameObject);
            this.Poise.Value = Mathf.Clamp(value, 0f, this.maxPoise.GetValue(gameObject));
        }

        public void SetRage(float value)
        {
            this.Rage.Value = Mathf.Clamp(value, 0f, this.maxRage.GetValue(gameObject));
        }

        public void AddPoise(float value)
        {
            SetPoise(Poise.Value + value);
        }

        public void AddRage(float value)
        {
            SetRage(Rage.Value + value);
        }

        public void SetDefense(float value)
        {
            if (!currentShield) return;

            defenseDelayCooldown = this.currentShield.delayDefense.GetValue(gameObject);
            Defense.Value = Mathf.Clamp(value, 0f, currentShield.maxDefense.GetValue(gameObject));
        }

        public void AddDefense(float value)
        {
            SetDefense(Defense.Value + value);
        }

        public void SetTargetFocus(TargetMelee target)
        {
            if (target == null) return;

            var direction = CharacterLocomotion.OVERRIDE_FACE_DIRECTION.Target;
            var position = new TargetPosition(TargetPosition.Target.Transform)
            {
                targetTransform = target.transform
            };

            this.Character.characterLocomotion.overrideFaceDirection = direction;
            this.Character.characterLocomotion.overrideFaceDirectionTarget = position;

            target.SetTracker(this);

            this.HasFocusTarget = true;
            if (this.EventFocus != null) this.EventFocus.Invoke(true);
        }

        public void ReleaseTargetFocus()
        {
            if (!this.HasFocusTarget) return;

            var direction = CharacterLocomotion.OVERRIDE_FACE_DIRECTION.None;
            this.Character.characterLocomotion.overrideFaceDirection = direction;

            this.HasFocusTarget = false;
            if (this.EventFocus != null) this.EventFocus.Invoke(false);
        }

        // VIRTUAL METHODS: -----------------------------------------------------------------------

        protected virtual void OnSheatheWeapon()
        { }

        protected virtual void OnDrawWeapon()
        { }

        // CALLBACK METHODS: ----------------------------------------------------------------------

        public void OnLightAttack()
        {
            if (!IsServer) { Debug.LogError("OnLightAttack() should only be called on the server."); return; }
        }

        public void OnHeavyAttack()
        {
            if (!IsServer) { Debug.LogError("OnHeavyAttack() should only be called on the server."); return; }

            AddPoise(-15);
        }

        public void OnDodge()
        {
            if (!IsServer) { Debug.LogError("OnDodge() should only be called on the server."); return; }

            AddPoise(-10);
        }

        private KeyValuePair<HitResult, MeleeClip> OnReceiveAttack(CharacterMelee attacker, MeleeClip attack, Vector3 bladeImpactPosition)
        {
            if (!IsServer) { Debug.LogError("OnReceiveAttack() should only be called on the server."); return new KeyValuePair<HitResult, MeleeClip>(HitResult.Ignore, null); }

            Character assailant = attacker.Character;
            CharacterMelee melee = this.Character.GetComponent<CharacterMelee>();

            // Please comment out instead of deleting this block
            #region Debug Results
            //print("=============");
            //print("name: " + melee.name);
            //print("characterAilment: " + melee.Character.characterAilment);
            //print("IsUninterruptable: " + melee.IsUninterruptable);
            //print("IsInvincible: " + melee.IsInvincible);
            //print("IsAttacking: " + melee.IsAttacking);
            //print("IsCastingAbility: " + melee.IsCastingAbility);
            //print ("IsDashing: " + melee.Character.isCharacterDashing());
            #endregion


            // Making sure didDodgeCancelAilment is Reset everytime targetMelee is attacked
            assailant.didDodgeCancelAilment = false;

            OnReceiveAttackClientRpc(assailant.NetworkObjectId, bladeImpactPosition, attack.name);

            this.ReleaseTargetFocus();

            MeleeClip hitReaction = null;

            if (this.currentWeapon == null) return new KeyValuePair<HitResult, MeleeClip>(HitResult.ReceiveDamage, hitReaction);
            // if (this.GetHP() <= 0) return new KeyValuePair<HitResult, MeleeClip>(HitResult.Ignore, hitReaction);
            if (this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.WasGrabbed) return new KeyValuePair<HitResult, MeleeClip>(HitResult.ReceiveDamage, hitReaction);
            if (this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown) return new KeyValuePair<HitResult, MeleeClip>(HitResult.ReceiveDamage, hitReaction);
            if (this.IsInvincible) return new KeyValuePair<HitResult, MeleeClip>(HitResult.Ignore, hitReaction);
            // Uninterruptable Abilities will not be cancelled
            if (melee.IsCastingAbility.Value && IsUninterruptable) { return new KeyValuePair<HitResult, MeleeClip>(HitResult.ReceiveDamage, hitReaction); }
            // Uninterruptable Heavy Attacks will not be cancelled
            if (melee.IsAttacking && melee.currentMeleeClip.isHeavy && IsUninterruptable) { return new KeyValuePair<HitResult, MeleeClip>(HitResult.ReceiveDamage, hitReaction); }

            // Prioritize damage taken over attack and non-invincible dodge frames
            melee.RevertAbilityCastingStatus();
            melee.SetUninterruptable(0f);
            melee.StopAttack();
            if (melee.adventureMotor != null) { melee.adventureMotor.allowOrbitInput = true; }
            CharacterAnimator.StopGesture(0.10f);

            // Remove this functionality for now as it has unpredictable results
            // if (melee.Character.isCharacterDashing()) { melee.Character.Stagger(attacker.Character, melee.Character); }
            this.isStaggered = true;

            float attackVectorAngle = Vector3.SignedAngle(transform.forward, attacker.transform.position - this.transform.position, Vector3.up);

            MeleeWeapon.HitLocation hitLocation = this.GetHitLocation(attackVectorAngle);

            #region Attack and Defense handlers

            float attackAngle = attackVectorAngle;

            float defenseAngle = this.currentShield != null
                ? this.currentShield.defenseAngle.GetValue(gameObject)
                : 0f;

            if (this.currentShield != null &&
                attack.isBlockable && IsBlocking.Value &&
                hitLocation == MeleeWeapon.HitLocation.FrontMiddle)
            {
                this.AddDefense(-attack.defenseDamage);
                if (this.Defense.Value > 0)
                {
                    if (GetTime() < this.startBlockingTime + this.currentShield.perfectBlockWindow)
                    {
                        if (attacker != null)
                        {
                            MeleeClip attackerReaction = this.Character.IsGrounded()
                                ? this.currentShield.groundPerfectBlockReaction
                                : this.currentShield.airbornPerfectBlockReaction;

                            attackerReaction.PlayNetworked(attacker);
                        }

                        if (currentShield.perfectBlockClip != null) { hitReaction = currentShield.perfectBlockClip; }

                        ExecuteEffects(
                            Blade.GetImpactPosition(),
                            currentShield.audioPerfectBlock,
                            currentShield.prefabImpactPerfectBlock
                        );

                        comboSystem.OnPerfectBlock();
                        return new KeyValuePair<HitResult, MeleeClip>(HitResult.PerfectBlock, hitReaction);
                    }

                    hitReaction = currentShield.GetBlockReaction();

                    ExecuteEffects(
                        bladeImpactPosition,
                        currentShield.audioBlock,
                        currentShield.prefabImpactBlock
                    );

                    comboSystem.OnBlock();
                    return new KeyValuePair<HitResult, MeleeClip>(HitResult.AttackBlock, hitReaction);
                }
                else if (Poise.Value >= 30)
                {
                    hitReaction = currentShield.GetBlockReaction();

                    ExecuteEffects(
                        bladeImpactPosition,
                        currentShield.audioBlock,
                        currentShield.prefabImpactBlock
                    );

                    comboSystem.OnBlock();

                    Defense.Value = 0f;

                    AddPoise(-30);
                    return new KeyValuePair<HitResult, MeleeClip>(HitResult.PoiseBlock, hitReaction);
                }
                else
                {
                    Defense.Value = 0f;
                    Poise.Value = 0f;
                    IsBlocking.Value = false;
                }
            }
            else // If we are not blocking, or if attack is not blockable, or we are hit by an attack outside of the defense angle
            {
                IsBlocking.Value = false;
            }
            #endregion

            // this.AddPoise(-attack.poiseDamage);



            bool isKnockback = attack.attackType == AttackType.Knockdown | Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown;
            bool isKnockup = attack.attackType == AttackType.Knockedup | Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp;
            bool isPulled = attack.attackType == AttackType.Pull;

            hitReaction = currentWeapon.GetHitReaction(
                Character.IsGrounded(),
                hitLocation,
                isKnockback,
                isKnockup,
                isPulled
            );

            ExecuteEffects(
                bladeImpactPosition,
                isKnockback
                    ? attacker.currentWeapon.audioImpactKnockback
                    : attacker.currentWeapon.audioImpactNormal,
                isKnockback
                    ? attacker.currentWeapon.prefabImpactKnockback
                    : attacker.currentWeapon.prefabImpactNormal
            );

            attack.ExecuteHitPause();

            // Play Reaction Clip only if the attackType is not an Ailment
            bool shouldPlayHitReaction = (IsUninterruptable && !IsCastingAbility.Value) || (!IsUninterruptable && attack.attackType == AttackType.None) || (!IsUninterruptable && attack.attackType == AttackType.Pull) || (attack.attackType == AttackType.Followup && !isKnockup);
            if (!shouldPlayHitReaction)
            {
                hitReaction = null;
            }

            return new KeyValuePair<HitResult, MeleeClip>(HitResult.ReceiveDamage, hitReaction);
        }

        [ClientRpc]
        void OnReceiveAttackClientRpc(ulong attackerNetObjId, Vector3 bladeImpactPosition, string meleeClipName)
        {
            CharacterMelee attacker = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<CharacterMelee>();
            MeleeClip attack = attacker.GetMeleeClipFromWeaponOrShieldByName(meleeClipName);

            float attackVectorAngle = Vector3.SignedAngle(transform.forward, attacker.transform.position - this.transform.position, Vector3.up);

            MeleeWeapon.HitLocation hitLocation = GetHitLocation(attackVectorAngle);

            #region Attack and Defense VFX

            float attackAngle = Vector3.Angle(
                 attacker.transform.TransformDirection(Vector3.forward),
                 this.transform.TransformDirection(Vector3.forward)
             );

            float defenseAngle = this.currentShield != null
                ? this.currentShield.defenseAngle.GetValue(gameObject)
                : 0f;

            if (currentShield != null &&
                attack.isBlockable && IsBlocking.Value &&
                hitLocation == MeleeWeapon.HitLocation.FrontMiddle)
            {
                if (this.Defense.Value > 0)
                {
                    if (GetTime() < this.startBlockingTime + this.currentShield.perfectBlockWindow)
                    {
                        this.ExecuteEffects(
                            this.Blade.GetImpactPosition(),
                            this.currentShield.audioPerfectBlock,
                            this.currentShield.prefabImpactPerfectBlock
                        );
                        return;
                    }

                    this.ExecuteEffects(
                        bladeImpactPosition,
                        this.currentShield.audioBlock,
                        this.currentShield.prefabImpactBlock
                    );
                    return;
                }
                else if (Poise.Value >= 30)
                {
                    this.ExecuteEffects(
                        bladeImpactPosition,
                        this.currentShield.audioBlock,
                        this.currentShield.prefabImpactBlock
                    );
                    return;
                }
            }
            #endregion

            // MeleeWeapon.HitLocation hitLocation = this.GetHitLocation(attackVectorAngle);
            bool isKnockback = attack.attackType == AttackType.Knockdown | this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown;
            bool isKnockup = attack.attackType == AttackType.Knockedup | this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp;
            bool isPulled = attack.attackType == AttackType.Pull;

            // MeleeClip hitReaction = this.currentWeapon.GetHitReaction(
            //     this.Character.IsGrounded(),
            //     hitLocation,
            //     isKnockback,
            //     isKnockup
            // );

            this.ExecuteEffects(
                bladeImpactPosition,
                isKnockback
                    ? attacker.currentWeapon.audioImpactKnockback
                    : attacker.currentWeapon.audioImpactNormal,
                isKnockback
                    ? attacker.currentWeapon.prefabImpactKnockback
                    : attacker.currentWeapon.prefabImpactNormal
            );

            // attack.ExecuteHitPause();
            // // Play Reaction Clip only if the attackType is not an Ailment
            // if ((IsUninterruptable && !IsCastingAbility)  || (!this.IsUninterruptable && attack.attackType == AttackType.None) || (attack.attackType == AttackType.Followup && !isKnockup))
            // {
            //     hitReaction.PlayNetworked(this);
            // }
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        protected virtual float GetTime()
        {
            return Time.time;
        }

        public void ExecuteSwingAudio()
        {
            if (currentWeapon.audioSwing) { PlayAudio(currentWeapon.audioSwing); }
        }

        public void ExecuteVoiceOver(AudioClip audio)
        {
            if (audio) { PlayAudioNoPitchModifier(audio); }
        }

        private void ExecuteEffects(Vector3 position, AudioClip audio, GameObject prefab)
        {
            this.PlayAudio(audio);

            if (prefab != null)
            {
                GameObject impact = PoolManager.Instance.Pick(prefab);
                impact.transform.position = position;
            }

            if (currentWeapon.prefabImpactHit != null)
            {
                GameObject impact = PoolManager.Instance.Pick(currentWeapon.prefabImpactHit);
                impact.transform.position = position;
            }
        }

        protected bool CanAttack()
        {
            if (this.IsSheathing) return false;
            if (this.IsDrawing) return false;
            if (this.IsStaggered) return false;

            return true;
        }

        protected float ResetState(CharacterState state, CharacterAnimation.Layer layer)
        {
            float time = TRANSITION;
            if (state != null)
            {
                if (state.exitClip != null)
                {
                    time = state.exitClip.length;
                }

                time = Mathf.Max(TRANSITION, time) * 0.5f;
                this.CharacterAnimator.ResetState(time, layer);
            }

            return time;
        }

        public ComboSystem GetComboSystem()
        {
            return comboSystem;
        }

        public float ChangeState(CharacterState state, AvatarMask mask, CharacterAnimation.Layer layer, CharacterAnimator animator)
        {
            float time = 0f;
            if (state != null)
            {
                if (state.enterClip != null)
                {
                    time = state.enterClip.length;
                }

                time = Mathf.Max(0f, time) * 0.5f;
                this.CharacterAnimator.SetState(state, mask, 1f, time, 1f, layer);

                // if (animator != null)
                // {
                //     animator.ResetControllerTopology(state.GetRuntimeAnimatorController(), true);
                // }
            }

            return time;
        }

        private MeleeWeapon.HitLocation GetHitLocation(float attackVectorAngle)
        {
            MeleeWeapon.HitLocation hitLocation;

            if (attackVectorAngle <= 45.00f && attackVectorAngle >= -45.00f)
            {
                hitLocation = MeleeWeapon.HitLocation.FrontMiddle;
            }
            else if (attackVectorAngle > 45.00f && attackVectorAngle < 135.00f)
            {
                hitLocation = MeleeWeapon.HitLocation.RightMiddle;
            }
            else if (attackVectorAngle < -45.00f && attackVectorAngle > -135.00f)
            {
                hitLocation = MeleeWeapon.HitLocation.LeftMiddle;
            }
            else
            {
                hitLocation = MeleeWeapon.HitLocation.BackMiddle;
            }

            return hitLocation;
        }

        private Boolean IsTargetInFront(float attackVectorAngle)
        {
            // checked if within 20 deg
            if (attackVectorAngle <= 10.00f && attackVectorAngle >= -10.00f)
            {
                return true;
            }

            return false;
        }

        public bool Grab(CharacterMelee targetCharacter)
        {
            CharacterMelee executorCharacter = this;

            MeleeWeapon executorWeapon = this.currentWeapon;

            if (targetCharacter == null || executorCharacter == null) return false;

            this.anim_ExecuterDuration = (executorWeapon.grabAttack.animationClip.length);
            this.anim_ExecutedDuration = (executorWeapon.grabReaction.animationClip.length);

            executorCharacter.IsGrabbing = true;
            targetCharacter.IsGrabbed = true;

            // Cancel any Melee input
            targetCharacter.StopAttack();
            executorCharacter.StopAttack();

            // Make Character Invincible
            targetCharacter.SetInvincibility(9999999999f);

            // Set posture to stagger to prevent melee from doing any execution
            executorCharacter.SetPosture(Posture.Stagger, anim_ExecutedDuration);
            targetCharacter.SetPosture(Posture.Stagger, anim_ExecutedDuration);

            executorWeapon.grabAttack.PlayNetworked(executorCharacter);
            executorWeapon.grabReaction.PlayNetworked(targetCharacter);

            CoroutinesManager.Instance.StartCoroutine(this.PostGrabRoutine(executorCharacter, targetCharacter));

            return true;
        }

        public IEnumerator PostGrabRoutine(CharacterMelee executorCharacter, CharacterMelee targetCharacter)
        {
            float initTime = Time.time;

            while (initTime + this.anim_ExecutedDuration >= Time.time)
            {
                executorCharacter.IsGrabbing = true;
                targetCharacter.IsGrabbed = true;

                yield return null;
            }

            executorCharacter.SetInvincibility(2.0f);

            this.anim_ExecuterDuration = 0.00f;
            this.anim_ExecutedDuration = 0.00f;

            yield return 0;
        }

        // Target Placement Compensation
        private void FocusTarget(MeleeClip meleeClip)
        {
            MeleeClip clip = meleeClip;

            Vector3 originalBoxCastHalfExtents = new Vector3(2.0f, 1.0f, 2.0f);

            this.boxCastHalfExtents = clip.isModifyFocus ? clip.boxCastHalfExtents : originalBoxCastHalfExtents;

            // Visualize boxcast
            Vector3 origin = transform.position;
            Quaternion orientation = characterCamera != null ? characterCamera.rotation : new Quaternion(0, 0, 0, 0);
            Vector3 direction = characterCamera != null ? characterCamera.forward : new Vector3(0, 0, 0);

            if (orientation != null && direction != null)
            {
                Debug.DrawRay(transform.position, direction * boxCastDistance, Color.green);
            }

            // Get all hits in boxcast
            RaycastHit[] allHits = Physics.BoxCastAll(origin, this.boxCastHalfExtents, direction * boxCastDistance, orientation, boxCastDistance);
            ExtDebug.DrawBoxCastOnHit(origin, this.boxCastHalfExtents, orientation, direction * boxCastDistance, boxCastDistance, Color.green, 1);
            // Sort hits by distance
            Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            // Iterate through box hits and find the first hit that has a CharacterMelee component
            Transform target = null;
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.root == transform) { continue; }
                if (hit.transform.root.TryGetComponent(out CharacterMelee melee))
                {
                    ExtDebug.DrawBoxCastOnHit(origin, this.boxCastHalfExtents, orientation, direction * hit.distance, hit.distance, Color.red, 1);
                    target = hit.transform.root;
                    break;
                }
            }

            if (!target) return;

            float attackVectorAngle = Vector3.SignedAngle(transform.forward, target.transform.position - transform.position, Vector3.up);

            if (IsTargetInFront(attackVectorAngle) == false) return;

            // If we have a target character, then look at them
            Vector3 relativePos = target.position - characterCamera.position;
            relativePos.y = 0;
            transform.rotation = Quaternion.LookRotation(relativePos);

            float distance = Vector3.Distance(transform.position, target.position);
            AnimationCurve clipMovementForward = clip.movementForward;

            if (!clip.isLunge && clip.isModifyFocus)
            {
                TargetMelee targetMelee = target.root.GetComponent<TargetMelee>();
                SetTargetFocus(targetMelee);
            }

            // If our distance from target is < boxCastHalfExtents.z, then reduce/increase movementForwardCurve
            if (distance <= boxCastHalfExtents.z && clip.isLunge)
            {
                TargetMelee targetMelee = target.root.GetComponent<TargetMelee>();
                SetTargetFocus(targetMelee);

                movementForward.keys = AdjustCurve(clipMovementForward, distance);
                isLunging = true;
            }
        }

        private Keyframe[] AdjustCurve(AnimationCurve curve, float newMaxDistance)
        {
            Keyframe[] keyframes = curve.keys;
            float prvMaxDistance = keyframes[keyframes.Length - 1].value;
            float sweetSpot = newMaxDistance > prvMaxDistance ? newMaxDistance * 0.60f : newMaxDistance * 0.75f;
            float pctDiffPrvNew = ComputePercentageDifference(prvMaxDistance, sweetSpot);


            for (int i = 0; i < keyframes.Length; i++)
            {
                Keyframe keyframe = keyframes[i];
                float keyVal = keyframe.value;

                if (pctDiffPrvNew < 0f)
                {
                    keyframe.value = Convert.ToSingle(keyVal) - (keyVal * Math.Abs(pctDiffPrvNew));
                }
                else if (pctDiffPrvNew > 0f)
                {
                    keyframe.value += Convert.ToSingle(keyVal) * pctDiffPrvNew;
                }

                keyframes[i] = keyframe;
            }

            return keyframes;
        }

        private float ComputePercentageDifference(float oldValue, float newValue)
        {
            // Compute the difference between the old and new values
            float difference = newValue - oldValue;

            return (difference / oldValue);
        }

        // CHARACTER STATUSES ===============================================================

        public NetworkVariable<float> damageMultiplier { get; private set; } = new NetworkVariable<float>(1);
        public void SetDamageMultiplier(float value, float duration)
        {
            if (!IsServer) { Debug.Log("CharacterMelee.SetDamageMultiplier() should only be called on the server."); return; }
            if (damageMultiplierCoroutine != null)
            {
                StopCoroutine(damageMultiplierCoroutine);
            }
            damageMultiplierCoroutine = StartCoroutine(SetDamageMultiplierCoroutine(value, duration));
        }

        private Coroutine damageMultiplierCoroutine;
        private IEnumerator SetDamageMultiplierCoroutine(float value, float duration)
        {
            damageMultiplier.Value = value;
            yield return new WaitForSeconds(duration);
            damageMultiplier.Value = 1;
        }



        public NetworkVariable<float> damageReductionMultiplier { get; private set; } = new NetworkVariable<float>(1);
        public void SetDamageReductionMultiplier(float value, float duration)
        {
            if (!IsServer) { Debug.Log("CharacterMelee.SetdamageReductionMultiplier() should only be called on the server."); return; }
            if (damageReductionMultiplierCoroutine != null)
            {
                StopCoroutine(damageReductionMultiplierCoroutine);
            }
            damageReductionMultiplierCoroutine = StartCoroutine(SetDamageReductionMultiplierCoroutine(value, duration));
        }

        private Coroutine damageReductionMultiplierCoroutine;
        private IEnumerator SetDamageReductionMultiplierCoroutine(float value, float duration)
        {
            damageReductionMultiplier.Value = value;
            yield return new WaitForSeconds(duration);
            damageReductionMultiplier.Value = 1;
        }



        public NetworkVariable<float> damageReceivedMultiplier { get; private set; } = new NetworkVariable<float>(1);
        public void SetDamageReceivedMultiplier(float value, float duration)
        {
            if (!IsServer) { Debug.Log("CharacterMelee.SetDamageReceivedMultiplier() should only be called on the server."); return; }
            if (damageReceivedMultiplierCoroutine != null)
            {
                StopCoroutine(damageReceivedMultiplierCoroutine);
            }
            damageReceivedMultiplierCoroutine = StartCoroutine(SetDamageReceivedMultiplierCoroutine(value, duration));
        }

        private Coroutine damageReceivedMultiplierCoroutine;
        private IEnumerator SetDamageReceivedMultiplierCoroutine(float value, float duration)
        {
            damageReceivedMultiplier.Value = value;
            yield return new WaitForSeconds(duration);
            damageReceivedMultiplier.Value = 1;
        }



        public NetworkVariable<float> healingMultiplier { get; private set; } = new NetworkVariable<float>(1);
        public void SetHealingMultiplier(float value, float duration)
        {
            if (!IsServer) { Debug.Log("CharacterMelee.SetHealingMultiplier() should only be called on the server."); return; }
            if (healingMultiplierCoroutine != null)
            {
                StopCoroutine(healingMultiplierCoroutine);
            }
            healingMultiplierCoroutine = StartCoroutine(SetHealingMultiplierCoroutine(value, duration));
        }

        private Coroutine healingMultiplierCoroutine;
        private IEnumerator SetHealingMultiplierCoroutine(float value, float duration)
        {
            healingMultiplier.Value = value;
            yield return new WaitForSeconds(duration);
            healingMultiplier.Value = 1;
        }



        public NetworkVariable<float> defenseIncreaseMultiplier { get; private set; } = new NetworkVariable<float>(1);
        public void SetDefenseIncreaseMultiplier(float value, float duration)
        {
            if (!IsServer) { Debug.Log("CharacterMelee.SetDefenseIncreaseMultiplier() should only be called on the server."); return; }
            if (defenseIncreaseMultiplierCoroutine != null)
            {
                StopCoroutine(defenseIncreaseMultiplierCoroutine);
            }
            defenseIncreaseMultiplierCoroutine = StartCoroutine(SetDefenseIncreaseMultiplierCoroutine(value, duration));
        }

        private Coroutine defenseIncreaseMultiplierCoroutine;
        private IEnumerator SetDefenseIncreaseMultiplierCoroutine(float value, float duration)
        {
            defenseIncreaseMultiplier.Value = value;
            yield return new WaitForSeconds(duration);
            defenseIncreaseMultiplier.Value = 1;
        }



        public NetworkVariable<float> defenseReductionMultiplier { get; private set; } = new NetworkVariable<float>(1);
        public void SetDefenseReductionMultiplier(float value, float duration)
        {
            if (!IsServer) { Debug.Log("CharacterMelee.SetDefenseReductionMultiplier() should only be called on the server."); return; }
            if (defenseReductionMultiplierCoroutine != null)
            {
                StopCoroutine(defenseReductionMultiplierCoroutine);
            }
            defenseReductionMultiplierCoroutine = StartCoroutine(SetDefenseReductionMultiplierCoroutine(value, duration));
        }

        private Coroutine defenseReductionMultiplierCoroutine;
        private IEnumerator SetDefenseReductionMultiplierCoroutine(float value, float duration)
        {
            defenseReductionMultiplier.Value = value;
            yield return new WaitForSeconds(duration);
            defenseReductionMultiplier.Value = 1;
        }

        public NetworkVariable<bool> drainActive { get; private set; } = new NetworkVariable<bool>();
        public void DrainHPOverTime(float value, float drainDuration, float delay)
        {
            if (!IsServer) { Debug.LogError("CharacterMelee.DrainHPOverTime() should only be called on the server."); return; }
            StartCoroutine(DrainHPCoroutine(value, drainDuration, delay));
        }

        private IEnumerator DrainHPCoroutine(float value, float drainDuration, float delay)
        {
            drainActive.Value = true;
            yield return new WaitForSeconds(delay);
            float elapsedTime = 0;

            while (elapsedTime < drainDuration)
            {
                float reductionAmount = GetHP() * value * Time.deltaTime;
                if (GetHP() - reductionAmount < 1)
                {
                    SetHP(1);
                }
                else
                {
                    AddHP(-1 * reductionAmount);
                }

                drainActive.Value = true;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            drainActive.Value = false;
        }

        public NetworkVariable<bool> healActive { get; private set; } = new NetworkVariable<bool>();
        public void HealHPOverTime(float value, float drainDuration, float delay)
        {
            if (!IsServer) { Debug.Log("CharacterMelee.HealHPOverTime() should only be called on the server."); return; }
            healActive.Value = true;
            StartCoroutine(HealHPCoroutine(value, drainDuration, delay));
        }

        private IEnumerator HealHPCoroutine(float value, float healDuration, float delay)
        {
            healActive.Value = true;
            yield return new WaitForSeconds(delay);
            float elapsedTime = 0;

            while (elapsedTime < healDuration)
            {
                float healAmount = maxHealth / GetHP() * value * Time.deltaTime * healingMultiplier.Value;
                if (GetHP() + healAmount > maxHealth)
                {
                    SetHP(maxHealth);
                }
                else
                {
                    AddHP(healAmount);
                }

                healActive.Value = true;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            healActive.Value = false;
        }

        public NetworkVariable<bool> slowed { get; private set; } = new NetworkVariable<bool>();
        private float slowEndTime;
        private NetworkVariable<float> slowAmount = new NetworkVariable<float>();
        public void SlowMovement(float value, float duration)
        {
            if (!IsServer) { Debug.LogError("CharacterMelee.SlowMovement() should only be called on the server"); return; }
            slowed.Value = true;
            slowEndTime = Time.time + duration;
            slowAmount.Value = value;
        }

        private NetworkVariable<float> overrideRunSpeed = new NetworkVariable<float>();
        public void SetRunSpeed(float value)
        {
            if (!IsServer) { Debug.LogError("CharacterMelee.SetRunSpeed() should only be called on the server"); return; }
            if (Time.time > slowEndTime)
            {
                overrideRunSpeed.Value = value;
            }
        }

        public void ResetRunSpeed()
        {
            if (!IsServer) { Debug.LogError("CharacterMelee.ResetRunSpeed() should only be called on the server"); return; }
            if (Time.time > slowEndTime)
            {
                overrideRunSpeed.Value = originalRunSpeed;
            }
        }

        public NetworkVariable<bool> rooted { get; private set; } = new NetworkVariable<bool>();
        public float rootEndTime { get; private set; }
        public void Root(float duration)
        {
            if (!IsServer) { Debug.LogError("CharacterMelee.Root() should only be called on the server"); return; }
            rooted.Value = true;
            rootEndTime = Time.time + duration;
            Character.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
        }

        private void OnRootedChange(bool prev, bool current)
        {
            if (IsServer & Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.None)
            {
                if (current)
                {
                    Character.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
                }
                else
                {
                    Character.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.CameraDirection, true);
                }
            }
        }

        public NetworkVariable<bool> silenced { get; private set; } = new NetworkVariable<bool>();
        public float silenceEndTime { get; private set; }
        public void Silence(float duration)
        {
            if (!IsServer) { Debug.LogError("CharacterMelee.Silence() should only be called on the server"); return; }
            silenced.Value = true;
            silenceEndTime = Time.time + duration;
        }

        public NetworkVariable<bool> fearing { get; private set; } = new NetworkVariable<bool>();
        public float fearEndTime { get; private set; }
        public void Fear(float duration)
        {
            if (!IsServer) { Debug.LogError("CharacterMelee.Fear() should only be called on the server"); return; }
            fearing.Value = true;
            fearEndTime = Time.time + duration;
        }

        private void OnDrawGizmos()
        {
            if (characterCamera)
            {
                Vector3 origin = transform.position;
                Quaternion orientation = characterCamera.rotation;
                Vector3 direction = characterCamera.forward;

                Gizmos.color = Color.yellow;
                Gizmos.matrix = Matrix4x4.TRS(origin, orientation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.forward * boxCastDistance, this.boxCastHalfExtents);
            }
        }
    }
}
