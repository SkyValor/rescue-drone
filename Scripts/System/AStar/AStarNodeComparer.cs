namespace RescueDrone;

using System.Collections.Generic;

public partial class AStarGraph
{
    public class AStarNodeComparer : IComparer<AStarNode>
    {
        public int Compare(AStarNode x, AStarNode y)
        {
            if (x is null || y is null) return 0;
            
            int compare = x.F.CompareTo(y.F);
            return compare == 0 ? x.Id.CompareTo(y.Id) : compare;
        }
    }
}
