namespace GameCreator.Camera
{
    using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using GameCreator.Core;
    using GameCreator.Variables;


    #if UNITY_EDITOR
	using UnityEditor;
    #endif

    [AddComponentMenu("")]
    public class ActionOverrideMotorPosition : IAction
    {
        public CameraMotor cameraMotor;
        public GameObject targetPlayer;
        // private float duration = 0.2f;

        

        // EXECUTABLE: ----------------------------------------------------------------------------


        // public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        // {
        //     CameraMotor motor = this.cameraMotor;
        //     if (motor != null && motor.cameraMotorType.GetType() == typeof(CameraMotorTypeAdventure))
        //     {
        //         CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;
        //     }

        //     return true;
        // }

        // public override IEnumerator Execute(GameObject target, IAction[] actions, int index)
        // {
        //     CameraMotor motor = this.cameraMotor;
        //     if (motor != null && motor.cameraMotorType.GetType() == typeof(CameraMotorTypeAdventure))
        //     {
        //         float initTime = Time.time;
        //         CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;

        //         Vector3 aTargetOffset = adventureMotor.targetOffset;
        //         Vector3 aPivotOffset = adventureMotor.pivotOffset;

        //         var foo = this.targetPlayer;

        //         while (initTime + this.duration >= Time.time)
        //         {
        //             // float t = Mathf.Clamp01((Time.time - initTime) / this.duration);
        //             // adventureMotor.targetOffset = new Vector3(10f, 0f, 10f);

        //             yield return null;
        //         }

        //         adventureMotor.targetOffset = aTargetOffset;
        //         adventureMotor.pivotOffset = aPivotOffset;
        //     }

        //     yield return 0;
        // }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+
        #if UNITY_EDITOR
        public new static string NAME = "Camera/Override Motor Position";
        private const string NODE_TITLE = "Set Camera Motor Position";
        #endif

    }
}