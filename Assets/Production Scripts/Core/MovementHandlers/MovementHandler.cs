using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Utility;
using Vi.Core.GameModeManagers;
using UnityEngine.AI;
using Vi.Core.Structures;

namespace Vi.Core.MovementHandlers
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(PooledObject))]
	[RequireComponent(typeof(ObjectiveHandler))]
	public abstract class MovementHandler : NetworkBehaviour
    {
        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        public static Quaternion IsolateYRotation(Quaternion input)
		{
			return Quaternion.Euler(0, input.eulerAngles.y, 0);
		}

		public static readonly string[] layersToAccountForInMovement = new string[]
		{
			"Default",
			"ProjectileCollider"
		};

		protected List<NetworkInteractable> interactablesInRange { get; private set; } = new List<NetworkInteractable>();
		public void SetInteractableInRange(NetworkInteractable interactable, bool isInRange)
		{
			if (isInRange)
			{
				interactablesInRange.Add(interactable);
			}
			else
			{
				interactablesInRange.Remove(interactable);
            }
		}

		public bool TryGetNetworkInteractableInRange(out NetworkInteractable networkInteractable)
		{
			networkInteractable = null;
			foreach (NetworkInteractable netInter in interactablesInRange)
			{
				networkInteractable = netInter;
                return true;
            }
			return false;
		}

		public virtual void SetOrientation(Vector3 newPosition, Quaternion newRotation)
		{
			if (!IsServer) { Debug.LogError("MovementHandler.SetOrientation should only be called on the server!"); return; }
			transform.position = newPosition;
			transform.rotation = newRotation;

			if (IsSpawned)
            {
				TeleportPositionRpc(newPosition);
			}
		}

		[Rpc(SendTo.NotServer)] protected virtual void TeleportPositionRpc(Vector3 newPosition) { transform.position = newPosition; }

        public virtual Vector3 GetPosition() { return transform.position; }

		private Quaternion cachedRotation;
		public virtual Quaternion GetRotation() { return cachedRotation; }

		public virtual Vector2[] GetMoveInputQueue() { return new Vector2[0]; }

		public virtual void ReceiveOnCollisionEnterMessage(Collision collision) { }
		public virtual void ReceiveOnCollisionStayMessage(Collision collision) { }
		public virtual void ReceiveOnCollisionExitMessage(Collision collision) { }

		public Vector3 BodyHeightOffset { get { return new Vector3(0, bodyHeightOffset, 0); } }
		[SerializeField] private float bodyHeightOffset = 2;

		protected Vector3 GetRandomDestination()
        {
			Vector3 randomDirection = Random.insideUnitSphere * Random.Range(1, destinationNavMeshDistanceThreshold);
			randomDirection += GetPosition();
			if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, destinationNavMeshDistanceThreshold, navMeshQueryFilter))
            {
				return hit.position;
			}
			else
            {
				Debug.LogError("Unable to get random destination! " + name);
				return GetPosition();
            }
		}

		[Header("Movement Handler")]
		[SerializeField] protected float stoppingDistance = 1;
		protected Vector3 Destination { get { return destination.Value; } }
		private NetworkVariable<Vector3> destination = new NetworkVariable<Vector3>();

		private const float destinationNavMeshDistanceThreshold = 20;
		public bool SetDestination(Vector3 destination)
        {
			if (!IsSpawned) { return false; }
			if (!IsServer) { Debug.LogError("MovementHandler.SetDestination() should only be called on the server!"); return false; }

			if (NavMesh.SamplePosition(destination, out NavMeshHit myNavHit, destinationNavMeshDistanceThreshold, navMeshQueryFilter))
			{
				this.destination.Value = myNavHit.position;
				return true;
			}
			else
			{
				//Debug.LogWarning("Destination point is not on nav mesh! " + name);
				this.destination.Value = destination;
				return false;
			}
		}

		public bool SetDestination(CombatAgent combatAgent)
        {
			if (!combatAgent) { Debug.LogError("Combat agent is null! " + combatAgent); return false; }
            if (combatAgent.NetworkCollider)
            {
				Vector3 destinationPoint = combatAgent.NetworkCollider.GetClosestPoint(GetPosition());

                if (NavMesh.SamplePosition(destinationPoint, out NavMeshHit myNavHit, destinationNavMeshDistanceThreshold, navMeshQueryFilter))
				{
					destination.Value = myNavHit.position;
					return true;
				}
				else
				{
					//Debug.LogWarning("Destination point is not on nav mesh! " + name);
					destination.Value = destinationPoint;
					return false;
				}
			}
			else
            {
				Debug.LogError("Combat agent has no network collider! " + combatAgent);
			}
            return false;
        }

		public bool SetDestination(Structure structure)
        {
			if (!structure) { Debug.LogError("Structure is null! " + structure); return false; }
			
			Vector3 destinationPoint = structure.GetClosestPoint(GetPosition());

			if (NavMesh.SamplePosition(destinationPoint, out NavMeshHit myNavHit, destinationNavMeshDistanceThreshold, navMeshQueryFilter))
			{
				destination.Value = myNavHit.position;
				return true;
			}
			else
			{
				Debug.LogWarning("Destination point is not on nav mesh! " + name);
				destination.Value = destinationPoint;
				return false;
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

		protected bool CalculatePath(Vector3 startPosition)
        {
			if (!IsSpawned) { return false; }
			if (!IsServer) { Debug.LogError("MovementHandler.CalculatePath() should only be called on the server!"); return false; }

			if (NavMesh.SamplePosition(startPosition, out NavMeshHit hit, startPositionNavMeshDistanceThreshold, navMeshQueryFilter))
            {
				startPosition = hit.position;
				if (NavMesh.CalculatePath(startPosition, Destination, navMeshQueryFilter, path))
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
					//Debug.LogWarning("Path calculation failed! " + name);
					//SetOrientation(myNavHit.position, transform.rotation);
					nextPosition.Value = Destination;
					return false;
				}
			}
			else
            {
				// Uncomment this to force bots to stay on nav mesh at all times
				//Debug.LogWarning("Start Position is not on navmesh! " + name);
				//if (NavMesh.SamplePosition(startPosition, out NavMeshHit myNavHit, Mathf.Infinity, navMeshQueryFilter))
				//{
				//	SetOrientation(myNavHit.position, transform.rotation);
				//}
				nextPosition.Value = Destination;
				return false;
            }
		}

		NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter()
		{
			agentTypeID = 0,
			areaMask = NavMesh.AllAreas
		};

        public ObjectiveHandler ObjectiveHandler { get; private set; }

        protected WeaponHandler weaponHandler;
		protected PlayerInput playerInput;
		protected InputAction moveAction;
		protected InputAction lookAction;

		[SerializeField] private string navMeshAgentTypeName = "Humanoid";

		private int navMeshAgentTypeID;

        protected virtual void Awake()
        {
            ObjectiveHandler = GetComponent<ObjectiveHandler>();

            path = new NavMeshPath();
			weaponHandler = GetComponent<WeaponHandler>();
			playerInput = GetComponent<PlayerInput>();
			if (playerInput)
            {
				moveAction = playerInput.actions.FindAction("Move");
				lookAction = playerInput.actions.FindAction("Look");
            }

			bool agentTypeFound = false;
			for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
			{
				int agentTypeID = NavMesh.GetSettingsByIndex(i).agentTypeID;
				if (navMeshAgentTypeName == NavMesh.GetSettingsNameFromID(agentTypeID))
                {
					navMeshAgentTypeID = agentTypeID;
					agentTypeFound = true;
					navMeshQueryFilter = new NavMeshQueryFilter()
					{
						agentTypeID = navMeshAgentTypeID,
						areaMask = NavMesh.AllAreas
					};
					break;
                }
            }

			if (!agentTypeFound)
            {
				Debug.LogError(this + " agent type not found! " + navMeshAgentTypeName);
			}
        }

        protected virtual void OnEnable()
		{
			RefreshStatus();
		}

        public override void OnNetworkSpawn()
        {
			RefreshStatus();
			if (!NetworkObject.IsPlayerObject & IsServer)
            {
				SetDestination(transform.position);
				CalculatePath(transform.position);
			}
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

		protected virtual void LateUpdate()
		{
            cachedRotation = transform.rotation;
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

			if (GameModeManager.Singleton)
			{
				if (GameModeManager.Singleton.GetPostGameStatus() != GameModeManager.PostGameStatus.None) { return Vector2.zero; }
			}

			bool shouldUseZoomSensMultiplier = false;
            if (weaponHandler) { shouldUseZoomSensMultiplier = weaponHandler.IsAiming(); }
            
			Vector2 adjustedLookInput = shouldUseZoomSensMultiplier ? lookInput * zoomSensitivityMultiplier : lookInput;
			adjustedLookInput *= lookSensitivity;
			adjustedLookInput /= QualitySettings.resolutionScalingFixedDPIFactor > 0 ? QualitySettings.resolutionScalingFixedDPIFactor : 1;
            return adjustedLookInput;
        }

		public void ResetLookInput()
        {
			if (FasterPlayerPrefs.IsMobilePlatform)
            {
				lookInput = Vector2.zero;
			}
        }

		public void SetLookInput(Vector2 lookInput)
		{
			this.lookInput += lookInput;
		}

        public Vector2 GetPlayerMoveInput()
		{
			if (moveAction != null)
			{
				if (!moveAction.enabled) { return Vector2.zero; }
			}
			return moveInput;
		}

		public Vector2 GetPathMoveInput(bool useTransform)
        {
			if (Vector3.Distance(Destination, useTransform ? transform.position : GetPosition()) < stoppingDistance) { return Vector2.zero; }
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

        protected virtual void OnDrawGizmos()
        {
			if (Application.isPlaying)
			{
				if (TryGetComponent(out CombatAgent combatAgent))
				{
					if (combatAgent.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death) { return; }
				}

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
			else
			{
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(transform.position + transform.rotation * new Vector3(0, bodyHeightOffset, 0), 0.5f);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, transform.forward * stoppingDistance);
            }
        }
	}
}