@tool
extends Node3D

@export var angle_degrees: float = 45.0:
	set(value):
		angle_degrees = value
		queue_redraw_cone()

@export var max_range: float = 10.0:
	set(value):
		max_range = value
		queue_redraw_cone()

@export var segments: int = 16
@export var line_color: Color = Color(1, 0.3, 0.3, 0.8)

func _ready() -> void:
	queue_redraw_cone()

func queue_redraw_cone() -> void:
	# Clean up old lines before drawing new ones
	for child in get_children():
		if child is MeshInstance3D:
			child.queue_free()
			
	# Generate the wireframe structure
	var immediate_mesh: ImmediateMesh = ImmediateMesh.new()
	var material: ORMMaterial3D = ORMMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = line_color
	
	immediate_mesh.surface_begin(Mesh.PRIMITIVE_LINES, material)
	
	var rad_angle: float = deg_to_rad(angle_degrees / 2.0)
	var base_radius: float = max_range * tan(rad_angle)
	var prev_point: Vector3 = Vector3.ZERO
	
	# Loop around the base circle
	for i in range(segments + 1):
		var theta = (PI * 2.0 / segments) * i
		# Facing down forward Z axis
		var current_point: Vector3 = Vector3(cos(theta) * base_radius, sin(theta) * base_radius, -max_range)
		
		# Draw the line forming the base circumference
		if i > 0:
			immediate_mesh.surface_add_vertex(prev_point)
			immediate_mesh.surface_add_vertex(current_point)
			
		# Draw the spine lines radiating from the apex (origin) to the base perimeter
		if i % (segments / 4) == 0: 
			immediate_mesh.surface_add_vertex(Vector3.ZERO)
			immediate_mesh.surface_add_vertex(current_point)
			
		prev_point = current_point
		
	immediate_mesh.surface_end()
	
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()
	mesh_instance.mesh = immediate_mesh
	add_child(mesh_instance)
