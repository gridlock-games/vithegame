using System.Collections;
using System.Collections.Generic;
using GameCreator.Core;
using GameCreator.Characters;
using GameCreator.Melee;
using UnityEngine;

public class IgniterAilment : Igniter
{
    #if UNITY_EDITOR
        public new static string NAME = "Variables/On Ailment Change";
    #endif
    
    public TargetCharacter character = new TargetCharacter(TargetCharacter.Target.Invoker);

    void Start()
    {
        Character target = this.character.GetCharacter(gameObject);
        if (target != null)
        {
            target.onAilmentEvent.RemoveListener(this.OnUpdateAilment);
            target.onAilmentEvent.AddListener(this.OnUpdateAilment);
        }
    }

     private void OnDestroy()
    {
        if (this.isExitingApplication) return;
        Character target = this.character.GetCharacter(gameObject);
        if (target != null)
        {
            target.onAilmentEvent.RemoveListener(this.OnUpdateAilment);
        }
    }

    void OnUpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS characterAilment) {
        Character target = this.character.GetCharacter(gameObject);
        this.ExecuteTrigger(target.gameObject);
    }
}
