using UnityEngine;

public class AmbientAudio : MonoBehaviour
{
    public float Volume = 1.0f;

    private AudioSource audioSource;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void FixedUpdate()
    {
        if (audioSource == null) return;

        if (!audioSource.loop)
        {
            audioSource.loop = true;
        }

        if (audioSource.volume != Volume * Game.Settings.MasterVolume)
        {
            audioSource.volume = Volume * Game.Settings.MasterVolume;
        }
    }
}
