using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Utility;
using Vi.Core.GameModeManagers;
using UnityEngine.AI;
using Vi.ScriptableObjects;

namespace Vi.Core
{
	public abstract class MovementHandler : NetworkBehaviour
	{
		public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);

		public static readonly string[] layersToAccountForInMovement = new string[]
		{
			"Default",
			"ProjectileCollider"
		};

		public virtual void SetOrientation(Vector3 newPosition, Quaternion newRotation)
		{
			if (rb)
			{
				rb.position = newPosition;
				rb.velocity = Vector3.zero;
			}
			transform.position = newPosition;
			transform.rotation = newRotation;
		}

        public override void OnNetworkSpawn()
        {
			rb.interpolation = IsClient ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
			rb.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
		}

        public virtual Vector3 GetPosition() { return rb.position; }

		public virtual Quaternion GetRotation() { return transform.rotation; }

		public virtual void OnServerActionClipPlayed() { }

		public virtual void ReceiveOnCollisionEnterMessage(Collision collision) { }
		public virtual void ReceiveOnCollisionStayMessage(Collision collision) { }
		public virtual void ReceiveOnCollisionExitMessage(Collision collision) { }

		protected static readonly Vector3 bodyHeightOffset = new Vector3(0, 1, 0);
		protected const float bodyRadius = 0.5f;

		[SerializeField] protected float stoppingDistance = 2;
		protected Vector3 Destination { get { return destination.Value; } }
		private NetworkVariable<Vector3> destination = new NetworkVariable<Vector3>();

		private const float destinationNavMeshDistanceThreshold = 20;
		protected bool SetDestination(Vector3 destination, bool useExactDestination)
        {
			if (!IsSpawned) { return false; }
			if (!IsServer) { Debug.LogError("MovementHandler.SetDestination() should only be called on the server!"); return false; }

			if (useExactDestination)
            {
				if (NavMesh.SamplePosition(destination, out NavMeshHit myNavHit, destinationNavMeshDistanceThreshold, NavMesh.AllAreas))
				{
					this.destination.Value = myNavHit.position;
					return true;
				}
				else
				{
                    Debug.LogError("Destination point is not on nav mesh! " + name);
                    this.destination.Value = destination;
					return false;
				}
			}
			else
            {
				if (NavMesh.SamplePosition(destination - (destination - GetPosition()).normalized, out NavMeshHit myNavHit, destinationNavMeshDistanceThreshold, NavMesh.AllAreas))
				{
					this.destination.Value = myNavHit.position;
					return true;
				}
				else
				{
                    Debug.LogError("Destination point is not on nav mesh! " + name);
					this.destination.Value = destination;
					return false;
				}
			}
        }

		protected bool IsAffectedByExternalForce { get; private set; }
		public void ExternalForceAffecting()
        {
			IsAffectedByExternalForce = true;
			if (resetIsAffectedByExternalForceCoroutine != null) { StopCoroutine(resetIsAffectedByExternalForceCoroutine); }
			resetIsAffectedByExternalForceCoroutine = StartCoroutine(ResetIsAffectedByExternalForce());
        }

		private Coroutine resetIsAffectedByExternalForceCoroutine;
		private IEnumerator ResetIsAffectedByExternalForce()
        {
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			IsAffectedByExternalForce = false;
        }

        

        private NavMeshPath path;
		protected Vector3 NextPosition { get { return nextPosition.Value; } }
		private NetworkVariable<Vector3> nextPosition = new NetworkVariable<Vector3>();

		private const float nextPositionAngleThreshold = 10;
		private const float nextPositionDistanceThreshold = 1;
		private const float startPositionNavMeshDistanceThreshold = 20;

