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
    [Node] private Node3D BulletSpawnPoint { get; set; }

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

    public void TryShooting(Vector3 targetPoint)
    {
        if (!weaponLoaded)
            return;

        GD.Print("Enemy drone shooting!");
        var bullet = BulletPrefab.Instantiate<EnemyBullet>();
        GetTree().Root.AddChild(bullet);
        bullet.LookAt(targetPoint, Vector3.Up);
        OnWeaponFired();
    }

    private void OnWeaponFired()
    {
        weaponLoaded = false;
        timer.Start();
    }
    
    private void OnCooldownFinished() => weaponLoaded = true;
    
}
