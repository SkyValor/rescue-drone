namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class EnemyWeapon : Node3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] public PackedScene BulletPrefab { get; set; }
    [Export] public float WeaponCooldown { get; set; }

    private Timer timer;
    private bool weaponLoaded;

    public void OnEnterTree()
    {
        timer = GetChildOrNull<Timer>(0);
        if (timer is null)
        {
            timer = new Timer();
            AddChild(timer);
        }

        timer.WaitTime = WeaponCooldown;
        timer.Timeout += OnCooldownFinished;
    }

    public void OnExitTree()
    {
        if (timer is not null)
            timer.Timeout -= OnCooldownFinished;
    }

    public void TryShooting()
    {
        if (!weaponLoaded)
            return;
        
        // TODO: Instantiate the bullet and point it towards target
        
        OnWeaponFired();
    }

    private void OnWeaponFired()
    {
        weaponLoaded = false;
        timer.Start();
    }
    
    private void OnCooldownFinished() => weaponLoaded = true;
    
}
