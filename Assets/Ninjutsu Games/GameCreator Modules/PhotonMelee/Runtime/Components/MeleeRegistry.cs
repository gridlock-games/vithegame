using System.Collections.Generic;
using GameCreator.Core;
using GameCreator.Melee;
using UnityEngine;

namespace NJG.PUN.Melee
{
    [AddComponentMenu("Game Creator/Melee/Melee Registry (Photon)", 0)]
    public class MeleeRegistry : Singleton<MeleeRegistry>
    {
        public List<MeleeWeapon> weapons = new List<MeleeWeapon>();
        public List<MeleeShield> shields = new List<MeleeShield>();
        public List<MeleeClip> clips = new List<MeleeClip>();
    }
}