using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using GameCreator.Core;
using GameCreator.Characters;
using GameCreator.Variables;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ActionUpdateAilment : IAction
{

    public CharacterLocomotion.CHARACTER_AILMENTS characterAilment;
    public TargetCharacter target = new TargetCharacter(TargetCharacter.Target.Invoker);
    // Start is called before the first frame update
    public override bool InstantExecute(GameObject target, IAction[] actions, int index)
    {
        Character charTarget = this.target.GetCharacter(target);
        if (charTarget != null) {
            charTarget.UpdateAilment(characterAilment, null);
        }
        return true;
    }
    
    // +--------------------------------------------------------------------------------------+
    // | EDITOR                                                                               |
    // +--------------------------------------------------------------------------------------+

    #if UNITY_EDITOR

    public static new string NAME = "Character/Change Ailment";
    private const string NODE_TITLE = "Change {0} {1}";

    // PROPERTIES: ----------------------------------------------------------------------------

    private SerializedProperty spTarget;
    private SerializedProperty spCharacterAilment;

    // INSPECTOR METHODS: ---------------------------------------------------------------------

    public override string GetNodeTitle()
    {
        return string.Format(
            NODE_TITLE,
            this.target,
            this.spCharacterAilment
        );
    }

    protected override void OnEnableEditorChild ()
    {
        this.spTarget = this.serializedObject.FindProperty("target");
        this.spCharacterAilment = this.serializedObject.FindProperty("characterAilment");
    }

    protected override void OnDisableEditorChild ()
    {
        this.spTarget = null;
        this.spCharacterAilment = null;
    }

    public override void OnInspectorGUI()
    {
        this.serializedObject.Update();

        EditorGUILayout.PropertyField(this.spTarget);
        EditorGUILayout.PropertyField(this.spCharacterAilment);
        this.serializedObject.ApplyModifiedProperties();
    }

    #endif
}
