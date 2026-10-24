extends Node3D

@export var player_scene: PackedScene
@onready var players: Node3D = $Players
@onready var spawner: MultiplayerSpawner = $MultiplayerSpawner
@onready var connect_panel: Control = $Menu/ConnectPanel
@onready var ip_edit: LineEdit = $Menu/ConnectPanel/IPEdit
@onready var host_button: Button = $Menu/ConnectPanel/HostButton
@onready var join_button: Button = $Menu/ConnectPanel/JoinButton
@onready var lobby: Control = $Menu/Lobby

var lobby_open = false

func _ready() -> void:
	spawner.spawn_function = _spawn_player
	$Menu/ConnectPanel/HostButton.pressed.connect(_on_host)
	$Menu/ConnectPanel/JoinButton.pressed.connect(_on_join)
	lobby.game_started.connect(_on_game_started)
	
	NetworkManager.connected.connect(_open_lobby)
	NetworkManager.failed.connect(_on_connection_failed)
	multiplayer.server_disconnected.connect(_on_server_disconnected)
	
func _on_host() -> void:
	_set_buttons_disabled(true)
	NetworkManager.host()
	multiplayer.peer_disconnected.connect(_remove_player)
	_open_lobby()
	_spawn_player(1)
	
	$RoundManager.call("StartRound")

func _on_join() -> void:
	_set_buttons_disabled(true)
	NetworkManager.join(ip_edit.text.strip_edges())

func _set_buttons_disabled(value: bool) -> void:
	host_button.disabled = value
	join_button.disabled = value

func _open_lobby() -> void:
	if lobby_open:
		return
	lobby_open = true
	connect_panel.hide()
	lobby.open()

func _on_connection_failed() -> void:
	print("Connection Failed")
	_set_buttons_disabled(false)

func _on_server_disconnected() -> void:
	multiplayer.multiplayer_peer = null
	get_tree().reload_current_scene()

func _on_game_started(lobby_players: Dictionary) -> void:
	if not multiplayer.is_server():
		return
	var slot := 0
	for id in lobby_players:
		spawner.spawn({
			"id": id,
			"slot": slot,
			"name": lobby_players[id].name,
			"skin": lobby_players[id].skin
		})
		slot += 1

func _spawn_player(data: Dictionary) -> Node:
	var p := player_scene.instantiate() as PlayerCharacter
	p.name = str(data.id)
	p.position = Vector3(data.slot % 4 * 3.0, 1.0, 0.0)
	p.set_multiplayer_authority(data.id)
	p.setup(data.name, data.skin)
	return p

func _remove_player(id: int) -> void:
	var p := players.get_node_or_null(str(id))
	if p :
		p.queue_free()
