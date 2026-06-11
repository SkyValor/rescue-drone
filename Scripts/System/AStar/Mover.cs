namespace RescueDrone;

using Godot;

public partial class Mover : CharacterBody3D
{
    [Export] public float Speed { get; private set; } = 5f;
    [Export] public float Accuracy { get; private set; } = 1f;
    [Export] public float TurnSpeed { get; private set; } = 5f;
    [Export] public OctreeGeneratorGroup OctreeGenerator { get; private set; }
    
    private SparseVoxelOctreeShape svo;
    private Vector3[] aStarPath;
    private int pathIndex;
    
    private MeshInstance3D pathInstance;
    private StandardMaterial3D pathMaterial;

    public override void _EnterTree()
    {
        SetPhysicsProcess(false);
    }

    public override void _Ready()
    {
        CallDeferred(MethodName.InitiateBehavior);
    }

    private void InitiateBehavior()
    {
        svo = OctreeGenerator.Tree;
        if (svo is null)
        {
            GD.PrintErr("SVO cannot be found in call deferred.");
            return;
        }
        
        CreateRandomPath();
        SetPhysicsProcess(true);
    }

    public override void _Process(double delta)
    {
        DrawAStarPath();
        // DrawAStarCurvePath();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (svo is null) return;

        if (aStarPath is null || aStarPath.Length == 0 || pathIndex >= aStarPath.Length)
        {
            CreateRandomPath();
            return;
        }
        
        var distanceToVoxel = GlobalPosition.DistanceTo(aStarPath[pathIndex]);
        if (distanceToVoxel < Accuracy)
        {
            pathIndex++;
            return;
        }
        
        // Smoothly rotate towards the destination point.
        var deltaTime = (float) delta;
        var destination = aStarPath[pathIndex];
        var nextTransform = Transform.LookingAt(destination, Vector3.Up);
        GlobalTransform = GlobalTransform.InterpolateWith(nextTransform, TurnSpeed * deltaTime);
        Velocity = -Basis.Z * Speed * deltaTime;
        MoveAndSlide();
    }

    private void CreateRandomPath()
    {
        var closestVoxel = svo.FindClosestEmptyLeaf(GlobalPosition);
        int randomLeafID;
        do
        {
            randomLeafID = GD.RandRange(0, svo.EmptyLeavesCount - 1);
            aStarPath = svo.CreatePath(closestVoxel.Id, randomLeafID);

        } while (closestVoxel.Id == randomLeafID && aStarPath.Length == 0);
        pathIndex = 0;
    }

    private void CreateVoxelPathMesh()
    {
        pathInstance ??= new MeshInstance3D();
        pathInstance.Mesh?.Free();
        
        var mesh = new ImmediateMesh();
        pathInstance.Mesh = mesh;
        
        if (!pathInstance.IsInsideTree()) 
            GetTree().Root.AddChild(pathInstance);
        
        if (pathMaterial is null)
        {
            pathMaterial = new StandardMaterial3D();
            pathMaterial.AlbedoColor = Colors.Cyan;
            pathMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded; // Makes it visible without lights
        }
        
        mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip, pathMaterial);
	
        // Create a Curve3D to handle the smoothing (Bézier math)
        var curve = new Curve3D();
        foreach (var voxelPosition in aStarPath)
            curve.AddPoint(voxelPosition);
        
        // Bake the curve into many small segments for a smooth look
        var bakedPoints = curve.GetBakedPoints();
	
        foreach (var point in bakedPoints)
            mesh.SurfaceAddVertex(point);
		
        mesh.SurfaceEnd();
    }

    private void DrawAStarPath()
    {
        if (svo is null || aStarPath.Length == 0) return;
    
        DebugDraw3D.DrawSphere(aStarPath[0], 0.7f, Colors.Blue);
        DebugDraw3D.DrawSphere(aStarPath[^1], 0.7f, Colors.Red);
    
        for (int i = 0; i < aStarPath.Length; i++)
        {
            DebugDraw3D.DrawSphere(aStarPath[i], 0.5f, i == pathIndex ? Colors.Gold : Colors.Green);

            if (i == aStarPath.Length - 1) continue;
            
            var start = aStarPath[i];
            var end = aStarPath[i + 1];
            DebugDraw3D.DrawLine(start, end, Colors.Green);
        }
    }

    private void DrawAStarCurvePath()
    {
        if (svo is null || aStarPath.Length == 0) return;
    }
    
}
