using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Manages the hierarchical representation of the game world's spatial structure.
/// Provides a singleton interface for world navigation and area management.
/// </summary>
/// </remarks>
public class WorldTree : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the WorldTree.
    /// </summary>
    [ShowInInspector]
    [ReadOnly]
    [PropertyOrder(-20)]
    public static WorldTree Instance { get; private set; }
    
    /// <summary>
    /// Internal dictionary mapping area names to their corresponding AreaNodes.
    /// </summary>
    [Tooltip("Lookup table for all areas and sections in the world")]
    [DictionaryDrawerSettings(
        KeyLabel = "Area/Section Name", 
        ValueLabel = "Area Node Details")]
    [ShowInInspector]
    [PropertyOrder(-15)]
    private Dictionary<string, AreaNode> worldAreas = new Dictionary<string, AreaNode>();

    /// <summary>
    /// Root node representing the entire game world.
    /// </summary>
    [Tooltip("Root node of the entire world hierarchy")]
    [PropertyOrder(-10)]
    [ShowInInspector]
    [ReadOnly]
    private AreaNode worldRoot;

    /// <summary>
    /// Transform used as the root for world structure generation.
    /// </summary>
    [Tooltip("Transform containing the hierarchical world structure")]
    [PropertyOrder(-5)]
    [Required]
    [SerializeField]
    private Transform worldRootTransform;

    /// <summary>
    /// Initializes the singleton instance and builds the world tree.
    /// </summary>
    /// <remarks>
    /// Ensures only one instance exists and builds world structure on awake.
    /// Destroys duplicate instances to prevent multiple world trees.
    /// </remarks>
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildWorldTree();
    }

    /// <summary>
    /// Constructs the world tree hierarchy based on the world root transform.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if world root transform is not assigned</exception>
    [Button("Rebuild World Tree")]
    [PropertyOrder(-3)]
    private void BuildWorldTree()
    {
        if (worldRootTransform == null)
        {
            Debug.LogError("World Root Transform is not assigned!");
            return;
        }
        
        // Create world root node
        worldRoot = new AreaNode("World",  worldRootTransform.gameObject, null);
        //worldAreas["World"] = worldRoot;

        TraverseAreas(worldRootTransform, worldRoot);
    }

    /// <summary>
    /// Recursively processes areas within the given parent transform.
    /// </summary>
    /// <param name="parentTransform">Transform to search for child areas</param>
    /// <param name="parentNode">Parent AreaNode in the hierarchy</param>
    private void TraverseAreas(Transform parentTransform, AreaNode parentNode)
    {
        foreach (Transform child in parentTransform)
        {
            if (child.CompareTag("Area"))
            {
                var areaNode = new AreaNode(child.name, child.gameObject,  parentNode);
                worldAreas[child.name] = areaNode;
                parentNode.AddChild(areaNode);
                
                ProcessSections(child, areaNode);
            }
        }
    }

    /// <summary>
    /// Processes sections within a given area transform.
    /// </summary>
    /// <param name="areaTransform">Transform representing the area</param>
    /// <param name="areaNode">AreaNode representing the parent area</param>
    private void ProcessSections(Transform areaTransform, AreaNode areaNode)
    {
        foreach (Transform child in areaTransform)
        {
            if (child.CompareTag("Sector"))
            {
                var sectionNode = new AreaNode(child.name, child.gameObject, areaNode);
                areaNode.AddChild(sectionNode);

                var fullPath = $"{areaNode.Name}:{sectionNode.Name}";
                worldAreas[fullPath] = sectionNode;
                
                ProcessObjects(child, sectionNode);
            }
        }
    }

    /// <summary>
    /// Processes objects within a given area transform.
    /// </summary>
    /// <param name="sectionTransform">Transform representing the section</param>
    /// <param name="sectionNode">AreaNode representing the parent section</param>
    private void ProcessObjects(Transform sectionTransform, AreaNode sectionNode)
    {
        foreach (Transform child in sectionTransform)
        {
            if (child.CompareTag("Object"))
            {
                var objectNode = new AreaNode(child.name, child.gameObject, sectionNode)
                {
                    IsObject = true
                };
                
                sectionNode.AddChild(objectNode);
                
                var fullPath = $"{sectionNode.GetFullPath()}:{objectNode.Name}";
                fullPath = fullPath.Replace("World:", string.Empty);
                worldAreas[fullPath] = objectNode;
            }
        }
        
    }

    /// <summary>
    /// Retrieves all top-level areas in the world.
    /// </summary>
    /// <returns>List of area names without nested sections</returns>
    public List<string> GetAllAreas()
    {
        return worldAreas.Keys.Where(k => !k.Contains(":")).ToList();
    }

    /// <summary>
    /// Gets all sections within a specific area.
    /// </summary>
    /// <param name="areaName">Name of the area to search</param>
    /// <returns>List of section names in the area, or empty list if area not found</returns>
    public List<string> GetSectionsInArea(string areaName)
    {
        if (!worldAreas.TryGetValue(areaName, out var area))
        {
            return new List<string>();
        }
        
        return area.Children
            .Where(child => !child.IsObject)
            .Select(child => child.Name)
            .ToList();
    }
    
    /// <summary>
    /// Gets all objects within a specific section.
    /// </summary>
    /// <param name="sectionName">Name of the section to search</param>
    /// <returns>List of object names in the section, or empty list if section not found</returns>
    public List<string> GetObjectsInSection(string areaName, string sectionName)
    {
        var fullPath = $"{areaName}:{sectionName}";
        if (!worldAreas.TryGetValue(fullPath, out var section))
        {
            return new List<string>();
        }
        
        return section.Children
            .Where(child => child.IsObject)
            .Select(child => child.Name)
            .ToList();
    }

    /// <summary>
    /// Retrieves the AreaNode for a given area name.
    /// </summary>
    /// <param name="areaName">Name of the area to retrieve</param>
    /// <returns>AreaNode if found, otherwise null</returns>
    public AreaNode GetAreaNode(string areaName)
    {
        return worldAreas.GetValueOrDefault(areaName);
    }

    /// <summary>
    /// Generates a natural language description of the world structure.
    /// </summary>
    /// <returns>Textual representation of the world hierarchy</returns>
    [Button("Generate World Description")]
    public string GetNaturalLanguageDescription()
    {
        //TODO: implement function
        // Generate a full natural language description of the world
        // This method would traverse the tree and build sentences
        // like "The House has a Living Room, Kitchen, and Bedroom"
        // ...implementation details...
        Debug.LogWarning("Natural language description generation not implemented yet.");
        return string.Empty;
    }
    
    // Debug method to visualize entire world structure
    [Button("Print World Hierarchy")]
    private void PrintWorldHierarchy()
    {
        if (worldRoot == null)
        {
            Debug.LogWarning("World root is not initialized.");
            return;
        }

        PrintNodeHierarchy(worldRoot);
    }

    // Recursive helper method to print node hierarchy
    private void PrintNodeHierarchy(AreaNode node, int depth = 0)
    {
        var indent = new string('-', depth * 2);
        Debug.Log($"{indent}{node.Name}");

        foreach (var child in node.Children)
        {
            PrintNodeHierarchy(child, depth + 1);
        }
    }

    public GameObject GetObjectGameObject(string areaName, string sectionName, string objectName)
    {
        var fullPath = $"{areaName}:{sectionName}:{objectName}";
        if (!worldAreas.TryGetValue(fullPath, out var objectNode))
        {
            return null;
        }

        return objectNode.WorldObject;
    }
}
