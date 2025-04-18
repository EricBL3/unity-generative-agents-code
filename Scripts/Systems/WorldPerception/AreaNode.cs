using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Represents a node in the hierarchical world structure, allowing nested representation of areas and sections.
/// </summary>
/// <remarks>
/// Design Patterns: Composite Pattern
/// Supports building a tree-like representation of game world locations
/// Enables recursive path generation and hierarchical navigation
/// </remarks>

public class AreaNode
{
    /// <summary>
    /// The unique identifier for this area or section.
    /// </summary>
    [Tooltip("Unique name of the area or section")]
    [PropertyOrder(-10)]
    [LabelText("Area/Section Name")]
    [ValidateInput("@ValidateName", "Name must be non-empty and contain valid characters")]
    public string Name { get; private set; }
    
    /// <summary>
    /// Parent node in the hierarchical structure. Null indicates a root node.
    /// </summary>
    [Tooltip("Parent area containing this section")]
    [PropertyOrder(-9)]
    [ShowInInspector]
    [ReadOnly]
    public AreaNode Parent { get; private set; }
    
    /// <summary>
    /// Collection of child nodes representing subsections or nested areas.
    /// </summary>
    [Tooltip("Subsections or nested areas within this location")]
    [ListDrawerSettings(
        ShowIndexLabels = true, 
        DraggableItems = true, 
        ShowItemCount = true)]
    [PropertyOrder(-8)]
    public List<AreaNode> Children { get; private set; }
    
    // Flag to identify if this node is an object (as opposed to an area or section)
    public bool IsObject { get; set; }
    
    // Reference to the actual GameObject in the scene
    public GameObject WorldObject { get; private set; }

    /// <summary>
    /// Initializes a new AreaNode with a name and optional parent.
    /// </summary>
    /// <param name="name">Unique identifier for the area/section</param>
    /// <param name="worldObject">The game object that represents the node</param>
    /// <param name="parent">Parent node in the hierarchy (can be null)</param>
    /// <exception cref="System.ArgumentException">Thrown if name is null or empty</exception>
    public AreaNode(string name, GameObject worldObject, AreaNode parent)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(name))
            throw new System.ArgumentException("Area name cannot be null or empty", nameof(name));
        
        Name = name;
        WorldObject = worldObject;
        Parent = parent;
        Children = new List<AreaNode>();
        IsObject = false;
    }

    /// <summary>
    /// Adds a child node to the current area's hierarchy.
    /// </summary>
    /// <param name="child">Child AreaNode to be added</param>
    /// <exception cref="System.ArgumentNullException">Thrown if child is null</exception>
    [Button(ButtonSizes.Medium)]
    [PropertyOrder(-7)]
    public void AddChild(AreaNode child)
    {
        if (child == null)
            throw new System.ArgumentNullException(nameof(child), "Cannot add null child node");
        
        Children.Add(child);
    }

    /// <summary>
    /// Checks if a direct child with the specified name exists.
    /// </summary>
    /// <param name="childName">Name of the child to search for</param>
    /// <returns>True if a child with the given name exists, otherwise false</returns>
    public bool HasChild(string childName)
    {
        return Children.Any(child => child.Name == childName);
    }

    /// <summary>
    /// Generates a full hierarchical path from the root to this node.
    /// </summary>
    /// <returns>Colon-separated path representation (e.g., "World:City:Park")</returns>
    [Button(ButtonSizes.Small)]
    [PropertyOrder(-6)]
    public string GetFullPath()
    {
        if (Parent == null)
            return Name;
        
        return $"{Parent.GetFullPath()}:{Name}";
    }
    
    // Helper method to determine node type
    public string GetNodeType()
    {
        if (IsObject)
            return "Object";
        else if (Parent != null && Parent.Parent != null)
            return "Section";
        else if (Parent != null)
            return "Area";
        else
            return "World";
    }
    
    // Validation method for Odin Inspector
    private bool ValidateName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && 
               name.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '_');
    }
    
    // Debug method to visualize node hierarchy
    [Button("Print Hierarchy")]
    [PropertyOrder(-5)]
    private void PrintHierarchy(int depth = 0)
    {
        Debug.Log($"{new string('-', depth)}{Name}");
        foreach (var child in Children)
        {
            child.PrintHierarchy(depth + 2);
        }
    }
}
