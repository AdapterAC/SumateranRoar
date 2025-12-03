using UnityEngine;
using Linework.Common;

public class RepairableObjective : InteractableTemplate
{
    [Header("Objective Settings")]
    [Tooltip("Apakah objek ini sudah diperbaiki?")]
    private bool isRepaired = false;

    [Header("Outline Settings")]
    [Tooltip("Warna outline saat objective belum diperbaiki")]
    [ColorUsage(true, true)]
    public Color outlineColor = new Color(1f, 0.8f, 0f, 1f); // Kuning/Gold HDR
    
    [Tooltip("Rendering Layer untuk outline (harus sesuai dengan Fast/Soft/Wide Outline Settings di URP Renderer)")]
    public int outlineRenderingLayer = 1;
    
    [Tooltip("Aktifkan outline otomatis saat Start")]
    public bool enableOutlineOnStart = true;

    private ExitGate exitGate;
    private Renderer[] renderers;
    private OutlineOverride[] outlineOverrides;
    private uint originalRenderingLayerMask;

    void Start()
    {
        // Cari ExitGate di scene
        exitGate = FindObjectOfType<ExitGate>();
        if (exitGate == null)
        {
            Debug.LogError("Tidak ada ExitGate di scene!");
        }

        // Setup outline untuk objective
        SetupOutline();
    }

    /// <summary>
    /// Setup outline untuk semua renderer pada objective ini
    /// </summary>
    private void SetupOutline()
    {
        // Cari semua renderer di object ini dan child-nya
        renderers = GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[RepairableObjective] {gameObject.name} tidak memiliki Renderer!");
            return;
        }

        outlineOverrides = new OutlineOverride[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];
            
            // Simpan rendering layer mask original
            if (i == 0) originalRenderingLayerMask = rend.renderingLayerMask;
            
            // Tambahkan OutlineOverride jika belum ada
            OutlineOverride outlineOverride = rend.GetComponent<OutlineOverride>();
            if (outlineOverride == null)
            {
                outlineOverride = rend.gameObject.AddComponent<OutlineOverride>();
            }
            
            outlineOverrides[i] = outlineOverride;
            
            // Set rendering layer mask untuk outline (bit shift untuk layer)
            // Rendering Layer 1 = bit 0 (0x1), Layer 2 = bit 1 (0x2), dst.
            uint outlineLayerMask = (uint)(1 << outlineRenderingLayer);
            rend.renderingLayerMask |= outlineLayerMask;
            
            // Set warna outline via OutlineOverride
            outlineOverride.AddColorOverride("_OutlineColor", outlineColor);
        }

        // Aktifkan outline jika diset
        if (enableOutlineOnStart && !isRepaired)
        {
            EnableOutline(true);
        }
    }

    /// <summary>
    /// Aktifkan atau nonaktifkan outline
    /// </summary>
    public void EnableOutline(bool enable)
    {
        if (outlineOverrides == null) return;
        
        foreach (var outlineOverride in outlineOverrides)
        {
            if (outlineOverride != null)
            {
                outlineOverride.enabled = enable;
            }
        }
        
        // Toggle rendering layer mask
        if (renderers != null)
        {
            uint outlineLayerMask = (uint)(1 << outlineRenderingLayer);
            foreach (var rend in renderers)
            {
                if (rend != null)
                {
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
        }
    }

    /// <summary>
    /// Set warna outline secara dinamis
    /// </summary>
    public void SetOutlineColor(Color newColor)
    {
        outlineColor = newColor;
        
        if (outlineOverrides == null) return;
        
        foreach (var outlineOverride in outlineOverrides)
        {
            if (outlineOverride != null)
            {
                // Clear overrides dan tambah baru
                outlineOverride.overrides.Clear();
                outlineOverride.AddColorOverride("_OutlineColor", newColor);
            }
        }
    }

    public override bool Interact(GameObject interactor)
    {
        if (isRepaired)
        {
            Debug.Log(gameObject.name + " sudah diperbaiki.");
            return false;
        }

        isRepaired = true;
        
        // Nonaktifkan outline setelah diperbaiki
        EnableOutline(false);
        
        // Panggil event onInteract jika ada
        base.Interact(interactor);

        if (exitGate != null)
        {
            exitGate.OnObjectiveCompleted();
        }

        Debug.Log(interactor.name + " telah memperbaiki " + gameObject.name);
        return true;
    }

    /// <summary>
    /// Cek apakah objective sudah diperbaiki
    /// </summary>
    public bool IsRepaired()
    {
        return isRepaired;
    }

    private void OnDestroy()
    {
        // Cleanup outline overrides jika perlu
        if (outlineOverrides != null)
        {
            foreach (var outlineOverride in outlineOverrides)
            {
                if (outlineOverride != null && outlineOverride.gameObject != null)
                {
                    // Hanya destroy jika komponen ditambahkan oleh script ini
                    // dan bukan dari Inspector
                }
            }
        }
    }
}
