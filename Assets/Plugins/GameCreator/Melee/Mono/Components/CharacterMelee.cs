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

        private int KNOCK_UP_FOLLOWUP_LIMIT = 6;

        private const CharacterAnimation.Layer LAYER_DEFEND = CharacterAnimation.Layer.Layer3;

        private static readonly Vector3 PLANE = new Vector3(1, 0, 1);

        // PROPERTIES: ----------------------------------------------------------------------------

        public MeleeWeapon currentWeapon;
        public MeleeShield currentShield;
        
        public MeleeWeapon previousWeapon;
        public MeleeShield previousShield;
        
        protected ComboSystem comboSystem;
        protected InputBuffer inputBuffer;

        public NetworkVariable<float> Poise { get; private set; } = new NetworkVariable<float>();
        private float poiseDelayCooldown;

        public NumberProperty delayPoise = new NumberProperty(1f);
        public NumberProperty maxPoise = new NumberProperty(5f);
        public NumberProperty poiseRecoveryRate = new NumberProperty(1f);

        public NetworkVariable<float> Defense { get; protected set; } = new NetworkVariable<float>();
        private float defenseDelayCooldown;

        public bool IsDrawing { get; protected set; }
        public bool IsSheathing { get; protected set; }

        public bool IsAttacking { get; private set; }
        public bool IsBlocking { get; private set; }
        public bool HasFocusTarget { get; private set; }


        public bool IsGrabbing { get; private set; }
        public bool IsGrabbed { get; private set; }

        public bool IsStaggered => this.isStaggered && GetTime() <= this.staggerEndtime;
        public bool IsInvincible => this.isInvincible && GetTime() <= this.invincibilityEndTime;
        public bool IsUninterruptable => this.isUninterruptable && GetTime() <= this.uninterruptableEndTime;

        public Action<MeleeWeapon> EventDrawWeapon;
        public Action<MeleeWeapon> EventSheatheWeapon;
        public event Action<MeleeClip> EventAttack;
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

        // ACCESSORS: -----------------------------------------------------------------------------

        public Character Character { get; protected set; }
        public CharacterAnimator CharacterAnimator { get; protected set; }
        public BladeComponent Blade { get; protected set; }

        public List<BladeComponent> Blades { get; protected set; }

        // INITIALIZERS: --------------------------------------------------------------------------

        protected virtual void Awake()
        {
            this.Character = GetComponent<Character>();
            this.CharacterAnimator = GetComponent<CharacterAnimator>();
            this.inputBuffer = new InputBuffer(INPUT_BUFFER_TIME);
        }
        
        // UPDATE: --------------------------------------------------------------------------------

        protected virtual void Update()
        {
            if (IsServer)
            {
                this.UpdatePoise();
                this.UpdateDefense();
            }

            if (this.comboSystem != null)
            {
                this.comboSystem.Update();

                if (this.CanAttack() && this.inputBuffer.HasInput())
                {
                    ActionKey key = this.inputBuffer.GetInput();
                    MeleeClip meleeClip = this.comboSystem.Select(key);

                    if (meleeClip)
                    {
                        this.inputBuffer.ConsumeInput();
                        bool checkDash = this.Character.characterLocomotion.isDashing;

                        if (IsServer)
                        {
                            if (key == ActionKey.A) // Light Attack
                            {
                                OnLightAttack();
                            }
                            else if (key == ActionKey.B) // Heavy Attack
                            {
                                if (Poise.Value <= 20) { return; }
                                OnHeavyAttack();
                            }
                        }
                        
                        this.currentMeleeClip = meleeClip;
                        this.targetsEvaluated = new HashSet<int>();

                        this.Blades.ForEach(blade => blade.isOrbitLocked = meleeClip.isOrbitLocked);

                        this.currentMeleeClip.Play(this);

                        if (this.EventAttack != null) this.EventAttack.Invoke(meleeClip);
                    }
                }
            }
        }

        public UnityEngine.Events.UnityEvent EventKnockedUpHitLimitReached;
        public NetworkVariable<int> knockedUpHitCount = new NetworkVariable<int>();
        private void LateUpdate()
        {
            this.IsAttacking = false;

            if (this.Character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None) {
                this.isStaggered = true;
            }

            if (this.comboSystem != null)
            {
                int phase = this.comboSystem.GetCurrentPhase();
                this.IsAttacking = phase >= 0f;

                // Only want hit registration on server
                if (!IsServer) { return; }

                if (this.Blades != null && this.Blades.Count > 0 && phase == 1)
                {
                    foreach (var blade in this.Blades)
                    {
                        if (!this.currentMeleeClip.affectedBones.Contains(blade.weaponBone)) continue;

                        GameObject[] hits = blade.CaptureHits();

                        for (int i = 0; i < hits.Length; ++i)
                        {
                            int hitInstanceID = hits[i].GetInstanceID();

                            if (this.targetsEvaluated.Contains(hitInstanceID)) continue;
                            if (hits[i].transform.IsChildOf(this.transform)) continue;

                            HitResult hitResult = HitResult.ReceiveDamage;

                            CharacterMelee targetMelee = hits[i].GetComponent<CharacterMelee>();
                            MeleeClip attack = this.comboSystem.GetCurrentClip();

                            if (targetMelee != null && !targetMelee.IsInvincible)
                            {
                                if (targetMelee.knockedUpHitCount.Value >= this.KNOCK_UP_FOLLOWUP_LIMIT)
                                {
                                    targetMelee.knockedUpHitCount.Value = 0;
                                    if(attack.attackType != AttackType.Knockdown)
                                        targetMelee.Character.Knockdown(this.Character, targetMelee.Character);
                                    targetMelee.EventKnockedUpHitLimitReached.Invoke();
                                }

                                // Set Ailments here
                                switch(attack.attackType) {
                                    case AttackType.Stun:
                                        targetMelee.Character.Stun();
                                        break;
                                    case AttackType.Knockdown:
                                        if (targetMelee.knockedUpHitCount.Value < this.KNOCK_UP_FOLLOWUP_LIMIT)
                                            targetMelee.Character.Knockdown(this.Character, targetMelee.Character);
                                        break;
                                    case AttackType.Knockedup:
                                        targetMelee.Character.Knockup(this.Character, targetMelee.Character);
                                        break;
                                    case AttackType.None:
                                        if(targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned) {
                                            targetMelee.Character.CancelAilment();
                                        }
                                        break;
                                }

                                if (targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp) { 
                                    targetMelee.knockedUpHitCount.Value++; 
                                }

                                if (targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.None ||
                                    targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp ||
                                    targetMelee.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown) {
                                    hitResult = targetMelee.OnReceiveAttack(this, attack, blade);
                                    if (hitResult == HitResult.ReceiveDamage)
                                        targetMelee.HP.Value -= attack.baseDamage;
                                    else if (hitResult == HitResult.PoiseBlock)
                                        targetMelee.HP.Value -= (int)(attack.baseDamage * 0.7f);
                                }
                                else
                                {
                                    targetMelee.HP.Value -= attack.baseDamage;
                                }
                            }

                            IgniterMeleeOnReceiveAttack[] triggers = hits[i].GetComponentsInChildren<IgniterMeleeOnReceiveAttack>();

                            bool hitSomething = triggers.Length > 0;
                            if (hitSomething)
                            {
                                for (int j = 0; j < triggers.Length; ++j)
                                {
                                    triggers[j].OnReceiveAttack(this, attack, hitResult);
                                }
                            }

                            if (hitSomething && attack != null && targetMelee != null)
                            {
                                Vector3 position = blade.GetImpactPosition();
                                attack.ExecuteActionsOnHit(position, hits[i].gameObject);
                            }

                            if (attack != null && attack.pushForce > float.Epsilon)
                            {
                                Rigidbody[] rigidbodies = hits[i].GetComponents<Rigidbody>();
                                for (int j = 0; j < rigidbodies.Length; ++j)
                                {
                                    Vector3 direction = rigidbodies[j].transform.position - transform.position;
                                    rigidbodies[j].AddForce(direction.normalized * attack.pushForce, ForceMode.Impulse);
                                }
                            }

                            this.targetsEvaluated.Add(hitInstanceID);
                        }
                    }
                }
            }
        }

        protected void UpdatePoise()
        {
            this.poiseDelayCooldown = Mathf.Max(0f, poiseDelayCooldown - Time.deltaTime);
            if (this.poiseDelayCooldown > float.Epsilon) return;

            this.Poise.Value += this.poiseRecoveryRate.GetValue(gameObject) * Time.deltaTime;
            this.Poise.Value = Mathf.Min(this.Poise.Value, this.maxPoise.GetValue(gameObject));
        }

        protected void UpdateDefense()
        {
            if (this.IsBlocking) return;
            if (!this.currentShield) return;

            this.defenseDelayCooldown = Mathf.Max(0f, defenseDelayCooldown - Time.deltaTime);
            if (this.defenseDelayCooldown > float.Epsilon) return;

            this.Defense.Value += this.currentShield.defenseRecoveryRate.GetValue(gameObject) * Time.deltaTime;
            this.Defense.Value = Mathf.Min(this.Defense.Value, this.currentShield.maxDefense.GetValue(gameObject));
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

        public void TestDraw() => StartCoroutine(Draw(this.currentWeapon,  currentShield));

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

        public int maxHealth = 100;
        private NetworkVariable<int> HP = new NetworkVariable<int>();
        private NetworkVariable<bool> isBlockingNetworked = new NetworkVariable<bool>();

        public int GetHP()
        {
            return HP.Value;
        }

        public override void OnNetworkSpawn()
        {
            HP.Value = maxHealth;
            isBlockingNetworked.OnValueChanged += OnIsBlockingNetworkedChange;
            HP.OnValueChanged += OnHPChanged;
        }

        public override void OnNetworkDespawn()
        {
            isBlockingNetworked.OnValueChanged -= OnIsBlockingNetworkedChange;
            HP.OnValueChanged -= OnHPChanged;
        }

        private void OnHPChanged(int prev, int current)
        {
            foreach (HitRenderer hitRenderer in GetComponentsInChildren<HitRenderer>())
            {
                hitRenderer.RenderHit();
            }
        }

        void OnIsBlockingNetworkedChange(bool prev, bool current)
        {
            if (current) // Start blocking
            {
                if (this.currentShield.defendState != null)
                {
                    this.CharacterAnimator.SetState(
                        this.currentShield.defendState,
                        this.currentShield.defendMask,
                        1f, 0.15f, 1f,
                        LAYER_DEFEND
                    );
                }

                if (!this.IsBlocking && this.EventBlock != null)
                {
                    this.EventBlock.Invoke(true);
                }

                this.startBlockingTime = GetTime();
                this.IsBlocking = true;
            }
            else // Stop blocking
            {
                if (this.EventBlock != null) this.EventBlock.Invoke(false);
                this.CharacterAnimator.ResetState(0.25f, LAYER_DEFEND);
                this.IsBlocking = false;
            }
        }

        [ServerRpc]
        public void StartBlockingServerRpc()
        {
            if (this.Character.characterLocomotion.isBusy) return;

            if (this.IsDrawing) return;
            if (this.IsSheathing) return;
            if (this.IsStaggered) return;
            if (this.IsAttacking) return;

            if (this.currentShield == null) return;

            isBlockingNetworked.Value = true;
        }

        [ServerRpc]
        public void StopBlockingServerRpc()
        {
            if (!this.IsBlocking) return;

            isBlockingNetworked.Value = false;
        }

        public virtual void Execute(ActionKey actionKey)
        {
            if (!this.currentWeapon) return;
            if (!this.CanAttack()) return;

            if (IsOwner) this.StopBlockingServerRpc();
            this.inputBuffer.AddInput(actionKey);
        }

        public void StopAttack()
        {
            if (this != null && this.currentMeleeClip != null && this.currentMeleeClip.isAttack == true)
            {
                if (this.inputBuffer.HasInput())
                {
                    this.inputBuffer.ConsumeInput();
                }
                this.comboSystem.Stop();
                this.currentMeleeClip.Stop(this);
            }
        }

        public int GetCurrentPhase()
        {
            if (this.comboSystem == null) return -1;
            return this.comboSystem.GetCurrentPhase();
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

        public void SetPosture(MeleeClip.Posture posture, float duration)
        {
            if (!this.IsStaggered && posture == MeleeClip.Posture.Stagger)
            {
                this.comboSystem.Stop();
                if (EventStagger != null) EventStagger.Invoke(duration);
            }

            this.isStaggered = posture == MeleeClip.Posture.Stagger;
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

        public void AddPoise(float value)
        {
            this.SetPoise(this.Poise.Value + value);
        }

        public void SetDefense(float value)
        {
            if (!this.currentShield) return;

            this.defenseDelayCooldown = this.currentShield.delayDefense.GetValue(gameObject);
            this.Defense.Value = Mathf.Clamp(value, 0f, this.currentShield.maxDefense.GetValue(gameObject));
        }

        public void AddDefense(float value)
        {
            this.SetDefense(this.Defense.Value + value);
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

            AddPoise(-20);
        }

        public void OnDodge()
        {
            if (!IsServer) { Debug.LogError("OnDodge() should only be called on the server."); return; }

            AddPoise(-10);
        }

        public HitResult OnReceiveAttack(CharacterMelee attacker, MeleeClip attack, BladeComponent blade)
        {
            if (!IsServer) { Debug.LogError("OnReceiveAttack() should only be called on the server."); return HitResult.Ignore; }
            
            if (blade == null)
            {
                Debug.LogError("No BladeComponent found. Add one in your Weapon Asset", this);
                return HitResult.Ignore;
            }

            Character assailant = attacker.Character;
            CharacterMelee melee = this.Character.GetComponent<CharacterMelee>();
            BladeComponent meleeWeapon = melee.Blades[0];
            Character player = this.Character.GetComponent<PlayerCharacter>();

            Vector3 bladeImpactPosition = blade.GetImpactPosition();
            OnReceiveAttackClientRpc(assailant.NetworkObjectId, bladeImpactPosition);

            if (this.currentWeapon == null) return HitResult.ReceiveDamage;
            if (this.Character.characterAilment ==  CharacterLocomotion.CHARACTER_AILMENTS.WasGrabbed) return HitResult.ReceiveDamage;
            if (this.Character.characterAilment ==  CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown) return HitResult.ReceiveDamage;
            if (this.IsInvincible) return HitResult.Ignore;

            float attackVectorAngle = Vector3.SignedAngle(transform.forward, attacker.transform.position - this.transform.position, Vector3.up);

            #region Attack and Defense handlers

           float attackAngle = Vector3.Angle(
                attacker.transform.TransformDirection(Vector3.forward),
                this.transform.TransformDirection(Vector3.forward)
            );

            float defenseAngle = this.currentShield != null
                ? this.currentShield.defenseAngle.GetValue(gameObject)
                : 0f;

            if (this.currentShield != null &&
                attack.isBlockable && this.IsBlocking &&
                180f - attackAngle < defenseAngle / 2f)
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

                            attackerReaction.Play(attacker);
                        }

                        if (this.currentShield.perfectBlockClip != null)
                        {
                            this.currentShield.perfectBlockClip.Play(this);
                        }

                        this.ExecuteEffects(
                            this.Blade.GetImpactPosition(),
                            this.currentShield.audioPerfectBlock,
                            this.currentShield.prefabImpactPerfectBlock
                        );

                        this.comboSystem.OnPerfectBlock();
                        return HitResult.PerfectBlock;
                    }

                    MeleeClip blockReaction = this.currentShield.GetBlockReaction();
                    if (blockReaction != null) blockReaction.Play(this);

                    this.ExecuteEffects(
                        bladeImpactPosition,
                        this.currentShield.audioBlock,
                        this.currentShield.prefabImpactBlock
                    );

                    this.comboSystem.OnBlock();
                    return HitResult.AttackBlock;
                }
                else if (Poise.Value >= 30)
                {
                    MeleeClip blockReaction = this.currentShield.GetBlockReaction();
                    if (blockReaction != null) blockReaction.Play(this);

                    this.ExecuteEffects(
                        bladeImpactPosition,
                        this.currentShield.audioBlock,
                        this.currentShield.prefabImpactBlock
                    );

                    this.comboSystem.OnBlock();

                    this.Defense.Value = 0f;

                    AddPoise(-30);
                    return HitResult.PoiseBlock;
                }
                else
                {
                    this.Defense.Value = 0f;
                    this.Poise.Value = 0f;
                    if (IsOwner) this.StopBlockingServerRpc();

                    if (this.EventBreakDefense != null) this.EventBreakDefense.Invoke();
                }
            }
            #endregion


            this.AddPoise(-attack.poiseDamage);

            
            MeleeWeapon.HitLocation hitLocation = this.GetHitLocation(attackVectorAngle);
            bool isKnockback = attack.attackType == AttackType.Knockdown | this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown;
            bool isKnockup = attack.attackType == AttackType.Knockedup | this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp;

            MeleeClip hitReaction = this.currentWeapon.GetHitReaction(
                this.Character.IsGrounded(),
                hitLocation,
                isKnockback,
                isKnockup
            );

            this.ExecuteEffects(
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
            if (!this.IsUninterruptable && attack.attackType == AttackType.None)
            {
                hitReaction.Play(this);
            }
            return HitResult.ReceiveDamage;
        }

        [ClientRpc]
        void OnReceiveAttackClientRpc(ulong attackerNetObjId, Vector3 bladeImpactPosition)
        {
            CharacterMelee attacker = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<CharacterMelee>();
            MeleeClip attack = attacker.comboSystem.GetCurrentClip();

            Character assailant = attacker.Character;
            CharacterMelee melee = this.Character.GetComponent<CharacterMelee>();
            BladeComponent meleeWeapon = melee.Blades[0];
            Character player = this.Character.GetComponent<PlayerCharacter>();

            if (this.currentWeapon == null) return;

            float attackVectorAngle = Vector3.SignedAngle(transform.forward, attacker.transform.position - this.transform.position, Vector3.up);

            #region Attack and Defense handlers

            float attackAngle = Vector3.Angle(
                 attacker.transform.TransformDirection(Vector3.forward),
                 this.transform.TransformDirection(Vector3.forward)
             );

            float defenseAngle = this.currentShield != null
                ? this.currentShield.defenseAngle.GetValue(gameObject)
                : 0f;

            if (this.currentShield != null &&
                attack.isBlockable && this.IsBlocking &&
                180f - attackAngle < defenseAngle / 2f)
            {
                if (this.Defense.Value > 0)
                {
                    if (GetTime() < this.startBlockingTime + this.currentShield.perfectBlockWindow)
                    {
                        if (attacker != null)
                        {
                            MeleeClip attackerReaction = this.Character.IsGrounded()
                                ? this.currentShield.groundPerfectBlockReaction
                                : this.currentShield.airbornPerfectBlockReaction;

                            attackerReaction.Play(attacker);
                        }

                        if (this.currentShield.perfectBlockClip != null)
                        {
                            this.currentShield.perfectBlockClip.Play(this);
                        }

                        this.ExecuteEffects(
                            this.Blade.GetImpactPosition(),
                            this.currentShield.audioPerfectBlock,
                            this.currentShield.prefabImpactPerfectBlock
                        );

                        this.comboSystem.OnPerfectBlock();
                        return;
                    }

                    MeleeClip blockReaction = this.currentShield.GetBlockReaction();
                    if (blockReaction != null) blockReaction.Play(this);

                    this.ExecuteEffects(
                        bladeImpactPosition,
                        this.currentShield.audioBlock,
                        this.currentShield.prefabImpactBlock
                    );

                    this.comboSystem.OnBlock();
                    return;
                }
                else if (Poise.Value >= 30)
                {
                    MeleeClip blockReaction = this.currentShield.GetBlockReaction();
                    if (blockReaction != null) blockReaction.Play(this);

                    this.ExecuteEffects(
                        bladeImpactPosition,
                        this.currentShield.audioBlock,
                        this.currentShield.prefabImpactBlock
                    );

                    this.comboSystem.OnBlock();
                    return;
                }
                else
                {
                    if (this.EventBreakDefense != null) this.EventBreakDefense.Invoke();
                }
            }
            #endregion

            MeleeWeapon.HitLocation hitLocation = this.GetHitLocation(attackVectorAngle);
            bool isKnockback = attack.attackType == AttackType.Knockdown | this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown;
            bool isKnockup = attack.attackType == AttackType.Knockedup | this.Character.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp;

            MeleeClip hitReaction = this.currentWeapon.GetHitReaction(
                this.Character.IsGrounded(),
                hitLocation,
                isKnockback,
                isKnockup
            );

            this.ExecuteEffects(
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
            if (!this.IsUninterruptable && attack.attackType == AttackType.None)
            {
                hitReaction.Play(this);
            }
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        protected virtual float GetTime()
        {
            return Time.time;
        }

        private void ExecuteEffects(Vector3 position, AudioClip audio, GameObject prefab)
        {
            this.PlayAudio(audio);

            if (prefab != null)
            {
                GameObject impact = PoolManager.Instance.Pick(prefab);
                impact.transform.position = position;
            }

            if (currentWeapon.prefabImpactHit != null) {
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

        public float ChangeState(CharacterState state, AvatarMask mask, CharacterAnimation.Layer layer, CharacterAnimator animator)
        {
            float time = TRANSITION;
            if (state != null)
            {
                if (state.enterClip != null)
                {
                    time = state.enterClip.length;
                }

                time = Mathf.Max(TRANSITION, time) * 0.5f;
                this.CharacterAnimator.SetState(state, mask, 1f, time, 1f, layer);

                if(animator != null) {
                    animator.ResetControllerTopology(state.GetRuntimeAnimatorController(), false);
                }
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

        public bool Grab(CharacterMelee targetCharacter) {
            CharacterMelee executorCharacter = this;

            MeleeWeapon executorWeapon = this.currentWeapon;

            if(targetCharacter == null || executorCharacter == null) return false;

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

            executorWeapon.grabAttack.Play(executorCharacter);
            executorWeapon.grabReaction.Play(targetCharacter);

            CoroutinesManager.Instance.StartCoroutine(this.PostGrabRoutine(executorCharacter, targetCharacter));

            return true;
        }

        public IEnumerator PostGrabRoutine(CharacterMelee executorCharacter, CharacterMelee targetCharacter)
        {
            float initTime = Time.time;

            while (initTime + this.anim_ExecutedDuration >= Time.time) {
                executorCharacter.IsGrabbing = true;
                targetCharacter.IsGrabbed = true;

                yield return null;
            }

            executorCharacter.SetInvincibility(2.0f);

            this.anim_ExecuterDuration = 0.00f;
            this.anim_ExecutedDuration = 0.00f;

            yield return 0;
        }
    }
}
