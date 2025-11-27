using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerSpawnManager))]
public class GameManagerNetwork : NetworkBehaviour
{
    public static GameManagerNetwork Instance;

    private PlayerSpawnManager spawnManager;

    private void Awake()
    {
        Instance = this;
        spawnManager = GetComponent<PlayerSpawnManager>();
    }

    public override void OnNetworkSpawn()
    {
        ScreenLogger.Log($"GameManagerNetwork - OnNetworkSpawn - Berhasil dijalankan", ScreenLogger.LogType.Success);
        if (IsServer && !NetworkObject.IsSpawned) NetworkObject.Spawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        ScreenLogger.Log($"StartGameServerRpc - Berhasil dijalankan", ScreenLogger.LogType.Success);
        NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
    }
}
