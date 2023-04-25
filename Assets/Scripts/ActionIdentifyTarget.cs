using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreator.Melee;
using static GameCreator.Melee.MeleeClip;
using GameCreator.Core;
using GameCreator.Variables;
using GameCreator.Core.Math;
using GameCreator.Core.Hooks;
using GameCreator.Characters;

#if UNITY_EDITOR
    using UnityEditor;
#endif

[AddComponentMenu("")]
public class ActionIdentifyTarget : IAction
{
    public enum Action
    {
        Attach
    }

    public TargetCharacter characterExecutioner = new TargetCharacter();
    public TargetCharacter characterTarget = new TargetCharacter();

    public TargetGameObject StartRaycastFrom = new TargetGameObject(TargetGameObject.Target.Invoker);
    public VariableProperty StoreHitColliderTo = new VariableProperty(Variable.VarType.GlobalVariable);
    public NumberProperty RaycastLength = new NumberProperty(3f);
    public bool CheckForTag;
    public string TagName = "";
    public LayerMask RaycastLayer;
    Ray ray;
    bool m_HitDetect;

    RaycastHit hit;
    public Action action = Action.Attach;

    public HumanBodyBones bone = HumanBodyBones.Head;
    public TargetGameObject instance = new TargetGameObject();
    public Space space = Space.Self;
    public Vector3 position = Vector3.zero;
    public Vector3 rotation = Vector3.zero;
    public MeleeClip meleeClipExecution;
    public MeleeClip meleeClipExecuted;

    private float duration = 1.00f;
    private CharacterMelee characterMeleeA;
    private CharacterMelee characterMeleeB;

    private bool wasAControllable;
    private bool wasBControllable;

    private static readonly Vector3 PLANE = new Vector3(1, 0, 1);

    // Start is called before the first frame update
    public override bool InstantExecute(GameObject target, IAction[] actions, int index)
    {
        #region FireRayCast
        var gameObject = StartRaycastFrom.GetGameObject(target);

        if(gameObject == null) return false;

        var RayDistance = RaycastLength.GetValue(target);

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        m_HitDetect = false;
        RaycastHit[] allHits = Physics.RaycastAll(transform.position + Vector3.up, transform.forward, 10, -1, QueryTriggerInteraction.Ignore);
        Debug.DrawRay(transform.position + Vector3.up, transform.forward * 10, Color.red, RayDistance);
        System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
        foreach (RaycastHit rayHit in allHits)
        {
            if (rayHit.transform == transform) { continue; }

            m_HitDetect = true;
            hit = rayHit;
            break;
        }

        //Draw a cube at the maximum distance
        // Debug.DrawWireCube(transform.position + transform.forward * 10.0f, transform.localScale);

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

        #region HandleMeleeClip


        Character executioner = this.characterExecutioner.GetCharacter(target);
        Character targetChar = this.characterTarget.GetCharacter(target);

        if(targetChar != null && executioner != null) {
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
                meleeClipExecution.Play(characterMeleeA);
                meleeClipExecuted.Play(characterMeleeB);

                this.StoreHitColliderTo.Set(null, null);
                return true;
            }
            return false;
        }
        #endregion

        return true;
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

	    public static new string NAME = "Character/Identify Target";
        private const string NODE_TITLE = "{0} from {1}";

		// PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spCharacterExecutioner;
		private SerializedProperty spCharacterTarget;
        private SerializedProperty spAction;

        private SerializedProperty spBone;
        private SerializedProperty spInstance;
        private SerializedProperty spSpace;
        private SerializedProperty spPosition;
        private SerializedProperty spRotation;

        private SerializedProperty spmeleeClipExecution;
        private SerializedProperty spmeleeClipExecuted;
        private SerializedProperty spattachRayCastFrom;
        private SerializedProperty sprayCastLength;
        private SerializedProperty spstoreHitColliderTo;
        private SerializedProperty spRayCastLayer;





		// INSPECTOR METHODS: ---------------------------------------------------------------------

		public override string GetNodeTitle()
		{
			return string.Format(
                NODE_TITLE, 
                this.action.ToString(),
                this.bone.ToString()
            );
		}

		protected override void OnEnableEditorChild ()
		{
            this.spCharacterExecutioner = this.serializedObject.FindProperty("characterExecutioner");
            this.spCharacterTarget = this.serializedObject.FindProperty("characterTarget");
            this.spAction = this.serializedObject.FindProperty("action");

            this.spBone = this.serializedObject.FindProperty("bone");
            this.spInstance = this.serializedObject.FindProperty("instance");
            this.spSpace = serializedObject.FindProperty("space");
            this.spPosition = this.serializedObject.FindProperty("position");
            this.spRotation = this.serializedObject.FindProperty("rotation");
            this.spmeleeClipExecuted = this.serializedObject.FindProperty("meleeClipExecuted");
            this.spmeleeClipExecution = this.serializedObject.FindProperty("meleeClipExecution");

            this.spattachRayCastFrom = this.serializedObject.FindProperty("StartRaycastFrom");
            this.sprayCastLength = this.serializedObject.FindProperty("RaycastLength");
            this.spstoreHitColliderTo = this.serializedObject.FindProperty("StoreHitColliderTo");
            this.spRayCastLayer = this.serializedObject.FindProperty("RaycastLayer");
		}

		protected override void OnDisableEditorChild ()
		{
            this.spCharacterExecutioner = null;
            this.spCharacterTarget = null;
            this.spmeleeClipExecuted = null;
            this.spmeleeClipExecution = null;
            this.spAction = null;

            this.spBone = null;
            this.spInstance = null;
            this.spSpace = null;
            this.spPosition = null;
            this.spRotation = null;
		}

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();
            EditorGUILayout.LabelField("Executioner", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.spCharacterExecutioner);
            EditorGUILayout.PropertyField(this.spmeleeClipExecution);
            EditorGUILayout.PropertyField(this.spattachRayCastFrom);
            EditorGUILayout.PropertyField(this.sprayCastLength);
            EditorGUILayout.PropertyField(this.spstoreHitColliderTo);
            EditorGUILayout.PropertyField(this.spRayCastLayer);

            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Executed", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.spCharacterTarget);
            EditorGUILayout.PropertyField(this.spmeleeClipExecuted);
            EditorGUILayout.PropertyField(this.spAction);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spBone);
            if (this.spAction.intValue == (int)Action.Attach)
            {
                EditorGUILayout.PropertyField(this.spInstance);
                EditorGUILayout.PropertyField(this.spSpace);
                EditorGUILayout.PropertyField(this.spPosition);
                EditorGUILayout.PropertyField(this.spRotation);
            }

			this.serializedObject.ApplyModifiedProperties();
		}

#endif
}

