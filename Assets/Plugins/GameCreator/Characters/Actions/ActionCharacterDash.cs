namespace GameCreator.Characters
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
    using GameCreator.Core;
    using GameCreator.Characters;
    using GameCreator.Variables;
    using GameCreator.Melee;
    using Unity.Netcode;

    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    [AddComponentMenu("")]
	public class ActionCharacterDash : IAction
	{
        private static readonly Vector3 PLANE = new Vector3(1, 0, 1);

        public enum Direction
        {
            CharacterMovement3D,
            TowardsTarget,
            TowardsPosition,
            MovementSidescrollXY,
            MovementSidescrollZY
        }

        public TargetCharacter character = new TargetCharacter(TargetCharacter.Target.Player);

        public Direction direction = Direction.CharacterMovement3D;
        public TargetGameObject target = new TargetGameObject();
        public TargetPosition position = new TargetPosition();

        public NumberProperty impulse = new NumberProperty(5f);
        public NumberProperty duration = new NumberProperty(0f);
        public float drag = 10f;

        [Space]
        public AnimationClip dashClipForward;
        public AnimationClip dashClipBackward;
        public AnimationClip dashClipRight;
        public AnimationClip dashClipLeft;

        private static readonly Keyframe[] DEFAULT_KEY_MOVEMENT = {
            new Keyframe(0f, 0f),
            new Keyframe(1f, 0f)
        };

        private AnimationCurve spMovementForward = new AnimationCurve(DEFAULT_KEY_MOVEMENT);
        private AnimationCurve spMovementSides = new AnimationCurve(DEFAULT_KEY_MOVEMENT);
        private AnimationCurve spMovementVertical = new AnimationCurve(DEFAULT_KEY_MOVEMENT);

        [ServerRpc]
        void DodgeServerRpc(Vector3 targetPosition, Quaternion targetRotation, string targetName)
        {
            GameObject target = new GameObject(targetName);
            target.transform.position = targetPosition;
            target.transform.rotation = targetRotation;
            DodgeClientRpc(targetPosition, targetRotation, targetName, InstantExecuteLocally(target));
        }

        [ClientRpc]
        void DodgeClientRpc(Vector3 targetPosition, Quaternion targetRotation, string targetName, float angle)
        {
            GameObject target = new GameObject(targetName);
            target.transform.position = targetPosition;
            target.transform.rotation = targetRotation;
            InstantExecuteLocally(target, angle);
        }

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            if (IsOwner) { DodgeServerRpc(target.transform.position, target.transform.rotation, target.name); }
            return false;
        }

        public float InstantExecuteLocally(GameObject target)
        {
            Character characterTarget = this.character.GetCharacter(target);
            if (characterTarget == null) return 0;

            CharacterLocomotion locomotion = characterTarget.characterLocomotion;
            CharacterAnimator animator = characterTarget.GetCharacterAnimator();
            Vector3 moveDirection = Vector3.zero;

            CharacterMelee melee = characterTarget.GetComponent<CharacterMelee>();

            MeleeWeapon meleeweapon = new MeleeWeapon();

            if (melee != null)
            {
                if (melee.currentMeleeClip != null && melee.currentMeleeClip.isAttack == true)
                {
                    melee.StopAttack();
                    animator.StopGesture(0f);
                    melee.currentMeleeClip = null;
                }

                meleeweapon = melee.currentWeapon;
            }

            switch (this.direction)
            {
                case Direction.CharacterMovement3D:
                    moveDirection = locomotion.GetMovementDirection();
                    break;

                case Direction.TowardsTarget:
                    Transform targetTransform = this.target.GetTransform(target);
                    if (targetTransform != null)
                    {
                        moveDirection = targetTransform.position - characterTarget.transform.position;
                        moveDirection.Scale(PLANE);
                    }
                    break;

                case Direction.TowardsPosition:
                    Vector3 targetPosition = this.position.GetPosition(target);
                    moveDirection = targetPosition - characterTarget.transform.position;
                    moveDirection.Scale(PLANE);
                    break;

                case Direction.MovementSidescrollXY:
                    moveDirection = locomotion.GetMovementDirection();
                    moveDirection.Scale(new Vector3(1, 1, 0));
                    break;

                case Direction.MovementSidescrollZY:
                    moveDirection = locomotion.GetMovementDirection();
                    moveDirection.Scale(new Vector3(0, 1, 1));
                    break;
            }

            Vector3 charDirection = Vector3.Scale(
                characterTarget.transform.TransformDirection(Vector3.forward),
                PLANE
            );

            float angle = Vector3.SignedAngle(moveDirection, charDirection, Vector3.up);
            AnimationClip clip = null;

            float speed = 1.0f;
            float transitionIn = 0.25f;
            float transitionOut = 0.25f;

            MeleeClip dodgeMeleeClip;

            #region Compute Angle
            if (angle <= 15f && angle >= -15f)
            {
                dodgeMeleeClip = meleeweapon.dodgeF;
                clip = meleeweapon.dodgeF.animationClip;
                speed = meleeweapon.dodgeF.animSpeed;
            }
            else if (angle < 80f && angle > 15f)
            {
                dodgeMeleeClip = meleeweapon.dodgeFL;
                clip = meleeweapon.dodgeFL.animationClip;
                speed = meleeweapon.dodgeFL.animSpeed;
            }
            else if (angle > -80f && angle < -15f)
            {
                dodgeMeleeClip = meleeweapon.dodgeFR;
                clip = meleeweapon.dodgeFR.animationClip;
                speed = meleeweapon.dodgeFR.animSpeed;
            }
            else if (angle > 80f && angle < 100f)
            {
                dodgeMeleeClip = meleeweapon.dodgeL;
                clip = meleeweapon.dodgeL.animationClip;
                speed = meleeweapon.dodgeL.animSpeed;
            }
            else if (angle < -80f && angle > -100f)
            {
                dodgeMeleeClip = meleeweapon.dodgeR;
                clip = meleeweapon.dodgeR.animationClip;
                speed = meleeweapon.dodgeR.animSpeed;
            }
            else if (angle < -100f && angle > -170f)
            {
                dodgeMeleeClip = meleeweapon.dodgeBR;
                clip = meleeweapon.dodgeBR.animationClip;
                speed = meleeweapon.dodgeBR.animSpeed;
            }
            else if (angle > 100f && angle < 170f)
            {
                dodgeMeleeClip = meleeweapon.dodgeBL;
                clip = meleeweapon.dodgeBL.animationClip;
                speed = meleeweapon.dodgeBL.animSpeed;
            }
            else
            {
                dodgeMeleeClip = meleeweapon.dodgeB;
                clip = meleeweapon.dodgeB.animationClip;
                speed = meleeweapon.dodgeB.animSpeed;
            }

            #endregion

            float duration = ((clip.length - (clip.length * 0.50f)) / (speed)) * .75f;

            bool isDashing = characterTarget.Dash(
                moveDirection.normalized,
                this.impulse.GetValue(target),
                duration,
                1.0f
            );

            if (clip != null && animator != null)
            {
                characterTarget.characterLocomotion.RootMovement(
                    dodgeMeleeClip.movementMultiplier,
                    duration,
                    1.0f,
                    dodgeMeleeClip.movementForward,
                    dodgeMeleeClip.movementSides,
                    dodgeMeleeClip.movementVertical
                );

                animator.CrossFadeGesture(clip, speed, null, 0.15f, 0.4f);
            }

            return angle;
        }

        public float InstantExecuteLocally(GameObject target, float angle)
        {
            Character characterTarget = this.character.GetCharacter(target);
            if (characterTarget == null) return 0;

            CharacterLocomotion locomotion = characterTarget.characterLocomotion;
            CharacterAnimator animator = characterTarget.GetCharacterAnimator();
            Vector3 moveDirection = Vector3.zero;

            CharacterMelee melee = characterTarget.GetComponent<CharacterMelee>();

            MeleeWeapon meleeweapon = new MeleeWeapon();

            if (melee != null)
            {
                if (melee.currentMeleeClip != null && melee.currentMeleeClip.isAttack == true)
                {
                    melee.StopAttack();
                    animator.StopGesture(0f);
                    melee.currentMeleeClip = null;
                }

                meleeweapon = melee.currentWeapon;
            }

            switch (this.direction)
            {
                case Direction.CharacterMovement3D:
                    moveDirection = locomotion.GetMovementDirection();
                    break;

                case Direction.TowardsTarget:
                    Transform targetTransform = this.target.GetTransform(target);
                    if (targetTransform != null)
                    {
                        moveDirection = targetTransform.position - characterTarget.transform.position;
                        moveDirection.Scale(PLANE);
                    }
                    break;

                case Direction.TowardsPosition:
                    Vector3 targetPosition = this.position.GetPosition(target);
                    moveDirection = targetPosition - characterTarget.transform.position;
                    moveDirection.Scale(PLANE);
                    break;

                case Direction.MovementSidescrollXY:
                    moveDirection = locomotion.GetMovementDirection();
                    moveDirection.Scale(new Vector3(1, 1, 0));
                    break;

                case Direction.MovementSidescrollZY:
                    moveDirection = locomotion.GetMovementDirection();
                    moveDirection.Scale(new Vector3(0, 1, 1));
                    break;
            }

            Vector3 charDirection = Vector3.Scale(
                characterTarget.transform.TransformDirection(Vector3.forward),
                PLANE
            );

            AnimationClip clip = null;

            float speed = 1.0f;

            MeleeClip dodgeMeleeClip;

            #region Compute Angle
            if (angle <= 15f && angle >= -15f)
            {
                dodgeMeleeClip = meleeweapon.dodgeF;
                clip = meleeweapon.dodgeF.animationClip;
                speed = meleeweapon.dodgeF.animSpeed;
                transitionIn = meleeweapon.dodgeF.transitionIn;
                transitionOut = meleeweapon.dodgeF.transitionOut;
            } else if (angle < 80f && angle > 15f) {
                dodgeMeleeClip = meleeweapon.dodgeFL;
                clip = meleeweapon.dodgeFL.animationClip;
                speed = meleeweapon.dodgeFL.animSpeed;
                transitionIn = meleeweapon.dodgeFL.transitionIn;
                transitionOut = meleeweapon.dodgeFL.transitionOut;
            } else if (angle > -80f && angle < -15f) {
                dodgeMeleeClip = meleeweapon.dodgeFR;
                clip = meleeweapon.dodgeFR.animationClip;
                speed = meleeweapon.dodgeFR.animSpeed;
                transitionIn = meleeweapon.dodgeFR.transitionIn;
                transitionOut = meleeweapon.dodgeFR.transitionOut;
            } else if (angle > 80f && angle < 100f) {
                dodgeMeleeClip = meleeweapon.dodgeL;
                clip = meleeweapon.dodgeL.animationClip;
                speed = meleeweapon.dodgeL.animSpeed;
                transitionIn = meleeweapon.dodgeL.transitionIn;
                transitionOut = meleeweapon.dodgeL.transitionOut;
            } else if (angle < -80f && angle > -100f) {
                dodgeMeleeClip = meleeweapon.dodgeR;
                clip = meleeweapon.dodgeR.animationClip;
                speed = meleeweapon.dodgeR.animSpeed;
                transitionIn = meleeweapon.dodgeR.transitionIn;
                transitionOut = meleeweapon.dodgeR.transitionOut;
            } else if (angle < -100f && angle > -170f) {
                dodgeMeleeClip = meleeweapon.dodgeBR;
                clip = meleeweapon.dodgeBR.animationClip;
                speed = meleeweapon.dodgeBR.animSpeed;
                transitionIn = meleeweapon.dodgeBR.transitionIn;
                transitionOut = meleeweapon.dodgeBR.transitionOut;
            } else if (angle > 100f && angle < 170f) {
                dodgeMeleeClip = meleeweapon.dodgeBL;
                clip = meleeweapon.dodgeBL.animationClip;
                speed = meleeweapon.dodgeBL.animSpeed;
                transitionIn = meleeweapon.dodgeBL.transitionIn;
                transitionOut = meleeweapon.dodgeBL.transitionOut;
            } else {
                dodgeMeleeClip = meleeweapon.dodgeB;
                clip = meleeweapon.dodgeB.animationClip;
                speed = meleeweapon.dodgeB.animSpeed;
                transitionIn = meleeweapon.dodgeF.transitionIn;
                transitionOut = meleeweapon.dodgeF.transitionOut;
            }


            #endregion

            float duration = ((clip.length) * 0.40f);

            bool isDashing = characterTarget.Dash(
                moveDirection.normalized,
                this.impulse.GetValue(target),
                duration,
                1.0f
            );

            if (clip != null && animator != null)
            {
                characterTarget.characterLocomotion.RootMovement(
                    dodgeMeleeClip.movementMultiplier,
                    duration,
                    1.0f,
                    dodgeMeleeClip.movementForward,
                    dodgeMeleeClip.movementSides,
                    dodgeMeleeClip.movementVertical
                );

                float transiition = ((clip.length) / (speed)) * 0.18f;

                animator.CrossFadeGesture(clip, speed, null, transitionIn, transitionOut);
            }

            return angle;
        }

