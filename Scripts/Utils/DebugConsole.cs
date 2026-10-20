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

    public static void PrintError(string message, bool includeTimestamp = true)
    {
        var debuglog = "";
        if (includeTimestamp)
            debuglog += $"[{DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}] ";

        debuglog += $"[color=red]{message}[/color]";
        GD.PrintRich(debuglog);
    }

    public static void PrintWarning(string message, bool includeTimestamp = true)
    {
        var debuglog = "";
        if (includeTimestamp)
            debuglog += $"[{DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}] ";
        debuglog += $"[color=yellow]{message}[/color]";
        GD.Print(debuglog);
    }
    
}
