using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Utility;

namespace Vi.Core
{
    public class MovementHandler : NetworkBehaviour
    {
        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);

		public static readonly string[] layersToAccountForInMovement = new string[]
		{
			"Default",
			"Projectile"
		};

        public virtual void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            transform.position = newPosition;
            transform.rotation = newRotation;
        }

        public virtual void ReceiveOnCollisionEnterMessage(Collision collision) { }
        public virtual void ReceiveOnCollisionStayMessage(Collision collision) { }
        public virtual void ReceiveOnCollisionExitMessage(Collision collision) { }

		public virtual void AddForce(Vector3 force) { }

        protected WeaponHandler weaponHandler;
        protected void Awake()
        {
            weaponHandler = GetComponent<WeaponHandler>();
        }

		public virtual void Flinch(Vector2 flinchAmount) { }

        protected Vector2 lookInput;
        public Vector2 GetLookInput()
        {
            Vector2 lookSensitivity = new Vector2(FasterPlayerPrefs.Singleton.GetFloat("MouseXSensitivity"), FasterPlayerPrefs.Singleton.GetFloat("MouseYSensitivity")) * (bool.Parse(FasterPlayerPrefs.Singleton.GetString("InvertMouse")) ? -1 : 1);
            if (weaponHandler)
            {
                if (weaponHandler.IsAiming()) { lookSensitivity *= FasterPlayerPrefs.Singleton.GetFloat("ZoomSensitivityMultiplier"); }
            }
            return lookInput * lookSensitivity;
        }

		public void ResetLookInput()
        {
			if (Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer)
            {
				lookInput = Vector2.zero;
			}
        }

		public void SetLookInput(Vector2 lookInput) { this.lookInput += lookInput; }

        public Vector2 GetMoveInput() { return moveInput; }

		public void SetMoveInput(Vector2 moveInput) { this.moveInput = moveInput; }

        protected Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private NetworkVariable<bool> canMove = new NetworkVariable<bool>(true);

        public bool CanMove() { return canMove.Value; }

        public void SetCanMove(bool canMove)
        {
            if (!IsServer) { Debug.LogError("MovementHandler.SetCanMove() should only be called on the server!"); return; }
            this.canMove.Value = canMove;
        }
	}

	public static class ExtDebug
	{
		//Draws just the box at where it is currently hitting.
		public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float hitInfoDistance, Color color, float duration)
		{
			origin = CastCenterOnCollision(origin, direction, hitInfoDistance);
			DrawBox(origin, halfExtents, orientation, color, duration);
		}

		//Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
		public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float distance, Color color, float duration)
		{
			direction.Normalize();
			Box bottomBox = new Box(origin, halfExtents, orientation);
			Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

			Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color, duration);
			Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color, duration);
			Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color, duration);
			Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color, duration);
			Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color, duration);
			Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color, duration);
			Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color, duration);
			Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color, duration);

			DrawBox(bottomBox, color, duration);
			DrawBox(topBox, color, duration);
		}

		public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color, float duration)
		{
			DrawBox(new Box(origin, halfExtents, orientation), color, duration);
		}

		public static void DrawBox(Box box, Color color, float duration)
		{
			Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color, duration);
			Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color, duration);
			Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color, duration);
			Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color, duration);

			Debug.DrawLine(box.backTopLeft, box.backTopRight, color, duration);
			Debug.DrawLine(box.backTopRight, box.backBottomRight, color, duration);
			Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color, duration);
			Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color, duration);

			Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color, duration);
			Debug.DrawLine(box.frontTopRight, box.backTopRight, color, duration);
			Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color, duration);
			Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color, duration);
		}

		public struct Box
		{
			public Vector3 localFrontTopLeft { get; private set; }
			public Vector3 localFrontTopRight { get; private set; }
			public Vector3 localFrontBottomLeft { get; private set; }
			public Vector3 localFrontBottomRight { get; private set; }
			public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
			public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
			public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
			public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

			public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
			public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
			public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
			public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
			public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
			public Vector3 backTopRight { get { return localBackTopRight + origin; } }
			public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
			public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

			public Vector3 origin { get; private set; }

			public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
			{
				Rotate(orientation);
			}
			public Box(Vector3 origin, Vector3 halfExtents)
			{
				this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
				this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
				this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
				this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

				this.origin = origin;
			}


			public void Rotate(Quaternion orientation)
			{
				localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
				localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
				localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
				localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
			}
		}

		//This should work for all cast types
		static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
		{
			return origin + (direction.normalized * hitInfoDistance);
		}

		static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
		{
			Vector3 direction = point - pivot;
			return pivot + rotation * direction;
		}
	}
}