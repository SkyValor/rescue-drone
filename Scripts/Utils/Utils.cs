namespace RescueDrone;

using System;
using System.Linq;
using System.Text;
using Godot;

public static class Utils
{
    public static T GetChildNode<T>(Node fromNode) where T : Node
    {
        var type = typeof(T);
        var children = fromNode.FindChildren(pattern: "*", type: type.Name, recursive: false);
        if (children.Count == 0) return null;

        var foundNode = children.FirstOrDefault(item => item.GetType() == type);
        return (T) foundNode;
    }

    public static string ToSnakeCase(this string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        
        if (text.Length < 2) return text.ToLowerInvariant();

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(text[0]));
        for (int i = 1; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
    
}
