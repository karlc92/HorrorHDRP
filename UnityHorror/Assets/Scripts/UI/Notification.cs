using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class Notification : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInTime = 0.2f;
    [SerializeField] private float fadeOutTime = 0.2f;

    private AudioSource audioSource;
    private CanvasGroup canvasGroup;

    private Coroutine routine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Add/get CanvasGroup for fading whole notification (bg + text).
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        label.gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
    }

    public void ShowNotification(string text, float duration = 3f, float notificationVolume = 0.5f)
    {
        label.text = text;
        label.gameObject.SetActive(true);

        audioSource.volume = Game.Settings.MasterVolume * notificationVolume;
        if (audioSource.clip != null)
            audioSource.PlayOneShot(audioSource.clip);

        // Restart if already showing
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowFadeRoutine(duration));
    }

    private IEnumerator ShowFadeRoutine(float duration)
    {
        // Fade in
        yield return FadeCanvasGroup(0f, 1f, fadeInTime);

        // Hold at full alpha
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        // Fade out
        yield return FadeCanvasGroup(1f, 0f, fadeOutTime);

        Destroy(gameObject);
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float time)
    {
        if (time <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        canvasGroup.alpha = from;

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);

            // Optional smoothing (looks nicer than linear)
            k = Mathf.SmoothStep(0f, 1f, k);

            canvasGroup.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}