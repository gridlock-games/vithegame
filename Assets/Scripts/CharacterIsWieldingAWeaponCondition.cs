namespace GameCreator.Core
{
    using GameCreator.Characters;
    using GameCreator.Melee;
    using UnityEngine;

    [AddComponentMenu("")]
    public class CharacterIsWieldingAWeaponCondition : ICondition
    {
        public TargetCharacter Character = new TargetCharacter();
        public MeleeWeapon MeleeWeapon;

        public override bool Check(GameObject target)
        {
            Character charTarget = this.Character.GetCharacter(target);
            CharacterMelee melee = charTarget.GetComponent<CharacterMelee>();
            return melee.currentWeapon == MeleeWeapon;
        }

#if UNITY_EDITOR
        public static new string NAME = "Custom/Character Is Wielding A Weapon";

		public override string GetNodeTitle()
        {
            return "Character is wielding a weapon";
        }
#endif
    }
}
