namespace RescueDrone;

public class GraphEdge(GraphNode a, GraphNode b)
{
    public readonly GraphNode A = a;
    public readonly GraphNode B = b;

    public override bool Equals(object obj)
    {
        return obj is GraphEdge other && ((A == other.A && B == other.B) || (A == other.B && B == other.A));
    }

    public override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode();
}
