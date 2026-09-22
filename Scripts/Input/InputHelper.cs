namespace RescueDrone;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class InputHelper : Node
{
    // string device, int device_index
    public event Action<string, int> DeviceChanged;
    // string action, InputEvent input
    public event Action<string, InputEvent> KeyboardInputChanged;
    // string action, InputEvent input
    public event Action<string, InputEvent> JoypadInputChanged;
    // long deviceIndex, bool isConnected
    public event Action<long, bool> JoypadChanged;
    
    public const string DEVICE_KEYBOARD = "Keyboard";
    public const string DEVICE_XBOX_CONTROLLER = "xbox";
    public const string DEVICE_SWITCH_CONTROLLER = "switch";
    public const string DEVICE_PLAYSTATION_CONTROLLER = "playstation";
    public const string DEVICE_STEAMDECK_CONTROLLER = "steamdeck";
    public const string DEVICE_GENERIC = "generic";
    
    public int MouseMotionThreshold { get; private set; } = 100;
    public float Deadzone { get; private set; } = 0.2f;
    
    public string LastKnownJoypadDevice { get; private set; }
    public int LastKnownJoypadIndex { get; private set; }

    public string Device { get; private set; } = GuessDeviceName();
    public int DeviceIndex { get; private set; } = HasJoypad() ? 0 : -1;

    private int deviceLastChangedAt;

    // TODO: The way Singletons are made in C# is different!!
    
    public override void _Ready()
    {
        if (!Engine.HasSingleton("InputHelper"))
            Engine.RegisterSingleton("InputHelper", this);

        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    public override void _ExitTree()
    {
        Input.JoyConnectionChanged -= OnJoyConnectionChanged;
    }

    private void OnJoyConnectionChanged(long deviceID, bool connected) => JoypadChanged?.Invoke(deviceID, connected);

    public override void _Input(InputEvent @event)
    {
        var nextDevice = Device;
        var nextDeviceIndex = DeviceIndex;
        
        // Did we just press a key on the keyboard or move the mouse?
        if (@event is InputEventKey ||
            @event is InputEventMouseButton ||
            (@event is InputEventMouseMotion mouseMotion && mouseMotion.Relative.LengthSquared() > MouseMotionThreshold))
        {
            nextDevice = DEVICE_KEYBOARD;
            nextDeviceIndex = -1;
        }
        
        // Did we just use a joypad?
        else if (@event is InputEventJoypadButton ||
                 (@event is InputEventJoypadMotion joypadMotion && Mathf.Abs(joypadMotion.AxisValue) > Deadzone))
        {
            nextDevice = GetSimplifiedDeviceName(GetJoyName(@event.Device));
            LastKnownJoypadDevice = nextDevice;
            nextDeviceIndex = @event.Device;
            LastKnownJoypadIndex = nextDeviceIndex;
        }
        
        // Debounce changes for 1 second because some joypads register twice in Windows for some reason
        var notChangedInLastSecond = Engine.GetFramesDrawn() - deviceLastChangedAt > Engine.GetFramesPerSecond();
        if ((nextDevice != Device || nextDeviceIndex != DeviceIndex) && notChangedInLastSecond)
        {
            deviceLastChangedAt = Engine.GetFramesDrawn();
            
            Device = nextDevice;
            DeviceIndex = nextDeviceIndex;
            DeviceChanged?.Invoke(Device, DeviceIndex);
        }
    }

    /// <summary>
    /// Get the name of a joypad.
    /// </summary>
    /// <param name="atDeviceIndex"></param>
    /// <returns></returns>
    private static string GetJoyName(int atDeviceIndex)
    {
        var joyName = Input.GetJoyName(atDeviceIndex);
        var joyInfo = Input.GetJoyInfo(atDeviceIndex);
        var isEmpty = string.IsNullOrEmpty(joyName);
        
        if (isEmpty && joyInfo.Count > 0 && joyInfo.Keys.ToList()[0].As<string>().Contains("xinput"))
            return "XInput";
        
        return joyName;
    }

    /// <summary>
    /// Get the device name from an event.
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    private static string GetDeviceFromEvent(InputEvent @event)
    {
        return @event switch
        {
            InputEventKey or InputEventMouseButton or InputEventMouseMotion => DEVICE_KEYBOARD,
            InputEventJoypadButton or InputEventJoypadMotion => GetSimplifiedDeviceName(GetJoyName(@event.Device)),
            _ => DEVICE_GENERIC
        };
    }

    /// <summary>
    /// Get the device index from an event.
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    private static int GetDeviceIndexFromEvent(InputEvent @event)
    {
        if (@event is InputEventJoypadButton or InputEventJoypadMotion)
            return @event.Device;
        
        return -1;
    }
    
    /// <summary>
    /// Convert a Godot device identifier to a simplified string.
    /// </summary>
    /// <param name="rawName"></param>
    /// <returns></returns>
    private static string GetSimplifiedDeviceName(string rawName)
    {
        rawName = rawName.ToLower();
        var keywords = new Dictionary<string, string[]>
        {
            { DEVICE_XBOX_CONTROLLER, ["XBox", "XInput"] },
            { DEVICE_PLAYSTATION_CONTROLLER, ["Sony", "PS3", "PS4", "PS5", "DUALSHOCK 4", "DualSense", "Nacon Revolution Unlimited Pro Controller"] },
            { DEVICE_STEAMDECK_CONTROLLER, ["Steam"] },
            { DEVICE_SWITCH_CONTROLLER, ["Switch", "Joy-Con", "PowerA Core Controller"] },
        };

        foreach (var deviceKey in keywords.Keys)
        {
            foreach (var keyword in keywords[deviceKey])
            {
                if (rawName.Contains(keyword.ToLower()))
                    return deviceKey;
            }
        }

        return DEVICE_GENERIC;
    }

    /// <summary>
    /// Check if there is a connected joypad.
    /// </summary>
    /// <returns></returns>
    private static bool HasJoypad() => Input.GetConnectedJoypads().Count > 0;
    
    /// <summary>
    /// Guess the initial input device.
    /// </summary>
    /// <returns></returns>
    private static string GuessDeviceName()
    {
        return HasJoypad() ? GetSimplifiedDeviceName(GetJoyName(0)) : DEVICE_KEYBOARD;
    }
}
