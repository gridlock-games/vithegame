using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using GameCreator.Melee;
using UnityEngine.UI;

namespace LightPat.Core
{
    public class SpectatorCamera : NetworkBehaviour
    {
        public float moveSpeed = 10;
        public Vector2 sensitivity = new Vector2(1, 1);
        public float playerCardSpacing = 100;

        [SerializeField] private GameObject UICanvasInstance;
        [SerializeField] private Transform playerCardParent;
        [SerializeField] private GameObject playerCardLeftAnchorPrefab;
        [SerializeField] private GameObject playerCardRightAnchorPrefab;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                gameObject.AddComponent<GameCreator.Core.Hooks.HookCamera>();
                gameObject.AddComponent<AudioListener>();
                GetComponent<Camera>().enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                UICanvasInstance.SetActive(true);
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

        private void Update()
        {
            if (!IsSpawned) { return; }

            if (!IsOwner) { return; }

            // FPS Counter and Ping Display
            if (Time.unscaledTime > _timer)
            {
                int fps = (int)(1f / Time.unscaledDeltaTime);
                fpsCounterDisplay.SetText("FPS: " + fps);
                pingDisplay.SetText("Ping: " + roundTripTime.Value + " ms");
                _timer = Time.unscaledTime + _hudRefreshRate;
            }

            if (!pauseInstance)
            {
                Vector2 moveInput = Vector2.zero;
                if (Input.GetKey(KeyCode.W)) { moveInput.y = 1; }
                if (Input.GetKey(KeyCode.S)) { moveInput.y = -1; }
                if (Input.GetKey(KeyCode.D)) { moveInput.x = 1; }
                if (Input.GetKey(KeyCode.A)) { moveInput.x = -1; }
                transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed * Time.deltaTime);

                Vector2 lookInput = Vector2.zero;
                lookInput.x = Input.GetAxis("Mouse X");
                lookInput.y = Input.GetAxis("Mouse Y");
                lookInput.x *= sensitivity.x;
                lookInput.y *= sensitivity.y;
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x - lookInput.y, transform.localEulerAngles.y + lookInput.x, transform.localEulerAngles.z);
            }

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

            //if (!ClientManager.Singleton) { return; }

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

            // Replace testDict with ClientManager.Singleton.localNetworkPlayers
            Dictionary<ulong, GameObject> testDict = new Dictionary<ulong, GameObject>();
            int i = 0;
            foreach (CharacterMelee melee in FindObjectsOfType<CharacterMelee>())
            {
                testDict.Add((ulong)i, melee.gameObject);
                i++;
            }

            if (lastLocalNetworkPlayers != testDict)
            {
                foreach (Transform playerIcon in playerCardParent)
                {
                    Destroy(playerIcon.gameObject);
                }

                int rightCounter = 0;
                int leftCounter = 0;
                foreach (KeyValuePair<ulong, GameObject> valuePair in testDict)
                {
                    if (valuePair.Value.TryGetComponent(out CharacterMelee melee))
                    {
                        //Team playerTeam = ClientManager.Singleton.GetClient(valuePair.Key).team;
                        Team playerTeam = valuePair.Key % 2 == 0 ? Team.Red : Team.Blue;
                        if (playerTeam == Team.Blue)
                        {
                            GameObject playerCard = Instantiate(playerCardRightAnchorPrefab, playerCardParent);
                            playerCard.GetComponent<PlayerCard>().Instantiate(melee, playerTeam);
                            playerCard.transform.localPosition = new Vector3(playerCard.transform.localPosition.x, rightCounter * playerCardSpacing, playerCard.transform.localPosition.z);
                            rightCounter++;
                        }
                        else if (playerTeam == Team.Red)
                        {
                            GameObject playerCard = Instantiate(playerCardLeftAnchorPrefab, playerCardParent);
                            playerCard.GetComponent<PlayerCard>().Instantiate(melee, playerTeam);
                            playerCard.transform.localPosition = new Vector3(playerCard.transform.localPosition.x, leftCounter * playerCardSpacing, playerCard.transform.localPosition.z);
                            leftCounter++;
                        }
                        else
                        {
                            GameObject playerCard = Instantiate(playerCardLeftAnchorPrefab, playerCardParent);
                            playerCard.GetComponent<PlayerCard>().Instantiate(melee, playerTeam);
                            playerCard.transform.localPosition = new Vector3(playerCard.transform.localPosition.x, leftCounter * playerCardSpacing, playerCard.transform.localPosition.z);
                            leftCounter++;
                        }
                    }
                }
            }

            lastLocalNetworkPlayers = testDict;
        }

        private Dictionary<ulong, GameObject> lastLocalNetworkPlayers = new Dictionary<ulong, GameObject>();

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }
}