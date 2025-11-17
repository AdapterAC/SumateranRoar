using UnityEngine;

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
            // TODO: Tambahkan logika untuk memenangkan permainan di sini
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
