class_name Lobby
extends Control

signal game_started(players: Dictionary)

@onready var name_edit: LineEdit = $VBoxContainer/NameEdit
@onready var skin_option: OptionButton = $VBoxContainer/SkinOption
@onready var player_list: ItemList = $VBoxContainer/PlayerList
@onready var start_button: Button = $VBoxContainer/StartButton
@onready var status_label: Label = $VBoxContainer/StatusLabel

var players: Dictionary = {}
var started := false

func _ready() -> void:
	name_edit.max_length = 16
	for i in Skins.NAMES.size():
		skin_option.add_item(Skins.NAMES[i], i)
	skin_option.item_selected.connect(func(_i): _send_profile())
	name_edit.text_changed.connect(func(_t): _send_profile())
	name_edit.focus_exited.connect(_send_profile)
	
	start_button.pressed.connect(_on_start_pressed)
	
func open() -> void:
	show()
	var is_host := multiplayer.is_server()
	start_button.visible = is_host
	status_label.text = "You are the host" if is_host else "Waiting for host to start..."
	if is_host:
		if not multiplayer.peer_connected.is_connected(_on_peer_connected):
			multiplayer.peer_connected.connect(_on_peer_connected)
		if not multiplayer.peer_disconnected.is_connected(_on_peer_disconnected):
			multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	_send_profile()

func _send_profile() -> void:
	if not visible or started:
		return
	var pname := name_edit.text
	var skin := skin_option.selected
	if multiplayer.is_server():
		_set_profile(1, pname, skin)
	else: 
		submit_profile.rpc_id(1, pname, skin)

@rpc("any_peer", "call_remote", "reliable")
func submit_profile(pname: String, skin: int) -> void:
	if not multiplayer.is_server() or started:
		return
	_set_profile(multiplayer.get_remote_sender_id(), pname, skin)
	
func _set_profile(id: int, pname: String, skin: int) -> void:
	pname = pname.strip_edges().left(16)
	if pname.is_empty():
		pname = "Horse"
	players[id] = {"name": pname, "skin": clampi(skin, 0, Skins.COLORS.size() - 1)}
	sync_players.rpc(players)
	
@rpc("authority", "call_local", "reliable")
func sync_players(data: Dictionary) -> void:
	players = data
	_refresh_list()

func _refresh_list() -> void:
	player_list.clear()
	for id in players:
		var p: Dictionary = players[id]
		var label: String = p.name + (" (host)" if id == 1 else "")
		var idx := player_list.add_item(label)
		player_list.set_item_custom_fg_color(idx, Skins.COLORS[p.skin])
		
func _on_peer_connected(id: int) -> void:
	if started:
		multiplayer.multiplayer_peer.disconnect_peer(id)

func _on_peer_disconnected(id: int) -> void:
	if players.erase(id):
		sync_players.rpc(players)

func _on_start_pressed() -> void:
	if not multiplayer.is_server() or started:
		return
	for id in multiplayer.get_peers():
		if not players.has(id):
			_set_profile(id, "", 0)
	begin.rpc()

@rpc("authority", "call_local", "reliable")
func begin() -> void:
	started = true
	hide()
	game_started.emit(players)

	if multiplayer.is_server():
		get_node("../../RoundManager").call("StartRound")
