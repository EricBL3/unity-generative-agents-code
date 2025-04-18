using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logging
{
    public class LogViewer : SerializedMonoBehaviour
    {
        [Title("UI References")]
        [SerializeField] 
        private GameObject logEntryPrefab;
        
        [SerializeField] 
        private Transform logContainer;
        
        [SerializeField] 
        private TMP_InputField searchField;
        
        [Title("Filter Controls")]
        [TableList]
        [SerializeField] 
        private List<CategoryToggle> categoryToggles = new List<CategoryToggle>();
        
        [TableList]
        [SerializeField] 
        private List<LevelToggle> levelToggles = new List<LevelToggle>();
        
        [Title("Agent Filtering")]
        [SerializeField] 
        private TMP_Dropdown agentDropdown;
        
        [Title("Settings")]
        [SerializeField] 
        private int maxVisibleLogs = 100;
        
        [SerializeField] 
        private float refreshInterval = 1.0f;
        
        [SerializeField] 
        private bool autoRefresh = true;
        
        [Serializable]
        public class CategoryToggle
        {
            [TableColumnWidth(100)]
            public LogCategory Category;
            
            [TableColumnWidth(60)]
            public bool Enabled = true;
        }
        
        [Serializable]
        public class LevelToggle
        {
            [TableColumnWidth(100)]
            public LogLevel Level;
            
            [TableColumnWidth(60)]
            public bool Enabled = true;
        }
        
        // Filter state
        private Dictionary<LogCategory, bool> categoryFilters = new Dictionary<LogCategory, bool>();
        private Dictionary<LogLevel, bool> levelFilters = new Dictionary<LogLevel, bool>();
        private string currentSearchTerm = "";
        private string selectedAgentId = null;
        
        // UI state
        private float timeSinceLastRefresh = 0f;
        private List<GameObject> logEntryObjects = new List<GameObject>();
        
        [Button("Refresh Logs")]
        public void ManualRefresh()
        {
            RefreshLogs();
        }
        
        private void Start()
        {
            InitializeFilters();
            
            if (searchField != null)
            {
                searchField.onValueChanged.AddListener(OnSearchChanged);
            }
            
            if (agentDropdown != null)
            {
                agentDropdown.onValueChanged.AddListener(OnAgentSelectionChanged);
                PopulateAgentDropdown();
            }
            
            RefreshLogs();
        }
        
        private void Update()
        {
            if (autoRefresh)
            {
                timeSinceLastRefresh += Time.deltaTime;
                if (timeSinceLastRefresh >= refreshInterval)
                {
                    RefreshLogs();
                    timeSinceLastRefresh = 0f;
                }
            }
        }
        
        private void InitializeFilters()
        {
            foreach (var toggle in categoryToggles)
            {
                categoryFilters[toggle.Category] = toggle.Enabled;
            }
            
            foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
            {
                if (!categoryFilters.ContainsKey(category))
                {
                    categoryFilters[category] = true;
                }
            }
            
            foreach (var toggle in levelToggles)
            {
                levelFilters[toggle.Level] = toggle.Enabled;
            }
            
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                if (!levelFilters.ContainsKey(level))
                {
                    levelFilters[level] = true;
                }
            }
        }
        
        private void PopulateAgentDropdown()
        {
            if (agentDropdown == null || LogSystem.Instance == null)
                return;
                
            // Get all agent IDs
            var agentIds = LogSystem.Instance.GetAllAgentIds();
            
            // Create dropdown options
            var options = new List<TMP_Dropdown.OptionData>();
            options.Add(new TMP_Dropdown.OptionData("All Agents"));
            
            foreach (var agentId in agentIds)
            {
                options.Add(new TMP_Dropdown.OptionData(agentId));
            }
            
            // Update dropdown
            agentDropdown.ClearOptions();
            agentDropdown.AddOptions(options);
        }
        
        private void OnAgentSelectionChanged(int index)
        {
            if (index == 0)
            {
                // "All Agents" selected
                selectedAgentId = null;
            }
            else if (agentDropdown.options.Count > index)
            {
                selectedAgentId = agentDropdown.options[index].text;
            }
            
            RefreshLogs();
        }
        
        private void OnSearchChanged(string newValue)
        {
            currentSearchTerm = newValue;
            RefreshLogs();
        }
        
        private void RefreshLogs()
        {
            if (logContainer == null || LogSystem.Instance == null)
                return;
                
            // Update agent dropdown if needed
            if (agentDropdown != null && Time.frameCount % 30 == 0)
            {
                PopulateAgentDropdown();
            }
            
            // Clear existing log entries
            foreach (var obj in logEntryObjects)
            {
                Destroy(obj);
            }
            logEntryObjects.Clear();
            
            // Apply filters to logs
            List<LogEntry> filteredLogs;
            
            if (selectedAgentId != null)
            {
                filteredLogs = LogSystem.Instance.GetAgentLogs(selectedAgentId, maxVisibleLogs);
            }
            else
            {
                filteredLogs = LogSystem.Instance.GetRecentLogs(maxVisibleLogs);
            }
            
            // Apply category and level filters
            filteredLogs = filteredLogs
                .Where(log => categoryFilters.TryGetValue(log.Category, out bool catEnabled) && catEnabled)
                .Where(log => levelFilters.TryGetValue(log.Level, out bool lvlEnabled) && lvlEnabled)
                .ToList();
                
            // Apply search filter
            if (!string.IsNullOrEmpty(currentSearchTerm))
            {
                filteredLogs = filteredLogs
                    .Where(log => log.Message.Contains(currentSearchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            
            // Take only most recent logs up to max visible
            filteredLogs = filteredLogs.TakeLast(maxVisibleLogs).ToList();
            
            // Create UI for each log entry
            foreach (var log in filteredLogs)
            {
                var entryObj = Instantiate(logEntryPrefab, logContainer);
                var entryText = entryObj.GetComponentInChildren<TextMeshProUGUI>();
                
                entryText.enableWordWrapping = true;

                // Get the RectTransform of either the entry object or text
                var rectTransform = entryObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Force proper width within parent
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.sizeDelta = new Vector2(0, rectTransform.sizeDelta.y);
                }
                
                entryText.text = log.ToString();
                
                // Color based on level
                Color textColor = log.Level switch
                {
                    LogLevel.Debug => Color.black,
                    LogLevel.Info => Color.white,
                    LogLevel.Warning => Color.yellow,
                    LogLevel.Error => Color.red,
                    _ => Color.white
                };
                entryText.color = textColor;
                
                logEntryObjects.Add(entryObj);
            }
        }
        
        // Category filter controls - can be called from UI buttons
        public void ToggleCategory(int categoryIndex)
        {
            if (categoryIndex >= 0 && categoryIndex < categoryToggles.Count)
            {
                var category = categoryToggles[categoryIndex].Category;
                categoryToggles[categoryIndex].Enabled = !categoryToggles[categoryIndex].Enabled;
                categoryFilters[category] = categoryToggles[categoryIndex].Enabled;
                RefreshLogs();
            }
        }
        
        // Level filter controls - can be called from UI buttons
        public void ToggleLevel(int levelIndex)
        {
            if (levelIndex >= 0 && levelIndex < levelToggles.Count)
            {
                var level = levelToggles[levelIndex].Level;
                levelToggles[levelIndex].Enabled = !levelToggles[levelIndex].Enabled;
                levelFilters[level] = levelToggles[levelIndex].Enabled;
                RefreshLogs();
            }
        }
        
        // Public controls
        public void ToggleVisibility()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            
            if (gameObject.activeSelf)
            {
                RefreshLogs();
            }
        }
        
        public void ClearFilters()
        {
            // Reset all category filters
            foreach (var toggle in categoryToggles)
            {
                toggle.Enabled = true;
                categoryFilters[toggle.Category] = true;
            }
            
            // Reset all level filters
            foreach (var toggle in levelToggles)
            {
                toggle.Enabled = true;
                levelFilters[toggle.Level] = true;
            }
            
            // Clear search
            if (searchField != null)
            {
                searchField.text = "";
            }
            
            // Reset agent selection
            if (agentDropdown != null)
            {
                agentDropdown.value = 0; // "All Agents"
            }
            
            // Refresh display
            RefreshLogs();
        }
    }
}