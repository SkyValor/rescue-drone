namespace RescueDrone;

using Godot;
using Godot.Collections;

public interface IDronePathfinding
{
    Vector3[] GetAStarPath(SparseVoxelOctreeShape svo, World3D world3D, Vector3 initialPosition, float droneRadius);
    Vector3[] SmoothPath(Vector3[] rawPath, World3D world3D, float droneRadius, Array<Rid> exclusion);
}
