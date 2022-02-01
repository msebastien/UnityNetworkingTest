using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Networking.Transport;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private Button startServerButton = null;

    [SerializeField]
    private Button startHostButton = null;

    [SerializeField]
    private Button startClientButton = null;

    [SerializeField]
    private Button executePhysicsButton = null;

    [SerializeField]
    private TMP_InputField joinCodeInput = null;

    [SerializeField]
    private TextMeshProUGUI playersInGameText = null;

    private bool hasServerStarted = false;

    private void Awake()
    {
        Cursor.visible = true;
    }

    private void Start()
    {
        playersInGameText.text = "Players in game: 0";

        // Server
        // Start a local server
        startServerButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartServer())
            {
                Logger.Instance.LogInfo("Server started...");
                executePhysicsButton.gameObject.SetActive(true);
            }
            else
            {
                Logger.Instance.LogError("Server could not be started...");
            }
        });

        // Host
        // Can be used to start a host and a relay server (online multiplayer)
        startHostButton.onClick.AddListener(async () =>
        {
            if (RelayManager.Instance.IsRelayServerEnabled)
                await RelayManager.Instance.SetupRelay();

            if (NetworkManager.Singleton.StartHost())
            {
                Logger.Instance.LogInfo("Host started...");
                executePhysicsButton.gameObject.SetActive(true);
            }
            else
            {
                Logger.Instance.LogError("Host could not be started...");
            }
        });

        // Client
        // Can be used to start a client and join a relay server with a join code (online multiplayer)
        startClientButton.onClick.AddListener(async () =>
        {
            if (RelayManager.Instance.IsRelayServerEnabled && !string.IsNullOrEmpty(joinCodeInput.text))
                await RelayManager.Instance.JoinRelay(joinCodeInput.text);


            if (NetworkManager.Singleton.StartClient())
            {
                Logger.Instance.LogInfo("Client started...");
                executePhysicsButton.gameObject.SetActive(true);
            }
            else
            {
                Logger.Instance.LogError("Client could not be started...");
            }
        });

        NetworkManager.Singleton.OnServerStarted += () =>
        {
            hasServerStarted = true;
        };

        executePhysicsButton.onClick.AddListener(() => 
        {
            if(!hasServerStarted)
            {
                Logger.Instance.LogWarning("Server has not started...");
                return;
            }
            
            SpawnController.Instance.SpawnObjects();
        });
    }

    private void Update()
    {
        playersInGameText.text = $"Players in game: {PlayersManager.Instance.PlayersInGame}";
    }
}
