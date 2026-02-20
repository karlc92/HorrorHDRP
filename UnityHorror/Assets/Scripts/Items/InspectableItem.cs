using System;
using UnityEngine;

public class InspectableItem : Interactable
{
    public GameObject InspectPrefab;
    public bool Inspecting;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Interact()
    {
        Debug.Log("Inspected item AT " + DateTime.Now);
    }
}
