using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public GameObject GameTitle;
    public GameObject MainMenuContent;
    public GameObject SettingsContent;
    public GameObject PlayMenuContent;
    public List<TextMeshProUGUI> SlotButtonLabels;

    void Awake()
    {
        ShowMainMenu();

        for (int i = 0; i < SlotButtonLabels.Count; i++)
        {
            SlotButtonLabels[i].text = GetSavedDataDisplay(i + 1);
        }
    }

    void HideAllMenus()
    {
        GameTitle.SetActive(false);
        MainMenuContent.SetActive(false);
        SettingsContent.SetActive(false);
        PlayMenuContent.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllMenus();
        GameTitle.SetActive(true);
        MainMenuContent.SetActive(true);
    }

    public void ShowSettingsMenu()
    {
        HideAllMenus();
        SettingsContent.SetActive(true);
    }

    public void ShowPlayMenu()
    {
        HideAllMenus();
        PlayMenuContent.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public string GetSavedDataDisplay(int slot)
    {
        if (!Game.TryReadSaveState(slot, out var state))
            return $"New Game";

        string playTime = FormatPlayTimeHHmm(state.TotalPlayTimeSeconds);
        string objective = state.Story != null && !string.IsNullOrWhiteSpace(state.Story.CurrentObjectiveTitle)
            ? state.Story.CurrentObjectiveTitle
            : "Continue";
        return $"Continue\n{objective}\n{playTime}";
    }

    public void SlotButtonClick(int slot)
    {
        if (Game.HasSaveFile(slot))
            Game.LoadGame(slot);
        else
            Game.StartNewGame(slot);
    }

    string FormatPlayTimeHHmm(float totalSeconds)
    {
        // Round up to whole minutes; minimum 1 minute.
        int totalMinutes = Mathf.Max(1, Mathf.CeilToInt(totalSeconds / 60f));

        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        return $"{hours:00}:{minutes:00}";
    }
}
