using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// This class is used to configure the memory features that will be enabled during the simulation.
/// </summary>
[CreateAssetMenu(fileName = "MemoryFeatureFlags", menuName = "NPC/MemoryFeatureFlags")]
[Title("Memory Configuration", titleAlignment: TitleAlignments.Centered)]
public class MemoryFeatureFlags : ScriptableObject
{
    #region Constants

    private const string MEMORY_TYPE_GROUP = "Memory Type";
    private const string FEATURES_GROUP = "Features";
    private const string SENSORY_MEMORY_CONDITION = "@this.memoryFeatureType != MemoryFeatureType.SensoryMemory";
    private const int STANDARD_LABEL_WIDTH = 200;
    
    #endregion
    
    #region Memory Type
    
    [FormerlySerializedAs("memoryType")]
    [BoxGroup(MEMORY_TYPE_GROUP, centerLabel: true)]
    [Tooltip("The primary memory type that NPCs will use")]
    [EnumToggleButtons, HideLabel]
    public MemoryFeatureType memoryFeatureType;
    
    #endregion

    #region Feature Flags
    
    [BoxGroup(FEATURES_GROUP, centerLabel: true)]
    [ShowIf(SENSORY_MEMORY_CONDITION)]
    [LabelText("Enable Planning & Reacting")]
    [LabelWidth(STANDARD_LABEL_WIDTH)]
    [Tooltip("Allows the agent to plan future actions and react to environment changes")]
    public bool planningAndReacting;

    [BoxGroup(FEATURES_GROUP, centerLabel: true)]
    [ShowIf(SENSORY_MEMORY_CONDITION)]
    [LabelText("Enable Reflections")]
    [LabelWidth(STANDARD_LABEL_WIDTH)]
    [Tooltip("Allows the agent to reflect on past experiences")]
    public bool reflections;
    
    #endregion

}

/// <summary>
/// This enum represents the different types of memory in the human brain.
/// </summary>
public enum MemoryFeatureType
{
    [TabGroup("Sensory Memory")]
    [Tooltip("Brief retention of sensory information")]
    SensoryMemory,
    
    [LabelText("Short-Term Memory")]
    [Tooltip("Temporary, active information storage")]
    ShortTermMemory,
    
    [LabelText("Long-Term Memory")]
    [Tooltip("Permanent storage of important information")]
    LongTermMemory
}
