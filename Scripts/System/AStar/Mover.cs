namespace RescueDrone;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Mover : CharacterBody3D
{
    [Export] public float Speed { get; private set; } = 5f;
    [Export] public float Accuracy { get; private set; } = 1f;
    [Export] public float TurnSpeed { get; private set; } = 5f;
    [Export] public OctreeGenerator OctreeGenerator { get; private set; }
    
    private int currentWaypoint;
    private OctreeNode currentNode;
    private Vector3 destination;
    private AStarGraph graph;

    private MeshInstance3D pathInstance;

    public override void _EnterTree()
    {
        SetPhysicsProcess(false);
    }

    public override void _Ready()
    {
        graph = OctreeGenerator.Graph;
        currentNode = GetClosestNode(GlobalPosition);
        if (currentNode is null) GD.PrintErr("Mover has no starting current node.");
        GetRandomDestination();
        
        SetPhysicsProcess(true);
    }

    public override void _Process(double delta)
    {
        // DrawAStarPath();
        DrawAStarCurvePath();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (graph is null) return;

        if (graph.GetPathLength() == 0 || currentWaypoint >= graph.GetPathLength())
        {
            GetRandomDestination();
            return;
        }

        var distanceToDestination = graph.GetPathNode(currentWaypoint).Bounds.GetCenter().DistanceTo(GlobalPosition);
        if (distanceToDestination < Accuracy)
        {
            currentWaypoint++;
            GD.Print($"Waypoint {currentWaypoint} reached.");
        }

        if (currentWaypoint < graph.GetPathLength())
        {
            currentNode = graph.GetPathNode(currentWaypoint);
            destination = currentNode.Bounds.GetCenter();

            // Smoothly rotate towards the destination point.
            var deltaTime = (float) delta;
            var nextTransform = Transform.LookingAt(destination, Vector3.Up);
            GlobalTransform = GlobalTransform.InterpolateWith(nextTransform, TurnSpeed * deltaTime);
            Velocity = -Basis.Z * Speed * deltaTime;
            MoveAndSlide();
        }
        else
        {
            GetRandomDestination();
        }
    }

    private OctreeNode GetClosestNode(Vector3 position) => OctreeGenerator.Tree.FindClosestNode(position);

    private void GetRandomDestination()
    {
        OctreeNode destinationNode;
        do
        {
            var rand = GD.RandRange(0, graph.Nodes.Count - 1);
            destinationNode = graph.Nodes.ElementAt(rand).Key;
        } while (!graph.AStar(currentNode, destinationNode));
        currentWaypoint = 0;
        CallDeferred(MethodName.CreateCurvePath);
    }

    private void CreateCurvePath()
    {
        var points = new List<Vector3>();
        for (int i = 0; i < graph.GetPathLength(); i++)
        {
            points.Add(graph.GetPathNode(i).Bounds.GetCenter());
        }

        pathInstance = new MeshInstance3D();
        var mesh = new ImmediateMesh();
        pathInstance.Mesh = mesh;
        
        if (!pathInstance.IsInsideTree()) 
            GetTree().Root.AddChild(pathInstance);
        
        // Create a simple material
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = Colors.Cyan;
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded; // Makes it visible without lights
        
        mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip, mat);
	
        // Create a Curve3D to handle the smoothing (Bézier math)
        var curve = new Curve3D();
        foreach (var point in points)
            curve.AddPoint(point);
        
        // Bake the curve into many small segments for a smooth look
        var bakedPoints = curve.GetBakedPoints();
	
        foreach (var point in bakedPoints)
            mesh.SurfaceAddVertex(point);
		
        mesh.SurfaceEnd();
    }

    private void DrawAStarPath()
    {
        if (graph is null || graph.GetPathLength() == 0) return;
    
        DebugDraw3D.DrawSphere(graph.GetPathNode(0).Bounds.GetCenter(), 0.7f, Colors.Red);
        DebugDraw3D.DrawSphere(graph.GetPathNode(graph.GetPathLength() - 1).Bounds.GetCenter(), 0.7f, Colors.Blue);
    
        for (int i = 0; i < graph.GetPathLength(); i++)
        {
            DebugDraw3D.DrawSphere(graph.GetPathNode(i).Bounds.GetCenter(), 0.5f,
                i == currentWaypoint ? Colors.Gold : Colors.Green);

            if (i == graph.GetPathLength() - 1) continue;
            
            var start = graph.GetPathNode(i).Bounds.GetCenter();
            var end = graph.GetPathNode(i + 1).Bounds.GetCenter();
            DebugDraw3D.DrawLine(start, end, Colors.Green);
        }
    }

    private void DrawAStarCurvePath()
    {
        if (graph is null || graph.GetPathLength() == 0) return;
    }
    
}
