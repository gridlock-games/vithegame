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

[AddComponentMenu("")]
public class ActionIdentifyTarget : IAction
{

    GameObject cameraMotor;
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
    public MeleeClip meleeClipExecution;
    public MeleeClip meleeClipExecuted;

    // +--------------------------------------------------------------------------------------+
    // | Privates                                                                               |
    // +--------------------------------------------------------------------------------------+

    private float duration = 1.00f;
    private CharacterMelee characterMeleeA;
    private CharacterMelee characterMeleeB;

    private static readonly Vector3 PLANE = new Vector3(1, 0, 1);

    private float anim_ExecuterDuration = 0.0f;
    private float anim_ExecutedDuration = 0.0f;
    private CharacterController chrCtrl_target;
    private CharacterController chrCtrl_executioner;

    // Start is called before the first frame update
    public override bool InstantExecute(GameObject target, IAction[] actions, int index)
    {

        this.anim_ExecuterDuration = (this.meleeClipExecution.animationClip.length);
        this.anim_ExecutedDuration = (this.meleeClipExecuted.animationClip.length);

        #region FireRayCast
        var gameObject = StartRaycastFrom.GetGameObject(target);

        if (gameObject == null) return false;

        var RayDistance = RaycastLength.GetValue(target);

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        m_HitDetect = false;
        RaycastHit[] allHits = Physics.RaycastAll(transform.position + Vector3.up, transform.forward, 10, -1, QueryTriggerInteraction.Ignore);
        Debug.DrawRay(transform.position + Vector3.up, transform.forward * RayDistance, Color.red, RayDistance);
        System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit rayHit in allHits)
        {
            if (rayHit.transform == transform) { continue; }

            m_HitDetect = true;
            hit = rayHit;
            break;
        }

        if (m_HitDetect)
        {
            if (CheckForTag == true)
            {
                if (hit.collider.CompareTag(this.TagName))
                {
                    this.StoreHitColliderTo.Set(hit.collider.gameObject, gameObject);

                    print(hit.transform.name);
                }
            }
            else
            {
                this.StoreHitColliderTo.Set(hit.collider.gameObject, gameObject);

                print(hit.transform.name);
            }
        }

        #endregion

        Character executioner = this.characterExecutioner.GetCharacter(target);
        Character targetChar = this.characterTarget.GetCharacter(target);

        #region SetTargetParent

        if (this.GrabPlaceholder != null && targetChar != null && executioner != null)
        {

            this.chrCtrl_target = targetChar.GetComponent<CharacterController>();
            this.chrCtrl_executioner = executioner.GetComponent<CharacterController>();

            chrCtrl_executioner.radius = 0.05f;
            chrCtrl_target.radius = 0.05f;

            CameraMotor motor = Camera.main.GetComponent<CameraController>().currentCameraMotor;

            CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;
            

            targetChar.transform.position = GrabPlaceholder.transform.position;
        }

        #endregion

        #region HandleMeleeClip

        if (targetChar != null && executioner != null)
        {
            Vector3 rotationDirection = (
                executioner.gameObject.transform.position - targetChar.gameObject.transform.position
            );

            Quaternion targetRotation = Quaternion.LookRotation(rotationDirection, executioner.transform.up);

            rotationDirection = Vector3.Scale(rotationDirection, PLANE).normalized;
            this.duration = Vector3.Angle(
                targetChar.transform.TransformDirection(Vector3.forward),
                rotationDirection
            ) / targetChar.characterLocomotion.angularSpeed;

            targetChar.characterLocomotion.SetRotation(rotationDirection);
            targetChar.transform.rotation = targetRotation;
        }

        if (executioner != null && targetChar != null)
        {
            characterMeleeA = executioner.GetComponent<CharacterMelee>();
            characterMeleeB = targetChar.GetComponent<CharacterMelee>();
            if (characterMeleeA != null && characterMeleeB != null)
            {
                characterMeleeA.SetPosture(Posture.Stagger, anim_ExecutedDuration);
                characterMeleeB.SetPosture(Posture.Stagger, anim_ExecutedDuration);

                characterMeleeA.StopAttack();
                characterMeleeB.StopAttack();

                meleeClipExecution.Play(characterMeleeA);
                meleeClipExecuted.Play(characterMeleeB);

                this.StoreHitColliderTo.Set(null, null);

                CoroutinesManager.Instance.StartCoroutine(this.EnableOrbitRoutine(executioner, targetChar));
                return true;
            }
            return false;
        }
        #endregion

        return true;
    }

    public IEnumerator EnableOrbitRoutine(Character executioner, Character targetChar)
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
                var direction = CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection;
                executioner.characterLocomotion.isBusy = true;
                executioner.characterLocomotion.overrideFaceDirection = direction;
                executioner.characterLocomotion.isControllable = false;
                targetChar.characterLocomotion.isBusy = true;
                targetChar.characterLocomotion.overrideFaceDirection = direction;
                // targetChar.characterLocomotion.isControllable = false;


                targetChar.transform.SetParent(null, true);
                yield return null;
            }

            var directionUpdate = CharacterLocomotion.OVERRIDE_FACE_DIRECTION.CameraDirection;
            executioner.characterLocomotion.overrideFaceDirection = directionUpdate;
            executioner.characterLocomotion.isBusy = false;
            // THIS IS FOR THE TARGET
            // targetChar.characterLocomotion.overrideFaceDirection = directionUpdate;
            // targetChar.characterLocomotion.isControllable = true;
            targetChar.characterLocomotion.isBusy = false;

            chrCtrl_executioner.radius = 0.50f;
            chrCtrl_target.radius = 0.50f;

            
            executioner.characterLocomotion.isControllable = true;
        }

        yield return 0;
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
        private SerializedProperty spmeleeClipExecution;
        private SerializedProperty spmeleeClipExecuted;

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
            this.spmeleeClipExecuted = this.serializedObject.FindProperty("meleeClipExecuted");
            this.spmeleeClipExecution = this.serializedObject.FindProperty("meleeClipExecution");

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
            this.spmeleeClipExecuted = null;
            this.spmeleeClipExecution = null;
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
            EditorGUILayout.PropertyField(this.spCharacterExecutioner);
            EditorGUILayout.PropertyField(this.spmeleeClipExecution);
            EditorGUILayout.PropertyField(this.spGrabPlaceholder);

            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Executed", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.spCharacterTarget);
            EditorGUILayout.PropertyField(this.spmeleeClipExecuted);

			this.serializedObject.ApplyModifiedProperties();
		}

#endif
}

