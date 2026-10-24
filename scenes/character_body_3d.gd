extends CharacterBody3D


@export_group("Speed")
@export var max_speed := 20.0
@export var acceleration := 6.0

@export_group("Steering")
@export var turn_rate := 3.0
@export var steer_smoothing := 8.0
@export var full_steer_speed := 6.0

@export_group("Grip")
@export var grip := 3.0
@export var stun_grip := 0.8

@export_group("Wall hit")
@export var wall_bounce := 0.5
@export var wall_speed_loss := 0.6
@export var min_impact := 4.0
@export var hit_stun_time := 0.4
@export var deflect_amount := 0.6

@export_group("Slow effect")
@export var slow_multiplier := 0.4
@export var slow_duration := 2.0


var steer := 0.0
var speed := 0.0
var stun_timer := 0.0
var slow_timer := 0.0
var slow_mult := 1.0

func _physics_process(delta: float) -> void:
	stun_timer = maxf(stun_timer - delta, 0.0)
	slow_timer = maxf(slow_timer - delta, 0.0)
	var stunned := stun_timer > 0.0

	var input := 0.0  if stunned else Input.get_axis("ui_left","ui_right")
	steer = lerp(steer, input, 1.0 -exp(-steer_smoothing * delta))
	
	var current_max := max_speed * (slow_mult if slow_timer > 0.0 else 1.0)
	speed = move_toward(speed, current_max, acceleration * delta)
	
	var horizontal := Vector3(velocity.x, 0.0, velocity.z)
	var steer_strength := clampf(horizontal.length() / full_steer_speed, 0.0, 1.0)
	rotate_y(-steer * turn_rate * steer_strength * delta)
	
	var forward := -global_transform.basis.z
	var current_grip := stun_grip if stunned else grip
	var target := forward * speed
	horizontal = horizontal.lerp(target, 1.0 - exp(-current_grip * delta))
	velocity.x = horizontal.x
	velocity.z = horizontal.z
	
	if not is_on_floor():
		velocity += get_gravity() * delta
	else:
		velocity.y = 0.0
	
	var pre_velocity := velocity
	move_and_slide()
	
	_handle_collision(pre_velocity)
		
	#make cam follow player
	$Camera_Controller.global_position = lerp($Camera_Controller.global_position, global_position, 1.0 - exp(-10.0 * delta))
	$Camera_Controller.global_rotation.y = lerp_angle($Camera_Controller.global_rotation.y, global_rotation.y, 1.0 - exp(-5.0 * delta))

func _handle_collision(pre_velocity: Vector3) -> void:
	if stun_timer > 0.0:
		return
	for i in get_slide_collision_count():
		var col := get_slide_collision(i)
		var normal := col.get_normal()
		if normal.y > 0.5:
			continue
		
		var impact := maxf(-pre_velocity.dot(normal), 0.0)
		if impact < min_impact:
			continue
		
		var other := col.get_collider()
		var is_player: bool = other.is_in_group("Player")
		_bounce(normal, pre_velocity, impact, 0.5 if is_player else 1.0)
		break

func _bounce(normal: Vector3, pre_velocity: Vector3, impact: float, strength: float) -> void:
	var bounced := pre_velocity.bounce(normal)
	bounced.y = 0.0
	velocity = bounced * wall_bounce * strength
	
	speed *= 1.0 - wall_speed_loss * strength * clampf(impact /max_speed, 0.0, 1.0)
	
	if bounced.length() > 0.1:
		var target_yaw := atan2(-bounced.x, -bounced.z)
		rotation.y = lerp_angle(rotation.y, target_yaw, deflect_amount)
	stun_timer = hit_stun_time
	
func apply_slow(duration := slow_duration, mult := slow_multiplier) -> void:
	slow_timer = duration
	slow_mult = mult
	speed = minf(speed, max_speed * mult)

func apply_knockback(impulse: Vector3, stun := hit_stun_time) -> void:
	impulse.y = 0.0
	velocity += impulse
	stun_timer = stun
