using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class InteractableTemplate : MonoBehaviour
{
    [Tooltip("Durasi animasi interaksi (detik). Samakan dengan panjang clip Interact).")]
    public float interactDuration = 1.5f;

    [Tooltip("Event yang dipicu saat interaksi dimulai.")]
    public UnityEvent onInteract;

    public virtual bool Interact(GameObject interactor)
    {
        onInteract?.Invoke();
        return true;
    }
}