using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMUnity;
using Logging;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NpcMemorySystem))]
[RequireComponent(typeof(LLMChatManager))]
public class NpcWorldPerception : MonoBehaviour
{
    private WorldTree worldTree;
    private MemoryFeatureFlags memoryConfig;

    private NpcMemorySystem memorySystem;

    private string currentArea;
    private string currentSection;
    
    private string previousArea;
    private string previousSection;
    
    [ListDrawerSettings(ShowFoldout = true)]
    [ShowInInspector]
    private Dictionary<string, bool> knownAreas = new Dictionary<string, bool>();
    
    [ListDrawerSettings(ShowFoldout = true)]
    [ShowInInspector]
    private Dictionary<string, string> knownSections = new Dictionary<string, string>();
    
    [ListDrawerSettings(ShowFoldout = true)]
    [ShowInInspector]
    private Dictionary<string, string> knownObjects = new Dictionary<string, string>();

    private float timeSinceLastPerception = 0f;
    
    private LLMChatManager llmChatManager;
    
    [BoxGroup("Knowledge Configuration")]
    [ListDrawerSettings(ShowFoldout = true)]
    [ShowInInspector, ReadOnly]
    public List<LocationKnowledgeProfile> knowledgeProfiles { get; private set; } = new List<LocationKnowledgeProfile>();
    
