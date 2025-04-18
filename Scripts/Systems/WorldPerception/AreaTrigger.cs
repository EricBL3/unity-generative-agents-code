using UnityEngine;

public class AreaTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        Area,
        Section
    }
    
    public TriggerType type;
    
    void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is an NPC
        var npcPerception = other.GetComponent<NpcWorldPerception>();
        
        if (npcPerception != null)
        {
            switch (type)
            {
                case TriggerType.Area:
                    npcPerception.EnterArea(gameObject.name);
                    break;
                case TriggerType.Section:
                    npcPerception.EnterSection(gameObject.name);
                    break;
            }
        }
    }
}
