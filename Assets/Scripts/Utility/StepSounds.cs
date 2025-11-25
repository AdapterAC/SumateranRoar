using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class StepSounds : NetworkBehaviour
{
    [Header("Audio Source")]
    public AudioSource stepAudioSource;

    [Header("Surface Sounds")]
    public SurfaceSound[] surfaceSounds;

    [Header("Sound Settings")]
    [Tooltip("Minimum time between step sounds to prevent overlapping (in seconds)")]
    public float stepSoundCooldown = 0.3f;
    
    [Header("Debug")]
    [Tooltip("Enable to see debug logs when sounds are played or blocked")]
    public bool enableDebugLogs = false;

    private string currentSurfaceTag = "DirtyGround";
    
    // Cooldown tracking
    private float lastWalkSoundTime = -999f;
    private float lastRunSoundTime = -999f;
    private float lastJumpStartSoundTime = -999f;
    private float lastJumpEndSoundTime = -999f;
    
    void Awake()
    {
        // Ensure AudioSource exists and is enabled
        if (stepAudioSource == null)
        {
            stepAudioSource = GetComponent<AudioSource>();
            if (stepAudioSource == null)
            {
                stepAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning("StepSounds: AudioSource was missing, added automatically.");
            }
        }
        
        // Always ensure AudioSource is enabled
        if (stepAudioSource != null)
        {
            stepAudioSource.enabled = true;
            stepAudioSource.playOnAwake = false;
        }
    }

    // Last played clips to avoid repetition
    private AudioClip lastWalkSound;
    private AudioClip lastRunSound;
    private AudioClip lastJumpStartSound;
    private AudioClip lastJumpEndSound;

    void FixedUpdate()
    {
        if (!IsOwner) return;
        // Raycast to detect ground surface tag
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 3f))
        {
            currentSurfaceTag = hit.collider.tag;
        }
    }

    // Called from Animation Event
    public void PlayWalkSound()
    {
        if (!IsOwner) return;
        
        // Check cooldown to prevent rapid successive calls
        if (Time.time - lastWalkSoundTime < stepSoundCooldown)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[StepSounds] Walk sound blocked - cooldown active ({Time.time - lastWalkSoundTime:F3}s since last)");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StepSounds] Playing walk sound at {Time.time:F2}");
        }
        
        lastWalkSoundTime = Time.time;
        PlayStepSound("walk");
    }

    // Called from Animation Event
    public void PlayRunSound()
    {
        if (!IsOwner) return;
        
        // Check cooldown to prevent rapid successive calls
        if (Time.time - lastRunSoundTime < stepSoundCooldown)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[StepSounds] Run sound blocked - cooldown active ({Time.time - lastRunSoundTime:F3}s since last)");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StepSounds] Playing run sound at {Time.time:F2}");
        }
        
        lastRunSoundTime = Time.time;
        PlayStepSound("run");
    }

    // Called from MoveBehaviour
    public void PlayJumpStartSound()
    {
        if (!IsOwner) return;
        
        // Check cooldown to prevent rapid successive calls
        if (Time.time - lastJumpStartSoundTime < stepSoundCooldown)
        {
            return;
        }
        
        lastJumpStartSoundTime = Time.time;
        PlayStepSound("jumpStart");
    }

    // Called from MoveBehaviour
    public void PlayJumpEndSound()
    {
        if (!IsOwner) return;
        
        // Check cooldown to prevent rapid successive calls
        if (Time.time - lastJumpEndSoundTime < stepSoundCooldown)
        {
            return;
        }
        
        lastJumpEndSoundTime = Time.time;
        PlayStepSound("jumpEnd");
    }

    private void PlayStepSound(string type)
    {
        SurfaceSound surface = GetCurrentSurfaceSound();
        if (surface == null) return;

        AudioClip[] clips = null;
        AudioClip lastClip = null;

        switch (type)
        {
            case "walk": clips = surface.walkClips; lastClip = lastWalkSound; break;
            case "run": clips = surface.runClips; lastClip = lastRunSound; break;
            case "jumpStart": clips = surface.jumpStartClips; lastClip = lastJumpStartSound; break;
            case "jumpEnd": clips = surface.jumpEndClips; lastClip = lastJumpEndSound; break;
        }

        if (clips != null && clips.Length > 0)
        {
            int clipIndex = GetRandomClipIndex(clips, lastClip);

            // Play sound locally immediately for the owner
            PlaySoundInternal(clipIndex, type, currentSurfaceTag);

            // Tell the server to tell other clients to play the sound
            PlayStepSoundServerRpc(clipIndex, type, currentSurfaceTag);
        }
    }

    [ServerRpc]
    private void PlayStepSoundServerRpc(int clipIndex, string type, string surfaceTag)
    {
        PlayStepSoundClientRpc(clipIndex, type, surfaceTag, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                // Send to all clients except the one who sent the RPC
                TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds.Where(id => id != OwnerClientId).ToArray()
            }
        });
    }

    [ClientRpc]
    private void PlayStepSoundClientRpc(int clipIndex, string type, string surfaceTag, ClientRpcParams clientRpcParams = default)
    {
        PlaySoundInternal(clipIndex, type, surfaceTag);
    }

    private void PlaySoundInternal(int clipIndex, string type, string surfaceTag)
    {
        SurfaceSound surface = null;
        foreach (var s in surfaceSounds)
        {
            if (s.tag == surfaceTag)
            {
                surface = s;
                break;
            }
        }
        if (surface == null) return;

        AudioClip[] clips = null;
        AudioClip clipToPlay = null;

        switch (type)
        {
            case "walk":
                clips = surface.walkClips;
                if(clipIndex < clips.Length) {
                    clipToPlay = clips[clipIndex];
                    lastWalkSound = clipToPlay;
                }
                break;
            case "run":
                clips = surface.runClips;
                 if(clipIndex < clips.Length) {
                    clipToPlay = clips[clipIndex];
                    lastRunSound = clipToPlay;
                }
                break;
            case "jumpStart":
                clips = surface.jumpStartClips;
                 if(clipIndex < clips.Length) {
                    clipToPlay = clips[clipIndex];
                    lastJumpStartSound = clipToPlay;
                }
                break;
            case "jumpEnd":
                clips = surface.jumpEndClips;
                 if(clipIndex < clips.Length) {
                    clipToPlay = clips[clipIndex];
                    lastJumpEndSound = clipToPlay;
                }
                break;
        }

        if (clipToPlay != null && stepAudioSource != null)
        {
            // Ensure AudioSource is enabled before playing
            if (!stepAudioSource.enabled)
            {
                stepAudioSource.enabled = true;
            }
            
            stepAudioSource.PlayOneShot(clipToPlay);
        }
    }

    private SurfaceSound GetCurrentSurfaceSound()
    {
        foreach (var surfaceSound in surfaceSounds)
        {
            if (surfaceSound.tag == currentSurfaceTag)
            {
                return surfaceSound;
            }
        }
        // Return default if no tag is found
        foreach (var surfaceSound in surfaceSounds)
        {
            if (surfaceSound.tag == "DirtyGround")
            {
                return surfaceSound;
            }
        }
        return null;
    }

    private int GetRandomClipIndex(AudioClip[] clips, AudioClip lastClip)
    {
        if (clips.Length == 1) return 0;

        int index;
        do
        {
            index = Random.Range(0, clips.Length);
        } while (clips[index] == lastClip);

        return index;
    }
}
