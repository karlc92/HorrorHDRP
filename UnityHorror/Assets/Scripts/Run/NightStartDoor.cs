using UnityEngine;

public class NightStartDoor : MonoBehaviour
{
    private bool hasStartedNight;

    public void NotifyNightStarted()
    {
        if (hasStartedNight)
            return;

        hasStartedNight = true;
        if (RunManager.Instance != null)
            RunManager.Instance.OnNightStarted();
    }
}
