namespace RescueDrone;

using Godot;

public interface IDronePathfinding
{
    Vector3[] GetAStarPath(SparseVoxelOctreeShape svo, Vector3 initialPosition);
    Vector3[] SmoothPath(Vector3[] rawPath, World3D world3D, float droneRadius);
}
