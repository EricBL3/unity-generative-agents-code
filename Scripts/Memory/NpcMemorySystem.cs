using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMUnity;
using Logging;
using Sirenix.OdinInspector;
using UnityEngine;
using VHierarchy.Libs;
using Random = Unity.Mathematics.Random;

/// <summary>
/// Controls the memory system of an NPC, handling both short-term and long-term memory.
/// </summary>
/// <remarks>
/// - Short-term memory has a limited size and will remove the oldest memories when full.
/// - Long-term memory is persistent and can store important events.
/// </remarks>
/// 
[RequireComponent(typeof(LLMChatManager))]
[RequireComponent(typeof(ReflectionSystem))]
public class NpcMemorySystem : MonoBehaviour
{
    #region Identity Properties

    [Required] 
    [SerializeField] 
    public NpcTemplate npcTemplate;
    
    [TitleGroup("Identity")]
    [BoxGroup("Identity/Basic Information")]
    [Tooltip("The NPC's full name")]
    [ShowInInspector] 
    public string FullName => $"{firstName} {lastName}";
    
    public string AgentId => FullName.Replace(' ', '_');
    
    [BoxGroup("Identity/Basic Information")]
    public string firstName { get; private set; }
    
    [BoxGroup("Identity/Basic Information")]
    [SerializeField] private string lastName;
    
    [BoxGroup("Identity/Basic Information")]
    [SerializeField] private int age;
    
    [BoxGroup("Identity/Traits")]
    [Tooltip("L0 - Permanent core traits that never change")]
    [TextArea(2,3)]
    [SerializeField] private string innateTraits;
    
    [BoxGroup("Identity/Traits")]
    [Tooltip("L1 - Stable traits that evolve very slowly through reflection")]
    [TextArea(2,3)]
    [SerializeField] private string learnedTraits;
    
    [BoxGroup("Identity/Traits")]
    [Tooltip("L2 - Current state or focus that can change based on experiences")]
    [TextArea(2,3)]
    [SerializeField] private string currentState;
    
    [BoxGroup("Identity/Environment")]
    [TextArea(2,3)]
    [SerializeField] private string lifestyle;
    
    [BoxGroup("Identity/Environment")]
    [TextArea(2,3)]
    [SerializeField] private string livingArea;
    
    #endregion
    
    #region Memory Properties
    
    [Title("Memory Settings")]
    [Tooltip("The maximum number of memories in the short-term memory stream.")]
    [SerializeField, Min(1)]
    private int shortTermMemoryLimit = 10;
    
    [Title("Memory Storage")]
    
