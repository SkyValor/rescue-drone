@tool
extends Node3D

# Export variables update the shape live in the editor
@export_range(1.0, 50.0, 0.5) var radius: float = 5.0:
	set(value):
		radius = value
		_update_aoe_mesh()

@export_range(10.0, 360.0, 1.0) var angle_degrees: float = 90.0:
	set(value):
		angle_degrees = value
		_update_aoe_mesh()

@export var aoe_color: Color = Color(1.0, 0.0, 0.0, 0.3): # Translucent Red
	set(value):
		aoe_color = value
		_update_aoe_mesh()

@onready var mesh_instance: MeshInstance3D = $MeshInstance3D

func _ready() -> void:
	_update_aoe_mesh()

func _update_aoe_mesh() -> void:
	# Safely catch early calls before the child node is initialized in the editor
	if not is_inside_tree() or not has_node("MeshInstance3D"):
		return
		
	var mi: MeshInstance3D = $MeshInstance3D
	var imm_mesh := ImmediateMesh.new()
	
	# Create a simple, unshaded, translucent material so it doesn't block lights
	var mat := StandardMaterial3D.new()
	mat.transparency = StandardMaterial3D.TRANSPARENCY_ALPHA
	mat.shading_mode = StandardMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = aoe_color
	
	mi.material_override = mat

	# Begin drawing triangles
	imm_mesh.surface_begin(Mesh.PRIMITIVE_TRIANGLES)
	
	var segments := 32
	var rad_angle := deg_to_rad(angle_degrees)
	var half_angle := rad_angle / 2.0
	
	var prev_vertex := Vector3(sin(-half_angle) * radius, 0, cos(-half_angle) * radius)
	
	for i in range(1, segments + 1):
		var current_pct := float(i) / segments
		# Interpolate angle across the cone arc
		var current_angle := -half_angle + (rad_angle * current_pct)
		var next_vertex := Vector3(sin(current_angle) * radius, 0, cos(current_angle) * radius)
		
		# Define a triangle face (Origin -> Previous point -> Next point)
		imm_mesh.surface_add_vertex(Vector3.ZERO)
		imm_mesh.surface_add_vertex(prev_vertex)
		imm_mesh.surface_add_vertex(next_vertex)
		
		prev_vertex = next_vertex
		
	imm_mesh.surface_end()
	mi.mesh = imm_mesh
