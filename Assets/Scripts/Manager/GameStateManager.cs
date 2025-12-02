using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Mengelola state win/lose dalam game.
/// - HumanWin: Semua human berhasil masuk ke ExitGate
/// - TigerWin: Semua human mati
/// </summary>
public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Win/Lose Scenes")]
    [SerializeField] private string humanWinSceneName = "HumanWin";
    [SerializeField] private string tigerWinSceneName = "TigerWin";

    [Header("Game State")]
    private NetworkVariable<int> totalHumans = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> humansExited = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> humansDead = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Track player roles (clientId -> isTiger)
    private Dictionary<ulong, bool> playerRoles = new Dictionary<ulong, bool>();

    // Track living humans (for faster lookup)
    private HashSet<ulong> livingHumanClientIds = new HashSet<ulong>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Make persistent across scene loads
        // Note: NetworkObject with Spawn(destroyWithScene: false) already handles this
        // but we add extra safety
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Debug.Log("[GameStateManager] Spawned on Server - Ready to track game state");
        }
        else
        {
            Debug.Log("[GameStateManager] Spawned on Client");
        }

        // Subscribe to network variable changes for logging
        totalHumans.OnValueChanged += OnTotalHumansChanged;
        humansExited.OnValueChanged += OnHumansExitedChanged;
        humansDead.OnValueChanged += OnHumansDeadChanged;
        gameEnded.OnValueChanged += OnGameEndedChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        totalHumans.OnValueChanged -= OnTotalHumansChanged;
        humansExited.OnValueChanged -= OnHumansExitedChanged;
        humansDead.OnValueChanged -= OnHumansDeadChanged;
        gameEnded.OnValueChanged -= OnGameEndedChanged;
    }

    #region Player Registration

    /// <summary>
    /// Register player dengan role-nya (dipanggil dari GamePlayController saat spawn)
    /// </summary>
    public void RegisterPlayer(ulong clientId, bool isTiger)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[GameStateManager] RegisterPlayer hanya bisa dipanggil di Server!");
            return;
        }

        playerRoles[clientId] = isTiger;

        if (!isTiger)
        {
            totalHumans.Value++;
            livingHumanClientIds.Add(clientId);
            Debug.Log($"[GameStateManager] Human registered (ClientId: {clientId}). Total humans: {totalHumans.Value}");
        }
        else
        {
            Debug.Log($"[GameStateManager] Tiger registered (ClientId: {clientId})");
        }
    }

    /// <summary>
    /// Cek apakah player adalah tiger
    /// </summary>
    public bool IsTiger(ulong clientId)
    {
        return playerRoles.TryGetValue(clientId, out bool isTiger) && isTiger;
    }

    /// <summary>
    /// Cek apakah player adalah human
    /// </summary>
    public bool IsHuman(ulong clientId)
    {
        return playerRoles.TryGetValue(clientId, out bool isTiger) && !isTiger;
    }

    #endregion

    #region Human Exit Tracking

    /// <summary>
    /// Dipanggil saat human berhasil masuk ke ExitGate
    /// </summary>
    public void OnHumanExited(ulong clientId)
    {
        if (!IsServer)
        {
            OnHumanExitedServerRpc(clientId);
            return;
        }

        if (!IsHuman(clientId))
        {
            Debug.LogWarning($"[GameStateManager] ClientId {clientId} bukan human!");
            return;
        }

        if (gameEnded.Value)
        {
            Debug.LogWarning("[GameStateManager] Game sudah berakhir, tidak bisa exit lagi.");
            return;
        }

        // Remove from living humans
        livingHumanClientIds.Remove(clientId);

        humansExited.Value++;
        Debug.Log($"[GameStateManager] Human exited (ClientId: {clientId}). {humansExited.Value}/{totalHumans.Value} exited.");

        CheckWinCondition();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnHumanExitedServerRpc(ulong clientId)
    {
        OnHumanExited(clientId);
    }

    #endregion

    #region Human Death Tracking

    /// <summary>
    /// Dipanggil saat human mati
    /// </summary>
    public void OnHumanDied(ulong clientId)
    {
        Debug.Log($"[GameStateManager] OnHumanDied called for ClientId: {clientId}");
        
        if (!IsServer)
        {
            Debug.Log($"[GameStateManager] Not server, calling ServerRpc");
            OnHumanDiedServerRpc(clientId);
            return;
        }

        if (!IsHuman(clientId))
        {
            Debug.LogWarning($"[GameStateManager] ClientId {clientId} bukan human! (Mungkin Tiger atau tidak terdaftar)");
            return;
        }

        if (gameEnded.Value)
        {
            Debug.LogWarning("[GameStateManager] Game sudah berakhir, tidak ada efek death.");
            return;
        }

        // Remove from living humans
        livingHumanClientIds.Remove(clientId);

        humansDead.Value++;
        Debug.Log($"[GameStateManager] Human died (ClientId: {clientId}). Progress: {humansDead.Value}/{totalHumans.Value} dead.");

        CheckLoseCondition();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnHumanDiedServerRpc(ulong clientId)
    {
        OnHumanDied(clientId);
    }

    #endregion

    #region Win/Lose Conditions

    /// <summary>
    /// Cek apakah semua human sudah exit (Human Win)
    /// </summary>
    private void CheckWinCondition()
    {
        if (!IsServer) return;
        if (gameEnded.Value) return;

        if (humansExited.Value >= totalHumans.Value && totalHumans.Value > 0)
        {
            Debug.Log("[GameStateManager] *** HUMAN WIN! Semua human berhasil keluar! ***");
            TriggerHumanWin();
        }
    }

    /// <summary>
    /// Cek apakah semua human sudah mati (Tiger Win)
    /// </summary>
    private void CheckLoseCondition()
    {
        if (!IsServer) return;
        if (gameEnded.Value) return;

        if (humansDead.Value >= totalHumans.Value && totalHumans.Value > 0)
        {
            Debug.Log("[GameStateManager] *** TIGER WIN! Semua human mati! ***");
            TriggerTigerWin();
        }
    }

    /// <summary>
    /// Trigger Human Win - load HumanWin scene
    /// </summary>
    private void TriggerHumanWin()
    {
        if (!IsServer) return;
        if (gameEnded.Value) return;

        gameEnded.Value = true;

        Debug.Log($"[GameStateManager] Loading {humanWinSceneName} scene...");

        // Load scene melalui NetworkManager untuk sinkronisasi semua client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(humanWinSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("[GameStateManager] NetworkManager atau SceneManager tidak tersedia!");
        }
    }

    /// <summary>
    /// Trigger Tiger Win - load TigerWin scene
    /// </summary>
    private void TriggerTigerWin()
    {
        if (!IsServer) return;
        if (gameEnded.Value) return;

        gameEnded.Value = true;

        Debug.Log($"[GameStateManager] Loading {tigerWinSceneName} scene...");

        // Load scene melalui NetworkManager untuk sinkronisasi semua client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(tigerWinSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("[GameStateManager] NetworkManager atau SceneManager tidak tersedia!");
        }
    }

    #endregion

    #region Network Variable Callbacks (for logging)

    private void OnTotalHumansChanged(int previousValue, int newValue)
    {
        Debug.Log($"[GameStateManager] Total humans changed: {previousValue} -> {newValue}");
    }

    private void OnHumansExitedChanged(int previousValue, int newValue)
    {
        Debug.Log($"[GameStateManager] Humans exited changed: {previousValue} -> {newValue}");
    }

    private void OnHumansDeadChanged(int previousValue, int newValue)
    {
        Debug.Log($"[GameStateManager] Humans dead changed: {previousValue} -> {newValue}");
    }

    private void OnGameEndedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("[GameStateManager] Game has ended!");
        }
    }

    #endregion

    #region Public Getters (for UI)

    public int GetTotalHumans() => totalHumans.Value;
    public int GetHumansExited() => humansExited.Value;
    public int GetHumansDead() => humansDead.Value;
    public int GetLivingHumans() => totalHumans.Value - humansExited.Value - humansDead.Value;
    public bool IsGameEnded() => gameEnded.Value;

    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
