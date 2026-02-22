using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] GameObject deadUI;
    [SerializeField] GameObject deadMenu;
    [SerializeField] GameObject loadPreviousSaveButton;
    private float deadMenuActivationTimer = -1;

    private void Start()
    {
        deadMenuActivationTimer = -1;
        deadUI.SetActive(false);
        deadMenu.SetActive(false); 
    }

    public void ShowDeadUI()
    {
        if (!deadUI.activeSelf)
        {
            deadUI.SetActive(true);
            deadMenuActivationTimer = Time.time + 3f;
        }
    }

    private void Update()
    {
        if (deadMenuActivationTimer != -1 && deadMenuActivationTimer < Time.time && !deadMenu.activeSelf)
        {
            loadPreviousSaveButton.SetActive(Game.HasSaveFile(Game.State.Slot));
            deadMenu.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ReturnToMainMenu()
    {
        Game.ReturnToMainMenu();
    }

    public void LoadPreviousSave()
    {
        Game.LoadGame(Game.State.Slot);
    }

}
