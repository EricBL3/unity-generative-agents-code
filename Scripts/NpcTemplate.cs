using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcTemplate", menuName = "NPC/NpcTemplate")]
public class NpcTemplate : ScriptableObject
{
    [BoxGroup("Basic Information")]
    [Required]
    public string firstName;
    
    [BoxGroup("Basic Information")]
    [Required]
    public string lastName;
    
    [BoxGroup("Basic Information")]
    [ShowInInspector]
    [Required]
    public string fullName => $"{firstName} {lastName}";

    [BoxGroup("Basic Information")]
    [Min(1)]
    [Required]
    public int age;

    [BoxGroup("L0 - Permanent Core Traits")]
    [TextArea(2,3)]
    [Required]
    public string innateTraits;
    
    [BoxGroup("L1 - Stable Learned Traits")]
    [TextArea(2,3)]
    [Required]
    public string learnedTraits;
    
    [BoxGroup("L2 - Current External State")]
    [TextArea(2,3)]
    [Required]
    public string currentState;
    
    [BoxGroup("L2 - Current External State")]
    [TextArea(2,3)]
    [Required]
    public string lifestyle;
    
    [BoxGroup("L2 - Current External State")]
    [TextArea(2,3)]
    [Required]
    public string livingArea;
    
    [BoxGroup("Seed Memories")]
    [TextArea(5,10)]
    [Required]
    public string seedMemoriesText; // Semicolon-delimited initial memories
    
    [BoxGroup("Knowledge Configuration")]
    [InfoBox("Add knowledge profiles for reusable knowledge patterns.")]
    [ListDrawerSettings(ShowFoldout = true)]
    public List<LocationKnowledgeProfile> knowledgeProfiles = new List<LocationKnowledgeProfile>();
    
    [BoxGroup("Instance-Specific Knowledge")]
    [InfoBox("Add areas only this specific NPC knows about.")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true)]
    public List<LocationKnowledgeProfile.KnownArea> instanceKnownAreas = new List<LocationKnowledgeProfile.KnownArea>();
    
}
