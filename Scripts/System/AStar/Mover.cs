namespace RescueDrone;

using System.Diagnostics;
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
    private Graph graph;

    public override void _EnterTree()
    {
        SetPhysicsProcess(false);
    }

    public override void _Ready()
    {
        graph = OctreeGenerator.Waypoints;
        currentNode = GetClosestNode(GlobalPosition);
        if (currentNode is null) GD.PrintErr("Mover has no starting current node.");
        GetRandomDestination();
        
        SetPhysicsProcess(true);
    }

    public override void _Process(double delta)
    {
        if (graph is null || graph.GetPathLength() == 0) return;

        DebugDraw3D.DrawSphere(graph.GetPathNode(0).Bounds.GetCenter(), 0.7f, Colors.Red);
        DebugDraw3D.DrawSphere(graph.GetPathNode(graph.GetPathLength() - 1).Bounds.GetCenter(), 0.7f, Colors.Blue);

        for (int i = 0; i < graph.GetPathLength(); i++)
        {
            DebugDraw3D.DrawSphere(graph.GetPathNode(i).Bounds.GetCenter(), 0.5f,
                i == currentWaypoint ? Colors.Gold : Colors.Green);

            if (i < graph.GetPathLength() - 1)
            {
                var start = graph.GetPathNode(i).Bounds.GetCenter();
                var end = graph.GetPathNode(i + 1).Bounds.GetCenter();
                DebugDraw3D.DrawLine(start, end, Colors.Green);
            }
        }
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
    
    // private OctreeNode GetClosestNode(Vector3 position)
    // {
    //     OctreeNode closestNode = null;
    //     var closestDistanceSqr = Mathf.Inf;
    //
    //     foreach (var nodePair in graph.Nodes)
    //     {
    //         var node = nodePair.Key;
    //         var distanceSqr = (node.Bounds.GetCenter() - position).LengthSquared();
    //         if (distanceSqr >= closestDistanceSqr) continue;
    //         
    //         closestDistanceSqr = distanceSqr;
    //         closestNode = node;
    //     }
    //     return closestNode;
    // }

    private void GetRandomDestination()
    {
        OctreeNode destinationNode;
        do
        {
            var rand = GD.RandRange(0, graph.Nodes.Count - 1);
            GD.Print(graph.Nodes.Count + " | " + rand);
            destinationNode = graph.Nodes.ElementAt(rand).Key;
        } while (!graph.AStar(currentNode, destinationNode));
        currentWaypoint = 0;
    }
    
}