#if UNITY_EDITOR
        public static new string NAME = "Character/Character Dash";
        private const string TITLE_NAME = "Character {0} dash {1}";

        public override string GetNodeTitle()
        {
            return string.Format(
                TITLE_NAME,
                this.character,
                this.direction
            );
        }

        private SerializedProperty spCharacter;
        private SerializedProperty spDirection;
        private SerializedProperty spTarget;
        private SerializedProperty spPosition;

        private SerializedProperty spImpulse;
        private SerializedProperty spDuration;
        private SerializedProperty spDrag;

        private SerializedProperty spDashForward;
        private SerializedProperty spDashBackward;
        private SerializedProperty spDashRight;
        private SerializedProperty spDashLeft;

        protected override void OnEnableEditorChild()
        {
            this.spCharacter = this.serializedObject.FindProperty("character");
            this.spDirection = this.serializedObject.FindProperty("direction");
            this.spTarget = this.serializedObject.FindProperty("target");
            this.spPosition = this.serializedObject.FindProperty("position");

            this.spImpulse = this.serializedObject.FindProperty("impulse");
            this.spDuration = this.serializedObject.FindProperty("duration");
            this.spDrag = this.serializedObject.FindProperty("drag");

            this.spDashForward = this.serializedObject.FindProperty("dashClipForward");
            this.spDashBackward = this.serializedObject.FindProperty("dashClipBackward");
            this.spDashRight = this.serializedObject.FindProperty("dashClipRight");
            this.spDashLeft = this.serializedObject.FindProperty("dashClipLeft");
        }

        protected override void OnDisableEditorChild()
        {
            this.spCharacter = null;
            this.spDirection = null;
            this.spTarget = null;
            this.spPosition = null;

            this.spImpulse = null;
            this.spDuration = null;
            this.spDrag = null;

            this.spDashForward = null;
            this.spDashBackward = null;
            this.spDashRight = null;
            this.spDashLeft = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(this.spCharacter);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(this.spDirection);
            switch (this.spDirection.enumValueIndex)
            {
                case (int)Direction.TowardsTarget:
                    EditorGUILayout.PropertyField(this.spTarget);
                    break;

                case (int)Direction.TowardsPosition:
                    EditorGUILayout.PropertyField(this.spPosition);
                    break;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spImpulse);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spDuration);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spDrag);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spDashForward);
            EditorGUILayout.PropertyField(this.spDashBackward);
            EditorGUILayout.PropertyField(this.spDashRight);
            EditorGUILayout.PropertyField(this.spDashLeft);

            serializedObject.ApplyModifiedProperties();
        }
        #endif
    }
}
