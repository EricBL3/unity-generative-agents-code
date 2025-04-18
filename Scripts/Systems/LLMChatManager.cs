using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LLMUnity;
using Logging;
using UnityEngine;


[RequireComponent(typeof(LLMCharacter))]
public class LLMChatManager : MonoBehaviour
{
    private LLMCharacter llmCharacter;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        llmCharacter = GetComponent<LLMCharacter>();
    }

    /// <summary>
    /// Gets a response from the LLM.
    /// </summary>
    /// <param name="prompt">The prompt for the LLM</param>
    /// <returns></returns>
    public async Task<string> GetLLMResponseAsync(string prompt, string agentId)
    {
        llmCharacter.playerName = agentId;
        
        LogSystem.Instance.LogLLMPrompt(agentId, prompt);
        
        var responseStartTime = DateTime.Now;
        
        var response = await llmCharacter.Chat(prompt, addToHistory: false);
        
        var responseEndTime = DateTime.Now;

        
        
        await llmCharacter.Save(llmCharacter.save);
        
        //llmCharacter.ClearChat();
        
        var responseTime = responseEndTime - responseStartTime;
        LogSystem.Instance.LogLLMResponse(agentId, response, responseTime.TotalMilliseconds);
        
        return response;
    }

    public void SetSaveFileName(string saveFileName)
    {
        llmCharacter.save = saveFileName;
    }
}
