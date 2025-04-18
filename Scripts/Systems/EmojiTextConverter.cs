using System;
using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(NpcMemorySystem))]
public class EmojiTextConverter : MonoBehaviour
{
    [SerializeField]
    private TMP_Text emojiText;
    
    private NpcMemorySystem memorySystem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        memorySystem = GetComponent<NpcMemorySystem>();
        emojiText.text = "";
    }

    private void OnEnable()
    {
        NpcMemorySystem.ActionChanged += PromptForEmoji;
    }

    private void OnDisable()
    {
        NpcMemorySystem.ActionChanged -= PromptForEmoji;
    }

    private async void PromptForEmoji(string action)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Convert an action description to an emoji (important: use two or less emojis).");
        prompt.AppendLine($"Action description: {action}");
        prompt.AppendLine("Output ONLY the emoji(s)");
        
        var response = await memorySystem.llmChatManager.GetLLMResponseAsync(prompt.ToString(), memorySystem.AgentId);
        
        emojiText.text = response;
    }

}
