namespace RescueDrone;

using System.Collections.Generic;
using Godot;

public partial class DroneFormation : Node3D
{
    [Export] private float Spacing { get; set; } = 3f;
    [Export] private float VerticalSpacing { get; set; } = 1.5f;

    private readonly List<SmallDrone> followers = [];

    public void AddDrone(SmallDrone drone)
    {
        if (followers.Contains(drone))
            return;
        
        followers.Add(drone);
        EventRepository.Instance.InvokePlayerSmallDronesFollowing((ushort)followers.Count);
        drone.SetFormation(this, followers.Count - 1);
    }

    public void RemoveDrone(SmallDrone drone)
    {
        followers.Remove(drone);
        EventRepository.Instance.InvokePlayerSmallDronesFollowing((ushort)followers.Count);
        ReassignIndexes();
    }

    public List<SmallDrone> GetFollowers()
    {
        return [..followers];
    }

    public Vector3 GetSlotPosition(int index)
    {
        return GetSlotLocationCircular(index);
    }

    private Vector3 GetSlotLocationCircular(int index)
    {
        var radius = 2.5f + (index / 6) * 2f;
        var angle = index * Mathf.Pi * 2f / 6f;
        
        var localOffset = new Vector3(
            x: Mathf.Cos(angle) * radius,
            y: (index / 6) * 1.2f,
            z: Mathf.Sin(angle) * radius);
        
        return GlobalPosition + GlobalTransform.Basis * localOffset;
    }

    private Vector3 GetSlotLocationGridBehindPlayer(int index)
    {
        // Simple layered grid formation behind player
        var row = index / 4;
        var column = index % 4;

        var xOffset = (column - 1.5f) * Spacing;
        var yOffset = row * VerticalSpacing;
        var zOffset = (row + 1) * Spacing;

        var localOffset = new Vector3(xOffset, yOffset, zOffset);

        return GlobalTransform.Origin + GlobalTransform.Basis * localOffset;
    }

    private void ReassignIndexes()
    {
        for (int i = 0; i < followers.Count; i++)
        {
            followers[i].SetFormation(this, i);
        }
    }
    
}