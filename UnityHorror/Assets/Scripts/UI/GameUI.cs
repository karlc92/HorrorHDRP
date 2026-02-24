using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] GameObject deadUI;
    [SerializeField] GameObject deadMenu;
    [SerializeField] GameObject loadPreviousSaveButton;
    [SerializeField] GameObject notificationUI;
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

    public void ShowNotification(string text, float duration = 3f, float notificationVolume = 0.5f)
    {
        var objectToSpawn = ResourceCache.Get<GameObject>("Prefabs/UI/Notification");
        var spawnedObject = Instantiate(objectToSpawn, notificationUI.transform);
        var notification = spawnedObject.GetComponent<Notification>();
        if (notification != null)
        {
            notification.ShowNotification(text, duration, notificationVolume);
        }
    }

}
