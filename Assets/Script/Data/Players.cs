using System;
using UnityEngine;

[System.Serializable]
public class Players : IComparable<Players>
{
    public string Name;
    public int Velocity;
    public string Color;
    public Color trueColor;
    public string Icon;
    public Sprite iconSprite;

    /// <summary>
    /// Compare players by velocity
    /// </summary>
    public int CompareTo(Players other)
    {
        return this.Velocity.CompareTo(other.Velocity);
    }
}
