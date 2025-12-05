using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class GamePlayController : NetworkBehaviour
{
    [SerializeField] private GameObject playerHumanPrefab;
    [SerializeField] private GameObject playerTigerPrefab;

    [Header("Game State Manager")]
    [Tooltip("Prefab GameStateManager untuk di-spawn di server")]
    [SerializeField] private GameObject gameStateManagerPrefab;
    
    [Header("Spawn Areas (Optional)")]
    [Tooltip("Area spawn untuk Human. Jika kosong, fallback ke posisi default.")]
    [SerializeField] private SpawnArea[] humanSpawnAreas;
    
    [Tooltip("Area spawn untuk Tiger. Jika kosong, fallback ke posisi default.")]
    [SerializeField] private SpawnArea[] tigerSpawnAreas;

    [Header("Rules")]
    [SerializeField] private float minTigerToHumanDistance = 20f; // Jarak aman Tiger↔Human
    [SerializeField] private int maxAttemptsPerSpawn = 100;
    
    [Header("Spawn Timing")]
    [Tooltip("Waktu tunggu sebelum spawn untuk memastikan scene sudah siap")]
    [SerializeField] private float sceneReadyDelay = 2f;
    [Tooltip("Tinggi raycast untuk validasi spawn di atas terrain")]
    [SerializeField] private float raycastHeight = 100f;
    [Tooltip("LayerMask untuk terrain/ground")]
    [SerializeField] private LayerMask groundLayerMask = ~0;

    // Fallback jika tidak memakai SpawnArea
    private Vector3 spawnHumanPosition = new(341f, 4f, 811f);
    private Vector3 spawnTigerPosition = new(341f, 4f, 824f);

    // Track spawned players untuk koordinasi
    private Dictionary<ulong, GameObject> spawnedPlayers = new Dictionary<ulong, GameObject>();
    private NetworkVariable<bool> allPlayersSpawned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private ulong tigerClientId;

    private void Awake()
    {
        // Pastikan SpawnArea tidak di-destroy saat scene load
        DontDestroySpawnAreas();
    }

    private void DontDestroySpawnAreas()
    {
        // Pastikan SpawnArea tetap ada selama gameplay
        if (humanSpawnAreas != null)
        {
            foreach (var area in humanSpawnAreas)
            {
                if (area != null && area.gameObject != null)
                {
                    // Jangan DontDestroyOnLoad, cukup pastikan tidak di-destroy
                    // SpawnArea harus tetap di scene
                }
            }
        }
        if (tigerSpawnAreas != null)
        {
            foreach (var area in tigerSpawnAreas)
            {
                if (area != null && area.gameObject != null)
                {
                    // Jangan DontDestroyOnLoad, cukup pastikan tidak di-destroy
                }
            }
        }
    }

    private IEnumerator InitializeGameplay()
    {
        // 1) Spawn GameStateManager FIRST
        yield return StartCoroutine(SpawnGameStateManager());
        
        // 2) Verify GameStateManager is ready
        float timeout = 5f;
        float elapsed = 0f;
        while (GameStateManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("[GamePlayController] GameStateManager failed to spawn after timeout!");
            yield break;
        }
        
        Debug.Log("[GamePlayController] GameStateManager ready, starting spawn sequence...");
        
        // 3) Now spawn players
        yield return StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnGameStateManager()
    {
        if (!IsServer) yield break;

        // Cek apakah GameStateManager sudah ada di scene
        if (GameStateManager.Instance != null)
        {
            Debug.Log("[GamePlayController] GameStateManager already exists in scene.");
            yield break;
        }

        // Spawn GameStateManager dari prefab jika disediakan
        if (gameStateManagerPrefab != null)
        {
            GameObject gsmInstance = Instantiate(gameStateManagerPrefab);
            if (gsmInstance.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.Spawn(true); // Spawn as persistent (DontDestroyOnLoad)
                Debug.Log("[GamePlayController] GameStateManager spawned from prefab.");
            }
            else
            {
                Debug.LogError("[GamePlayController] GameStateManager prefab tidak memiliki NetworkObject component!");
                Destroy(gsmInstance);
                yield break;
            }
        }
        else
        {
            // Fallback: Buat GameStateManager baru di scene jika tidak ada prefab
            Debug.LogWarning("[GamePlayController] GameStateManager prefab tidak di-assign. Membuat instance baru...");
            GameObject gsmInstance = new GameObject("GameStateManager");
            var netObj = gsmInstance.AddComponent<NetworkObject>();
            gsmInstance.AddComponent<GameStateManager>();
            netObj.Spawn(true);
            Debug.Log("[GamePlayController] GameStateManager created and spawned.");
        }
        
        // Wait beberapa frame untuk GameStateManager selesai OnNetworkSpawn
        yield return new WaitForSeconds(0.2f);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(InitializeGameplay());
        }
        
        // Subscribe ke perubahan allPlayersSpawned untuk unlock movement
        allPlayersSpawned.OnValueChanged += OnAllPlayersSpawnedChanged;
    }

    public override void OnNetworkDespawn()
    {
        allPlayersSpawned.OnValueChanged -= OnAllPlayersSpawnedChanged;
    }

    private void OnAllPlayersSpawnedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            // Semua player sudah spawn, unlock movement untuk semua
            UnlockAllPlayersMovement();
        }
    }

    private IEnumerator SpawnSequence()
    {
        // 1) Tunggu scene dan terrain siap
        yield return StartCoroutine(WaitForSceneReady());
        
        // 2) Cari SpawnArea jika belum di-assign (fallback)
        yield return null; // Tunggu 1 frame
        FindSpawnAreasIfNeeded();
        
        // 3) Tentukan siapa Tiger
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        if (clients.Count == 0) yield break;
        
        int tigerIndex = Random.Range(0, clients.Count);
        tigerClientId = clients[tigerIndex].ClientId;
        // tigerClientId = 1; // Sementara, client terakhir jadi Tiger
        tigerClientId = 0; // Sementara, client terakhir jadi Tiger
        
        // 4) Hitung semua posisi spawn terlebih dahulu
        var spawnPositions = CalculateAllSpawnPositions(clients, tigerClientId);
        
        // 5) Spawn Host terlebih dahulu
        ulong hostClientId = NetworkManager.ServerClientId;
        if (spawnPositions.ContainsKey(hostClientId))
        {
            yield return StartCoroutine(SpawnPlayerAndWait(hostClientId, spawnPositions[hostClientId], hostClientId == tigerClientId));
        }
        
        // 6) Spawn semua Client (bukan host)
        foreach (var client in clients)
        {
            if (client.ClientId == hostClientId) continue; // Skip host, sudah di-spawn
            
            if (spawnPositions.ContainsKey(client.ClientId))
            {
                yield return StartCoroutine(SpawnPlayerAndWait(client.ClientId, spawnPositions[client.ClientId], client.ClientId == tigerClientId));
            }
        }
        
        // 7) Tunggu semua client konfirmasi ready
        yield return StartCoroutine(WaitForAllClientsReady());
        
        // 8) Set flag bahwa semua player sudah spawn
        allPlayersSpawned.Value = true;
        
        Debug.Log("All players spawned and ready!");
    }

    private IEnumerator WaitForSceneReady()
    {
        Debug.Log("Waiting for scene to be ready...");
        
        // Tunggu beberapa frame untuk memastikan scene objects sudah di-load
        yield return new WaitForSeconds(sceneReadyDelay);
        
        // Tunggu sampai Terrain aktif (jika ada)
        float timeout = 10f;
        float elapsed = 0f;
        while (Terrain.activeTerrain == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (Terrain.activeTerrain != null)
        {
            Debug.Log($"Terrain found: {Terrain.activeTerrain.name}");
        }
        else
        {
            Debug.LogWarning("No active terrain found, using raycast for ground detection");
        }
        
        // Tunggu physics ready
        yield return new WaitForFixedUpdate();
        
        Debug.Log("Scene is ready for spawning");
    }

    private void FindSpawnAreasIfNeeded()
    {
        // Jika SpawnArea belum di-assign, coba cari di scene
        if (humanSpawnAreas == null || humanSpawnAreas.Length == 0)
        {
            var foundAreas = FindObjectsByType<SpawnArea>(FindObjectsSortMode.None);
            if (foundAreas.Length > 0)
            {
                humanSpawnAreas = foundAreas;
                Debug.Log($"Found {foundAreas.Length} SpawnArea(s) in scene for humans");
            }
        }
        
        if (tigerSpawnAreas == null || tigerSpawnAreas.Length == 0)
        {
            tigerSpawnAreas = humanSpawnAreas; // Default: sama dengan human
        }
    }

    private Dictionary<ulong, Vector3> CalculateAllSpawnPositions(IReadOnlyList<NetworkClient> clients, ulong tigerClientId)
    {
        var positions = new Dictionary<ulong, Vector3>();
        var humanPositions = new List<Vector3>();

        // Hitung posisi Human dulu
        foreach (var client in clients)
        {
            if (client.ClientId == tigerClientId) continue;

            Vector3 pos = GetValidSpawnPosition(humanSpawnAreas, spawnHumanPosition);
            positions[client.ClientId] = pos;
            humanPositions.Add(pos);
        }

        // Hitung posisi Tiger (jauh dari Human)
        Vector3 tigerPos = GetTigerSpawnPosition(humanPositions);
        positions[tigerClientId] = tigerPos;

        return positions;
    }

    private Vector3 GetValidSpawnPosition(SpawnArea[] areas, Vector3 fallbackPos)
    {
        Vector3 pos = fallbackPos;
        
        if (areas != null && areas.Length > 0)
        {
            SpawnArea randomArea = areas[Random.Range(0, areas.Length)];
            if (randomArea != null)
            {
                for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
                {
                    if (randomArea.TryGetRandomPoint(out var p))
                    {
                        // Validasi posisi di atas terrain
                        Vector3 validatedPos = ValidatePositionOnGround(p);
                        if (validatedPos != Vector3.zero)
                        {
                            pos = validatedPos;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            // Validasi fallback position juga
            pos = ValidatePositionOnGround(fallbackPos);
            if (pos == Vector3.zero) pos = fallbackPos;
        }
        
        return pos;
    }

    private Vector3 GetTigerSpawnPosition(List<Vector3> humanPositions)
    {
        Vector3 tigerPos = spawnTigerPosition;
        
        if (tigerSpawnAreas != null && tigerSpawnAreas.Length > 0)
        {
            SpawnArea randomTigerArea = tigerSpawnAreas[Random.Range(0, tigerSpawnAreas.Length)];
            if (randomTigerArea != null)
            {
                tigerPos = randomTigerArea.transform.position;
                
                for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
                {
                    if (!randomTigerArea.TryGetRandomPoint(out var p)) continue;

                    // Cek jarak ke semua Human
                    bool far = true;
                    foreach (var humanPos in humanPositions)
                    {
                        if (Vector3.Distance(humanPos, p) < minTigerToHumanDistance)
                        {
                            far = false;
                            break;
                        }
                    }
                    
                    if (far)
                    {
                        // Validasi posisi di atas terrain
                        Vector3 validatedPos = ValidatePositionOnGround(p);
                        if (validatedPos != Vector3.zero)
                        {
                            tigerPos = validatedPos;
                            break;
                        }
                    }
                }
            }
        }
        
        return tigerPos;
    }

    private Vector3 ValidatePositionOnGround(Vector3 position)
    {
        // Coba pakai Terrain dulu
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float terrainY = terrain.SampleHeight(position) + terrain.transform.position.y;
            return new Vector3(position.x, terrainY + 1f, position.z); // +1f offset agar tidak clip
        }
        
        // Fallback ke raycast
        Vector3 rayOrigin = new Vector3(position.x, position.y + raycastHeight, position.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 1f; // +1f offset
        }
        
        // Tidak ditemukan ground, return zero untuk skip
        Debug.LogWarning($"Could not validate ground position at {position}");
        return position; // Return original jika tidak ada ground (bisa terjadi di area valid)
    }

    private IEnumerator SpawnPlayerAndWait(ulong clientId, Vector3 position, bool isTiger)
    {
        GameObject prefab = isTiger ? playerTigerPrefab : playerHumanPrefab;
        string playerType = isTiger ? "Tiger" : "Human";
        
        ScreenLogger.Log($"Spawning {playerType} for client {clientId} at {position}", ScreenLogger.LogType.Success);
        
        GameObject playerInstance = Instantiate(prefab, position, Quaternion.identity);
        
        // Pastikan posisi sudah benar sebelum spawn
        playerInstance.transform.position = position;
        
        if (playerInstance.TryGetComponent<NetworkObject>(out var netObj))
        {
            // Freeze movement sementara di server SEBELUM spawn
            SetPlayerMovementEnabled(playerInstance, false);
            
            // Spawn sebagai player object
            netObj.SpawnAsPlayerObject(clientId);
            spawnedPlayers[clientId] = playerInstance;
            
            // Register player ke GameStateManager
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.RegisterPlayer(clientId, isTiger);
                Debug.Log($"[GamePlayController] Player {clientId} registered as {(isTiger ? "Tiger" : "Human")} to GameStateManager");
            }
            else
            {
                Debug.LogError("[GamePlayController] CRITICAL: GameStateManager tidak ditemukan saat spawn player!");
            }
            
            // Pastikan transform sudah di-set dengan benar setelah spawn
            playerInstance.transform.position = position;
            
            // Kirim RPC ke client untuk teleport dan freeze
            // Client yang punya authority akan melakukan teleport
            TeleportAndFreezeClientRpc(position, clientId);
            
            // Tunggu beberapa frame untuk sinkronisasi
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            Debug.LogError($"Player prefab for client {clientId} is missing a NetworkObject component.");
            Destroy(playerInstance);
        }
    }

    private IEnumerator WaitForAllClientsReady()
    {
        // Tunggu sebentar untuk memastikan semua client menerima spawn
        yield return new WaitForSeconds(1f);
        
        // Verifikasi semua player sudah spawn
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        foreach (var client in clients)
        {
            float timeout = 5f;
            float elapsed = 0f;
            while (!spawnedPlayers.ContainsKey(client.ClientId) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        Debug.Log("All clients verified spawned");
    }

    private void SetPlayerMovementEnabled(GameObject player, bool enabled)
    {
        string state = enabled ? "ENABLED" : "DISABLED";
        Debug.Log($"[SetPlayerMovementEnabled] Setting movement {state} for {player.name}");
        
        // Disable/Enable komponen movement
        if (player.TryGetComponent<Rigidbody>(out var rb))
        {
            if (!enabled)
            {
                // Set velocity sebelum kinematic
                rb.linearVelocity = Vector3.zero;
                if (!rb.isKinematic)
                {
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
                Debug.Log($"[SetPlayerMovementEnabled] Rigidbody set to kinematic");
            }
            else
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero; // Clear velocity on unlock
                rb.angularVelocity = Vector3.zero;
                Debug.Log($"[SetPlayerMovementEnabled] Rigidbody set to non-kinematic");
            }
        }
        
        if (player.TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = enabled;
            Debug.Log($"[SetPlayerMovementEnabled] CharacterController {state}");
        }
        
        // Disable movement scripts (cari berdasarkan nama umum)
        var monoBehaviours = player.GetComponents<MonoBehaviour>();
        int scriptsModified = 0;
        foreach (var mb in monoBehaviours)
        {
            string typeName = mb.GetType().Name.ToLower();
            if (typeName.Contains("movement") || typeName.Contains("controller") || typeName.Contains("behaviour"))
            {
                // Skip NetworkBehaviour dan komponen penting
                if (mb is NetworkBehaviour) continue;
                if (typeName.Contains("network")) continue;
                
                mb.enabled = enabled;
                scriptsModified++;
            }
        }
        Debug.Log($"[SetPlayerMovementEnabled] Modified {scriptsModified} movement scripts");
    }

    private void UnlockAllPlayersMovement()
    {
        // Unlock di server
        foreach (var kvp in spawnedPlayers)
        {
            if (kvp.Value != null)
            {
                SetPlayerMovementEnabled(kvp.Value, true);
            }
        }
        
        // Kirim RPC ke semua client untuk unlock
        UnlockMovementClientRpc();
    }

    [ClientRpc]
    private void TeleportAndFreezeClientRpc(Vector3 position, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        
        StartCoroutine(TeleportAndFreezeCoroutine(position));
    }

    private IEnumerator TeleportAndFreezeCoroutine(Vector3 position)
    {
        float timeout = 5f;
        float elapsed = 0f;
        while (NetworkManager.Singleton.LocalClient?.PlayerObject == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            
            // Disable CharacterController dulu untuk teleport
            CharacterController cc = null;
            if (playerObject.TryGetComponent<CharacterController>(out cc))
            {
                cc.enabled = false;
            }
            
            // Freeze Rigidbody
            if (playerObject.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                if (!rb.isKinematic)
                {
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
            
            // Set posisi langsung
            playerObject.transform.position = position;
            
            // Tunggu beberapa frame
            yield return new WaitForSeconds(0.2f);
            
            // Set posisi lagi untuk memastikan
            playerObject.transform.position = position;
            
            // Jika menggunakan ClientNetworkTransform, panggil Teleport dari client (pemilik authority)
            if (playerObject.TryGetComponent<NetworkTransform>(out var networkTransform))
            {
                // Client adalah owner dan punya authority, jadi bisa teleport
                if (networkTransform.IsOwner)
                {
                    networkTransform.Teleport(position, Quaternion.identity, playerObject.transform.localScale);
                }
            }
            
            // Freeze movement
            SetPlayerMovementEnabled(playerObject.gameObject, false);
            
            Debug.Log($"Client teleported and frozen at: {position}");
        }
        else
        {
            Debug.LogError("TeleportAndFreezeCoroutine: PlayerObject not found after timeout!");
        }
    }

    [ClientRpc]
    private void UnlockMovementClientRpc()
    {
        if (NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            
            Debug.Log($"[Client] UnlockMovementClientRpc called for local player");
            
            // Re-enable Rigidbody FIRST
            if (playerObject.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero; // Clear any residual velocity
                rb.angularVelocity = Vector3.zero;
                Debug.Log("[Client] Rigidbody re-enabled and velocities cleared");
            }
            
            // Re-enable CharacterController
            if (playerObject.TryGetComponent<CharacterController>(out var cc))
            {
                cc.enabled = true;
                Debug.Log("[Client] CharacterController re-enabled");
            }
            
            // Re-enable movement scripts
            SetPlayerMovementEnabled(playerObject.gameObject, true);
            
            Debug.Log("Movement unlocked for local player - should be controllable now");
        }
        else
        {
            Debug.LogWarning("[Client] UnlockMovementClientRpc: PlayerObject is null!");
        }
    }

    // Legacy methods untuk kompatibilitas (tidak dipakai lagi)
    private void SpawnPlayers() { }
    
    [ClientRpc]
    private void TeleportClientRpc(Vector3 position, ulong targetClientId) { }
}
