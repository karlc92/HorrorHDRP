using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StoryPanelManager : MonoBehaviour
{
    public static StoryPanelManager Instance { get; private set; }

    [Header("Debug UI")]
    [SerializeField] private bool autoCreateOverlay = true;
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

        if (autoCreateOverlay)
            EnsureOverlay();

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

    private void EnsureOverlay()
    {
        if (overlayCanvas == null)
        {
            var canvasObject = new GameObject("StoryPanelCanvas");
            canvasObject.transform.SetParent(transform, false);

            overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (overlayText == null)
        {
            var textObject = new GameObject("StoryPanelText");
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

        if (StoryGameManager.Instance == null)
        {
            overlayText.text = "No story manager.";
            return;
        }

        string title = ResolveText(StoryGameManager.Instance.GetCurrentObjectiveTitle());
        string detail = ResolveText(StoryGameManager.Instance.GetCurrentObjectiveDetail());

        if (string.IsNullOrWhiteSpace(detail))
            overlayText.text = $"Story\n\n{title}";
        else
            overlayText.text = $"Story\n\n{title}\n\n{detail}";
    }

    private static string ResolveText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (LocalizationManager.Instance == null)
            return text;

        return LocalizationManager.Instance.Get(text, text);
    }
}
