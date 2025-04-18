using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// This class represents a single memory instance that an NPC has.
/// </summary>
[Serializable]
public class Memory
{
    [BoxGroup("Timestamps")]
    [Tooltip("When this memory was initially created")]
    [ReadOnly, ShowInInspector]
    public SimDateTime createdAt { get; private set; }
    
    [BoxGroup("Timestamps")]
    [Tooltip("When this memory was last accessed")]
    [ReadOnly, ShowInInspector]
    public SimDateTime accessedAt { get; private set; }
    
    [BoxGroup("Memory Content")]
    [TextArea(3, 10)]
    [Tooltip("Description of the memory content")]
    [SerializeField] 
    private string description;
    
    [BoxGroup("Memory Content")]
    [Range(1, 10)]
    [Tooltip("How important this memory is (1-10)")]
    [SerializeField] 
    private int importance;
    
    [BoxGroup("Memory Content")]
    [EnumToggleButtons]
    [Tooltip("The type of this memory")]
    private MemoryType memoryType;
    
    /// <summary>
    /// The content description of this memory
    /// </summary>
    public string Description
    {
        get => description;
        set => description = value;
    }

    /// <summary>
    /// How important this memory is (1-10)
    /// </summary>
    public int Importance
    {
        get => importance;
        set => importance = Mathf.Clamp(value, 1, 10);
    }

    /// <summary>
    /// The type of this memory. Can be one of these values:
    /// - Observation
    /// - Reflection
    /// - Plan
    /// - Conversation
    /// </summary>
    public MemoryType MemoryType
    {
        get => memoryType;
        set => memoryType = value;
    }

    /// <summary>
    /// Default constructor (required for serialization)
    /// </summary>
    public Memory()
    {
        //TODO: get timestamp to update createdAt and accessedAt
    }

    /// <summary>
    /// Creates a new memory with the given description and importance level
    /// </summary>
    /// <param name="description">The content of the memory</param>
    /// <param name="importance">How important this memory is (1-10)</param>
    /// <param name="memoryType">The type of this memory</param>
    public Memory(string description, int importance, MemoryType memoryType)
    {
        createdAt = DaytimeCycle.Instance.GetSimDateTime();
        accessedAt = createdAt;
        
        Description = description;
        Importance = importance;
        MemoryType = memoryType;
    }

    /// <summary>
    /// Updates the timestamp of when this memory was last accessed
    /// </summary>
    public void UpdateAccessedAt()
    {
        accessedAt = DaytimeCycle.Instance.GetSimDateTime();
    }
    
}
