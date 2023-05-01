using System.Collections;
using System.Collections.Generic;
using GameCreator.Core;
using GameCreator.Characters;
using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif

public class ConditionCharacterAilment : ICondition
{

    public TargetCharacter target = new TargetCharacter(TargetCharacter.Target.Invoker);
    public CharacterLocomotion.CHARACTER_AILMENTS characterAilments = CharacterLocomotion.CHARACTER_AILMENTS.None;
    // Update is called once per frame
    public override bool Check(GameObject target)
    {
        Character character = this.target.GetCharacter(gameObject);
        if (character == null) return true;

        return true;
    }

    // +--------------------------------------------------------------------------------------+
    // | EDITOR                                                                               |
    // +--------------------------------------------------------------------------------------+

    #if UNITY_EDITOR

    //public const string CUSTOM_ICON_PATH = "Assets/[Custom Path To Icon]";

    public static new string NAME = "Characters/Character Ailment";
    private const string NODE_TITLE = "Character {0} {1}";

    // PROPERTIES: ----------------------------------------------------------------------------

    private SerializedProperty spTarget;
    private SerializedProperty spProperty;

    // INSPECTOR METHODS: ---------------------------------------------------------------------

    public override string GetNodeTitle()
    {
        return string.Format(
            NODE_TITLE, 
            (this.target == null ? "(undefined)" : this.target.ToString()),
            this.characterAilments.ToString()
        );
    }

    protected override void OnEnableEditorChild ()
    {
        this.spTarget = this.serializedObject.FindProperty("target");
        this.spProperty = this.serializedObject.FindProperty("characterAilments");
    }

    public override void OnInspectorGUI()
    {
        this.serializedObject.Update();

        EditorGUILayout.PropertyField(this.spTarget);
        EditorGUILayout.PropertyField(this.spProperty);

        this.serializedObject.ApplyModifiedProperties();
    }

    #endif
}
