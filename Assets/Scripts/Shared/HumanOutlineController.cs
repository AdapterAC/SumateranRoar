using System.Linq;
using UnityEngine;
using Linework.Common;

/// Controls Linework outline for human characters:
/// - Outline ON only when walking (Speed > threshold and not crouching)
/// - Outline OFF when idle or crouching
/// Visibility is driven by URP Rendering Layer; set the same layer index
/// used by your Soft/Fast/Wide Outline Settings in the renderer.
[RequireComponent(typeof(Animator))]
public class HumanOutlineController : MonoBehaviour
{
    [Header("Outline Settings")] 
    [Tooltip("Rendering Layer index used by Outline renderer feature (match URP Outline Settings)")] 
    [Range(0, 31)]
    public int outlineRenderingLayer = 1; // default matches RepairableObjective

    [Tooltip("Speed threshold above which we consider the player walking")] 
    public float walkSpeedThreshold = 0.05f;

    [Tooltip("Animator float parameter name for movement speed")] 
    public string speedParam = "Speed";

    [Tooltip("Animator bool parameter name for crouch state")] 
    public string crouchParam = "IsCrouching";

    private Animator animator;
    private int speedHash;
    private int crouchHash;

    private Renderer[] renderers;
    private OutlineOverride[] outlineOverrides;
    private uint outlineLayerMask;

    void Awake()
    {
        animator = GetComponent<Animator>();
        speedHash = Animator.StringToHash(speedParam);
        crouchHash = Animator.StringToHash(crouchParam);

        // Collect all visible renderers (SkinnedMeshRenderer + MeshRenderer)
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true)
            .Where(r => !(r is ParticleSystemRenderer))
            .ToArray();

        outlineOverrides = new OutlineOverride[renderers.Length];
        outlineLayerMask = (uint)(1 << outlineRenderingLayer);

        for (int i = 0; i < renderers.Length; i++)
        {
            var rend = renderers[i];
            var ov = rend.GetComponent<OutlineOverride>();
            if (ov == null)
            {
                ov = rend.gameObject.AddComponent<OutlineOverride>();
            }
            ov.enabled = false; // default OFF
            outlineOverrides[i] = ov;
        }
    }

    void OnEnable()
    {
        // Ensure outline starts disabled
        SetOutlineEnabled(false);
    }

    void Update()
    {
        if (animator == null) return;

        float speed = animator.GetFloat(speedHash);
        bool isCrouching = animator.GetBool(crouchHash);

        bool shouldOutline = !isCrouching && speed > walkSpeedThreshold;
        SetOutlineEnabled(shouldOutline);
    }

    private void SetOutlineEnabled(bool enable)
    {
        if (outlineOverrides == null || renderers == null) return;

        for (int i = 0; i < outlineOverrides.Length; i++)
        {
            var ov = outlineOverrides[i];
            var rend = renderers[i];
            if (ov == null || rend == null) continue;

            ov.enabled = enable;

            // Toggle rendering layer bit for the outline feature visibility
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
