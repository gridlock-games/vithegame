using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreator.Melee;
using static GameCreator.Melee.MeleeClip;
using GameCreator.Core;
using GameCreator.Characters;

[AddComponentMenu("")]
public class ActionPlayMeleeClip : IAction
{
    [SerializeField] private MeleeClip meleeclip;
    [SerializeField] private TargetCharacter character = new TargetCharacter();
    private CharacterMelee characterMelee;

    public override bool InstantExecute(GameObject target, IAction[] actions, int index)
    {
        Character charTarget = this.character.GetCharacter(target);
        if (charTarget != null)
        {
            characterMelee = charTarget.GetComponent<CharacterMelee>();
            if (characterMelee != null)
            {
                meleeclip.Play(characterMelee);
                return true;
            }
            return false;
        }
        return false;
    }
#if UNITY_EDITOR

    public static new string NAME = "Custom/ActionPlayMeleeClip";
    public override string GetNodeTitle()
    {
        string NODE_TITLE = "Play Melee Clip";
        return string.Format(NODE_TITLE);
    }
#endif
}
