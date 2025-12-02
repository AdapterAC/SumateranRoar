using UnityEngine;
using Unity.Netcode;

public class ExitGate : InteractableTemplate
{
    private int totalObjectives = 0;
    private int completedObjectives = 0;

    void Start()
    {
        // Deteksi otomatis semua RepairableObjective di scene
        RepairableObjective[] objectives = FindObjectsOfType<RepairableObjective>();
        totalObjectives = objectives.Length;

        Debug.Log("[ExitGate] Ditemukan " + totalObjectives + " objektif yang harus diselesaikan.");
    }

    public void OnObjectiveCompleted()
    {
        completedObjectives++;
        Debug.Log("[ExitGate] Progres: " + completedObjectives + " / " + totalObjectives);

        if (AllObjectivesCompleted())
        {
            Debug.Log("[ExitGate] Semua objektif selesai! Pintu keluar sekarang bisa dibuka.");
        }
    }

    public bool AllObjectivesCompleted()
    {
        return completedObjectives >= totalObjectives && totalObjectives > 0;
    }

    public override bool Interact(GameObject interactor)
    {
        if (AllObjectivesCompleted())
        {
            Debug.Log("[ExitGate] Pintu terbuka! " + interactor.name + " berhasil keluar.");
            
            // Panggil event onInteract untuk memicu animasi pintu terbuka atau logika kemenangan
            base.Interact(interactor);
            
            // Notify GameStateManager jika interactor adalah human player
            if (interactor.TryGetComponent<NetworkObject>(out var netObj))
            {
                ulong clientId = netObj.OwnerClientId;
                
                // Cek apakah GameStateManager tersedia
                if (GameStateManager.Instance == null)
                {
                    Debug.LogError("[ExitGate] GameStateManager tidak ditemukan! Human exit tidak tercatat.");
                    return true; // Still allow exit for gameplay
                }
                
                // Cek apakah ini human (bukan tiger)
                if (GameStateManager.Instance.IsHuman(clientId))
                {
                    Debug.Log($"[ExitGate] Human player (ClientId: {clientId}) berhasil keluar!");
                    GameStateManager.Instance.OnHumanExited(clientId);
                }
                else if (GameStateManager.Instance.IsTiger(clientId))
                {
                    Debug.Log("[ExitGate] Tiger tidak bisa keluar melalui exit gate!");
                    return false;
                }
                else
                {
                    Debug.LogWarning($"[ExitGate] Player {clientId} tidak terdaftar di GameStateManager!");
                }
            }
            
            return true;
        }
        else
        {
            int remaining = totalObjectives - completedObjectives;
            Debug.Log("[ExitGate] Pintu masih terkunci. Selesaikan " + remaining + " objektif lagi.");
            // Mungkin bisa memutar suara pintu terkunci atau menampilkan pesan di UI
            return false;
        }
    }
}
