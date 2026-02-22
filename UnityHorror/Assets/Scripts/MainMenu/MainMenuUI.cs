using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        foreach (var label in SlotButtonLabels)
        {
            var index = SlotButtonLabels.IndexOf(label);
            label.text = GetSavedDataDisplay(index + 1);
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
        if (!HasSaveFile(slot))
            return $"SLOT {slot}\nNEW GAME";

        if (TryReadSaveState(slot, out var state))
        {
            string playTime = FormatPlayTimeHHmm(state.TotalPlayTimeSeconds);
            return $"SLOT {slot}\nCONTINUE\nNIGHT {state.Night}\n{playTime}";
        }

        // Save exists but couldn't be read (corrupt / partial write etc).
        return $"SLOT {slot}\nCONTINUE";
    }

    public void SlotButtonClick(int slot)
    {
        if (HasSaveFile(slot))
        {
            Game.LoadGame(slot);
        }
        else
        {
            Game.StartNewGame(slot);
        }
    }

    bool HasSaveFile(int slot)
    {
        if (slot < 1 || slot > 3)
            return false;

        string path = Path.Combine(Application.persistentDataPath, $"{Game.SaveFilePrefix}{slot}.json");
        return File.Exists(path);
    }

    bool TryReadSaveState(int slot, out GameState state)
    {
        state = null;

        try
        {
            string path = Path.Combine(Application.persistentDataPath, $"{Game.SaveFilePrefix}{slot}.json");
            if (!File.Exists(path))
                return false;

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            state = JsonUtility.FromJson<GameState>(json);
            return state != null;
        }
        catch
        {
            return false;
        }
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