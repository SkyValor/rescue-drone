namespace RescueDrone;

using System.Linq;
using Godot;

public sealed class Utils
{
    public static T GetChildNode<T>(Node fromNode) where T : Node
    {
        var type = typeof(T);
        var children = fromNode.FindChildren(pattern: "*", type: type.Name, recursive: false);
        if (children.Count == 0) return null;

        var foundNode = children.FirstOrDefault(item => item.GetType() == type);
        return (T) foundNode;
    }
}
