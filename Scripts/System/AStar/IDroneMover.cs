namespace RescueDrone;

using Godot;

public interface IDroneMover
{
    Vector3[] GetAStarPath(SparseVoxelOctreeShape svo, Vector3 closestPosition);
    Vector3[] SmoothPath(Vector3[] rawPath, World3D world3D, float droneRadius);
}
