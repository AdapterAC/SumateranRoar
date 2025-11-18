using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    public AudioSource audioSource;
    public SurfaceSound[] surfaceSounds;

    private string currentSurfaceTag = "DirtyGround";
    private AudioClip lastWalkSound;
    private AudioClip lastRunSound;
    private AudioClip lastJumpStartSound;
    private AudioClip lastJumpEndSound;

    void FixedUpdate()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 3f))
        {
            currentSurfaceTag = hit.collider.tag;
        }
    }

    public void PlayWalkSound()
    {
        SurfaceSound surfaceSound = GetCurrentSurfaceSound();
        if (surfaceSound != null && surfaceSound.walkClips.Length > 0)
        {
            AudioClip clipToPlay = GetRandomClip(surfaceSound.walkClips, lastWalkSound);
            lastWalkSound = clipToPlay;
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    public void PlayRunSound()
    {
        SurfaceSound surfaceSound = GetCurrentSurfaceSound();
        if (surfaceSound != null && surfaceSound.runClips.Length > 0)
        {
            AudioClip clipToPlay = GetRandomClip(surfaceSound.runClips, lastRunSound);
            lastRunSound = clipToPlay;
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    public void PlayJumpStartSound()
    {
        SurfaceSound surfaceSound = GetCurrentSurfaceSound();
        if (surfaceSound != null && surfaceSound.jumpStartClips.Length > 0)
        {
            AudioClip clipToPlay = GetRandomClip(surfaceSound.jumpStartClips, lastJumpStartSound);
            lastJumpStartSound = clipToPlay;
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    public void PlayJumpEndSound()
    {
        SurfaceSound surfaceSound = GetCurrentSurfaceSound();
        if (surfaceSound != null && surfaceSound.jumpEndClips.Length > 0)
        {
            AudioClip clipToPlay = GetRandomClip(surfaceSound.jumpEndClips, lastJumpEndSound);
            lastJumpEndSound = clipToPlay;
            audioSource.PlayOneShot(clipToPlay);
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

    private AudioClip GetRandomClip(AudioClip[] clips, AudioClip lastClip)
    {
        if (clips.Length == 1)
        {
            return clips[0];
        }

        AudioClip clip;
        do
        {
            clip = clips[Random.Range(0, clips.Length)];
        } while (clip == lastClip);

        return clip;
    }
}
