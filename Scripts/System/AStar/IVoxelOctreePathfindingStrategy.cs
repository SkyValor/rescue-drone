namespace RescueDrone;

using Godot;

public interface IVoxelOctreePathfindingStrategy
{
    Vector3[] CreatePath(Vector3 start, Vector3 end);
    VoxelNode GetLeafAtPosition(Vector3 position);
}
