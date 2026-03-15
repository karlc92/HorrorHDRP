using UnityEngine;

[AddComponentMenu("Horror/Zone")]
[RequireComponent(typeof(BoxCollider))]
public class Zone : MonoBehaviour
{
    public string ZoneId;
    public bool IsPlayerInside => playerOverlapCount > 0;

    [SerializeField] private bool triggerOnly = true;

    private int playerOverlapCount;

    private void Reset()
    {
        EnsureColliderSetup();
    }

    private void OnValidate()
    {
        EnsureColliderSetup();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        playerOverlapCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);
    }

    public void ApplyActiveState(bool active)
    {
        gameObject.SetActive(active);
    }

    private void EnsureColliderSetup()
    {
        if (!TryGetComponent<BoxCollider>(out var boxCollider))
            return;

        if (triggerOnly)
            boxCollider.isTrigger = true;
    }
}
