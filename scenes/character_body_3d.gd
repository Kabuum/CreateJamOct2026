extends CharacterBody3D


@export_group("Speed")
@export var max_speed := 20.0
@export var acceleration := 6.0

@export_group("steering")
@export var turn_rate := 3.0
@export var steer_smoothing := 8.0
@export var full_steer_speed := 6.0

@export_group("Grip")
@export var grip := 4.0

var steer := 0.0
var speed := 0.0

func _physics_process(delta: float) -> void:
	var input = Input.get_axis("ui_left","ui_right")
	steer = lerp(steer, input, 1.0 -exp(-steer_smoothing * delta))
	
	speed = move_toward(speed, max_speed, acceleration * delta)
	
	var horizontal := Vector3(velocity.x, 0.0, velocity.z)
	var steer_strength := clampf(horizontal.length() / full_steer_speed, 0.0, 1.0)
	rotate_y(-steer * turn_rate * steer_strength * delta)
	
	var forward := -global_transform.basis.z
	var target := forward * speed
	horizontal = horizontal.lerp(target, 1.0 - exp(-grip * delta))
	velocity.x = horizontal.x
	velocity.z = horizontal.z
	
	if not is_on_floor():
		velocity += get_gravity() * delta
	else:
		velocity.y = 0.0
	
	move_and_slide()
	if get_slide_collision_count() > 0:
		speed = minf(speed, maxf(velocity.dot(forward), 0))
