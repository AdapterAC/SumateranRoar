using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GamePlayController : NetworkBehaviour
{
    [SerializeField] private GameObject playerHumanPrefab;
    [SerializeField] private GameObject playerTigerPrefab;

    [Header("Spawn Areas (Optional)")]
    [Tooltip("Area spawn untuk Human. Jika kosong, fallback ke posisi default.")]
    [SerializeField] private SpawnArea[] humanSpawnAreas;
    
    [Tooltip("Area spawn untuk Tiger. Jika kosong, fallback ke posisi default.")]
    [SerializeField] private SpawnArea[] tigerSpawnAreas;

    [Header("Rules")]
    [SerializeField] private float minTigerToHumanDistance = 20f; // Jarak aman Tiger↔Human
    [SerializeField] private int maxAttemptsPerSpawn = 100;

    // Fallback jika tidak memakai SpawnArea
    private Vector3 spawnHumanPosition = new(341f, 4f, 811f);
    private Vector3 spawnTigerPosition = new(341f, 4f, 824f);


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

        // Jika ada SpawnArea, gunakan random spawn + aturan jarak Tiger↔Human (human boleh berdekatan)
        if (humanSpawnAreas != null && humanSpawnAreas.Length > 0 && tigerSpawnAreas != null && tigerSpawnAreas.Length > 0)
        {
            // 1) Tentukan posisi semua Human lebih dulu
            var humanPositions = new Dictionary<ulong, Vector3>();
            foreach (var client in clients)
            {
                if (client.ClientId == tigerClientId) continue; // skip harimau dulu

                // Pilih random area dari humanSpawnAreas
                SpawnArea randomHumanArea = humanSpawnAreas[Random.Range(0, humanSpawnAreas.Length)];
                Vector3 pos = randomHumanArea.transform.position; // default center
                for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
                {
                    if (randomHumanArea.TryGetRandomPoint(out var p))
                    {
                        pos = p;
                        break;
                    }
                }
                humanPositions[client.ClientId] = pos;
            }

            // 2) Cari posisi Tiger yang cukup jauh dari semua Human
            SpawnArea randomTigerArea = tigerSpawnAreas[Random.Range(0, tigerSpawnAreas.Length)];
            Vector3 tigerPos = randomTigerArea.transform.position; // default center
            bool foundTiger = false;
            for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
            {
                if (!randomTigerArea.TryGetRandomPoint(out var p)) continue;

                bool far = true;
                foreach (var kv in humanPositions)
                {
                    if (Vector3.Distance(kv.Value, p) < minTigerToHumanDistance)
                    {
                        far = false; break;
                    }
                }
                if (far)
                {
                    tigerPos = p;
                    foundTiger = true;
                    break;
                }
            }
            if (!foundTiger)
            {
                Debug.LogWarning("GamePlayController: Gagal menemukan posisi Tiger yang cukup jauh; gunakan fallback center area.");
            }

            // 3) Spawn networked
            foreach (var client in clients)
            {
                GameObject playerInstance;
                if (client.ClientId == tigerClientId)
                {
                    ScreenLogger.Log($"Spawning Tiger for client {client.ClientId}", ScreenLogger.LogType.Success);
                    playerInstance = Instantiate(playerTigerPrefab, tigerPos, Quaternion.identity);
                }
                else
                {
                    var hp = humanPositions[client.ClientId];
                    ScreenLogger.Log($"Spawning Human for client {client.ClientId}", ScreenLogger.LogType.Success);
                    playerInstance = Instantiate(playerHumanPrefab, hp, Quaternion.identity);
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
        else
        {
            // Fallback perilaku lama: pakai posisi tetap
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
}
