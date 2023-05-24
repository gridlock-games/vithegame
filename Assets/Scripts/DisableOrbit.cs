namespace GameCreator.Camera
{
	using System.Collections;
	using System.Collections.Generic;
    using System.Threading.Tasks;
	using UnityEngine;
	using GameCreator.Core;
    using GameCreator.Melee;
    using GameCreator.Characters;

	#if UNITY_EDITOR
	using UnityEditor;
	#endif

	[AddComponentMenu("")]
	public class DisableOrbit : IAction
	{
        private float duration = 1.0f;
        private CharacterMelee melee;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            
            Character character = target.GetComponent<Character>();
            this.melee = target.GetComponent<CharacterMelee>();
            CameraMotor motor = CameraMotor.MAIN_MOTOR;
            if (motor != null && motor.cameraMotorType.GetType() == typeof(CameraMotorTypeAdventure))
            {
                CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;
                adventureMotor.allowOrbitInput = false;

                StartCoroutine(this.EnableOrbitRoutine());
            }

            return true;
        }

        public IEnumerator EnableOrbitRoutine()
        {
            CameraMotor motor = CameraMotor.MAIN_MOTOR;
            if (motor != null && motor.cameraMotorType.GetType() == typeof(CameraMotorTypeAdventure))
            {
                float initTime = Time.time;
                CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;

                while (initTime + this.duration >= Time.time)
                {
                    adventureMotor.allowOrbitInput = false;

                    yield return null;
                }

                adventureMotor.allowOrbitInput = true;
            }

            yield return 0;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

        #if UNITY_EDITOR

        public static new string NAME = "Camera/Orbit Camera Settings";
        private const string NODE_TITLE = "Disable Orbit Input";

		// PROPERTIES: ----------------------------------------------------------------------------
        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
            
			return string.Format(NODE_TITLE);
		}

		#endif
	}
}

