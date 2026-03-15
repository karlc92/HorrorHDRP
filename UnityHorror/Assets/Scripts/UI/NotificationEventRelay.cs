using UnityEngine;

[AddComponentMenu("Horror/UI/Notification Event Relay")]
public class NotificationEventRelay : MonoBehaviour
{
    [TextArea(1, 3)]
    [SerializeField] private string message = "Notification";
    [SerializeField, Min(0.1f)] private float duration = 2.5f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    public void Configure(string notificationMessage, float notificationDuration = 2.5f, float notificationVolume = 0.5f)
    {
        message = notificationMessage;
        duration = Mathf.Max(0.1f, notificationDuration);
        volume = Mathf.Clamp01(notificationVolume);
    }

    public void ShowConfiguredNotification()
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var gameUi = FindFirstObjectByType<GameUI>();
        gameUi?.ShowNotification(message, duration, volume);
    }
}
