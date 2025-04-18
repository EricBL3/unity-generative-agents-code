using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class MemoryFeatureManager : MonoBehaviour
{
    public static MemoryFeatureManager Instance;

    [Required]
    [SerializeField] 
    private MemoryFeatureFlags configuration;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public MemoryFeatureFlags GetConfiguration()
    {
        return configuration;
    }
}
