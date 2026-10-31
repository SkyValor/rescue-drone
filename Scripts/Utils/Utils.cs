namespace RescueDrone;

using System;
using System.Linq;
using System.Text;
using Godot;

public static class Utils
{
    public static uint Get3DLayerValueByName(string layerName)
    {
        for (int i = 1; i < 33; i++)
        {
            var settingName = $"layer_names/3d_physics/layer_{i}";
            var configuredName = ProjectSettings.GetSetting(settingName, "");
            if (configuredName.VariantType is Variant.Type.String && configuredName.AsString().Equals(layerName))
                return (uint) 1 << (i - 1); // Returns the bitmask value (e.g., 1, 2, 4, 8...)
        }

        return 0;
    }
    
    public static int Get3DLayerIndexByName(string layerName)
    {
        for (int i = 1; i < 33; i++)
        {
            var settingName = $"layer_names/3d_physics/layer_{i}";
            var configuredName = ProjectSettings.GetSetting(settingName, "");
            if (configuredName.VariantType is Variant.Type.String && configuredName.AsString().Equals(layerName))
                return i;
        }

        return -1;
    }
    
    // if (targetNumber == startNumber) --> 0.0
    // if (targetNumber == endNumber) --> 1.0
    public static float PercentageCalculate(float startNumber, float endNumber, float targetNumber)
    {
        var totalRange = endNumber - startNumber;
        var distanceFromStartToTarget = targetNumber - startNumber;
        return distanceFromStartToTarget / totalRange;
    }
    
    public static bool IsVectorApprox(Vector3 a, Vector3 b, float tolerance)
    {
        return 
            Mathf.Abs(a.X - b.X) < tolerance &&
            Mathf.Abs(a.Y - b.Y) < tolerance &&
            Mathf.Abs(a.Z - b.Z) < tolerance;
    }

    public static bool IsFloatApprox(float a, float b, float tolerance) => Math.Abs(a - b) < tolerance;
    
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
