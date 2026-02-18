using TMPro;
using UnityEngine;

public class FpsLabel : MonoBehaviour
{
    private TextMeshProUGUI label;

    // Optional smoothing so it doesn't flicker wildly
    [SerializeField, Range(0.0f, 1.0f)] private float smoothing = 0.1f;

    private float smoothedDeltaTime;

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponent<TextMeshProUGUI>();
        }
        smoothedDeltaTime = Time.unscaledDeltaTime;
    }

    private void Update()
    {
        if (!label) return;

        // Exponential smoothing of deltaTime (unscaled so timeScale changes don't affect FPS)
        smoothedDeltaTime = Mathf.Lerp(smoothedDeltaTime, Time.unscaledDeltaTime, smoothing);

        float fps = 1f / Mathf.Max(smoothedDeltaTime, 0.000001f);
        label.text = $"{fps:0} FPS";
    }
}
