using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyController : NetworkBehaviour
{
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI codeRoom;
    [SerializeField] private GameObject[] playerSlots;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        startButton.onClick.AddListener(OnStartButtonClicked);
        codeRoom.text = RelayManager.Instance.GetRoomCode();
    }

    private void OnCancelButtonClicked()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    private void OnStartButtonClicked()
    {
        // Arahkan ke gameplay scene dan spawn player secara random
        if (IsHost) StartGameServerRpc();
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

    // ====================================== SERVER RPC ======================================
    [ServerRpc]
    private void StartGameServerRpc()
    {
        // Spawn players and start the game
        Debug.Log("StartGameServerRpc - Game Started by Host!");
    }

        // ====================================== CLIENT RPC ======================================
    [ClientRpc]
    private void EndGameClientRpc()
    {
        // Spawn players and start the game
        Debug.Log("EndGameClientRpc - Game Ended!");
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
}