		protected bool CalculatePath(Vector3 startPosition, int areaMask)
        {
			if (!IsSpawned) { return false; }
			if (!IsServer) { Debug.LogError("MovementHandler.CalculatePath() should only be called on the server!"); return false; }

			if (NavMesh.SamplePosition(startPosition, out NavMeshHit hit, startPositionNavMeshDistanceThreshold, NavMesh.AllAreas))
            {
				startPosition = hit.position;
				if (NavMesh.CalculatePath(startPosition, Destination, areaMask, path))
				{
					// If there is a point in the path that has an angle of 0, use that as our next position
					Vector3 prevCorner = startPosition;
					bool overrideIndexFound = false;
					int overrideIndex = 0;
					for (int i = 0; i < Mathf.Min(path.corners.Length, 3); i++)
					{
						Vector3 corner = path.corners[i];

						Vector3 toTarget = corner - startPosition;
						toTarget.y = 0;

						Vector3 prevTo = corner - prevCorner;
						prevTo.y = 0;

						float angle = Vector3.Angle(prevTo, toTarget);
						if (angle <= nextPositionAngleThreshold)
						{
							if (Mathf.Abs(startPosition.y - corner.y) > nextPositionDistanceThreshold)
                            {
								overrideIndexFound = true;
								overrideIndex = i;
							}
						}

						prevCorner = corner;
					}

					if (overrideIndexFound)
					{
						nextPosition.Value = path.corners[overrideIndex];
						return true;
					}

					if (path.corners.Length > 1)
					{
						nextPosition.Value = path.corners[1];
					}
					else if (path.corners.Length > 0)
					{
						nextPosition.Value = path.corners[0];
					}
					else
					{
						nextPosition.Value = startPosition;
					}
					return true;
				}
				else
				{
					//Debug.LogError("Path calculation failed! " + name);
					//SetOrientation(myNavHit.position, transform.rotation);
					nextPosition.Value = Destination;
					return false;
				}
			}
			else
            {
				Debug.LogError("Start Position is not on navmesh! " + name);
				if (NavMesh.SamplePosition(startPosition, out NavMeshHit myNavHit, Mathf.Infinity, NavMesh.AllAreas))
				{
					SetOrientation(myNavHit.position, transform.rotation);
				}
				nextPosition.Value = Destination;
				return false;
            }
		}

		protected virtual void OnDrawGizmosSelected()
		{
			if (!Application.isPlaying) { return; }

			Vector3 prevCorner = transform.position;
			for (int i = 0; i < path.corners.Length; i++)
            {
				Vector3 corner = path.corners[i];

				Vector3 toTarget = corner - transform.position;
				toTarget.y = 0;

				Vector3 prevTo = corner - prevCorner;
				prevTo.y = 0;

				float angle = Vector3.Angle(prevTo, toTarget);

				Gizmos.color = Color.magenta;
				Gizmos.DrawSphere(corner, 0.25f);
				Gizmos.DrawLine(prevCorner, corner);
				Gizmos.color = Color.black;
#if UNITY_EDITOR
				UnityEditor.Handles.Label(corner + Vector3.up * 2, i + " | " + angle.ToString("F1"));
#endif
				prevCorner = corner;
			}
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(NextPosition, 0.3f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(Destination, 0.3f);
        }

		public Rigidbody GetRigidbody() { return rb; }

		protected WeaponHandler weaponHandler;
		protected PlayerInput playerInput;
		protected InputAction moveAction;
		protected InputAction lookAction;
		protected Rigidbody rb;

        protected void Awake()
		{
			path = new NavMeshPath();
			weaponHandler = GetComponent<WeaponHandler>();
			playerInput = GetComponent<PlayerInput>();
			rb = GetComponentInChildren<Rigidbody>();
			RefreshStatus();
			if (playerInput)
            {
				moveAction = playerInput.actions.FindAction("Move");
				lookAction = playerInput.actions.FindAction("Look");
            }
        }

        protected void OnEnable()
        {
			SetDestination(transform.position, true);
			CalculatePath(transform.position, NavMesh.AllAreas);
			if (!GetComponent<ActionVFX>()) { NetworkPhysicsSimulation.AddRigidbody(rb); }
		}

		private void OnDisable()
		{
			IsAffectedByExternalForce = false;
			if (!GetComponent<ActionVFX>()) { NetworkPhysicsSimulation.RemoveRigidbody(rb); }
		}

		private Vector2 lookSensitivity;
		private void RefreshStatus()
		{
			lookSensitivity = new Vector2(FasterPlayerPrefs.Singleton.GetFloat("MouseXSensitivity"), FasterPlayerPrefs.Singleton.GetFloat("MouseYSensitivity")) * (FasterPlayerPrefs.Singleton.GetBool("InvertMouse") ? -1 : 1);
			zoomSensitivityMultiplier = FasterPlayerPrefs.Singleton.GetFloat("ZoomSensitivityMultiplier");
		}

		protected void Update()
        {
			if (!IsLocalPlayer) { return; }
			if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }
		}

