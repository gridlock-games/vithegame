using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameCreator.Melee;
using static GameCreator.Melee.MeleeClip;
using GameCreator.Core;
using GameCreator.Variables;
using GameCreator.Core.Math;
using GameCreator.Core.Hooks;
using GameCreator.Characters;
using GameCreator.Camera;

#if UNITY_EDITOR
    using UnityEditor;
#endif

public class PreserveRotation {

    public PreserveRotation ( Quaternion targetRotation, Vector3 rotationDirection) {
        this.quaternion = targetRotation;
        this.vector3 = rotationDirection;
    }
    public Quaternion quaternion {get; private set;}
    public Vector3 vector3 {get; private set;}
}

[AddComponentMenu("")]
public class ActionIdentifyTarget : IAction
{
    public TargetCharacter characterExecutioner = new TargetCharacter();
    public TargetCharacter characterTarget = new TargetCharacter();

    public TargetGameObject StartRaycastFrom = new TargetGameObject(TargetGameObject.Target.Invoker);
    public VariableProperty StoreHitColliderTo = new VariableProperty(Variable.VarType.GlobalVariable);
    public NumberProperty RaycastLength;
    public LayerMask RaycastLayer;


    public bool CheckForTag;
    public string TagName = "";

    public GameObject GrabPlaceholder;
    Ray ray;
    bool m_HitDetect;

    RaycastHit hit;

    // +--------------------------------------------------------------------------------------+
    // | Privates                                                                               |
    // +--------------------------------------------------------------------------------------+

    private float duration = 1.00f;

    private static readonly Vector3 PLANE = new Vector3(1, 0, 1);

    private float anim_ExecuterDuration = 0.0f;
    private float anim_ExecutedDuration = 0.0f;
    private CharacterController chrCtrl_target;
    private CharacterController chrCtrl_executioner;

    // Start is called before the first frame update
    public override bool InstantExecute(GameObject target, IAction[] actions, int index)
    {

        #region FireRayCast
        var gameObject = StartRaycastFrom.GetGameObject(target);

        if (gameObject == null) return false;

        var RayDistance = RaycastLength.GetValue(target);

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        m_HitDetect = false;
        RaycastHit[] allHits = Physics.RaycastAll(transform.position + Vector3.up, transform.forward, 10, -1, QueryTriggerInteraction.Ignore);
        Debug.DrawRay(transform.position + Vector3.up, transform.forward * RayDistance, Color.red, 1.0f);
        System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

        LayerMask layer = this.RaycastLayer;

        foreach (RaycastHit rayHit in allHits)
        {
            if (rayHit.transform == transform) { continue; }

        
            if(rayHit.collider.gameObject.tag == "Character") {
                m_HitDetect = true;
                hit = rayHit;
                break;
            }
        }

        // Make sure that there detected target
        if (m_HitDetect == false) return false;

       this.StoreHitColliderTo.Set(hit.collider.gameObject, gameObject);

        #endregion

        Character executioner = this.characterExecutioner.GetCharacter(target);
        Character targetChar = this.characterTarget.GetCharacter(target);

        //Check if Target and Executioner should be able to enter Grab Phase
        if(targetChar.characterLocomotion.isBusy || executioner.characterLocomotion.isBusy) {
            return false;
        }
  
        PreserveRotation rotationConfig = Rotation(executioner.gameObject, targetChar);

        #region RotateCharacter
        if (targetChar != null && executioner != null)
        {
            targetChar.characterLocomotion.SetRotation(rotationConfig.vector3);
            // targetChar.transform.rotation = rotationConfig.quaternion;
        }
        #endregion

        // Handle Melee Clips
        CharacterMelee characterMeleeA = executioner.GetComponent<CharacterMelee>();
        CharacterMelee characterMeleeB = targetChar.GetComponent<CharacterMelee>();

        if(!characterMeleeA || !characterMeleeB) return false;

        if (executioner != null && targetChar != null)
        {
            // Sync placeholder position per weapon
            GrabPlaceholder.transform.localPosition = characterMeleeA.currentWeapon.grabPlaceholderPosition;

            // Change Camera Input and Player Controls
            var direction = CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection;
            executioner.Grab(direction, false);
            targetChar.Grab(direction, false);
            targetChar.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.WasGrabbed, characterMeleeA.currentWeapon.grabReactionState );

            // Teleport Target to GrabPlaceholder
            targetChar.transform.position = GrabPlaceholder.transform.position;
            targetChar.transform.rotation = rotationConfig.quaternion;
            
            targetChar.characterLocomotion.SetRotation(rotationConfig.vector3);

            // GetAnim Duration
            this.anim_ExecuterDuration = (characterMeleeA.currentWeapon.grabAttack.animationClip.length);
            this.anim_ExecutedDuration = (characterMeleeA.currentWeapon.grabReaction.animationClip.length);

            bool isGrabbing = characterMeleeA.Grab(characterMeleeB);

            if(!isGrabbing) return false;

            CoroutinesManager.Instance.StartCoroutine(this.PostGrabRoutine(executioner, targetChar));
            return true;
        }

