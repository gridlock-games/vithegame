using UnityEngine;
using GameCreator.Melee;

public class Ability : MonoBehaviour
{

    public enum AbilityType {
        Active,
        Passive,
        TeamBuff,
        TeamDebuff,
        SelfBuff,
        Debuff,
    }

    public Sprite skillImageFill;
    public MeleeClip meleeClip;
    public AbilityType abilityType = AbilityType.Active;
    public float coolDown = 0.00f;
}
