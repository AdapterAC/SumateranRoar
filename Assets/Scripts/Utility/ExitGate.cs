using UnityEngine;
using Unity.Netcode;
using Linework.Common;

public class ExitGate : InteractableTemplate
{
    private int totalObjectives = 0;
    private int completedObjectives = 0;

    [Header("Outline Settings")]
    [Tooltip("Warna outline saat gate siap dibuka")]
    [ColorUsage(true, true)]
    [SerializeField] private Color outlineColor = new Color(0.6f, 0.9f, 1f, 1f); // default: soft cyan HDR

    [Tooltip("Rendering Layer untuk outline (sesuaikan dengan pengaturan outline URP)")]
    [SerializeField] private int outlineRenderingLayer = 1;

    [Tooltip("Aktifkan outline saat Start (misalnya untuk debugging)")]
    [SerializeField] private bool enableOutlineOnStart = false;

    [Tooltip("Aktifkan outline otomatis saat semua objektif selesai")]
    [SerializeField] private bool enableOutlineWhenUnlocked = true;

    private Renderer[] renderers;
    private OutlineOverride[] outlineOverrides;

    void Start()
    {
        // Deteksi otomatis semua RepairableObjective di scene
        RepairableObjective[] objectives = FindObjectsOfType<RepairableObjective>();
        totalObjectives = objectives.Length;

        Debug.Log("[ExitGate] Ditemukan " + totalObjectives + " objektif yang harus diselesaikan.");

        SetupOutline();
        EnableOutline(enableOutlineOnStart);
    }

    public void OnObjectiveCompleted()
    {
        completedObjectives++;
        Debug.Log("[ExitGate] Progres: " + completedObjectives + " / " + totalObjectives);
        GameStateManager.Instance.AddActivatedExitGate();
        // GameStateManager.Instance.AddOnButtonServerRpc();
        Debug.Log("[ExitGate] Progres: " + GameStateManager.Instance.GetTotalActivatedExitGates() + " / " + totalObjectives);


        if (AllObjectivesCompleted())
        {
            Debug.Log("[ExitGate] Semua objektif selesai! Pintu keluar sekarang bisa dibuka.");

            if (enableOutlineWhenUnlocked)
            {
                EnableOutline(true);
            }
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
                    Debug.LogWarning($"[ExitGate] Player {clientId} tidak terdaftar di GameStateManager! Melakukan auto-register sebagai human.");
                    GameStateManager.Instance.EnsurePlayerRegistered(clientId, false);
                    // Setelah register paksa sebagai human, langsung catat exit agar state tidak tertahan
                    GameStateManager.Instance.OnHumanExited(clientId);
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

    private void SetupOutline()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning($"[ExitGate] {gameObject.name} tidak memiliki Renderer untuk outline.");
            return;
        }

        outlineOverrides = new OutlineOverride[renderers.Length];

        uint outlineLayerMask = (uint)(1 << outlineRenderingLayer);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];
            if (rend == null) continue;

            OutlineOverride outlineOverride = rend.GetComponent<OutlineOverride>();
            if (outlineOverride == null)
            {
                outlineOverride = rend.gameObject.AddComponent<OutlineOverride>();
            }

            outlineOverride.AddColorOverride("_OutlineColor", outlineColor);

            rend.renderingLayerMask |= outlineLayerMask;
            outlineOverrides[i] = outlineOverride;
        }
    }

    private void EnableOutline(bool enable)
    {
        if (outlineOverrides == null || renderers == null) return;

        uint outlineLayerMask = (uint)(1 << outlineRenderingLayer);

        foreach (var outlineOverride in outlineOverrides)
        {
            if (outlineOverride == null) continue;
            outlineOverride.enabled = enable;
        }

        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            if (enable)
            {
                rend.renderingLayerMask |= outlineLayerMask;
            }
            else
            {
                rend.renderingLayerMask &= ~outlineLayerMask;
            }
        }
    }

    public void SetOutlineColor(Color newColor)
    {
        outlineColor = newColor;

        if (outlineOverrides == null) return;

        foreach (var outlineOverride in outlineOverrides)
        {
            if (outlineOverride == null) continue;

            outlineOverride.overrides.Clear();
            outlineOverride.AddColorOverride("_OutlineColor", newColor);
        }
    }
}
