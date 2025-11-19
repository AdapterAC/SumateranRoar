using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GamePlayController : NetworkBehaviour
{
    [SerializeField] private GameObject playerHumanPrefab;
    [SerializeField] private GameObject playerTigerPrefab;

    private Vector3 spawnHumanPosition = new(2.76f, 5f, -25.32f);
    private Vector3 spawnTigerPosition = new(8.15f, 5f, -24.61f);


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(DelayedSpawnPlayers());
        }
    }

    private IEnumerator DelayedSpawnPlayers()
    {
        // Wait for 1 second to ensure all clients have loaded the scene
        yield return new WaitForSeconds(1f);
        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        if (!IsServer) return;

        var clients = NetworkManager.Singleton.ConnectedClientsList;
        if (clients.Count == 0) return;

        int tigerIndex = Random.Range(0, clients.Count);
        ulong tigerClientId = clients[tigerIndex].ClientId;

        foreach (var client in clients)
        {
            GameObject playerInstance;
            if (client.ClientId == tigerClientId)
            {
                ScreenLogger.Log($"Spawning Tiger for client {client.ClientId}", ScreenLogger.LogType.Success);
                playerInstance = Instantiate(playerTigerPrefab, spawnTigerPosition, Quaternion.identity);
            }
            else
            {
                ScreenLogger.Log($"Spawning Human for client {client.ClientId}", ScreenLogger.LogType.Success);
                playerInstance = Instantiate(playerHumanPrefab, spawnHumanPosition, Quaternion.identity);
            }

            if (playerInstance.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.SpawnAsPlayerObject(client.ClientId);
            }
            else
            {
                Debug.LogError($"Player prefab for client {client.ClientId} is missing a NetworkObject component.");
            }
        }
    }
}
