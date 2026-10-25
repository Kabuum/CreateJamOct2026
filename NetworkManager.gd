extends Node

const PORT := 7777
const MAX_PLAYERS := 4

signal connected
signal failed

var returning_to_menu := false


func _ready() -> void:
	multiplayer.connected_to_server.connect(
		func(): connected.emit()
	)

	multiplayer.connection_failed.connect(
		func(): failed.emit()
	)

	multiplayer.server_disconnected.connect(
		_on_server_disconnected
	)


func reset_session() -> void:
	var old_peer: MultiplayerPeer = multiplayer.multiplayer_peer

	# Restore offline multiplayer
	multiplayer.multiplayer_peer = OfflineMultiplayerPeer.new()

	# Close the previous connection
	if old_peer is ENetMultiplayerPeer:
		old_peer.close()


func host() -> void:
	returning_to_menu = false

	reset_session()

	var peer := ENetMultiplayerPeer.new()
	var err := peer.create_server(PORT, MAX_PLAYERS)

	if err != OK:
		push_error(
			"Hosting failed: %s" % error_string(err)
		)
		failed.emit()
		return

	multiplayer.multiplayer_peer = peer

	print("Hosting started successfully!")

	connected.emit()


func join(ip: String) -> void:
	returning_to_menu = false

	reset_session()

	var peer := ENetMultiplayerPeer.new()
	var err := peer.create_client(ip, PORT)

	if err != OK:
		push_error(
			"Joining failed: %s" % error_string(err)
		)
		failed.emit()
		return

	multiplayer.multiplayer_peer = peer


# Request a return to the connection menu
func return_to_menu() -> void:
	if returning_to_menu:
		return

	returning_to_menu = true

	# Wait until the current signal/callback has finished
	call_deferred("_finish_return_to_menu")


func _finish_return_to_menu() -> void:
	if not is_inside_tree():
		return

	print("Returning to connection menu...")

	reset_session()

	get_tree().reload_current_scene()

	# Keep returning_to_menu true until host() or join()
	# so late disconnect signals cannot reload the scene again.


func _on_server_disconnected() -> void:
	return_to_menu()