        return true;
    }

    public IEnumerator PostGrabRoutine(Character executioner, Character targetChar)
    {
        CameraMotor motor = Camera.main.GetComponent<CameraController>().currentCameraMotor;

        if (motor != null && motor.cameraMotorType.GetType() == typeof(CameraMotorTypeAdventure))
        {
            float initTime = Time.time;

            this.chrCtrl_target = targetChar.GetComponent<CharacterController>();
            this.chrCtrl_executioner = executioner.GetComponent<CharacterController>();

            CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;

            while (initTime + this.anim_ExecutedDuration >= Time.time)
            {
                // Reduce Collider Radius
                chrCtrl_executioner.radius = 0.05f;
                chrCtrl_target.radius = 0.05f;
                yield return null;
            }

            // Revert Collider Radius
            chrCtrl_executioner.radius = 0.50f;
            chrCtrl_target.radius = 0.50f;

            // Update Camera Input and Player Controls
            var directionUpdate = CharacterLocomotion.OVERRIDE_FACE_DIRECTION.CameraDirection;
            executioner.Grab(directionUpdate, true);
        }

        yield return 0;
    }


    private PreserveRotation Rotation(GameObject anchor, Character targetChar) {

         Vector3 rotationDirection = (
            anchor.transform.position - targetChar.gameObject.transform.position
        );

        Quaternion targetRotation = Quaternion.LookRotation(rotationDirection, anchor.transform.up);

        rotationDirection = Vector3.Scale(rotationDirection, PLANE).normalized;

        this.duration = Vector3.Angle(
            targetChar.transform.TransformDirection(Vector3.forward),
            rotationDirection
        ) / targetChar.characterLocomotion.angularSpeed;

        targetChar.characterLocomotion.SetRotation(rotationDirection);

        PreserveRotation preserveRotation = new PreserveRotation(targetRotation, rotationDirection);

        return preserveRotation;

    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        //Check if there has been a hit yet
        if (m_HitDetect)
        {
            //Draw a Ray forward from GameObject toward the hit
            Gizmos.DrawRay(transform.position, transform.forward * hit.distance);
            //Draw a cube that extends to where the hit exists
            Gizmos.DrawWireCube(transform.position + transform.forward * hit.distance, transform.localScale);
        }
        //If there hasn't been a hit yet, draw the ray at the maximum distance
        else
        {
            //Draw a Ray forward from GameObject toward the maximum distance
            Gizmos.DrawRay(transform.position, transform.forward * 10.0f);
            //Draw a cube at the maximum distance
            Gizmos.DrawWireCube(transform.position + transform.forward * 10.0f, transform.localScale);
        }
    }

    // +--------------------------------------------------------------------------------------+
    // | EDITOR                                                                               |
    // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

	    public static new string NAME = "Character/Grab Target";
        private const string NODE_TITLE = "Grab";

		// PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spCharacterExecutioner;
		private SerializedProperty spCharacterTarget;

        private SerializedProperty spattachRayCastFrom;
        private SerializedProperty sprayCastLength;
        private SerializedProperty spstoreHitColliderTo;
        private SerializedProperty spRayCastLayer;

        private SerializedProperty spGrabPlaceholder;

		// INSPECTOR METHODS: ---------------------------------------------------------------------

		public override string GetNodeTitle()
		{
			return string.Format(
                NODE_TITLE
            );
		}

		protected override void OnEnableEditorChild ()
		{
            this.spCharacterExecutioner = this.serializedObject.FindProperty("characterExecutioner");
            this.spCharacterTarget = this.serializedObject.FindProperty("characterTarget");

            this.spattachRayCastFrom = this.serializedObject.FindProperty("StartRaycastFrom");
            this.sprayCastLength = this.serializedObject.FindProperty("RaycastLength");
            this.spstoreHitColliderTo = this.serializedObject.FindProperty("StoreHitColliderTo");
            this.spRayCastLayer = this.serializedObject.FindProperty("RaycastLayer");

            this.spGrabPlaceholder = this.serializedObject.FindProperty("GrabPlaceholder");
		}

		protected override void OnDisableEditorChild ()
		{
            this.spCharacterExecutioner = null;
            this.spCharacterTarget = null;
            this.spGrabPlaceholder = null;
		}

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();
            
            EditorGUILayout.LabelField("Raycast", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.spattachRayCastFrom);
            EditorGUILayout.PropertyField(this.sprayCastLength);
            EditorGUILayout.PropertyField(this.spstoreHitColliderTo);
            EditorGUILayout.PropertyField(this.spRayCastLayer);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Executioner", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.spGrabPlaceholder);

            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Executed", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.spCharacterTarget);

			this.serializedObject.ApplyModifiedProperties();
		}

#endif
}