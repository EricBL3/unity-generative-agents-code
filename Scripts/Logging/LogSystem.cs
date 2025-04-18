using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Logging
{
    public class LogSystem : MonoBehaviour
    {
        public static LogSystem Instance { get; private set; }

        [FoldoutGroup("Configuration")]
        [Tooltip("Enable or disable all logging")]
        [SerializeField] 
        private bool enableLogging = true;
        
        [FoldoutGroup("Configuration")]
        [Tooltip("Minimum log level to record")]
        [SerializeField]
        private LogLevel minLogLevel = LogLevel.Debug;
        
        [FoldoutGroup("Output Settings")]
        [SerializeField]
        private bool logToConsole = true;
        
        [FoldoutGroup("Output Settings")]
        [SerializeField] private bool logToFile = true;
        
        [FoldoutGroup("Output Settings")]
        [ShowIf("logToFile")]
        [FolderPath]
        [SerializeField]
        private string logFileDirectory = "Logs";
        
        [FoldoutGroup("Output Settings")]
        [ShowIf("logToFile")]
        [SerializeField] private bool separateAgentLogs = true;
        
        [FoldoutGroup("Memory Settings")]
        [SerializeField]
        private int maxInMemoryLogs = 1000;
        
        [FoldoutGroup("Category Filters")]
        [TableList]
        [SerializeField] private List<CategoryFilter> categoryFilters = new List<CategoryFilter>();
        
        [Serializable]
        public class CategoryFilter
        {
            [TableColumnWidth(100)]
            public LogCategory Category;
            
            [TableColumnWidth(60)]
            public bool Enabled = true;
        }
        
        private Dictionary<LogCategory, bool> enabledCategories = new Dictionary<LogCategory, bool>();

        private string generalLogFilePath;
        
        private Dictionary<string, string> agentLogFilePaths = new Dictionary<string, string>();
        
        [ShowInInspector, ReadOnly]
        [FoldoutGroup("Debug")]
        private List<LogEntry> inMemoryLogs = new List<LogEntry>();
        
        
    
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLogging();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeLogging()
        {
            if (!enableLogging)
                return;
                
            foreach (var filter in categoryFilters)
            {
                enabledCategories[filter.Category] = filter.Enabled;
            }
            
            foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
            {
                enabledCategories.TryAdd(category, true);
            }
            
            if (logToFile)
            {
                var directory = Path.Combine(Application.persistentDataPath, logFileDirectory);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Create a session-specific subfolder
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var sessionDir = Path.Combine(directory, $"Session_{timestamp}");
                Directory.CreateDirectory(sessionDir);
                
                // Create the general log file
                var generalFileName = "General.log";
                generalLogFilePath = Path.Combine(sessionDir, generalFileName);
                
                string header = $"=== Generative Agents Log - {DateTime.Now} ===\n";
                File.WriteAllText(generalLogFilePath, header);
                
                Debug.Log($"Logging system initialized. Log directory: {sessionDir}");
            }
        }


        public void Log(LogCategory category, LogLevel level, string message, StringObjectDictionary context = null, 
            string agentId = null)
        {
            if (!enableLogging)
                return;
                
            if (!enabledCategories.TryGetValue(category, out bool enabled) || !enabled)
                return;
                
            if (level < minLogLevel)
                return;
                
            var logEntry = new LogEntry(category, level, message, context, agentId);
            
            inMemoryLogs.Add(logEntry);
            if (inMemoryLogs.Count > maxInMemoryLogs)
            {
                inMemoryLogs.RemoveAt(0);
            }
            
            if (logToConsole)
            {
                var consoleMessage = logEntry.ToString();
                
                switch (level)
                {
                    case LogLevel.Debug:
                        Debug.Log(consoleMessage);
                        break;
                    case LogLevel.Info:
                        Debug.Log(consoleMessage);
                        break;
                    case LogLevel.Warning:
                        Debug.LogWarning(consoleMessage);
                        break;
                    case LogLevel.Error:
                        Debug.LogError(consoleMessage);
                        break;
                }
            }
            
            if (logToFile)
            {
                try
                {
                    
                    var contextJson = SerializeContext(context);
                    var logLine = $"{logEntry.ToString()}  - {contextJson}";
                    
                    if (separateAgentLogs && !string.IsNullOrEmpty(agentId))
                    {
                        if (!agentLogFilePaths.TryGetValue(agentId, out var agentLogPath))
                        {
                            var directory = Path.GetDirectoryName(generalLogFilePath);
                            var agentFileName = $"Agent_{agentId}.log";
                            agentLogPath = Path.Combine(directory, agentFileName);
                            
                            var header = $"=== Agent {agentId} Log - {DateTime.Now} ===\n";
                            File.WriteAllText(agentLogPath, header);
                            
                            agentLogFilePaths[agentId] = agentLogPath;
                        }
                        
                        File.AppendAllText(agentLogPath, logEntry.ToString() + "\n");
                    }
                    else if (!string.IsNullOrEmpty(generalLogFilePath))
                    {
                        File.AppendAllText(generalLogFilePath, logEntry.ToString() + "\n");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to write to log file: {ex.Message}");
                }
            }
        }
        
        // Utility methods for getting logs
        public List<LogEntry> GetRecentLogs(int count)
        {
            count = Mathf.Min(count, inMemoryLogs.Count);
            return inMemoryLogs.GetRange(inMemoryLogs.Count - count, count);
        }
        
        // Agent-specific methods
        public List<LogEntry> GetAgentLogs(string agentId, int maxCount = int.MaxValue)
        {
            return inMemoryLogs
                .Where(log => log.AgentId == agentId)
                .TakeLast(maxCount)
                .ToList();
        }
        
        // Getting a list of all agents that have logs
        public List<string> GetAllAgentIds()
        {
            return inMemoryLogs
                .Where(log => !string.IsNullOrEmpty(log.AgentId))
                .Select(log => log.AgentId)
                .Distinct()
                .ToList();
        }
        
        // Helper methods for common Generative Agents logging scenarios
        
        // Memory system logging
        public void LogMemoryCreation(string agentId, string description, int importance, string memoryType)
        {
            Log(LogCategory.Memory, LogLevel.Info,
                $"Created memory: {description}",
                new StringObjectDictionary
                {
                    {"Importance", importance},
                    {"MemoryType", memoryType}
                }, 
                agentId);
        }
        
        public void LogMemoryRetrieval(string agentId, string memoryId, string description, float relevanceScore)
        {
            Log(LogCategory.Memory, LogLevel.Debug,
                $"Memory retrieved: {description}",
                new StringObjectDictionary
                {
                    {"MemoryId", memoryId},
                    {"RelevanceScore", relevanceScore.ToString("F2")}
                }, 
                agentId);
        }

        // LLM logging
        public void LogLLMPrompt(string agentId, string prompt)
        {
            Log(LogCategory.LLM, LogLevel.Debug,
                "Sending prompt to LLM",
                new StringObjectDictionary
                {
                    {"Prompt", prompt},
                    {"TokenCount", CountTokens(prompt)}
                }, 
                agentId);
        }

        public void LogLLMResponse(string agentId, string response, double responseDuration)
        {
            Log(LogCategory.LLM, LogLevel.Info,
                "Received LLM response",
                new StringObjectDictionary
                {
                    {"ResponseLength", response.Length},
                    {"ResponseDuration (ms)", responseDuration},
                    {"Response", response}
                }, 
                agentId);
        }

        // Action logging
        public void LogAction(string agentId, string actionDescription)
        {
            Log(LogCategory.Action, LogLevel.Info,
                $"Action: {actionDescription}",
                null, 
                agentId);
        }
        
        public int CountTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
        
            // Split by whitespace to count words
            string[] words = text.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    
            // Count special characters (punctuation often becomes separate tokens)
            int specialChars = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
    
            // Estimate: each word is ~1.3 tokens (accounts for subword tokenization)
            // Add special characters as potential separate tokens
            return (int)Math.Ceiling(words.Length * 1.3f + specialChars * 0.3f);
        }
        
        private string SerializeContext(Dictionary<string, object> context)
        {
            if (context == null || context.Count == 0)
                return string.Empty;
        
            // Filter out large text fields for the log message
            var filteredContext = new Dictionary<string, object>();
    
            foreach (var kvp in context)
            {
                // Skip large text fields like full prompts/responses
                if (kvp.Key == "Prompt" || kvp.Key == "Response")
                {
                    // For prompts and responses, just include length indicator
                    filteredContext[$"{kvp.Key}Length"] = kvp.Value is string s ? s.Length : 0;
                    continue;
                }
        
                // Include everything else
                filteredContext[kvp.Key] = kvp.Value;
            }
    
            // Use the simple bracket notation format
            var contextBuilder = new System.Text.StringBuilder();
            foreach (var kvp in filteredContext)
            {
                // Format each key-value pair with square brackets
                // Convert null values to "null" string
                var valueStr = kvp.Value?.ToString() ?? "null";
        
                // Escape any closing square brackets in the value to avoid parsing issues
                valueStr = valueStr.Replace("]", "\\]");
        
                contextBuilder.Append($"[{kvp.Key}={valueStr}] ");
            }
    
            return contextBuilder.ToString().TrimEnd();
        }

        // Helper class for JsonUtility serialization
        [Serializable]
        private class Serializable
        {
            public Dictionary<string, object> dict;
    
            public Serializable(Dictionary<string, object> dict)
            {
                this.dict = dict;
            }
        }
    }
}
