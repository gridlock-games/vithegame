using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetDevTools
{
    public class TestPlayerController : MonoBehaviour
    {
        [SerializeField] private Camera cameraInstance;
        [SerializeField] private Animator animator;

        private Vector2 sensitivity = new Vector2(1, 1);
        private Vector2 lookInput;

        public Vector2 GetLookInput()
        {
            return lookInput * sensitivity;
        }

        private void Update()
        {
            lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            if (Input.mouseScrollDelta != Vector2.zero)
            {
                sensitivity.x += Input.mouseScrollDelta.y;
                sensitivity.y += Input.mouseScrollDelta.y;
                Debug.Log("Updating sensitivity " + sensitivity);
            }
            
            ProcessMovement();
        }

        private const float movementSpeed = 5;
        private const float angularSpeed = 540;
        private const float runAnimationTransitionSpeed = 5;
        [SerializeField] private Vector3 gravitySphereCastPositionOffset = new Vector3(0, 0.6f, 0);
        [SerializeField] private float gravitySphereCastRadius = 0.6f;

        private void ProcessMovement()
        {
            Vector3 camDirection = cameraInstance.transform.TransformDirection(Vector3.forward);
            camDirection.Scale(Vi.Core.MovementHandler.HORIZONTAL_PLANE);
            Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(camDirection), Time.deltaTime * angularSpeed);

            // Handle gravity
            Vector3 gravity = Vector3.zero;
            RaycastHit[] allHits = Physics.SphereCastAll(transform.position + transform.rotation * gravitySphereCastPositionOffset,
                gravitySphereCastRadius, Physics.gravity,
                gravitySphereCastPositionOffset.magnitude, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            bool bHit = false;
            foreach (RaycastHit gravityHit in allHits)
            {
                gravity += Time.deltaTime * Mathf.Clamp01(gravityHit.distance) * Physics.gravity;
                bHit = true;
                break;
            }

            bool isGrounded;
            if (bHit)
            {
                isGrounded = true;
            }
            else // If no sphere cast hit
            {
                if (Physics.Raycast(transform.position + transform.rotation * gravitySphereCastPositionOffset,
                    Physics.gravity, 1, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
                {
                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                    gravity += Time.deltaTime * Physics.gravity;
                }
            }

            Vector2 inputVector = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) { inputVector.y = 1; }
            if (Input.GetKey(KeyCode.A)) { inputVector.x = -1; }
            if (Input.GetKey(KeyCode.S)) { inputVector.y = -1; }
            if (Input.GetKey(KeyCode.D)) { inputVector.x = 1; }
            inputVector = inputVector.normalized;

            Vector3 targetDirection = newRotation * new Vector3(inputVector.x, 0, inputVector.y);
            targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, Vi.Core.MovementHandler.HORIZONTAL_PLANE), 1);
            targetDirection *= isGrounded ? 1 : 0;
            Vector3 movement = targetDirection * Time.deltaTime * movementSpeed;
            Vector3 animDir = new Vector3(targetDirection.x, 0, targetDirection.z);

            float stairMovement = 0;
            float yOffset = 0.2f;
            Vector3 startPos = transform.position;
            startPos.y += yOffset;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }

                Debug.DrawRay(startPos, movement.normalized, Color.cyan, Time.deltaTime);
                startPos.y += yOffset;
                stairMovement = startPos.y - transform.position.y - yOffset;

                if (stairMovement > 0.5f)
                {
                    stairMovement = 0;
                    break;
                }
            }

            movement.y += stairMovement;

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));

            animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), animDir.z, Time.deltaTime * runAnimationTransitionSpeed));
            animator.SetFloat("MoveSides", Mathf.MoveTowards(animator.GetFloat("MoveSides"), animDir.x, Time.deltaTime * runAnimationTransitionSpeed));

            transform.rotation = newRotation;

            transform.position += movement + gravity;
        }
    }
}