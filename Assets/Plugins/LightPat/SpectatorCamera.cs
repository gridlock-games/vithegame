using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using GameCreator.Melee;
using System.Linq;

namespace LightPat.Core
{
    public class SpectatorCamera : NetworkBehaviour
    {
        public float moveSpeed = 10;
        public Vector2 sensitivity = new Vector2(1, 1);
        public float playerCardSpacing = 100;
        public Vector3 followCamOffset = new Vector3(0, 3, -3);

        [SerializeField] private GameObject UICanvasInstance;
        [SerializeField] private Transform playerCardParent;
        [SerializeField] private GameObject playerCardLeftAnchorPrefab;
        [SerializeField] private GameObject playerCardRightAnchorPrefab;

        public bool isDeathCam { get; private set; }

        private Player.NetworkPlayer networkPlayer;
        
        public void ActivateDeathCam(Player.NetworkPlayer networkPlayer)
        {
            isDeathCam = true;
            //gameObject.AddComponent<GameCreator.Core.Hooks.HookCamera>();
            gameObject.AddComponent<AudioListener>();
            GetComponent<Camera>().enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            UICanvasInstance.SetActive(true);
            tag = "MainCamera";
            this.networkPlayer = networkPlayer;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                gameObject.AddComponent<GameCreator.Core.Hooks.HookCamera>();
                gameObject.AddComponent<AudioListener>();
                GetComponent<Camera>().enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                UICanvasInstance.SetActive(true);
                tag = "MainCamera";
            }
            else
            {
                GetComponent<Camera>().enabled = false;
                Destroy(UICanvasInstance);
            }
        }

        [SerializeField] private GameObject pauseMenuPrefab;
        [SerializeField] private GameObject scoreboardPrefab;
        private GameObject pauseInstance;
        private GameObject scoreboardInstance;

        [SerializeField] private TextMeshProUGUI pingDisplay;
        [SerializeField] private TextMeshProUGUI fpsCounterDisplay;
        private readonly float _hudRefreshRate = 1f;
        private float _timer;

        [HideInInspector] public NetworkVariable<ulong> roundTripTime = new NetworkVariable<ulong>();

        private Transform followCamTarget;
        private Vector3 lastFollowCamPosition;
        private float followCamVerticalAngle;
        private int followCamIndex = -1;

        private void Update()
        {
            if (!isDeathCam)
            {
                if (!IsSpawned) { return; }
                if (!IsOwner) { return; }
            }

            // FPS Counter and Ping Display
            if (Time.unscaledTime > _timer)
            {
                int fps = (int)(1f / Time.unscaledDeltaTime);
                fpsCounterDisplay.SetText("FPS: " + fps);
                pingDisplay.SetText("Ping: " + roundTripTime.Value + " ms");
                _timer = Time.unscaledTime + _hudRefreshRate;
            }

            // Figure out what player we are trying to follow
            int followCamIndex = -1;
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                followCamIndex = 0;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                followCamIndex = 1;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                followCamIndex = 2;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                followCamIndex = 3;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                followCamIndex = 4;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                followCamIndex = 5;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                followCamIndex = 6;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                followCamIndex = 7;
                this.followCamIndex = followCamIndex;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                followCamIndex = 8;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                followCamIndex = 9;
            }
            else if (Input.GetMouseButtonDown(0)) // Left click
            {
                followCamIndex = this.followCamIndex + 1;
            }
            else if (Input.GetMouseButtonDown(1)) // Right click
            {
                followCamIndex = this.followCamIndex - 1;
            }

