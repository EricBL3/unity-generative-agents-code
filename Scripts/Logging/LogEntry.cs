using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Logging
{
    public class LogEntry
    {
        public DateTime TimeStamp { get; private set; }
        
        public SimDateTime SimTimeStamp { get; private set; }
        
        public LogCategory Category { get; private set; }
        
        public LogLevel Level { get; private set; }

        public string AgentId  { get; private set; }

        public string Message  { get; private set; }

        public StringObjectDictionary Context  { get; private set; }

        public LogEntry(LogCategory category, LogLevel level,
            string message, StringObjectDictionary context = null, string agentId = null)
        {
            this.TimeStamp = DateTime.Now;
            this.SimTimeStamp = DaytimeCycle.Instance.GetSimDateTime();
            this.Category = category;
            this.Level = level;
            this.Message = message;
            this.Context = context;
            this.AgentId = agentId;
        }

        public override string ToString()
        {
            var timestamp = $"[{TimeStamp:HH:mm:ss}]";
            var simTime = $"[Game: {SimTimeStamp.ToString()}]";
            var memoryType = $"[Memory type: {MemoryFeatureManager.Instance.GetConfiguration().memoryFeatureType.ToString()}]";
            var agent = string.IsNullOrEmpty(AgentId) ? "" : $"[{AgentId}]";
            
            var result =  $"{timestamp} {simTime} {memoryType} {agent} [{Category}] [{Level}]: {Message}";
            
            var contextStr = GetContextAsString();
            if (!string.IsNullOrEmpty(contextStr))
            {
                result += $" - {contextStr}";
            }

            return result;
        }
        
        public string GetContextAsString()
        {
            if (Context == null || Context.Count == 0)
                return "";
        
            var contextBuilder = new System.Text.StringBuilder();
            foreach (var kvp in Context)
            {
                var valueStr = kvp.Value?.ToString() ?? "null";
        
                valueStr = valueStr.Replace("]", "\\]");
        
                contextBuilder.Append($"[{kvp.Key}={valueStr}] ");
            }
    
            return contextBuilder.ToString().TrimEnd();
        }
    }
}