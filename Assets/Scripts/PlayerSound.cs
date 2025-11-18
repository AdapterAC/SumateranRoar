using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] walkSounds;
    public AudioClip[] runSounds;

    public void PlayWalkSound()
    {
        if (walkSounds.Length > 0)
        {
            int index = Random.Range(0, walkSounds.Length);
            audioSource.clip = walkSounds[index];
            audioSource.Play();
        }
    }

    public void PlayRunSound()
    {
        if (runSounds.Length > 0)
        {
            int index = Random.Range(0, runSounds.Length);
            audioSource.clip = runSounds[index];
            audioSource.Play();
        }
    }
}
