
/// <summary>
/// Represents different types of memories an NPC can have.
/// </summary>
public enum MemoryType
{
    /// <summary>
    /// Direct observations from the environment.
    /// </summary>
    Observation,
    
    /// <summary>
    /// Thoughts and inferences based on past observations.
    /// </summary>
    Reflection,
    
    /// <summary>
    /// Future intentions or scheduled actions.
    /// </summary>
    Plan,
    
    /// <summary>
    /// Social interactions with other characters.
    /// </summary>
    Conversation
}