        public virtual void Flinch(Vector2 flinchAmount) { }

		private float zoomSensitivityMultiplier = 1;
        protected Vector2 lookInput;
        public Vector2 GetLookInput()
        {
			if (lookAction != null)
            {
				if (!lookAction.enabled) { return Vector2.zero; }
            }

			bool shouldUseZoomSensMultiplier = false;
            if (weaponHandler) { shouldUseZoomSensMultiplier = weaponHandler.IsAiming(); }
            return shouldUseZoomSensMultiplier ? lookInput * lookSensitivity * zoomSensitivityMultiplier: lookInput * lookSensitivity;
        }

		public void ResetLookInput()
        {
			if (Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer)
            {
				lookInput = Vector2.zero;
			}
        }

		public void SetLookInput(Vector2 lookInput) { this.lookInput += lookInput; }

        public Vector2 GetMoveInput()
		{
			if (moveAction != null)
			{
				if (!moveAction.enabled) { return Vector2.zero; }
			}
			return moveInput;
		}

		public Vector2 GetPathMoveInput()
        {
			Vector3 moveInput = transform.InverseTransformDirection(NextPosition - GetPosition());
			return new Vector2(moveInput.x, moveInput.z).normalized;
		}

		public void SetMoveInput(Vector2 moveInput) { this.moveInput = moveInput; }

        protected Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        public bool CanMove()
		{
			if (GameModeManager.Singleton)
			{
				if (GameModeManager.Singleton.IsGameOver())
                {
					return false;
                }
				else if (GameModeManager.Singleton.GetRespawnType() == GameModeManager.RespawnType.Respawn & GameModeManager.Singleton.ShouldDisplayNextGameAction())
                {
					return false;
                }
				else
                {
					return GameModeManager.Singleton.GetRoundCount() > 0;
                }
			}
			return true;
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

			if (Application.isEditor)
            {
				Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color, duration);
				Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color, duration);
				Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color, duration);
				Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color, duration);
				Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color, duration);
				Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color, duration);
				Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color, duration);
				Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color, duration);
			}
			
			DrawBox(bottomBox, color, duration);
			DrawBox(topBox, color, duration);
		}

		public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color, float duration)
		{
			DrawBox(new Box(origin, halfExtents, orientation), color, duration);
		}

		public static void DrawBox(Box box, Color color, float duration)
		{
			if (Application.isEditor)
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

		public static void DrawWireCapsule(Vector3 p1, Vector3 p2, float radius)
		{
			#if UNITY_EDITOR
			// Special case when both points are in the same position
			if (p1 == p2)
			{
				// DrawWireSphere works only in gizmo methods
				Gizmos.DrawWireSphere(p1, radius);
				return;
			}
			using (new UnityEditor.Handles.DrawingScope(Gizmos.color, Gizmos.matrix))
			{
				Quaternion p1Rotation = Quaternion.LookRotation(p1 - p2);
				Quaternion p2Rotation = Quaternion.LookRotation(p2 - p1);
				// Check if capsule direction is collinear to Vector.up
				float c = Vector3.Dot((p1 - p2).normalized, Vector3.up);
				if (c == 1f || c == -1f)
				{
					// Fix rotation
					p2Rotation = Quaternion.Euler(p2Rotation.eulerAngles.x, p2Rotation.eulerAngles.y + 180f, p2Rotation.eulerAngles.z);
				}
				// First side
				UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.left, p1Rotation * Vector3.down, 180f, radius);
				UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.up, p1Rotation * Vector3.left, 180f, radius);
				UnityEditor.Handles.DrawWireDisc(p1, (p2 - p1).normalized, radius);
				// Second side
				UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.left, p2Rotation * Vector3.down, 180f, radius);
				UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.up, p2Rotation * Vector3.left, 180f, radius);
				UnityEditor.Handles.DrawWireDisc(p2, (p1 - p2).normalized, radius);
				// Lines
				UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.down * radius, p2 + p2Rotation * Vector3.down * radius);
				UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.left * radius, p2 + p2Rotation * Vector3.right * radius);
				UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.up * radius, p2 + p2Rotation * Vector3.up * radius);
				UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.right * radius, p2 + p2Rotation * Vector3.left * radius);
			}
			#endif
		}
	}
}