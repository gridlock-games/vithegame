using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreator.Core;
using GameCreator.Camera;
using GameCreator.Variables;


#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("")]
public class ActionOverrideAdventureMotorPosition : IAction
{
    public CameraMotor cameraMotor;
    public GameObject targetPlayer;
    private float duration = 0.2f;

        

    // EXECUTABLE: ----------------------------------------------------------------------------


    public override bool InstantExecute(GameObject target, IAction[] actions, int index)
    {
        CameraMotor motor = this.cameraMotor;
        if (motor != null && motor.cameraMotorType.GetType() == typeof(CameraMotorTypeAdventure))
        {
            CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;
            StartCoroutine(this.MoveMotorRoutine());
        }

        return true;
    }

    public IEnumerator MoveMotorRoutine()
    {
        CameraMotor motor = this.cameraMotor;
        if (motor != null && motor.cameraMotorType.GetType() == typeof(CameraMotorTypeAdventure))
        {
            float initTime = Time.time;
            CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;

            Vector3 aTargetOffset = adventureMotor.targetOffset;
            Vector3 aPivotOffset = adventureMotor.pivotOffset;

            Vector3 playerPosition = this.targetPlayer.transform.position;

            while (initTime + this.duration >= Time.time)
            {
                float t = Mathf.Clamp01((Time.time - initTime) / this.duration);
                adventureMotor.targetOffset = playerPosition;

                yield return null;
            }

            // adventureMotor.targetOffset = aTargetOffset;
            // adventureMotor.pivotOffset = aPivotOffset;
        }

        yield return 0;
    }


    // +--------------------------------------------------------------------------------------+
    // | EDITOR                                                                               |
    // +--------------------------------------------------------------------------------------+
    #if UNITY_EDITOR
    public new static string NAME = "Camera/Override Adventure Motor Position";
    private const string NODE_TITLE = "Set Adventure Motor Position";
    #endif
}
