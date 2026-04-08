namespace RescueDrone;

public class GraphEdge
{
    public readonly GraphNode A;
    public readonly GraphNode B;

    public GraphEdge(GraphNode a, GraphNode b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is GraphEdge other && ((A == other.A && B == other.B) || (A == other.B && B == other.A));
    }

    public override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode();
}