using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Logging;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Controls the reflection system of an npc. It only works with long-term memory.
/// </summary>
[RequireComponent(typeof(NpcMemorySystem))]
public class ReflectionSystem : MonoBehaviour
{

    /// <summary>
    /// The amount of most recent memories to access for a reflection.
    /// </summary>
    [Required]
    [MinValue(1)]
    [SerializeField]
    private int memoriesToAccess = 100;

    /// <summary>
    /// The number of high-level questions to generate for reflection
    /// </summary>
    [Required]
    [MinValue(1)]
    [SerializeField]
    private int numberOfQuestionsToGenerate = 3;

    /// <summary>
    /// The number of insights to generate for each reflection
    /// </summary>
    [Required]
    [MinValue(1)]
    [SerializeField]
    private int numberOfInsightsToGenerate = 1;

    private NpcMemorySystem memorySystem;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        memorySystem = GetComponent<NpcMemorySystem>();
    }

    public async Task PerformReflection()
    {
        if (MemoryFeatureManager.Instance.GetConfiguration().memoryFeatureType != MemoryFeatureType.LongTermMemory ||
            !MemoryFeatureManager.Instance.GetConfiguration().reflections)
        {
            return;
        }

        var recentMemories = memorySystem.RetrieveRecentMemories(memoriesToAccess);
        
        var questions = await GenerateReflectionQuestions(recentMemories);

        foreach (var question in questions)
        {
            var relevantMemories = await memorySystem.RetrieveRelevantMemories(question);
            
            var insights = await GenerateInsight(question, relevantMemories);

            foreach (var insight in insights)
            {
                var importance = await memorySystem.ScoreMemoryImportanceAsync(insight);
                var memory = new Memory(insight, importance, MemoryType.Reflection);
                await memorySystem.AddLongTermMemory(memory);
                LogSystem.Instance.LogMemoryCreation(
                    memorySystem.AgentId,
                    memory.Description,
                    importance,
                    memory.MemoryType.ToString()
                );
            }
            
        }
    }
    
    private async Task<string[]> GenerateReflectionQuestions(List<Memory> recentMemories)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Given only the information below, what are {numberOfQuestionsToGenerate} most salient " +
                          "high-level questions we can answer about the subject in the statements?");
        prompt.AppendLine();

        foreach (var memory in recentMemories)
        {
            prompt.AppendLine($"- {memory.Description}");
        }
        
        prompt.AppendLine("Please output ONLY the questions separated by |.");
        
        var response = await memorySystem.llmChatManager.GetLLMResponseAsync(prompt.ToString(), memorySystem.AgentId);
        
        var reflectionQuestions = response.Split(new string[] { "| " }, StringSplitOptions.None);

        return reflectionQuestions;
    }
    
    private async Task<string[]> GenerateInsight(string question, List<Memory> relevantMemories)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Question: {question}");
        prompt.AppendLine($"Statements about {memorySystem.FullName}:");
        for (int i = 0; i < relevantMemories.Count; i++)
        {
            prompt.AppendLine($"{i + 1}. {relevantMemories[i].Description}");
        }
        
        prompt.AppendLine($"What {numberOfInsightsToGenerate} high-level insights can you infer from the above statements? " +
                          $"(example format: <insight> (because of <statement 1>, <statement 5>, <statement 3>))");
        prompt.AppendLine("Please output ONLY the insights following the example format separated by |");
        
        var response = await memorySystem.llmChatManager.GetLLMResponseAsync(prompt.ToString(), memorySystem.AgentId);
        
        var insights = response.Split(new string[] { "| " }, StringSplitOptions.None);

        return insights;
    }

}
