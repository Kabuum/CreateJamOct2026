extends Node3D

@export var speed := 3.0
@export var direction := Vector3(0, 0, -1)
@export var player_layer := 3

var moving := false
var round_manager: Node

func _ready() -> void:
	add_to_group("Horde")
	round_manager = get_tree().root.find_child("RoundManager", true, false)
	for child in get_children():
		if child is CSGBox3D:
			_add_kill_area(child)

func _add_kill_area(box: CSGBox3D) -> void:
	var area := Area3D.new()
	area.collision_layer = 0
	area.collision_mask = 1 << (player_layer - 1)

	var shape := CollisionShape3D.new()
	var box_shape := BoxShape3D.new()
	box_shape.size = box.size
	shape.shape = box_shape

	area.add_child(shape)
	box.add_child(area)
	area.body_entered.connect(_on_body_entered)

func start_moving() -> void:
	moving = true

func _physics_process(delta: float) -> void:
	if moving:
		global_position += direction.normalized() * speed * delta

func _on_body_entered(body: Node3D) -> void:
	if not multiplayer.is_server():
		return
	if not moving:
		return
	if not (body is CharacterBody3D) or not body.get_collision_layer_value(player_layer):
		return
	round_manager.call("PlayerCaught", body.get_multiplayer_authority())
