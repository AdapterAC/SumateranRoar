using UnityEngine;
using Unity.Netcode;

public class PlayerVoice : NetworkBehaviour
{
    [Header("Audio Source")]
    public AudioSource voiceAudioSource;

    [Header("Sound Clips")]
    public AudioClip[] idleClips;
    public AudioClip[] hitClips;
    public AudioClip[] injuredWalkClips;

    [Header("Settings")]
    public float minIdleWaitTime = 8f;
    public float maxIdleWaitTime = 20f;
    public int injuredLayerIndex = 1;

    private Animator animator;
    private float idleTimer;
    private float timeUntilNextIdle;

    // Last played clips to avoid repetition
    private AudioClip lastIdleSound;
    private AudioClip lastHitSound;
    private AudioClip lastInjuredWalkSound;

    void Start()
    {
        animator = GetComponent<Animator>();
        ResetIdleTimer();
    }

    void Update()
    {
        if (!IsOwner) return;

        // Idle sound logic
        if (animator != null && animator.GetFloat("Speed") < 0.1f)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= timeUntilNextIdle)
            {
                PlayIdleSound();
                ResetIdleTimer();
            }
        }
        else
        {
            ResetIdleTimer();
        }
    }

    private void ResetIdleTimer()
    {
        idleTimer = 0f;
        timeUntilNextIdle = Random.Range(minIdleWaitTime, maxIdleWaitTime);
    }

    // Called from Animation Event
    public void PlayInjuredGroomingSound()
    {
        if (!IsOwner) return;
        if (animator != null && animator.GetLayerWeight(injuredLayerIndex) > 0.5f)
        {
            if (injuredWalkClips.Length > 0 && !voiceAudioSource.isPlaying)
            {
                int clipIndex = GetRandomClipIndex(injuredWalkClips, lastInjuredWalkSound);
                PlayVoiceSoundServerRpc(clipIndex, "injured");
            }
        }
    }

    public void PlayIdleSound()
    {
        if (idleClips.Length > 0 && !voiceAudioSource.isPlaying)
        {
            int clipIndex = GetRandomClipIndex(idleClips, lastIdleSound);
            PlayVoiceSoundServerRpc(clipIndex, "idle");
        }
    }

    public void PlayHitSound()
    {
        if (!IsOwner) return; // Can be called from a health script
        if (hitClips.Length > 0)
        {
            int clipIndex = GetRandomClipIndex(hitClips, lastHitSound);
            PlayVoiceSoundServerRpc(clipIndex, "hit");
        }
    }

    [ServerRpc]
    private void PlayVoiceSoundServerRpc(int clipIndex, string type)
    {
        PlayVoiceSoundClientRpc(clipIndex, type);
    }

    [ClientRpc]
    private void PlayVoiceSoundClientRpc(int clipIndex, string type)
    {
        AudioClip clipToPlay = null;
        switch (type)
        {
            case "idle":
                if (clipIndex < idleClips.Length)
                {
                    clipToPlay = idleClips[clipIndex];
                    lastIdleSound = clipToPlay;
                }
                break;
            case "hit":
                if (clipIndex < hitClips.Length)
                {
                    clipToPlay = hitClips[clipIndex];
                    lastHitSound = clipToPlay;
                }
                break;
            case "injured":
                 if (clipIndex < injuredWalkClips.Length)
                {
                    clipToPlay = injuredWalkClips[clipIndex];
                    lastInjuredWalkSound = clipToPlay;
                }
                break;
        }

        if (clipToPlay != null)
        {
            voiceAudioSource.PlayOneShot(clipToPlay);
        }
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
