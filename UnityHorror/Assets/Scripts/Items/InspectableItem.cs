using System;
using UnityEngine;

public class InspectableItem : Interactable
{
    [Header("Inspection")]
    public GameObject InspectPrefab;

    [TextArea(2, 8)]
    public string Description;

    // Optional: tweak how big it appears in the inspect view
    public float InspectScaleMultiplier = 1.0f;

    public bool Inspecting { get; private set; }

    public override void Interact()
    {
        if (InspectPrefab == null)
        {
            Debug.LogWarning($"{name}: No InspectPrefab assigned.");
            Console.Print($"{name}: No InspectPrefab assigned.");
            return;
        }

        Inspecting = true;
        InspectionManager.Instance?.Open(this);
    }

    public void NotifyInspectionClosed()
    {
        Inspecting = false;
    }
}