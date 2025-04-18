using System;
using System.Collections;
using System.Collections.Generic;
using Logging;
using TMPro;
using UnityEngine;

public class DaytimeCycle : MonoBehaviour
{
    public static DaytimeCycle Instance { get; private set; }

    public float timeScale = 1f;

    public float npcPerceptionInterval = 100f; // in sim time steps
    
    [SerializeField]
    private SimDateTime currentSimDateTime;
    
    [SerializeField]
    private TMP_Text simDateTimeText;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        
        // Log system startup
        LogSystem.Instance.Log(LogCategory.System, LogLevel.Info,
            "Generative Agents simulation started",
            new StringObjectDictionary()
            {
                {"MemoryMode", MemoryFeatureManager.Instance.GetConfiguration().memoryFeatureType.ToString()}
            });

        simDateTimeText.text = currentSimDateTime.ToString();
        StartCoroutine(DoTimeStep());
    }

    private IEnumerator DoTimeStep()
    {
        yield return new WaitForSeconds(1);
        currentSimDateTime.Seconds++;
        if (currentSimDateTime.Seconds >= 60f)
        {
            currentSimDateTime.Minutes++;
            currentSimDateTime.Seconds %= 60f;
        }
        
        // Count hours
        if (currentSimDateTime.Minutes >= 60)
        {
            currentSimDateTime.Hours++;
            currentSimDateTime.Minutes %= 60;
        }
        
        // Count days
        if (currentSimDateTime.Hours >= 24)
        {
            currentSimDateTime.Day++;
            currentSimDateTime.Hours %= 24;
        }
        
        simDateTimeText.text = currentSimDateTime.ToString();
        StartCoroutine(DoTimeStep());
    }

    public string GetSimDateTimeString()
    {
        return currentSimDateTime.ToString();
    }
    
    public string GetSimDateTimeString(SimDateTime simDateTime)
    {
        return simDateTime.ToString();
    }

    public SimDateTime GetSimDateTime()
    {
        return currentSimDateTime;
    }
    
    public SimDateTime GetSimDateTime(SimDateTime simDateTime)
    {
        return simDateTime;
    }
    
    /// <summary>
    /// Calculates the time difference in hours between two SimDateTime values.
    /// </summary>
    public double CalculateHoursBetween(SimDateTime from, SimDateTime to)
    {
        // Convert both times to hours
        var fromTotalHours = (from.Day - 1) * 24 + from.Hours + (from.Minutes / 60.0) + (from.Seconds / 3600.0);
        var toTotalHours = (to.Day - 1) * 24 + to.Hours + (to.Minutes / 60.0) + (to.Seconds / 3600.0);
    
        // Calculate difference
        return toTotalHours - fromTotalHours;
    }
    
    public double CalculateSecondsBetween(SimDateTime from, SimDateTime to)
    {
        // Convert both times to total seconds
        var fromTotalSeconds = (from.Day - 1) * 86400 + from.Hours * 3600 + from.Minutes * 60 + from.Seconds;
        var toTotalSeconds = (to.Day - 1) * 86400 + to.Hours * 3600 + to.Minutes * 60 + to.Seconds;

        // Calculate difference
        return toTotalSeconds - fromTotalSeconds;
    }

    public void AddHours(int hours)
    {
        currentSimDateTime.Hours += hours;
    }
}
