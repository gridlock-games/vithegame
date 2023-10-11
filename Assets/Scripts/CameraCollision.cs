using UnityEngine;
using System.Collections;

//THIS SCRIPT IS USED TO PREVENT THE CAMERA FROM GOING THROUGH WALLS
//ATTACH THIS SCRIPT TO A CAMERA
namespace LylekGames.Tools
{
    public class CameraCollision : MonoBehaviour
    {
        public Camera mainCamera;
        [Tooltip("Used as the central focal point. Should be centered on and above the player.")]
        public GameObject focusPoint;
        [Space]
        [Tooltip("Enable this if you are using a smoothFollow camera controller. See the Readme.txt for further information.")]
        public bool useCameraDuplicate;
        [Tooltip("If left empty the camera duplicate will be generated. To preserve camera effects, such as post processing, assign a camera prefab. This prefab should be removed of all camera controls.")]
        public Camera duplicatePrefab;
        [HideInInspector]
        public Camera duplicate;

        [Space]
        [Tooltip("Radius detection. Used to prevent the camera from peering through when standing up against a wall")]
        [Range(0.1f, 0.3f)]
        public float detectionRadius = 0.3f;
        [Tooltip("A 'cushioned' space between the camera and obstacles.")]
        [Range(0.1f, 3.0f)]
        public float cushionOffset = 1.0f;

        [Space]
        [Tooltip("If unchecked collison will only be checked against the first object hit between the focusPoint and camera. If the first object is Masked Tagged for ignore, the second object will also be ignored. This does not apply to Masked Layers.")]
        public bool checkMultipleCollisions = false;
        [Tooltip("Ignore mask.")]
        public string[] maskedTags;
        [Tooltip("Collision mask.")]
        public LayerMask maskedLayers = ~0;

        private Vector3 cameraPosition = new Vector3(0, 0, 0);
        private Vector3 focusPosition = new Vector3(0, 0, 0);
        private Vector3 rayDirection = new Vector3(0, 0, 0);
        float distance = 0;

        private bool _collision = false;

        public bool CollisionDetected
        {
            get
            {
                return _collision;
            }
        }

        private LightPat.Player.NetworkPlayer networkPlayer;
        private void Awake()
        {
            networkPlayer = GetComponentInParent<LightPat.Player.NetworkPlayer>();
        }

        public void Start()
        {
            if (!mainCamera)
            {
                mainCamera = GetComponent<Camera>();
            }
            if (!mainCamera)
            {
                Debug.LogError("MainCamera has not been assigned.");
            }
            if (!focusPoint)
            {
                Debug.LogError("FocusPoint has not been assigned. This should a centered point on your character. See the Readme for details.");
            }

            mainCamera.nearClipPlane = 0.01f;
        }

        public void LateUpdate()
        {
            cameraPosition = mainCamera.transform.position;
            focusPosition = focusPoint.transform.position;
            rayDirection = cameraPosition - focusPosition;
            distance = rayDirection.magnitude;

            if (distance > 0)
            {
                rayDirection = rayDirection / distance;
            }

            if (Physics.SphereCast(focusPosition, detectionRadius, rayDirection, out RaycastHit hit, distance + cushionOffset, maskedLayers) || Physics.Raycast(focusPosition, rayDirection, out hit, distance + cushionOffset, maskedLayers))
            {
                if (hit.transform.GetComponentInParent<GameCreator.Melee.CharacterMelee>()) { return; }

                if (!IsTagged(hit.transform.gameObject))
                {
                    CheckCollision(hit);
                }
                else if (checkMultipleCollisions)
                {
                    RaycastHit[] hits = Physics.SphereCastAll(focusPosition, detectionRadius, rayDirection, distance + cushionOffset, maskedLayers);
                    bool col = false;
                    foreach (RaycastHit h in hits)
                    {
                        if (h.transform.GetComponentInParent<GameCreator.Melee.CharacterMelee>()) { return; }

                        if (!IsTagged(h.transform.gameObject))
                        {
                            CheckCollision(h);
                            col = true;
                            break;
                        }
                    }

                    if (col == false && _collision == true) { EndCollision(); }
                }
                else
                {
                    if (_collision == true) { EndCollision(); }
                }
            }
            else
            {
                if (_collision == true) { EndCollision(); }
            }
        }

        public void EndCollision()
        {
            if (duplicate != null)
                duplicate.enabled = false;
            mainCamera.enabled = true;

            _collision = false;
        }

        public bool IsTagged(GameObject hit)
        {
            bool tagged = false;

            foreach (string tag in maskedTags)
            {
                if (hit.transform.gameObject.CompareTag(tag))
                {
                    tagged = true;
                    break;
                }
            }
            return tagged;
        }

        public void CheckCollision(RaycastHit hit)
        {
            if (_collision == false)
            {
                if (useCameraDuplicate)
                {
                    if (duplicate == null)
                    {
                        if (duplicatePrefab != null)
                        {
                            duplicate = Instantiate(duplicatePrefab, mainCamera.transform.position, mainCamera.transform.rotation).GetComponent<Camera>();
                            networkPlayer.cameraCollisionDuplicate = duplicate.GetComponent<Camera>();
                        }
                        else
                        {
                            duplicate = new GameObject("Camera [Duplicate]").AddComponent<Camera>();
                            duplicate.transform.position = mainCamera.transform.position;
                            duplicate.transform.rotation = mainCamera.transform.rotation;

                            duplicate.CopyProperties(mainCamera);

                            networkPlayer.cameraCollisionDuplicate = duplicate.GetComponent<Camera>();
                        }

                        duplicate.transform.SetParent(mainCamera.transform.parent);
                    }

                    mainCamera.enabled = false;
                    duplicate.enabled = true;
                }

                _collision = true;
            }

            distance = Vector3.Distance(hit.point, focusPoint.transform.position);

            if (useCameraDuplicate)
            {
                if (distance <= cushionOffset)
                    duplicate.transform.position = focusPosition;
                else
                    duplicate.transform.position = hit.point + (-rayDirection * cushionOffset);

                duplicate.transform.rotation = mainCamera.transform.rotation;
            }
            else
            {
                if (distance < cushionOffset)
                    mainCamera.transform.position = focusPosition;
                else
                    mainCamera.transform.position = hit.point + (-rayDirection * cushionOffset);
            }
        }
    }
}