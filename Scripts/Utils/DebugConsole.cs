namespace RescueDrone;

using System;
using System.Globalization;
using Godot;

public class DebugConsole
{
    public static void Print(string message, bool includeTimestamp = true)
    {
        var debuglog = "";
        if (includeTimestamp)
            debuglog += $"[{DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}] ";
        
        GD.Print(debuglog + message);
    }
}
