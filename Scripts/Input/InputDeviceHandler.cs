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
    
    /// <summary>
    /// The currently being used input type. This represents the device(s) that the user will use
    /// in order to interact with the game.
    /// </summary>
    public IAutoValue<InputType> CurrentInputType => inputType;
    private readonly AutoValue<InputType> inputType = new(InputType.KeyboardAndMouse);

    /// <summary>
    /// The device scheme that represents the current input type. This is reactive and will update based on
    /// user settings and what device most recently passed input.
    /// </summary>
    public IAutoValue<InputDeviceScheme> CurrentDeviceScheme => deviceScheme;
    private readonly AutoValue<InputDeviceScheme> deviceScheme = new(InputDeviceScheme.KeyboardAndMouse);

    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
    
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
        GD.Print("InputDeviceHandler setting initial input device.");
        var userSettings = GameRepo.UserSettings.Value;
        var preferredDevice = userSettings.PreferredInputDevice;

        switch (preferredDevice)
        {
            case InputType.KeyboardAndMouse:
                GD.Print("=> Setting current device to keyboard and mouse...");
                inputType.Value = InputType.KeyboardAndMouse;
                deviceScheme.Value = InputDeviceScheme.KeyboardAndMouse;
                break;
            case InputType.KeyboardOnly:
                GD.Print("=> Setting current device to keyboard only...");
                inputType.Value = InputType.KeyboardOnly;
                deviceScheme.Value = InputDeviceScheme.Keyboard;
                break;
            case InputType.Joypad:
                var connectedJoypads = Input.GetConnectedJoypads();
                if (connectedJoypads.Count == 0)
                {
                    GD.Print("=> No joypads connected. Setting current device to keyboard and mouse...");
                    inputType.Value = InputType.KeyboardAndMouse;
                    deviceScheme.Value =  InputDeviceScheme.KeyboardAndMouse;
                }
                else
                {
                    GD.Print("=> Setting current device to joypad...");
                    inputType.Value = InputType.Joypad;
                    deviceScheme.Value = userSettings.PreferredInputDeviceScheme;
                }
                break;
            default:
                throw new NotImplementedException($"Input type \"{preferredDevice}\" has no implementation.");
        }
    }

    private void OnDeviceChanged(string device, int deviceIndex)
    {
        // When we are using joypad input type, an input from a different controller should change
        // the current scheme.
        if (CurrentInputType.Value.IsKeyboardInclusive()) return;
        deviceScheme.Value = DeviceSchemeFromName(device);
    }

    private static InputType DeviceTypeFromName(string deviceName)
    {
        return deviceName switch
        {
            InputHelper.DEVICE_KEYBOARD => InputType.KeyboardAndMouse,
            _                           => InputType.Joypad
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
            inputType.Dispose();
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
