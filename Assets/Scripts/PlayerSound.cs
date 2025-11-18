using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] walkSounds;
    public AudioClip[] runSounds;

    private AudioClip lastWalkSound;
    private AudioClip lastRunSound;

    public void PlayWalkSound()
    {
        if (walkSounds.Length > 0)
        {
            AudioClip clipToPlay = GetRandomClip(walkSounds, lastWalkSound);
            lastWalkSound = clipToPlay;
            audioSource.clip = clipToPlay;
            audioSource.Play();
        }
    }

    public void PlayRunSound()
    {
        if (runSounds.Length > 0)
        {
            AudioClip clipToPlay = GetRandomClip(runSounds, lastRunSound);
            lastRunSound = clipToPlay;
            audioSource.clip = clipToPlay;
            audioSource.Play();
        }
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
