using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GamePlayController : NetworkBehaviour
{
    [SerializeField] private GameObject playerHumanPrefab;
    [SerializeField] private GameObject playerTigerPrefab;

    private Vector3 spawnHumanPosition = new(0f, 4f, 15f);
    private Vector3 spawnTigerPosition = new(5f, 4f, 15f);

    public override void OnNetworkSpawn()
    {
        ScreenLogger.Log($"OnNetworkSpawn - Berhasil dijalankan", ScreenLogger.LogType.Success);
        if (IsServer)
        {
            // NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeave;
            ScreenLogger.Log($"OnNetworkSpawn - Berhasil dijalankan IF-nya", ScreenLogger.LogType.Success);
            StartCoroutine(SpawnDelay());
        }
    }
    private IEnumerator SpawnDelay()
    {
        yield return new WaitForSeconds(0.5f);
        SpawnPlayers();
    }
    private void SpawnPlayers()
    {
        if (!IsServer) return;
        int totalPlayer = NetworkManager.ConnectedClients.Count;
        // int randomTigerId = UnityEngine.Random.Range(0, totalPlayer);
        int i = 0;
        ScreenLogger.Log($"SpawnPlayers - Berhasil dijalankan Total Player: {totalPlayer}", ScreenLogger.LogType.Success);
        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            if ((int)client.ClientId == 0)
            {
                Vector3 spawnPos = spawnTigerPosition + new Vector3(i * 2f, 0, 0);
                ScreenLogger.Log($"SpawnPlayers - Berhasil dijalankan ID: {client.ClientId} jadi HARIMAU", ScreenLogger.LogType.Success);
                GameObject playerInstance = Instantiate(playerTigerPrefab, spawnPos, Quaternion.identity);
                playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);
            }
            else
            {
                Vector3 spawnPos = spawnHumanPosition + new Vector3(i * 2f, 0, 0);
                ScreenLogger.Log($"SpawnPlayers - Berhasil dijalankan ID: {client.ClientId} jadi MANUSIA", ScreenLogger.LogType.Success);
                GameObject playerInstance = Instantiate(playerHumanPrefab, spawnPos, Quaternion.identity);
                playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);
            }
            i++;
        }
    }
    
}
