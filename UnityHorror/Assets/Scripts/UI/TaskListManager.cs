using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TaskListManager : MonoBehaviour
{
    public static TaskListManager Instance { get; private set; }

    [Header("Debug UI")]
    [SerializeField] private bool autoCreateDebugOverlay = true;
    [SerializeField] private TMP_FontAsset overlayFont;
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private TextMeshProUGUI overlayText;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (autoCreateDebugOverlay)
            EnsureDebugOverlay();

        ApplyOverlayState();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            IsOpen = !IsOpen;
            ApplyOverlayState();
        }

        if (IsOpen)
            RefreshOverlay();
    }

    public IReadOnlyList<TaskListEntryViewData> GetCurrentEntries()
    {
        return TaskManager.Instance != null
            ? TaskManager.Instance.GetCurrentNightTaskEntries()
            : new List<TaskListEntryViewData>();
    }

    private void EnsureDebugOverlay()
    {
        if (overlayCanvas == null)
        {
            var canvasObject = new GameObject("TaskListDebugCanvas");
            canvasObject.transform.SetParent(transform, false);

            overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (overlayText == null)
        {
            var textObject = new GameObject("TaskListDebugText");
            textObject.transform.SetParent(overlayCanvas.transform, false);

            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(700f, 900f);

            var image = textObject.AddComponent<Image>();
            image.color = new Color(0.05f, 0.03f, 0.01f, 0.82f);

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(textObject.transform, false);

            var labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 20f);
            labelRect.offsetMax = new Vector2(-20f, -20f);

            overlayText = labelObject.AddComponent<TextMeshProUGUI>();
            overlayText.fontSize = 26f;
            overlayText.textWrappingMode = TextWrappingModes.Normal;
            overlayText.alignment = TextAlignmentOptions.TopLeft;
            overlayText.color = new Color(0.96f, 0.93f, 0.82f, 1f);
            overlayText.font = overlayFont != null ? overlayFont : TMP_Settings.defaultFontAsset;
        }
        else if (overlayFont != null && overlayText.font != overlayFont)
        {
            overlayText.font = overlayFont;
        }
    }

    private void ApplyOverlayState()
    {
        if (overlayCanvas != null)
            overlayCanvas.gameObject.SetActive(IsOpen);

        if (IsOpen)
            RefreshOverlay();
    }

    private void RefreshOverlay()
    {
        if (overlayText == null)
            return;

        var sb = new StringBuilder();
        int nightNumber = Game.State?.Run?.CurrentNightNumber ?? 0;
        sb.Append("Night ").Append(nightNumber).AppendLine();
        sb.AppendLine();

        var entries = GetCurrentEntries();
        if (entries == null || entries.Count == 0)
        {
            sb.AppendLine("No active tasks.");
            overlayText.text = sb.ToString();
            return;
        }

        foreach (var entry in entries)
        {
            string title = ResolveText(entry.TitleKey);
            sb.Append(entry.Completed ? "[x] " : "[ ] ");
            sb.AppendLine(string.IsNullOrWhiteSpace(title) ? entry.TitleKey : title);

            if (entry.Details != null)
            {
                foreach (var detail in entry.Details)
                {
                    if (detail == null)
                        continue;

                    string detailText = ResolveText(detail.Key);
                    sb.Append("    ");
                    sb.Append(detail.IsSatisfied ? "- [x] " : "- [ ] ");
                    sb.AppendLine(string.IsNullOrWhiteSpace(detailText) ? detail.Key : detailText);
                }
            }

            sb.AppendLine();
        }

        overlayText.text = sb.ToString();
    }

    private static string ResolveText(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (LocalizationManager.Instance == null)
            return key;

        return LocalizationManager.Instance.Get(key, key);
    }
}
