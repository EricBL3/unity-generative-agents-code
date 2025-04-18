using System;
using UnityEngine;

public class ObjectState : MonoBehaviour
{
    // Reference to the parent section
    public string parentSectionName;
    
    // Reference to the parent area
    public string parentAreaName;
    
    // Current state of the object
    public string currentState = "normal";

    private void Start()
    {
        parentSectionName = gameObject.transform.parent.name;
        parentAreaName = gameObject.transform.parent.parent.name;
    }

    // Method to change the state of the object
    public void ChangeState(string newState)
    {
        currentState = newState;

        NotifyNearbyNPCs();
    }
    
    // Notify NPCs within perception range about the state change
    private void NotifyNearbyNPCs()
    {
        // Find NPCs within perception range
        var colliders = Physics.OverlapSphere(transform.position, 10f); // 10f is the perception range
        foreach (var collider in colliders)
        {
            var npcPerception = collider.GetComponent<NpcWorldPerception>();
            var npcMemory = collider.GetComponent<NpcMemorySystem>();
            
            if (npcPerception != null && npcMemory != null)
            {
                npcPerception.ObserveObject(parentAreaName, parentSectionName, gameObject.name);
            }
        }
    }
    
}
