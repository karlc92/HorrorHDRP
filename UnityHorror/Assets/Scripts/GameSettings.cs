using System;
using UnityEngine;

public enum Languages
{
    English,
    German
}

public enum TextureQuality
{
    Full = 0,
    Half = 1,
    Quarter = 2,
    Eighth = 3
}

[Serializable]
public sealed class PlayerInputSettings
{
    public event Action Changed;

    string moveUp = "<Keyboard>/w";
    string moveDown = "<Keyboard>/s";
    string moveLeft = "<Keyboard>/a";
    string moveRight = "<Keyboard>/d";
    string moveGamepad = "<Gamepad>/leftStick";
    string lookPointer = "<Pointer>/delta";
    string lookGamepad = "<Gamepad>/rightStick";
    string sprintKeyboard = "<Keyboard>/leftShift";
    string sprintGamepad = "<Gamepad>/leftStickPress";
    string crouchKeyboard = "<Keyboard>/c";
    string interactKeyboard = "<Keyboard>/e";
    string dropKeyboard = "<Keyboard>/g";
    string jumpKeyboard = "<Keyboard>/space";

    public string MoveUp { get => moveUp; set => SetBinding(ref moveUp, value); }
    public string MoveDown { get => moveDown; set => SetBinding(ref moveDown, value); }
    public string MoveLeft { get => moveLeft; set => SetBinding(ref moveLeft, value); }
    public string MoveRight { get => moveRight; set => SetBinding(ref moveRight, value); }
    public string MoveGamepad { get => moveGamepad; set => SetBinding(ref moveGamepad, value); }
    public string LookPointer { get => lookPointer; set => SetBinding(ref lookPointer, value); }
    public string LookGamepad { get => lookGamepad; set => SetBinding(ref lookGamepad, value); }
    public string SprintKeyboard { get => sprintKeyboard; set => SetBinding(ref sprintKeyboard, value); }
    public string SprintGamepad { get => sprintGamepad; set => SetBinding(ref sprintGamepad, value); }
    public string CrouchKeyboard { get => crouchKeyboard; set => SetBinding(ref crouchKeyboard, value); }
    public string InteractKeyboard { get => interactKeyboard; set => SetBinding(ref interactKeyboard, value); }
    public string DropKeyboard { get => dropKeyboard; set => SetBinding(ref dropKeyboard, value); }
    public string JumpKeyboard { get => jumpKeyboard; set => SetBinding(ref jumpKeyboard, value); }

    void SetBinding(ref string field, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(field, value, StringComparison.Ordinal))
            return;

        field = value;
        Changed?.Invoke();
    }
}

[Serializable]
public sealed class VideoSettings
{
    public event Action Changed;

    int targetFrameRate = -1;
    int resolutionWidth = 0;
    int resolutionHeight = 0;
    int refreshRate = 0;
    FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
    bool useVSync;
    int vSyncCount = 1;
    float renderScale = 1f;
    TextureQuality textureQuality = TextureQuality.Full;
    ShadowQuality shadowQuality = ShadowQuality.All;
    AnisotropicFiltering anisotropicFiltering = AnisotropicFiltering.Enable;

    public int TargetFrameRate { get => targetFrameRate; set => SetValue(ref targetFrameRate, Mathf.Max(-1, value)); }
    public int ResolutionWidth { get => resolutionWidth; set => SetValue(ref resolutionWidth, Mathf.Max(0, value)); }
    public int ResolutionHeight { get => resolutionHeight; set => SetValue(ref resolutionHeight, Mathf.Max(0, value)); }
    public int RefreshRate { get => refreshRate; set => SetValue(ref refreshRate, Mathf.Max(0, value)); }
    public FullScreenMode FullScreenMode { get => fullScreenMode; set => SetValue(ref fullScreenMode, value); }
    public bool UseVSync { get => useVSync; set => SetValue(ref useVSync, value); }
    public int VSyncCount { get => vSyncCount; set => SetValue(ref vSyncCount, Mathf.Clamp(value, 1, 4)); }
    public float RenderScale { get => renderScale; set => SetValue(ref renderScale, Mathf.Clamp(value, 0.5f, 2f)); }
    public TextureQuality TextureQuality { get => textureQuality; set => SetValue(ref textureQuality, value); }
    public ShadowQuality ShadowQuality { get => shadowQuality; set => SetValue(ref shadowQuality, value); }
    public AnisotropicFiltering AnisotropicFiltering { get => anisotropicFiltering; set => SetValue(ref anisotropicFiltering, value); }

    void SetValue<T>(ref T field, T value)
    {
        if (Equals(field, value))
            return;

        field = value;
        Changed?.Invoke();
    }
}

public sealed class GameSettings
{
    float mouseSensitivity = 2f;
    float masterVolume = 1f;
    bool subtitles = true;
    Languages language = Languages.English;

    public event Action InputBindingsChanged;
    public event Action VideoSettingsChanged;

    public GameSettings()
    {
        Input = new PlayerInputSettings();
        Video = new VideoSettings();

        Input.Changed += () => InputBindingsChanged?.Invoke();
        Video.Changed += HandleVideoSettingsChanged;
    }

    public PlayerInputSettings Input { get; }
    public VideoSettings Video { get; }

    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set => mouseSensitivity = value;
    }

    public float MasterVolume
    {
        get => masterVolume;
        set => masterVolume = Mathf.Clamp01(value);
    }

    public bool Subtitles
    {
        get => subtitles;
        set => subtitles = value;
    }

    public Languages Language
    {
        get => language;
        set => language = value;
    }

    public void ApplyVideoSettings()
    {
        GameVideoSettingsApplier.Apply(Video);
    }

    void HandleVideoSettingsChanged()
    {
        ApplyVideoSettings();
        VideoSettingsChanged?.Invoke();
    }
}
