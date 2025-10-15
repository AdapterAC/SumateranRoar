using Unity.Netcode;
using UnityEngine;

public class GamePlayController : NetworkBehaviour
{
    [SerializeField] private GameObject playerHumanPrefab;
    [SerializeField] private GameObject playerTigerPrefab;

    private Vector3 spawnHumanPosition = new(2.76f, 0.079f, -25.32f);
    private Vector3 spawnTigerPosition = new(8.15f, 0.08f, -24.61f);


    public override void OnNetworkSpawn()
    {
        ScreenLogger.Log($"OnNetworkSpawn - Berhasil dijalankan", ScreenLogger.LogType.Success);
        if (IsServer)
        {
            // NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeave;
            ScreenLogger.Log($"OnNetworkSpawn - Berhasil dijalankan IF-nya", ScreenLogger.LogType.Success);
            SpawnPlayers();
        }
    }

    private void SpawnPlayers()
    {
        if (!IsServer) return;
        int totalPlayer = NetworkManager.ConnectedClients.Count;
        // int randomTigerId = UnityEngine.Random.Range(0, totalPlayer);
        int i = 0;
        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            Debug.Log($"Total player: {totalPlayer}");
            if ((int)client.ClientId == 0)
            {
                ScreenLogger.Log($"SpawnPlayers - Berhasil dijalankan ID: {client.ClientId} jadi HARIMAU", ScreenLogger.LogType.Success);
                GameObject playerInstance = Instantiate(playerTigerPrefab, spawnTigerPosition, Quaternion.identity);
                playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);
                // AddPlayerCharacterListClientRpc(client.ClientId, CharacterType.Pocong);
            }
            else
            {
                ScreenLogger.Log($"SpawnPlayers - Berhasil dijalankan ID: {client.ClientId} jadi MANUSIA", ScreenLogger.LogType.Success);
                GameObject playerInstance = Instantiate(playerHumanPrefab, spawnHumanPosition, Quaternion.identity);
                playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);
                // AddPlayerCharacterListClientRpc(client.ClientId, CharacterType.Kid);
            }
            i++;
        }
    }
    
}
