using UnityEngine;

public class MonsterAudio : MonoBehaviour
{
    private AudioSource audioSource;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayFootstep()
    {
        if (audioSource == null) return;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.volume = Game.Settings.MasterVolume * 2f;
        audioSource.PlayOneShot(audioSource.clip);
    }
}
