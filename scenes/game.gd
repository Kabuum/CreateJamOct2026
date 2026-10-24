extends Node3D

@export var player_scene: PackedScene
@onready var players := $Players
@onready var menu: CanvasLayer = $Menu
@onready var ip_edit: LineEdit = $Menu/IPEdit

func _ready() -> void:
	$Menu/HostButton.pressed.connect(_on_host)
	$Menu/JoinButton.pressed.connect(_on_join)
	NetworkManager.connected.connect(func(): menu.hide())
	NetworkManager.failed.connect(func(): print("Connection Failed"))
	
func _on_host() -> void:
	NetworkManager.host()
	multiplayer.peer_connected.connect(_spawn_player)
	multiplayer.peer_disconnected.connect(_remove_player)
	_spawn_player(1)

func _on_join() -> void:
	NetworkManager.join(ip_edit.text.strip_edges())
	
func _spawn_player(id: int) -> void:
	var p := player_scene.instantiate()
	p.name = str(id)
	p.position = Vector3(id % 4 * 3.0, 1.0, 0.0)
	players.add_child(p, true)

func _remove_player(id: int) -> void:
	var p := players.get_node_or_null(str(id))
	if p :
		p.queue_free()
