using TMPro;
using UnityEngine;

public class Notification : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI label;

    AudioSource audioSource;
    public void ShowNotification(string text, float duration = 3f, float notificationVolume = 0.5f) 
    {
        timeToDestroy = Time.time + duration;
        label.text = text;
        label.gameObject.SetActive(true);
        audioSource.volume = Game.Settings.MasterVolume * notificationVolume;
        audioSource.PlayOneShot(audioSource.clip);
    }
    float timeToDestroy = -1;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        label.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (timeToDestroy > 0 && timeToDestroy < Time.time)
        {
            Destroy(gameObject);
        }
    }
}
