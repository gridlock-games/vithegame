using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Utility;
using Vi.Core.GameModeManagers;
using UnityEngine.AI;

namespace Vi.Core
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(PooledObject))]
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
			transform.position = newPosition;
			transform.rotation = newRotation;
		}

        public virtual Vector3 GetPosition() { return transform.position; }

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

		protected WeaponHandler weaponHandler;
		protected PlayerInput playerInput;
		protected InputAction moveAction;
		protected InputAction lookAction;

        protected virtual void Awake()
		{
			path = new NavMeshPath();
			weaponHandler = GetComponent<WeaponHandler>();
			playerInput = GetComponent<PlayerInput>();
			if (playerInput)
            {
				moveAction = playerInput.actions.FindAction("Move");
				lookAction = playerInput.actions.FindAction("Look");
            }
        }

        protected virtual void OnEnable()
		{
			RefreshStatus();
			SetDestination(transform.position, true);
			CalculatePath(transform.position, NavMesh.AllAreas);
		}

        public override void OnNetworkSpawn()
        {
			RefreshStatus();
		}

        protected virtual void OnDisable()
		{
			IsAffectedByExternalForce = false;
		}

		private Vector2 lookSensitivity;
		protected virtual void RefreshStatus()
		{
			lookSensitivity = new Vector2(FasterPlayerPrefs.Singleton.GetFloat("MouseXSensitivity"), FasterPlayerPrefs.Singleton.GetFloat("MouseYSensitivity")) * (FasterPlayerPrefs.Singleton.GetBool("InvertMouse") ? -1 : 1);
			zoomSensitivityMultiplier = FasterPlayerPrefs.Singleton.GetFloat("ZoomSensitivityMultiplier");
		}

		protected virtual void Update()
        {
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
}