    [BoxGroup("Instance-Specific Knowledge")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true)]
    [ShowInInspector, ReadOnly]
    public List<LocationKnowledgeProfile.KnownArea> instanceKnownAreas { get; private set; } = new List<LocationKnowledgeProfile.KnownArea>();
    
    /// <summary>
    /// Determines if the npc has already been initialized
    /// </summary>
    private bool initialized = false;
    
    /// <summary>
    /// Determine if currently prompting the llm.
    /// </summary>
    private bool prompting = false;

    public Action<string, string> ObjectStateChanged;

    private void Start()
    {
        worldTree = WorldTree.Instance;
        memoryConfig = MemoryFeatureManager.Instance.GetConfiguration();
        
        memorySystem = GetComponent<NpcMemorySystem>();
        llmChatManager = GetComponent<LLMChatManager>();
        
        knowledgeProfiles = memorySystem.npcTemplate.knowledgeProfiles;
        instanceKnownAreas = memorySystem.npcTemplate.instanceKnownAreas;

        InitializeKnowledge();
    }

    private void Update()
    {
        timeSinceLastPerception += Time.deltaTime * DaytimeCycle.Instance.timeScale;
        if (timeSinceLastPerception >= DaytimeCycle.Instance.npcPerceptionInterval)
        {
            if (!prompting)
            {
                timeSinceLastPerception = 0f;

                _ = PromptForAction();
            }
        }
    }

    private void InitializeKnowledge()
    {
        if (memoryConfig.memoryFeatureType == MemoryFeatureType.LongTermMemory)
        {
            InitializeLongTermKnowledge();
        }

        StartCoroutine(PromptForInitialAction());
    }

    private IEnumerator PromptForInitialAction()
    {
        yield return new WaitForSeconds(5f);
        initialized = true;
        var task = PromptForAction();
        
        yield return new WaitUntil(() => task.IsCompleted);
    }

    /// <summary>
    /// Generates the next action for the NPC based on their memory and current context.
    /// </summary>
    private async Task PromptForAction()
    {
        if (!initialized)
        {
            return;
        }
        
        prompting = true;

        var currentAction = memorySystem.CurrentAction;
        var currentTime = DaytimeCycle.Instance.GetSimDateTime();

        // Build the context for asking what the NPC should do next
        var context =  new StringBuilder();
        context.AppendLine("# ACTION DECISION REQUIRED");
        context.AppendLine();
        
        context.AppendLine("## CURRENT ENVIRONMENT");
        context.AppendLine(GetWorldPerceptionDescription());
        context.AppendLine();
        
        context.AppendLine($"## CURRENT TIME: {currentTime.ToString()}");
        context.AppendLine();
        
        context.AppendLine($"Objective: Determine the next action based on the current location (and known objects), time of day and the rest of the context for " +
                           $"{memorySystem.firstName}.");
        
        if (!string.IsNullOrEmpty(currentAction))
        {
            context.AppendLine("## CURRENT ACTION STATUS");
            context.AppendLine($"- Activity: {currentAction}");
            
            var actionStartTime = memorySystem.CurrentActionTimeStart;
            var elapsedTime = DaytimeCycle.Instance.CalculateSecondsBetween(actionStartTime, currentTime);
        
            context.AppendLine($"- Started at: {actionStartTime} (current time: {currentTime})");
            context.AppendLine($"- In progress for: {elapsedTime} seconds");
    
            // Extract the intended duration if possible
            var durationStart = currentAction.LastIndexOf("(", StringComparison.Ordinal);
            var durationEnd = currentAction.LastIndexOf(")", StringComparison.Ordinal);
            if (durationStart > 0 && durationEnd > durationStart)
            {
                var actionDuration = currentAction.Substring(durationStart + 1, durationEnd - durationStart - 1);
                context.AppendLine($"- Intended duration: {actionDuration}");
                
                double specifiedSeconds = ParseDuration(actionDuration);
        
                // Calculate elapsed minutes
                var elapsedMinutes = DaytimeCycle.Instance.CalculateSecondsBetween(
                    actionStartTime, currentTime);
            
                // Force a new action if elapsed time exceeds specified duration by 25%
                var shouldForceNewAction = elapsedTime > specifiedSeconds * 1.25;
                
                if (shouldForceNewAction)
                {
                    context.AppendLine();
                    context.AppendLine($"IMPORTANT: The intended duration for this action has been exceeded.");
                    context.AppendLine($"A NEW ACTION appropriate for the current location is required.");
                }
            }
    
            context.AppendLine();

        }
        
        context.AppendLine("## DECISION INSTRUCTION");
        context.AppendLine($"Determine the most appropriate action for {memorySystem.firstName} based on:");
        context.AppendLine("1. The current location and available objects");
        context.AppendLine("2. The current time of day");
        context.AppendLine("3. Context memories");
        context.AppendLine("4. Realistic human behavior patterns");
        context.AppendLine($"If {memorySystem.firstName} is in a location where it no longer makes sense to do the current action, please change it.");
        context.AppendLine($"IMPORTANT: The chosen action should make contextual sense given the current location and available objects. " +
                           $"If {memorySystem.firstName} should instead move to another of the known locations, please give that action.");
        context.AppendLine($"IMPORTANT: {memorySystem.firstName} can only use the objects given in the current environment description.");
        context.AppendLine();
        
        context.AppendLine("## RESPONSE FORMAT");
        context.AppendLine($"{memorySystem.firstName} is <action verb> <object if applicable> (<realistic duration>)");
        context.AppendLine("Examples:");
        context.AppendLine($"- {memorySystem.firstName} is making coffee with 'Coffee Machine' (3 minutes)");
        context.AppendLine($"- {memorySystem.firstName} is getting dressed (5 minutes)");
        context.AppendLine($"- {memorySystem.firstName} is walking to the kitchen (30 seconds)");
        context.AppendLine($"- {memorySystem.firstName} is reading a book in 'Couch_02' (30 minutes)");
        context.AppendLine();
        context.AppendLine("Output ONLY the formatted action WITHOUT explanation. The <realistic duration> should only be a number and 'seconds', 'minutes' or 'hours' like in the provided examples.");

        // Get the full prompt with character description and memories
        var prompt = await memorySystem.CreatePromptForLLM(context.ToString());
        
        // Send to LLM and get response
        var response = await llmChatManager.GetLLMResponseAsync(prompt, memorySystem.AgentId);
        
        Debug.Log(response);
        
        if (!response.Equals("No Action Change") && !response.Equals(currentAction))
        {
            memorySystem.SetCurrentAction(response);
            LogSystem.Instance.LogAction(memorySystem.AgentId, response);

            await ChangeObjectState(response);

        }
        else
        {
            Debug.Log($"No new action for {memorySystem.firstName}.");
        }
        
        prompting = false;
    }

    private async Task ChangeObjectState(string response)
    {
        var objectsInLocation = GetKnownObjectsInSection(currentArea, currentSection);
        
        var objectToChangeName = objectsInLocation.FirstOrDefault(i => response.Contains(i));
        if (!string.IsNullOrEmpty(objectToChangeName))
        {
            var objectToChange = GetObjectReference(currentArea, currentSection, objectToChangeName);
            if (objectToChange != null)
            {
                var objectState = objectToChange.GetComponent<ObjectState>();
                if (objectState != null)
                {
                    var prompt = new StringBuilder();
                    prompt.AppendLine("# OBJECT STATE CHANGE REQUIRED");
                    prompt.AppendLine($"Determine the most appropriate state for {objectToChange} based on the following action:");
                    prompt.AppendLine(response);
                    prompt.AppendLine("Output ONLY one word for the new state of the object.");
                    
                    var newState = await llmChatManager.GetLLMResponseAsync(prompt.ToString(), memorySystem.AgentId);
                    
                    objectState.ChangeState(newState);
                }
            }
        }
    }

    private double ParseDuration(string durationText)
    {
        var seconds = 0d;
    
        if (durationText.Contains("hour"))
        {
            var hours = double.Parse(durationText.Split(' ')[0]);
            seconds = hours * 60 * 60;
        }
        else if (durationText.Contains("minute"))
        {
            var minutes = double.Parse(durationText.Split(' ')[0]);
            seconds = minutes * 60;
        }
        else if (durationText.Contains("second"))
        {
            seconds = double.Parse(durationText.Split(' ')[0]);
        }
    
        return seconds;
    }
    
    private void InitializeLongTermKnowledge()
    {
        foreach (var profile in knowledgeProfiles)
        {
            ApplyKnowledgeProfile(profile);
        }
        
        foreach (var knownArea in instanceKnownAreas)
        {
            ApplyKnownArea(knownArea);
        }
    }
    
    private void ApplyKnowledgeProfile(LocationKnowledgeProfile profile)
    {
        foreach (var knownArea in profile.knownAreas)
        {
            ApplyKnownArea(knownArea);
        }
    }

    private void ApplyKnownArea(LocationKnowledgeProfile.KnownArea knownArea)
    {
        LearnArea(knownArea.areaName);
        
        if (knownArea.knowAllSections)
        {
            foreach (var section in worldTree.GetSectionsInArea(knownArea.areaName))
            {
                LearnSection(knownArea.areaName, section);
                
                    foreach (var obj in worldTree.GetObjectsInSection(knownArea.areaName, section))
                    {
                        LearnObject(knownArea.areaName, section, obj);
                    }
            }
        }
        else
        {
            foreach (var detailedSection in knownArea.sections)
            {
                LearnSection(knownArea.areaName, detailedSection.sectionName);
            
                if (detailedSection.knowAllObjects)
                {
                    foreach (var obj in worldTree.GetObjectsInSection(knownArea.areaName, detailedSection.sectionName))
                    {
                        LearnObject(knownArea.areaName, detailedSection.sectionName, obj);
                    }
                }
                else
                {
                    foreach (var obj in detailedSection.specificObjects)
                    {
                        LearnObject(knownArea.areaName, detailedSection.sectionName, obj);
                    }
                }
            }
        }
    }

    public void EnterArea(string areaName)
    {
        switch (memoryConfig.memoryFeatureType)
        {
            case MemoryFeatureType.SensoryMemory:
                knownAreas.Clear();
                knownSections.Clear();
                knownObjects.Clear();
                break;
            
            case MemoryFeatureType.ShortTermMemory:
                // Store previous location
                previousArea = currentArea;
                previousSection = currentSection;
                
                // Clear anything that's not the current or previous area
                if (previousArea != null && !previousArea.Equals(areaName))
                {
                    ClearOldMemories(previousArea);
                }
                break;
            
                case MemoryFeatureType.LongTermMemory:
                // Store previous location
                previousArea = currentArea;
                previousSection = currentSection;
                break;
        }
        
        currentArea = areaName;
        LearnArea(areaName);
        
        memorySystem.UpdateCurrentLocation(currentArea);
    }
    
    public void EnterSection(string sectionName)
    {
        switch (memoryConfig.memoryFeatureType)
        {
            case MemoryFeatureType.SensoryMemory:
                ClearSectionObjects(currentArea, currentSection);
                break;
            default:
                previousSection = currentSection;
                break;
        }
        
        currentSection = sectionName;
        
        if (!string.IsNullOrEmpty(currentArea))
        {
            LearnSection(currentArea, sectionName);
            
            // When entering a section, learn about objects in it
            PerceiveObjectsInCurrentSection();
            
            memorySystem.UpdateCurrentLocation($"{currentArea}:{currentSection}");
        }
    }
    
    // Helper method to clear old memories not needed in short-term memory
    private void ClearOldMemories(string exceptArea)
    {
        // Keep only current and previous area
        var areasToForget = knownAreas.Keys
            .Where(a => !a.Equals(currentArea) && !a.Equals(exceptArea))
            .ToList();
            
        foreach (var area in areasToForget)
        {
            knownAreas.Remove(area);
            
            // Remove sections in this area
            var sectionsToRemove = knownSections.Keys
                .Where(k => k.StartsWith($"{area}:"))
                .ToList();
                
            foreach (string section in sectionsToRemove)
            {
                knownSections.Remove(section);
            }
            
            // Remove objects in this area
            var objectsToRemove = knownObjects.Keys
                .Where(k => k.StartsWith($"{area}:"))
                .ToList();
                
            foreach (string obj in objectsToRemove)
            {
                knownObjects.Remove(obj);
            }
        }
    }
    
    // Helper method to clear objects in a specific section
    private void ClearSectionObjects(string areaName, string sectionName)
    {
        if (string.IsNullOrEmpty(areaName) || string.IsNullOrEmpty(sectionName))
            return;
            
        var prefix = $"{areaName}:{sectionName}:";
        
        // Find all object keys that start with this prefix
        var objectsToRemove = knownObjects.Keys
            .Where(k => k.StartsWith(prefix))
            .ToList();
            
        foreach (string obj in objectsToRemove)
        {
            knownObjects.Remove(obj);
        }
    }
    
    // Perceive objects in the current section
    private void PerceiveObjectsInCurrentSection()
    {
        if (!string.IsNullOrEmpty(currentArea) && !string.IsNullOrEmpty(currentSection))
        {
            foreach (var obj in worldTree.GetObjectsInSection(currentArea, currentSection))
            {
                LearnObject(currentArea, currentSection, obj);
                ObserveObject(currentArea, currentSection, obj);
            }
        }
    }
    
    // Record that the NPC has learned about an area
    public void LearnArea(string areaName)
    {
        knownAreas.TryAdd(areaName, true);
    }
    
    // Record that the NPC has learned about a section within an area
    public void LearnSection(string areaName, string sectionName)
    {
        string key = $"{areaName}:{sectionName}";
        knownSections.TryAdd(key, areaName);
        
    }
    
    // Record that the NPC has learned about an object within a section
    public void LearnObject(string areaName, string sectionName, string objectName)
    {
        var key = $"{areaName}:{sectionName}:{objectName}";
        if (!knownObjects.ContainsKey(key))
        {
            knownObjects[key] = $"{areaName}:{sectionName}";
        }
    }
    
    // Get a list of all areas the NPC knows about
    public List<string> GetKnownAreas()
    {
        return knownAreas.Keys.ToList();
    }
    
    // Get a list of all sections the NPC knows about in a specific area
    public List<string> GetKnownSectionsInArea(string areaName)
    {
        return knownSections.Keys
            .Where(k => k.StartsWith($"{areaName}:"))
            .Select(k => k.Split(':')[1])
            .ToList();
    }
    
    // Get a list of all objects the NPC knows about in a specific section
    public List<string> GetKnownObjectsInSection(string areaName, string sectionName)
    {
        var prefix = $"{areaName}:{sectionName}:";
        return knownObjects.Keys
            .Where(k => k.StartsWith(prefix))
            .Select(k => k.Split(':')[2])
            .ToList();
    }
    
    // Check if the NPC knows about a specific area
    public bool KnowsArea(string areaName)
    {
        return knownAreas.ContainsKey(areaName);
    }
    
    // Check if the NPC knows about a specific section
    public bool KnowsSection(string areaName, string sectionName)
    {
        var key = $"{areaName}:{sectionName}";
        return knownSections.ContainsKey(key);
    }
    
    // Check if the NPC knows about a specific object
    public bool KnowsObject(string areaName, string sectionName, string objectName)
    {
        var key = $"{areaName}:{sectionName}:{objectName}";
        return knownObjects.ContainsKey(key);
    }
    
    // Get a reference to a GameObject in the world
    public GameObject GetObjectReference(string areaName, string sectionName, string objectName)
    {
        if (!KnowsObject(areaName, sectionName, objectName))
            return null;
            
        return worldTree.GetObjectGameObject(areaName, sectionName, objectName);
    }
    
    // Method to create a new observation about an object (to be called by memory system)
    public void ObserveObject(string areaName, string sectionName, string objectName)
    {
        if (!KnowsObject(areaName, sectionName, objectName))
            return;
        
        GameObject obj = GetObjectReference(areaName, sectionName, objectName);
        if (obj == null)
            return;
            
        ObjectState objectState = obj.GetComponent<ObjectState>();
        if (objectState == null)
            return;
            
        var observation = $"The {objectName} in the {sectionName} is {objectState.currentState}";
        memorySystem.AddObservation(observation);
    }
    
    // Generate a natural language description of what the NPC knows about the world
    public string GetWorldPerceptionDescription()
    {
        var description = new StringBuilder();
        
        description.AppendLine("### CURRENT LOCATION INFORMATION");
        
        // Describe current location
        if (!string.IsNullOrEmpty(currentArea))
        {
            description.Append($"Currently in: {currentArea}:");
            
            if (!string.IsNullOrEmpty(currentSection))
            {
                description.AppendLine($" {currentSection}.");
                
                // For sensory memory and short-term memory, only describe objects in current section
                var currentObjects = GetKnownObjectsInSection(currentArea, currentSection);
                if (currentObjects.Count > 0)
                {
                    description.AppendLine($"Available objects: {string.Join(", ", currentObjects)}.");
                }
            }
            else
            {
                description.AppendLine();
            }
        }
        
        // For short-term memory, also describe the previous location if applicable
        if (memoryConfig.memoryFeatureType == MemoryFeatureType.ShortTermMemory && 
            !string.IsNullOrEmpty(previousArea) && KnowsArea(previousArea))
        {
            description.Append($"{memorySystem.firstName} was previously in {previousArea}:");
            
            if (!string.IsNullOrEmpty(previousSection) && KnowsSection(previousArea, previousSection))
            {
                description.AppendLine($" {previousSection}.");
            }
            else
            {
                description.AppendLine();
            }
        }
        
        // For long-term memory, describe all known areas and their sections
        if (memoryConfig.memoryFeatureType == MemoryFeatureType.LongTermMemory)
        {
            description.Append($"Available sections in current area: ");
            description.AppendLine(string.Join(", ", GetKnownSectionsInArea(currentArea)));
            
            description.Append($"Available areas: ");
            description.AppendLine(string.Join(", ", knownAreas.Keys));
        }
        
        return description.ToString();
    }
}