    [BoxGroup("Short-Term Memory")]
    [Tooltip("Recent memories stored temporarily")]
    [ShowInInspector, ReadOnly]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, NumberOfItemsPerPage = 5)]
    private Queue<Memory> shortTermMemoryStream = new Queue<Memory>();
    
    [BoxGroup("Long-Term Memory")]
    [Tooltip("Significant memories stored permanently.")]
    [ShowInInspector]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true)]
    private List<Memory> longTermMemoryStream = new List<Memory>();

    private MemoryFeatureFlags memoryConfig;

    [SerializeField]
    [Range(0, 1)]
    private float recencyWeight = 1f;
    
    [SerializeField]
    [Range(0, 1)]
    private float relevanceWeight = 1f;

    [SerializeField]
    [Range(0, 1)]
    private float importanceWeight = 1f;

    [SerializeField] 
    private int embeddingDimension = 384;
    
    private Dictionary<string, float[]> embeddingCache  = new Dictionary<string, float[]>();
    
    #endregion
    
    #region Current Status
    
    [Title("NPC Context")]
    [BoxGroup("Current Status")]
    [Tooltip("The current action the NPC is performing.")]
    [ShowInInspector]
    public string CurrentAction { get; private set; }
    
    [BoxGroup("Current Status")]
    [Tooltip("The time the current action the NPC is performing started at.")]
    [ShowInInspector, ReadOnly]
    public SimDateTime CurrentActionTimeStart { get; private set; }

    [BoxGroup("Current Status")]
    [Tooltip("The current location of the NPC.")]
    [ShowInInspector, ReadOnly]
    public string CurrentLocation { get; private set; }

    [BoxGroup("Previous State")]
    [Tooltip("The previous location of the NPC.")]
    [ShowInInspector, ReadOnly]
    public string PreviousLocation { get; private set; }

    [BoxGroup("Previous State")]
    [Tooltip("The NPC's plan from the previous day.")]
    [ShowInInspector, ReadOnly]
    public string PreviousDayPlan { get; private set; }
    
    #endregion
    
    #region Reflection Properties
    
    /// <summary>
    /// The threshold that determines the sum of the importance of memories for generating a reflection.
    /// </summary>
    [Title("Reflection Settings")]
    [Required]
    [MinValue(1)]
    [SerializeField]
    private int importanceThreshold = 150;
    
    /// <summary>
    /// The current sum of importance scores for generating a reflection.
    /// </summary>
    [ShowInInspector, ReadOnly]
    private int currentImportanceSum = 0;

    private ReflectionSystem reflectionSystem;
    
    #endregion
    
    
    public LLMChatManager llmChatManager { get; private set; }

    public static Action<string> ActionChanged;

    private void Start()
    {
        memoryConfig = MemoryFeatureManager.Instance.GetConfiguration();
        llmChatManager = GetComponent<LLMChatManager>();
        reflectionSystem = GetComponent<ReflectionSystem>();
        currentImportanceSum = 0;
        InitializeCharacter();
        
    }

    #region Identity Methods

    public void InitializeCharacter()
    {
        firstName = npcTemplate.firstName;
        lastName = npcTemplate.lastName;
        age = npcTemplate.age;
        innateTraits = npcTemplate.innateTraits;
        learnedTraits = npcTemplate.learnedTraits;
        currentState = npcTemplate.currentState;
        lifestyle = npcTemplate.lifestyle;
        livingArea = npcTemplate.livingArea;

        llmChatManager.SetSaveFileName($"{firstName}_{lastName}");

        InitializeSeedMemories(npcTemplate.seedMemoriesText);
    }
    
    /// <summary>
    /// Generates a complete character description for LLM prompting.
    /// </summary>
    public string GenerateCharacterDescription()
    {
        var description = new StringBuilder();
        
        description.AppendLine($"SIMULATION PRINCIPLE: {FullName} is a simulation of a real human being with authentic human behaviors, limitations, and thought processes. In all responses:");
        description.AppendLine();
        description.AppendLine("- Maintain human-like reasoning with appropriate limitations in knowledge and perception");
        description.AppendLine("- Consider natural human patterns for time (daily rhythms, appropriate activity timing)");
        description.AppendLine("- Account for physical and psychological needs (rest, food, social connection)");
        description.AppendLine("- Respond naturally to environmental contexts and social situations");
        description.AppendLine("- Demonstrate consistent personality while allowing for normal human variability");
        description.AppendLine("- Show authentic emotional responses proportional to situations");
        description.AppendLine("- Consider the constraints and capabilities of a typical human body and mind");
        description.AppendLine(" - Perform actions that go accordingly with the current location and objects that are present in the location.");
        description.AppendLine();
        description.AppendLine($"All responses should reflect how a real human would perceive, think, act, and respond in {FullName}'s circumstances.");
        description.AppendLine();
        
        // Basic identity
        description.AppendLine($"Name: {FullName} (age: {age})");
        description.AppendLine($"Innate traits: {innateTraits}");
        
        // Add learned traits if they exist
        if (!string.IsNullOrEmpty(learnedTraits))
        {
            description.AppendLine($"{firstName} {learnedTraits}");
        }
        
        
        // Add lifestyle and living area if they exist
        if (!string.IsNullOrEmpty(lifestyle))
        {
            description.AppendLine(lifestyle);
        }
        
        if (!string.IsNullOrEmpty(livingArea))
        {
            description.AppendLine(livingArea);
        }
        
        return description.ToString();
    }
    
    #endregion
    
    #region Memory Methods
    
    /// <summary>
    /// Initializes seed memories from a semicolon-delimited string.
    /// </summary>
    public void InitializeSeedMemories(string seedMemoriesText)
    {
        if (string.IsNullOrEmpty(seedMemoriesText))
            return;
            
        var seedStatements = seedMemoriesText.Split(';');
        
        foreach (var statement in seedStatements)
        {
            if (string.IsNullOrWhiteSpace(statement))
                continue;
                
            var trimmedStatement = statement.Trim();
            var seedMemory = new Memory(trimmedStatement, 8, MemoryType.Observation);
            
            longTermMemoryStream.Add(seedMemory);
        }
    }
    
    /// <summary>
    /// Adds a memory to short-term storage, ensuring the limit is maintained.
    /// </summary>
    public async Task AddShortMemory(Memory memory)
    {
        if (shortTermMemoryStream.Count >= shortTermMemoryLimit)
        {
            shortTermMemoryStream.Dequeue();
        }
        
        shortTermMemoryStream.Enqueue(memory);
        await CheckReflectionSystem(memory.Importance);
    }

    /// <summary>
    /// Adds a memory to the long-term memory stream.
    /// </summary>
    public async Task AddLongTermMemory(Memory memory)
    {
        longTermMemoryStream.Add(memory);
        await CheckReflectionSystem(memory.Importance);
    }

    
    public async void AddObservation(string observationDescription)
    {
        if (memoryConfig.memoryFeatureType == MemoryFeatureType.SensoryMemory)
            return;
        
        var importance = await ScoreMemoryImportanceAsync(observationDescription);
        var memory = new Memory(observationDescription, importance, MemoryType.Observation);

        switch (memoryConfig.memoryFeatureType)
        {
            case MemoryFeatureType.ShortTermMemory:
            {
                await AddShortMemory(memory);
                break;
            }
            case MemoryFeatureType.LongTermMemory:
                await AddLongTermMemory(memory);
                break;
        }
        
        LogSystem.Instance.LogMemoryCreation(
            AgentId,
            memory.Description,
            importance,
            memory.MemoryType.ToString()
        );
    }
    
    private async Task CheckReflectionSystem(int memoryImportance)
    {
        if (memoryConfig.memoryFeatureType == MemoryFeatureType.LongTermMemory && memoryConfig.reflections)
        {
            currentImportanceSum += memoryImportance;
            if (currentImportanceSum >= importanceThreshold)
            {
                currentImportanceSum = 0;
                await reflectionSystem.PerformReflection();
            }
        }
    }

    public List<Memory> RetrieveRecentMemories(int memoriesToAccess)
    {
        if (memoryConfig.memoryFeatureType != MemoryFeatureType.LongTermMemory)
        {
            return new List<Memory>();
        }
        
        var sortedMemories = new List<Memory>(longTermMemoryStream);
        sortedMemories.Sort((a, b) => b.createdAt.CompareTo(a.createdAt));

        return sortedMemories.Count <= memoriesToAccess ? sortedMemories : sortedMemories.GetRange(0, memoriesToAccess);
    }

    /// <summary>
    /// Retrieves memories relevant to the given context.
    /// </summary>
    public async Task<List<Memory>> RetrieveRelevantMemories(string context)
    {
        var allMemories = new List<Memory>();
        var maxMemories = 0;

        switch (memoryConfig.memoryFeatureType)
        {
            case MemoryFeatureType.SensoryMemory:
                return new List<Memory>(allMemories);
            case MemoryFeatureType.ShortTermMemory:
                maxMemories = 3;
                allMemories.AddRange(shortTermMemoryStream);
                break;
            case MemoryFeatureType.LongTermMemory:
                maxMemories = 5;
                allMemories.AddRange(longTermMemoryStream);
                break;
        }

        // Calculate raw scores for each memory
        var recencyScores = new Dictionary<Memory, float>();
        var importanceScores = new Dictionary<Memory, float>();
        var relevanceScores = new Dictionary<Memory, float>();
    
        // Track min and max values for normalization
        float minRecency = float.MaxValue, maxRecency = float.MinValue;
        float minImportance = float.MaxValue, maxImportance = float.MinValue;
        float minRelevance = float.MaxValue, maxRelevance = float.MinValue;

        // Calculate all raw scores first
        foreach (var memory in allMemories)
        {
            var recencyScore = CalculateRecencyScore(memory);
            recencyScores[memory] = recencyScore;
            minRecency = Math.Min(minRecency, recencyScore);
            maxRecency = Math.Max(maxRecency, recencyScore);
            
            
            var importanceScore = memory.Importance;
            importanceScores[memory] = importanceScore;
            minImportance = Math.Min(minImportance, importanceScore);
            maxImportance = Math.Max(maxImportance, importanceScore);
        }
        
        // Calculate relevance scores
        foreach (var memory in allMemories)
        {
            
            var relevanceScore = await CalculateRelevanceScore(memory.Description, context);
            relevanceScores[memory] = relevanceScore;
            minRelevance = Math.Min(minRelevance, relevanceScore);
            maxRelevance = Math.Max(maxRelevance, relevanceScore);
        }
        
        var scoredMemories = new Dictionary<Memory, float>();
        
        //Normalize scores and add them
        foreach (var memory in allMemories)
        {
            float normalizedRecency = maxRecency > minRecency 
                ? (recencyScores[memory] - minRecency) / (maxRecency - minRecency)
                : 0.5f;
            
            float normalizedImportance = maxImportance > minImportance 
                ? (importanceScores[memory] - minImportance) / (maxImportance - minImportance)
                : 0.5f;
            
            float normalizedRelevance = maxRelevance > minRelevance 
                ? (relevanceScores[memory] - minRelevance) / (maxRelevance - minRelevance)
                : 0.5f;
            
            var combinedScore = recencyWeight * normalizedRecency + relevanceWeight * normalizedRelevance + importanceWeight * normalizedImportance; 
            
            scoredMemories[memory] = combinedScore;
        }
        
        var retrievedMemories = new Dictionary<string, KeyValuePair<Memory, float>>();

        // Group by description, keeping only highest-scoring memory for each description
        foreach (var memoryPair in scoredMemories.OrderByDescending(kv => kv.Value))
        {
            var description = memoryPair.Key.Description;
    
            if (!retrievedMemories.ContainsKey(description) || 
                memoryPair.Value > retrievedMemories[description].Value)
            {
                retrievedMemories[description] = memoryPair;
            }
    
            if (retrievedMemories.Count >= maxMemories)
                break;
        }

        // Update access timestamps and log the retrievals
        var finalMemories = new List<Memory>();
        foreach (var entry in retrievedMemories.Values)
        {
            var memory = entry.Key;
            var score = entry.Value;
    
            memory.UpdateAccessedAt();
            LogSystem.Instance.LogMemoryRetrieval(
                AgentId,
                memory.MemoryType.ToString(),
                memory.Description,
                score
            );
    
            finalMemories.Add(memory);
        }

        return finalMemories;
    }
    
    /// <summary>
    /// Calculates recency score based on how recently the memory was accessed.
    /// Uses exponential decay function with factor 0.995 as described in the paper.
    /// </summary>
    /// <param name="memory">The memory to calculate recency for</param>
    /// <returns>A float between 0 and 1, where 1 is most recent</returns>
    private float CalculateRecencyScore(Memory memory)
    {
        var currentTime = DaytimeCycle.Instance.GetSimDateTime();
        var lastAccessTime = memory.accessedAt;
        var hoursSinceLastAccess = DaytimeCycle.Instance.CalculateHoursBetween(lastAccessTime, currentTime);
    
        // A higher recency factor means that memories will become less relevant faster.
        var recencyDecayFactor = 1f;
        switch (memoryConfig.memoryFeatureType)
        {
            case MemoryFeatureType.ShortTermMemory:
                recencyDecayFactor = 0.9f;
                break;
            case MemoryFeatureType.LongTermMemory:
                recencyDecayFactor = 0.995f;
                break;
        }
        
        // Apply exponential decay function: score = decayFactor^hoursSinceLastAccess
        var recencyScore = Mathf.Pow(recencyDecayFactor, (float)hoursSinceLastAccess);
    
        return recencyScore;
    }
    
    /// <summary>
    /// Full embedding-based relevance calculation as described in the paper.
    /// </summary>
    private async Task<float> CalculateRelevanceScore(string memoryDescription, string context)
    {
        if (!embeddingCache.ContainsKey(memoryDescription))
        {
            embeddingCache[memoryDescription] = await GetEmbeddingFromLLM(memoryDescription);
        }

        if (!embeddingCache.ContainsKey(context))
        {
            embeddingCache[context] = await GetEmbeddingFromLLM(context);
        }
        
        var memoryEmbedding = embeddingCache[memoryDescription];
        var contextEmbedding = embeddingCache[context];
        
        return CalculateCosineSimilarity(memoryEmbedding, contextEmbedding);
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    private float CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            return 0;
        
        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;
    
        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }
    
        magnitudeA = Mathf.Sqrt(magnitudeA);
        magnitudeB = Mathf.Sqrt(magnitudeB);
    
        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;
        
        return dotProduct / (magnitudeA * magnitudeB);
    }
    
    #endregion

    #region Status Update Methods
    
    /// <summary>
    /// Updates the NPC's current action.
    /// </summary>
    public void SetCurrentAction(string action)
    {
        CurrentAction = action;
        CurrentActionTimeStart = DaytimeCycle.Instance.GetSimDateTime();
        ActionChanged?.Invoke(action);
        
        if (memoryConfig.memoryFeatureType != MemoryFeatureType.SensoryMemory)
        {
            AddObservation($"{firstName} is {action}");
        }
    }

    /// <summary>
    /// Updates the NPC's current location and stores the previous one.
    /// </summary>
    public void UpdateCurrentLocation(string location)
    {
        
        AddObservation($"{firstName} entered location {location}.");
        
        if (memoryConfig.memoryFeatureType != MemoryFeatureType.SensoryMemory)
        {
            PreviousLocation = CurrentLocation;
        }
        
        CurrentLocation = location;
    }
    
    /// <summary>
    /// Sets the NPC's plan for the previous day.
    /// </summary>
    public void SetPreviousDayPlan(string plan)
    {
        PreviousDayPlan = plan;
    }
    
    #endregion
    
    #region LLM Prompting
    
    /// <summary>
    /// Creates a comprehensive prompt for the LLM with character description, 
    /// relevant memories, and current context.
    /// </summary>
    public async Task<string> CreatePromptForLLM(string context)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine(GenerateCharacterDescription());
        prompt.AppendLine();
        
        var relevantMemories = await RetrieveRelevantMemories(prompt.ToString());
        
        if (relevantMemories.Count > 0)
        {
            prompt.AppendLine("Summary of relevant context from memory:");
            
            foreach (var memory in relevantMemories)
            {
                prompt.AppendLine($"- {memory.Description}");
            }
            
            prompt.AppendLine();
        }
        
        prompt.AppendLine(context);
        
        return prompt.ToString();
    }
    
    /// <summary>
    /// Score the importance of a memory description using LLM.
    /// </summary>
    public async Task<int> ScoreMemoryImportanceAsync(string memoryDescription)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("On the scale of 1 to 10, rate the importance of this memory:");
        prompt.AppendLine($"- 1-2: Routine observations (normal objects, unchanged states)");
        prompt.AppendLine($"- 3-4: Minor changes in environment (moving between rooms, noticing full containers)");
        prompt.AppendLine($"- 5-6: Significant state changes (turning devices on/off, discovering unusual states)");
        prompt.AppendLine($"- 7-8: Important activities affecting daily function (finding broken items, completing tasks)");
        prompt.AppendLine($"- 9-10: Critical events with lasting impact (emergencies, major discoveries)");
        prompt.AppendLine($"Memory: {memoryDescription}");
        prompt.AppendLine($"Output ONLY the rating value (1-10).");

        var response = await llmChatManager.GetLLMResponseAsync(prompt.ToString(), AgentId);
        
        if (int.TryParse(response.Trim(), out var importance))
            return importance;
            
        return 5;
    }

    private async Task<float[]> GetEmbeddingFromLLM(string text)
    {
        return GenerateFallbackEmbedding(text);
        
        //TODO: figure out how to speed up embedding generation. Currently it creates a bottleneck during runtime.
        // try {
        //     var prompt = new StringBuilder();
        //     prompt.AppendLine($"I need a numerical vector embedding of the following text. " +
        //                       $"Please output ONLY a comma-separated list of {embeddingDimension} floating point numbers " +
        //                       $"between -1 and 1 representing the text's semantic meaning. No other text or explanation.");
        //     
        //     prompt.AppendLine($"Text: {text}");
        //     
        //     var response = await llmChatManager.GetLLMResponseAsync(prompt.ToString());
        //     
        //     // Parse the response into a float array
        //     return ParseEmbeddingResponse(response);
        // }
        // catch (Exception ex) {
        //     Debug.LogError($"Error generating embedding: {ex.Message}");
        //     return GenerateFallbackEmbedding(text);
        // }
    }

    #endregion
    
    #region Embedding Functions
    
    private float[] NormalizeVector(float[] vector)
    {
        var magnitude = 0f;
        foreach (var value in vector)
        {
            magnitude += value * value;
        }
        magnitude = Mathf.Sqrt(magnitude);
    
        if (magnitude < 0.00001f)
        {
            return vector;
        }
        
        var normalized = new float[vector.Length];
        for (var i = 0; i < vector.Length; i++)
        {
            normalized[i] = vector[i] / magnitude;
        }
    
        return normalized;
    }

    /// <summary>
    /// Create a deterministic but simplified embedding based on the text
    /// This is a fallback for when API calls fail
    /// </summary>
    /// <param name="text">The text to generate the embedding from.</param>
    /// <returns>simplified embedding.</returns>
    private float[] GenerateFallbackEmbedding(string text)
    {
        // Extract key terms (simple tokenization)
        HashSet<string> terms = new HashSet<string>(
            text.ToLower()
                .Split(new[] {' ', '.', ',', '!', '?', ':', ';'}, StringSplitOptions.RemoveEmptyEntries)
                .Where(term => term.Length > 3)
        );
    
        // Create a deterministic seed from the key terms
        var seed = string.Join("", terms.OrderBy(t => t)).GetHashCode();
        var random = new Random((uint)seed);
    
        float[] fallback = new float[embeddingDimension];
        for (var i = 0; i < fallback.Length; i++)
        {
            fallback[i] = (float)(random.NextDouble() * 2 - 1); // Values between -1 and 1
        }
    
        return NormalizeVector(fallback);
    }
    
    #endregion
    
}
