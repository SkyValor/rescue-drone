namespace RescueDrone;

using System;
using Chickensoft.Sync.Primitives;
using Godot;

public partial class InputDeviceHandler : Node, IDisposable
{
    public IAutoValue<InputDeviceType> CurrentDeviceType => deviceType;
    private AutoValue<InputDeviceType> deviceType;

    public IAutoValue<InputDeviceScheme> CurrentDeviceScheme => deviceScheme;
    private AutoValue<InputDeviceScheme> deviceScheme;
    
    [Export] public InputComponent CurrentInputComponent { get; private set; }
    [Export] public UserSettings UserSettings { get; private set; }
    
    public int LastDeviceID { get; protected set; }
    public InputDeviceScheme LastDeviceScheme { get; protected set; }
    public long LastTimePressedInput { get; protected set; }

    private int currentDeviceID;
    private string currentJoyName;
    private bool disposingValue;
    
    public override void _Ready()
    {
        var joypads = Input.GetConnectedJoypads();
        foreach (var deviceID in joypads)
            PrintDeviceName(deviceID);

        Input.JoyConnectionChanged += OnConnectionChanged;
    }

    public override void _ExitTree()
    {
        Input.JoyConnectionChanged -= OnConnectionChanged;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        var deviceID = @event.GetDevice();
        var joyName = Input.GetJoyName(deviceID);
        
        if (!@event.IsPressed()) return;
        
        if (string.IsNullOrEmpty(joyName))
        {
            GD.Print("Input comes from PC/Mouse. Device ID: " + deviceID);
        }
        else
        {
            GD.Print("Input comes from " + Input.GetJoyName(deviceID) + ". Device ID: " + deviceID);
        }
    }

    private static void OnConnectionChanged(long deviceID, bool connected)
    {
        if (connected) GD.Print("Controller connected: " + GetJoyName((int) deviceID));
        else GD.Print("Controller disconnected: " + GetJoyName((int) deviceID));
    }

    private static string GetJoyName(int deviceID) => Input.GetJoyName(deviceID);

    private static void PrintDeviceName(int deviceID)
    {
        var joyName = GetJoyName(deviceID);
        GD.Print(joyName);
        if (joyName.Contains("nintendo switch 2"))
        {
            GD.Print("Found Nintendo Switch 2 controller on device: " + deviceID);
        }
        else if (joyName.Contains("nintendo switch"))
        {
            GD.Print("Found Nintendo Switch controller on device: " + deviceID);
        }
        else if (joyName.Contains("xbox"))
        {
            GD.Print("Found Xbox controller on device: " + deviceID);
        }
        else if (joyName.Contains("playstation") || joyName.GetBaseName().Contains("ps4") || joyName.Contains("dualshock"))
        {
            GD.Print("Found Playstation controller on device: " + deviceID);
        }
        else
        {
            GD.Print("No external device detected. Defaulting to Keyboard and Mouse.");
        }
    }
    
    private static InputDeviceScheme DeviceSchemeFromName(string deviceName)
    {
        if (deviceName.Contains("steam")) return InputDeviceScheme.SteamController;
        if (deviceName.Contains("nintendo switch 2")) return InputDeviceScheme.NintendoSwitch2;
        if (deviceName.Contains("nintendo switch")) return  InputDeviceScheme.NintendoSwitch;
        if (deviceName.Contains("xbox")) return InputDeviceScheme.Xbox;
        if (deviceName.Contains("playstation") || deviceName.Contains("ps4") || deviceName.Contains("dualshock")) 
            return InputDeviceScheme.Playstation;
        
        return  InputDeviceScheme.KeyboardAndMouse;
    }

    #region Internals
    private void Dispose(bool disposing)
    {
        if (disposingValue) return;
        if (disposing)
        {
            deviceType.Dispose();
            deviceScheme.Dispose();
        }
        disposingValue = true;
    }
    
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
    
}
