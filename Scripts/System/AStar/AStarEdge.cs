namespace RescueDrone;

public class AStarEdge(AStarNode a, AStarNode b)
{
    public readonly AStarNode A = a;
    public readonly AStarNode B = b;

    public override bool Equals(object obj)
    {
        return obj is AStarEdge other && ((A == other.A && B == other.B) || (A == other.B && B == other.A));
    }

    public override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode();
    
}
