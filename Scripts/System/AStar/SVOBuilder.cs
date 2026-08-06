namespace RescueDrone;

using System.Threading.Tasks;
using Godot;

public partial class SVOBuilder : Node
{
    private float progress;
    private readonly object progressLock = new();

    public float Progress
    {
        get { lock (progressLock) { return progress; } }
        private set { lock (progressLock) { progress = value; } }
    }
    
    public bool IsDone { get; private set; }

    public VoxelNode SVOTreeRoot { get; private set; }

    public void StartAsyncGeneration(Vector3 worldBoundsCenter, float worldBoundsSize, float minNodeSize, World3D world)
    {
        Progress = 0f;
        IsDone = false;
        
        Task.Run(() => GenerateSVOThreaded(worldBoundsCenter, worldBoundsSize, minNodeSize, world));
    }

    private void GenerateSVOThreaded(Vector3 center, float size, float minNodeSize, World3D world)
    {
        // 1. Prepare Thread-Safe Physics State
        // Instead of using spaceState (which causes thread errors), 
        // we obtain a direct space state reference for shape queries.
        var spaceRid = world.Space;
        SVOTreeRoot = new VoxelNode(center, size);
        
        // 2. Divide root into the 8 top-level octants manually to track progress cleanly
        var mainOctants = SubdivideNode(SVOTreeRoot);
        for (int i = 0; i < mainOctants.Length; i++)
        {
            BuildOctreeRecursive(mainOctants[i], minNodeSize, spaceRid);
            Progress = (i + 1) / 8f;
        }
        
        // 3. Build AStar3D graph points here on thread.
        // ... (Build AStar connections) ...

        Progress = 1f;
        IsDone = true;
    }

    private static void BuildOctreeRecursive(VoxelNode node, float minNodeSize, Rid spaceRid)
    {
        var intersectsObstacle = CheckShapeCollisionThreadSafe(spaceRid, node.Position, node.Size);
        if (!intersectsObstacle)
        {
            // Empty leaf; stop subdividing
            node.IsLeaf = true;
            node.IsEmpty = true;
            return;
        }

        if (node.Size <= minNodeSize)
        {
            // Reached minNodeSize; stop subdividing
            node.IsLeaf = true;
            node.IsEmpty = false;
            return;
        }

        node.IsLeaf = false;
        node.Children = SubdivideNode(node);
        foreach (var child in node.Children)
            BuildOctreeRecursive(child, minNodeSize, spaceRid);
    }

    private static bool CheckShapeCollisionThreadSafe(Rid spaceRid, Vector3 position, float size)
    {
        var spaceState = PhysicsServer3D.SpaceGetDirectState(spaceRid);
        if (spaceState == null) return false;

        var query = new PhysicsShapeQueryParameters3D();

        using var box = new BoxShape3D();
        box.Size = Vector3.One * size;
        query.Shape = box;
        query.Transform = new Transform3D(Basis.Identity, position);
        query.CollisionMask = (1 << 4 - 1) | (1 << 8 - 1); // Buildings(4) and InvisibleBoundaries(8)

        var results = spaceState.IntersectShape(query, 1);
        return results.Count > 0;
    }

    private static VoxelNode[] SubdivideNode(VoxelNode node)
    {
        var children = new VoxelNode[8];
        var halfSize = node.Size * 0.5f;
        var quarterSize = node.Size * 0.25f;

        int index = 0;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    var childCenter = node.Position + new Vector3(x * quarterSize, y: y * quarterSize, z: z * quarterSize);
                    children[index] = new VoxelNode(childCenter, halfSize);
                    index++;
                }
            }
        }

        return children;
    }
}
