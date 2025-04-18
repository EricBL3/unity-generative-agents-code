using System.Collections.Generic;
using Logging;
using UnityEngine;

public class DemoHelper : MonoBehaviour
{
    [SerializeField] 
    private GameObject npc;

    [SerializeField]
    private List<Transform> teleportTransforms;

    private KeyCode[] teleportKeys;

    private Dictionary<KeyCode, int> timeAdvancements;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         teleportKeys = new[] { KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9 };
         
         timeAdvancements = new Dictionary<KeyCode, int>
         {
             { KeyCode.Alpha1, 1 },
             { KeyCode.Alpha3, 3 },
             { KeyCode.Alpha8, 8 }
         };
    }

    // Update is called once per frame
    void Update()
    {
        // Location teleports
        for (var i = 0; i < teleportKeys.Length; i++)
        {
            if (Input.GetKeyDown(teleportKeys[i]))
            {
                TeleportAgent(i);
            }
        }
        
        // Time advancement
        foreach (var kvp in timeAdvancements)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                AdvanceTime(kvp.Value);
            }
        }
    }

    private void AdvanceTime(int hours)
    {
        DaytimeCycle.Instance.AddHours(hours);
        LogSystem.Instance.Log(LogCategory.System, LogLevel.Debug, $"Advanced time by {hours} hours");
    }

    private void TeleportAgent(int index)
    {
        if (teleportTransforms.Count - 1 < index)
            return;
        
        npc.transform.position = teleportTransforms[index].position;
        npc.transform.rotation = teleportTransforms[index].rotation;
        
        LogSystem.Instance.Log(LogCategory.System, LogLevel.Debug, $"Teleported {npc.name} to location in index {index}");
    }
}
