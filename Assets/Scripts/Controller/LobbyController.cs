using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI codeRoom;
    [SerializeField] private GameObject[] playerSlots;
    [SerializeField] private GameObject gameManagerPrefab;

    int jumlahMati = 0;

    void Start()
    {
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        startButton.onClick.AddListener(OnStartButtonClicked);
        codeRoom.text = RelayManager.Instance.GetRoomCode();
        SpawnGameManager(); 
    }

    private void OnCancelButtonClicked()
    {
        ScreenLogger.Log($"OnCancelButtonClicked - Berhasil dijalankan", ScreenLogger.LogType.Success);
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    private void OnStartButtonClicked()
    {
        // Arahkan ke gameplay scene dan spawn player secara random
        ScreenLogger.Log($"OnStartButtonClicked - Berhasil dijalankan", ScreenLogger.LogType.Success);
        // Debug.Log($"OnStartButtonClicked - IsHost: {IsHost}");
        // Debug.Log($"OnStartButtonClicked - IsServer: {IsServer}");
        // Debug.Log($"OnStartButtonClicked - IsClient: {IsClient}");
        // Debug.Log($"OnStartButtonClicked - IsOwner: {IsOwner}");
        // Debug.Log($"OnStartButtonClicked - IsHost: {NetworkManager.Singleton.IsHost}");
        // if (NetworkManager.Singleton.IsHost) StartGameServerRpc();
        GameManagerNetwork.Instance.StartGameServerRpc();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlayerSlots();
    }

    private void UpdatePlayerSlots()
    {
        var players = NetworkManager.Singleton.ConnectedClients;
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i < players.Count) playerSlots[i].SetActive(true);
            else playerSlots[i].SetActive(false);
        }
    }

    private void SpawnGameManager()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            GameObject gm = Instantiate(gameManagerPrefab);
            gm.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    // ====================================== SERVER RPC ======================================
    // [ServerRpc(RequireOwnership = false)]
    // private void StartGameServerRpc()
    // {
    //     // Spawn players and start the game
    //     Debug.Log("StartGameServerRpc - Game Started by Host!");
    //     NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
    // }

    // // ====================================== CLIENT RPC ======================================
    // [ClientRpc(RequireOwnership = false)]
    // private void StartGameClientRpc()
    // {
    //     // Spawn players and start the game
    //     Debug.Log("StartGameServerRpc - Game Started to All Client!");
    //     NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
    // }
    // [ClientRpc]
    // private void EndGameClientRpc()
    // {
    //     // Spawn players and start the game
    //     Debug.Log("EndGameClientRpc - Game Ended!");
    //     NetworkManager.Singleton.Shutdown();
    //     SceneManager.LoadScene("MainMenu");
    // }

}