            // Check if the follow cam index is in our player list
            if (followCamIndex != -1)
            {
                List<GameObject> localNetworkPlayers = new List<GameObject>();
                foreach (KeyValuePair<ulong, GameObject> valuePair in ClientManager.Singleton.localNetworkPlayers)
                {
                    if (isDeathCam)
                    {
                        if (ClientManager.Singleton.GetClient(valuePair.Key).team != ClientManager.Singleton.GetClient(NetworkManager.LocalClientId).team) { continue; }
                        localNetworkPlayers.Add(valuePair.Value);
                    }
                    else
                    {
                        localNetworkPlayers.Add(valuePair.Value);
                    }
                }
                
                if (followCamIndex < localNetworkPlayers.Count)
                {
                    if (localNetworkPlayers[followCamIndex] != null)
                    {
                        this.followCamIndex = followCamIndex;

                        followCamTarget = localNetworkPlayers[followCamIndex].transform;
                        transform.localPosition = followCamTarget.transform.position + (followCamTarget.transform.rotation * followCamOffset);
                        lastFollowCamPosition = followCamTarget.position;
                        transform.LookAt(followCamTarget);

                        followCamVerticalAngle = transform.localEulerAngles.x;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                followCamTarget = null;
                followCamVerticalAngle = 0;
                this.followCamIndex = -1;
            }

            if (!pauseInstance | isDeathCam)
            {
                Vector2 lookInput = Vector2.zero;
                lookInput.x = Input.GetAxis("Mouse X");
                lookInput.y = Input.GetAxis("Mouse Y");
                lookInput.x *= sensitivity.x;
                lookInput.y *= sensitivity.y;

                if (followCamTarget)
                {
                    Player.NetworkPlayer networkPlayer = followCamTarget.GetComponent<Player.NetworkPlayer>();

                    transform.position = Vector3.Lerp(transform.position, networkPlayer.GetCamPosition(), Time.deltaTime * 8);
                    transform.rotation = Quaternion.Slerp(transform.rotation, networkPlayer.GetCamRotation(), Time.deltaTime * 8);

                    //transform.position += followCamTarget.position - lastFollowCamPosition;
                    //transform.RotateAround(followCamTarget.transform.position, Vector3.up, lookInput.x);
                    //transform.LookAt(followCamTarget);

                    //followCamVerticalAngle -= lookInput.y;
                    //transform.localEulerAngles = new Vector3(followCamVerticalAngle, transform.localEulerAngles.y, transform.localEulerAngles.z);

                    //lastFollowCamPosition = followCamTarget.position;
                }
                else
                {
                    Vector2 moveInput = Vector2.zero;
                    if (Input.GetKey(KeyCode.W)) { moveInput.y = 1; }
                    if (Input.GetKey(KeyCode.S)) { moveInput.y = -1; }
                    if (Input.GetKey(KeyCode.D)) { moveInput.x = 1; }
                    if (Input.GetKey(KeyCode.A)) { moveInput.x = -1; }
                    transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed * Time.deltaTime);

                    transform.localEulerAngles = new Vector3(transform.localEulerAngles.x - lookInput.y, transform.localEulerAngles.y + lookInput.x, transform.localEulerAngles.z);
                }
            }

            if (!isDeathCam)
            {
                // Pause menu
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (!pauseInstance)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        pauseInstance = Instantiate(pauseMenuPrefab);
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        pauseInstance.GetComponent<Menu>().DestroyAllMenus();
                        Destroy(pauseInstance);
                    }
                }

                if (!ClientManager.Singleton) { return; }

                // Scoreboard
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (!scoreboardInstance)
                        scoreboardInstance = Instantiate(scoreboardPrefab);
                }

                if (Input.GetKeyUp(KeyCode.Tab))
                {
                    if (scoreboardInstance)
                        Destroy(scoreboardInstance);
                }
            }

            string playersString = "";
            foreach (var kvp in ClientManager.Singleton.localNetworkPlayers)
            {
                playersString += kvp.Key.ToString() + kvp.Value.ToString();
            }

            if (lastPlayersString != playersString)
            {
                foreach (Transform playerIcon in playerCardParent)
                {
                    Destroy(playerIcon.gameObject);
                }

                int rightCounter = 0;
                int leftCounter = 0;
                int elseCounter = 0;
                foreach (KeyValuePair<ulong, GameObject> valuePair in ClientManager.Singleton.localNetworkPlayers)
                {
                    if (valuePair.Value.TryGetComponent(out CharacterMelee melee))
                    {
                        Team playerTeam = ClientManager.Singleton.GetClient(valuePair.Key).team;
                        if (isDeathCam)
                        {
                            Team localTeam = ClientManager.Singleton.GetClient(networkPlayer.OwnerClientId).team;
                            if (localTeam != playerTeam) { continue; }
                        }

                        if (playerTeam == Team.Blue)
                        {
                            GameObject playerCard = Instantiate(playerCardRightAnchorPrefab, playerCardParent);
                            playerCard.GetComponent<PlayerCard>().Instantiate(melee, playerTeam, false, this);
                            playerCard.transform.localPosition = new Vector3(playerCard.transform.localPosition.x, rightCounter * playerCardSpacing, playerCard.transform.localPosition.z);
                            rightCounter++;
                        }
                        else if (playerTeam == Team.Red)
                        {
                            GameObject playerCard = Instantiate(playerCardLeftAnchorPrefab, playerCardParent);
                            playerCard.GetComponent<PlayerCard>().Instantiate(melee, playerTeam, false, this);
                            playerCard.transform.localPosition = new Vector3(playerCard.transform.localPosition.x, leftCounter * playerCardSpacing, playerCard.transform.localPosition.z);
                            leftCounter++;
                        }
                        else
                        {
                            if (elseCounter % 2 == 0)
                            {
                                GameObject playerCard = Instantiate(playerCardLeftAnchorPrefab, playerCardParent);
                                playerCard.GetComponent<PlayerCard>().Instantiate(melee, playerTeam, false, this);
                                playerCard.transform.localPosition = new Vector3(playerCard.transform.localPosition.x, elseCounter * playerCardSpacing, playerCard.transform.localPosition.z);
                            }
                            else
                            {
                                GameObject playerCard = Instantiate(playerCardRightAnchorPrefab, playerCardParent);
                                playerCard.GetComponent<PlayerCard>().Instantiate(melee, playerTeam, false, this);
                                playerCard.transform.localPosition = new Vector3(playerCard.transform.localPosition.x, elseCounter * playerCardSpacing, playerCard.transform.localPosition.z);
                            }

                            elseCounter++;
                        }
                    }
                }
            }

            lastPlayersString = playersString;

            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }

        private string lastPlayersString;

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }
}