namespace RescueDrone;

using Godot;

public partial class SightSensorDebugVisual : Node3D
{
    private readonly Color DebugColor = new(1f, 1f, 0f, 0.02f);

    public float EndRadius { get; set; } = 10f;
    public float Range { get; set; } = 5f;
    
    private MeshInstance3D coneRenderer;
    
    public override void _Ready()
    {
        var mesh = new CylinderMesh();
        mesh.Material = new StandardMaterial3D
        {
            AlbedoColor = DebugColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        
        coneRenderer = new MeshInstance3D();
        coneRenderer.Mesh = mesh;
        AddChild(coneRenderer);
    }

    public void UpdateConeShape()
    {
        var coneShape = (CylinderMesh) coneRenderer.Mesh;
        coneShape.TopRadius = 0f;
        coneShape.BottomRadius = EndRadius;
        coneShape.Height = Range;
        coneRenderer.RotationDegrees = new Vector3(90f, 0f, 0f);
        coneRenderer.Position = coneRenderer.Position with { Z = -Range / 2f };
    }
    
}
