using UnityEngine;

public enum Languages
{
    English,
    German
}
public class GameSettings
{
    public float MouseSensitivity { get; set; } = 2f;
    public float MasterVolume { get; set; } = 1;
    public bool Subtitles { get; set; } = true;
    public Languages Language = Languages.English;
    public bool UseNonEnglishDialogueAudio { get; set; } = false;
}
