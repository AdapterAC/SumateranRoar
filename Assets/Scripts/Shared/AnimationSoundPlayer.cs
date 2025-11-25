using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Plays sounds based on Animation Events.
/// Attach this script to a GameObject with an AudioSource and animations.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AnimationSoundPlayer : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
    }

    [Tooltip("The list of sounds that can be played by this component.")]
    public List<Sound> sounds = new List<Sound>();
    
    private AudioSource audioSource;

    void Awake()
    {
        // Get the AudioSource component attached to this GameObject.
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// This method is called from Animation Events to play a sound by name.
    /// Use this method name in your Animation Events.
    /// </summary>
    /// <param name="soundName">The name of the sound to play, as defined in the 'sounds' list.</param>
    public void PlaySoundByName(string soundName)
    {
        // Find the sound in the list by its name.
        Sound soundToPlay = sounds.FirstOrDefault(s => s.name == soundName);

        if (soundToPlay != null && soundToPlay.clip != null)
        {
            // Play the found audio clip as a one-shot sound.
            // PlayOneShot allows multiple sounds to be played without cutting each other off.
            audioSource.PlayOneShot(soundToPlay.clip);
        }
        else
        {
            // Log a warning if the sound name is not found in the list.
            Debug.LogWarning("Sound not found in AnimationSoundPlayer: " + soundName, this);
        }
    }
}
