namespace RescueDrone;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.Sync.Primitives;
using Godot;
using NathanHoad;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class InputDeviceHandler : Node, IDisposable
{
    public override void _Notification(int what) => this.Notify(what);
    
    public IAutoValue<InputDeviceType> CurrentDeviceType => deviceType;
    private readonly AutoValue<InputDeviceType> deviceType = new(InputDeviceType.Computer);

    public IAutoValue<InputDeviceScheme> CurrentDeviceScheme => deviceScheme;
    private readonly AutoValue<InputDeviceScheme> deviceScheme = new(InputDeviceScheme.KeyboardAndMouse);

    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
    
    [Export] public InputComponent CurrentInputComponent { get; private set; }
    
    private bool disposingValue;

    public void OnReady()
    {
        GD.Print("Guessed device: " + InputHelper.GuessDeviceName());
        InputHelper.DeviceChanged += OnDeviceChanged;
    }

    public void OnExitTree()
    {
        InputHelper.DeviceChanged -= OnDeviceChanged;
    }

    public void OnResolved()
    {
        // Get the user settings and check which can be the initial input device
        var userSettings = GameRepo.UserSettings.Value;
        var preferredDevice = userSettings.PreferredInputDevice;
        if (preferredDevice is InputDeviceType.Computer)
        {
            SetCurrentDeviceAsKeyboard();
            return;
        }
        
        var connectedJoypads = Input.GetConnectedJoypads();
        if (connectedJoypads.Count == 0)
        {
            SetCurrentDeviceAsKeyboard();
            return;
        }
        
        var preferredScheme = userSettings.PreferredInputDeviceScheme;
        foreach (var joypadID in connectedJoypads)
        {
            var joypadName = Input.GetJoyName(joypadID);
            var deviceName = InputHelper.GetSimplifiedDeviceName(joypadName);
            var scheme = DeviceSchemeFromName(deviceName);
            
            if (scheme != preferredScheme) continue;
            
            deviceScheme.Value = scheme;
            break;
        }
    }

    private void SetCurrentDeviceAsKeyboard()
    {
        var userSettings = GameRepo.UserSettings.Value;
        
        deviceType.Value = InputDeviceType.Computer;
        deviceScheme.Value = userSettings.RotateCameraWithMouse
            ? InputDeviceScheme.KeyboardAndMouse
            : InputDeviceScheme.Keyboard;
    }

    private void OnDeviceChanged(string device, int deviceIndex)
    {
        GD.Print("Device changed: " + device + ", of index: " + deviceIndex);

        deviceType.Value = DeviceTypeFromName(device);
        if (deviceType.Value is not InputDeviceType.Computer)
            return;
        
        var userSettings = GameRepo.UserSettings.Value;
        deviceScheme.Value = userSettings.RotateCameraWithMouse 
            ? InputDeviceScheme.KeyboardAndMouse 
            : InputDeviceScheme.Keyboard;
    }

    private static InputDeviceType DeviceTypeFromName(string deviceName)
    {
        return deviceName switch
        {
            InputHelper.DEVICE_KEYBOARD => InputDeviceType.Computer,
            _                           => InputDeviceType.Joypad
        };
    }
    
    private static InputDeviceScheme DeviceSchemeFromName(string deviceName)
    {
        return deviceName switch
        {
            InputHelper.DEVICE_KEYBOARD               => InputDeviceScheme.Keyboard,
            InputHelper.DEVICE_PLAYSTATION_CONTROLLER => InputDeviceScheme.Playstation,
            InputHelper.DEVICE_STEAMDECK_CONTROLLER   => InputDeviceScheme.SteamController,
            InputHelper.DEVICE_SWITCH_CONTROLLER      => InputDeviceScheme.NintendoSwitch,
            InputHelper.DEVICE_XBOX_CONTROLLER        => InputDeviceScheme.Xbox,
            _                                         => InputDeviceScheme.GenericJoypad
        };
    }
    
    #region Internals
    private new void Dispose(bool disposing)
    {
        if (disposingValue) return;
        if (disposing)
        {
            deviceType.Dispose();
            deviceScheme.Dispose();
        }
        disposingValue = true;
    }
    
    public new void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
    
}
