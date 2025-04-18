using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Location Knowledge", menuName = "NPC/Location Knowledge Profile")]
public class LocationKnowledgeProfile : ScriptableObject
{

    [Serializable]
    public class KnownArea
    {
        [HorizontalGroup("Main")]
        [VerticalGroup("Main/Info")]
        public string areaName;
        
        [VerticalGroup("Main/Info")]
        [LabelText("Know All Sections")]
        public bool knowAllSections = true;
        
        [TitleGroup("Sections", "Specific Sections")]
        [ShowIf("@!knowAllSections")]
        [ListDrawerSettings(ShowIndexLabels = true, CustomAddFunction = "AddNewDetailedSection")]
        public List<KnownSection> sections = new List<KnownSection>();
        
        // Helper method for Odin's custom add function
        private KnownSection AddNewDetailedSection()
        {
            return new KnownSection();
        }
    }
    
    [System.Serializable]
    public class KnownSection
    {
        [HorizontalGroup("Section")]
        [VerticalGroup("Section/Info")]
        public string sectionName;
        
        [VerticalGroup("Section/Info")]
        [LabelText("Know All Objects")]
        public bool knowAllObjects = true;
        
        [VerticalGroup("Section/Objects")]
        [ShowIf("@!knowAllObjects")]
        [ListDrawerSettings(ShowFoldout = true)]
        [LabelText("Specific Objects")]
        public List<string> specificObjects = new List<string>();
    }
    
    [TitleGroup("Profile Information")]
    public string profileName;
    
    [TitleGroup("Profile Information")]
    [TextArea(3, 5)]
    public string description;
    
    [TitleGroup("Known Areas")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, 
                        CustomAddFunction = "AddNewKnownArea")]
    public List<KnownArea> knownAreas = new List<KnownArea>();
    
    // Helper method for Odin's custom add function
    private KnownArea AddNewKnownArea()
    {
        return new KnownArea();
    }
}
