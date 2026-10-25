extends CanvasLayer

@onready var connect_label: Label = $ConnectPanel/IPLabel
@onready var lobby_label: Label = $Lobby/IPLabel
@onready var join_ip: LineEdit = $ConnectPanel/IPEdit
@onready var lobby: Control = $Lobby

var local_ipv4: String = "Unavailable"


func _ready() -> void:
	local_ipv4 = get_local_ipv4()

	lobby.visibility_changed.connect(update_labels)
	NetworkManager.connected.connect(update_labels)

	update_labels()


func get_local_ipv4() -> String:
	for address in IP.get_local_addresses():

		if address.begins_with("192.168.") or address.begins_with("10."):
			return address

		if address.begins_with("172."):
			var parts := address.split(".")

			if parts.size() == 4:
				var second := int(parts[1])

				if second >= 16 and second <= 31:
					return address

	return "Unavailable"


func update_labels() -> void:
	# Host/Join screen
	connect_label.text = "IPv4: %s" % local_ipv4

	# Clients see the host address they connected to
	if multiplayer.multiplayer_peer is ENetMultiplayerPeer \
	and not multiplayer.is_server():

		lobby_label.text = "Host IPv4: %s" % join_ip.text.strip_edges()

	# Host sees their own IPv4
	else:
		lobby_label.text = "IPv4: %s" % local_ipv4
