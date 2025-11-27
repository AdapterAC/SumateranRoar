using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject playerPrefab;
    
    private List<Transform> spawnPoints = new List<Transform>();
    private int nextSpawnPointIndex = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Panggil saat scene selesai dimuat
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        // Hanya cari spawn points jika scene yang dimuat adalah GamePlay
        if (sceneName == "GamePlay")
        {
            FindAndRegisterSpawnPoints();
            
            // Spawn player untuk semua client yang sudah terhubung
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnPlayer(clientId);
            }
            
            // Hubungkan kembali callback untuk client yang baru join nanti
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            // Jika kita meninggalkan GamePlay, putuskan callback
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void FindAndRegisterSpawnPoints()
    {
        spawnPoints.Clear();
        // Cari semua GameObject dengan tag "SpawnPoint"
        var spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        
        if (spawnPointObjects.Length == 0)
        {
            Debug.LogError("No objects with tag 'SpawnPoint' found in the scene!");
            return;
        }

        foreach (var sp in spawnPointObjects)
        {
            spawnPoints.Add(sp.transform);
        }
        
        Debug.Log($"Found and registered {spawnPoints.Count} spawn points.");
    }

    private void OnClientConnected(ulong clientId)
    {
        // Spawn player untuk client yang baru saja terhubung
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("Cannot spawn player, no spawn points have been registered.");
            return;
        }

        // Cek apakah player sudah punya objek
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
        {
            Debug.Log($"Player for client {clientId} already exists. Skipping spawn.");
            return;
        }

        Transform spawnPoint = GetNextSpawnPoint();
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.SpawnAsPlayerObject(clientId, true);
            Debug.Log($"Spawning player for client {clientId} at {spawnPoint.name}");
        }
        else
        {
            Debug.LogError("Player prefab is missing a NetworkObject component.");
            Destroy(playerInstance);
        }
    }

    private Transform GetNextSpawnPoint()
    {
        Transform spawnPoint = spawnPoints[nextSpawnPointIndex];
        nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Count;
        return spawnPoint;
    }
}
