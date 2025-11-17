using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private GameObject playerPrefab;

    private int nextSpawnPointIndex = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            
            // Spawn host player
            if (IsHost)
            {
                SpawnPlayer(NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Spawn player for the newly connected client
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points assigned in PlayerSpawnManager.");
            return;
        }

        // Get spawn point and instantiate player
        Transform spawnPoint = GetNextSpawnPoint();
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Spawn the player on the network for the specific client
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.SpawnAsPlayerObject(clientId, true);
        }
        else
        {
            Debug.LogError("Player prefab is missing a NetworkObject component.");
        }
    }

    private Transform GetNextSpawnPoint()
    {
        Transform spawnPoint = spawnPoints[nextSpawnPointIndex];
        nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Count;
        return spawnPoint;
    }
}
