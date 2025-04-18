using System;
using UnityEngine;

/// <summary>
/// Represents the datetime of the simulation.
/// </summary>
[Serializable]
public struct SimDateTime : IComparable<SimDateTime>
{
    [SerializeField]
    private int day;
    [SerializeField]
    private int hours;
    [SerializeField]
    private int minutes;
    [SerializeField]
    private float seconds;

    // Ensure day is at least 1
    public int Day
    {
        get => day;
        set => day = Mathf.Max(1, value); 
    }

    // Hours should be between 0 and 24
    public int Hours
    {
        get => hours;
        set => hours = Mathf.Clamp(value, 0, 24); 
    }

    // Minutes should be between 0 and 60
    public int Minutes
    {
        get => minutes;
        set => minutes = Mathf.Clamp(value, 0, 60); 
    }

    // Seconds should be between 0 and 60
    public float Seconds
    {
        get => seconds;
        set => seconds = Mathf.Clamp(value, 0f, 60f); 
    }

    // Constructor to initialize the struct
    public SimDateTime(int day, int hours, int minutes, float seconds)
    {
        this.day = day;
        this.hours = hours;
        this.minutes = minutes;
        this.seconds = seconds;
    }

    // Method to get the simulation datetime as a formatted string
    public int CompareTo(SimDateTime other)
    {
        if (day != other.day)
            return day.CompareTo(other.day);

        if (hours != other.hours)
            return hours.CompareTo(other.hours);

        if (minutes != other.minutes)
            return minutes.CompareTo(other.minutes);
        
        return seconds.CompareTo(other.Seconds);
    }

    public override string ToString()
    {
        return $"Day {day} - {hours:00}:{minutes:00}:{seconds:00}";
    }
}
