using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public GameObject GameTitle;
    public GameObject MainMenuContent;
    public GameObject SettingsContent;
    public GameObject PlayMenuContent;

    void Awake()
    {
        ShowMainMenu();
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

    public void NewGame(int slot)
    {
        Game.StartNewGame(slot);
    }
}
