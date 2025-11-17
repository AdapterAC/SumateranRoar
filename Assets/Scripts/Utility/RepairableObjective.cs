using UnityEngine;

public class RepairableObjective : InteractableTemplate
{
    [Tooltip("Apakah objek ini sudah diperbaiki?")]
    private bool isRepaired = false;

    private ExitGate exitGate;

    void Start()
    {
        // Cari ExitGate di scene
        exitGate = FindObjectOfType<ExitGate>();
        if (exitGate == null)
        {
            Debug.LogError("Tidak ada ExitGate di scene!");
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (isRepaired)
        {
            Debug.Log(gameObject.name + " sudah diperbaiki.");
            return;
        }

        isRepaired = true;
        
        // Panggil event onInteract jika ada
        base.Interact(interactor);

        if (exitGate != null)
        {
            exitGate.OnObjectiveCompleted();
        }

        Debug.Log(interactor.name + " telah memperbaiki " + gameObject.name);
    }
}
