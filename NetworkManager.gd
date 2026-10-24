extends Node

const PORT := 7777
const MAX_PLAYERS := 4

signal connected
signal failed


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	multiplayer.connected_to_server.connect(func(): connected.emit())
	multiplayer.connection_failed.connect(func(): failed.emit())
	multiplayer.server_disconnected.connect(_on_server_disconnected)

func host() -> void:
	var peer := ENetMultiplayerPeer.new()
	var err := peer.create_server(PORT, MAX_PLAYERS)
	if err != OK:
		failed.emit()
		return
	multiplayer.multiplayer_peer = peer
	connected.emit()

func join(ip: String) -> void:
	var peer := ENetMultiplayerPeer.new()
	var err := peer.create_client(ip, PORT)
	if err != OK:
		failed.emit()
		return
	multiplayer.multiplayer_peer = peer
	

func _on_server_disconnected() -> void:
	multiplayer.multiplayer_peer = null
	get_tree().reload_current_scene()
