using UnityEngine;

public class Zone : MonoBehaviour
{
    public string ZoneId;

    public void ApplyActiveState(bool active)
    {
        gameObject.SetActive(active);
    }
}
