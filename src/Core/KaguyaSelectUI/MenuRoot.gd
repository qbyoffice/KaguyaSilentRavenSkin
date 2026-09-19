extends Control

signal option_selected(index: int, option_name: String)

@export var option_names: Array[String] = ["直播模式", "直播模式_猫", "NSFW", "NSFW_猫"]

@export_group("选项图片差分（每个数组填 4 张，顺序对应 option_names）")
@export var tex_normal_list:   Array[Texture2D] = []
@export var tex_hover_list:    Array[Texture2D] = []
@export var tex_pressed_list:  Array[Texture2D] = []
@export var tex_selected_list: Array[Texture2D] = []

@export_group("布局")
@export var option_width: float = 220.0
@export var option_height: float = 44.0
@export var option_gap: float = 6.0
@export var default_selected: int = 0

@export_group("动画")
@export var anim_duration: float = 0.22
@export var stagger_delay: float = 0.05

@export_group("存档")
@export var auto_save: bool = true

const SAVE_DIR    := "user://Kugaya.skin/KaguyaSilentRavenSkin"
const LAYOUT_FILE := "user://Kugaya.skin/KaguyaSilentRavenSkin/layout.json"
const MODE_FILE   := "user://Kugaya.skin/KaguyaSilentRavenSkin/kaguyaMode.json"

const MODE_VERSION := 2
const LAYOUT_VERSION := 1

@onready var drag_handle: Button = $DragHandle
@onready var settings_icon: TextureButton = $SettingsIcon
@onready var options_root: Control = $OptionsRoot

var _options: Array[OptionItem] = []
var _is_open := false
var _selected_index := -1
var _tween: Tween

var _dragging := false
var _drag_offset := Vector2.ZERO


func _read_json_safe(path: String) -> Dictionary:
	if not FileAccess.file_exists(path):
		return {}
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_warning("[KaguyaSilentRavenSkin]无法打开文件: %s" % path)
		return {}
	var content := file.get_as_text()
	file.close()

	if content.strip_edges() == "":
		return {}

	var json := JSON.new()
	var err := json.parse(content)
	if err != OK:
		push_warning("[KaguyaSilentRavenSkin]JSON 解析失败 (%s): %s" % [path, json.get_error_message()])
		return {}
	if not (json.data is Dictionary):
		push_warning("[KaguyaSilentRavenSkin]JSON 根节点不是字典: %s" % path)
		return {}
	return json.data

func _write_json_atomic(path: String, data: Dictionary) -> void:
	var tmp := path + ".tmp"
	var file := FileAccess.open(tmp, FileAccess.WRITE)
	if file == null:
		push_warning("[KaguyaSilentRavenSkin]无法写入临时文件: %s" % tmp)
		return
	file.store_string(JSON.stringify(data, "\t"))
	file.close()

	if FileAccess.file_exists(path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(path))

	var err := DirAccess.rename_absolute(
		ProjectSettings.globalize_path(tmp),
		ProjectSettings.globalize_path(path)
	)
	if err != OK:
		push_warning("[KaguyaSilentRavenSkin]重命名失败 (%d)，退回直接写入" % err)
		var f2 := FileAccess.open(path, FileAccess.WRITE)
		if f2 != null:
			f2.store_string(JSON.stringify(data, "\t"))
			f2.close()

func _ready() -> void:
	DirAccess.make_dir_recursive_absolute(SAVE_DIR)

	settings_icon.pressed.connect(_toggle)
	drag_handle.button_down.connect(_on_handle_down)
	drag_handle.button_up.connect(_on_handle_up)

	_build_options()
	_load_saved()
	_set_open(false, true)
	settings_icon.button_pressed = false

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST or what == NOTIFICATION_PREDELETE:
		_save_layout()
		_save_mode()

func _build_options() -> void:
	for i in option_names.size():
		var opt := OptionItem.new()
		opt.name = "Option%d" % i
		opt.position = Vector2(0, i * (option_height + option_gap))
		opt.option_pressed.connect(_on_option_pressed)
		options_root.add_child(opt)
		opt.setup(
			i, option_names[i], option_width, option_height,
			tex_normal_list[i], tex_hover_list[i],
			tex_pressed_list[i], tex_selected_list[i]
		)
		_options.append(opt)

func _toggle() -> void:
	_set_open(not _is_open)

func _set_open(open: bool, instant: bool = false) -> void:
	_is_open = open
	settings_icon.button_pressed = open
	if _tween and _tween.is_valid():
		_tween.kill()
	if instant:
		for opt in _options:
			opt.size.x = option_width if open else 0.0
			opt.mouse_filter = Control.MOUSE_FILTER_STOP if open else Control.MOUSE_FILTER_IGNORE
		return
	_tween = create_tween()
	_tween.set_parallel(true)
	_tween.set_ease(Tween.EASE_OUT)
	_tween.set_trans(Tween.TRANS_CUBIC)
	for i in _options.size():
		var opt := _options[i]
		opt.mouse_filter = Control.MOUSE_FILTER_STOP if open else Control.MOUSE_FILTER_IGNORE
		var target_x: float = option_width if open else 0.0
		var order: int = i if open else (_options.size() - 1 - i)
		_tween.tween_property(opt, "size:x", target_x, anim_duration) \
			.set_delay(order * stagger_delay)

func _select(index: int, emit := true) -> void:
	if index < 0 or index >= _options.size():
		index = clampi(default_selected, 0, max(0, _options.size() - 1))
	_selected_index = index
	for i in _options.size():
		_options[i].set_selected(i == index)
	if emit:
		option_selected.emit(index, option_names[index])

func _on_option_pressed(index: int) -> void:
	_select(index)
	_save_mode()

func _on_handle_down() -> void:
	_dragging = true
	_drag_offset = drag_handle.global_position - get_global_mouse_position()
	_set_open(false)

func _on_handle_up() -> void:
	_dragging = false
	if auto_save:
		_save_layout()

func _input(event: InputEvent) -> void:
	if not _dragging:
		return
	if event is InputEventMouseMotion:
		var new_pos: Vector2 = get_global_mouse_position() + _drag_offset
		var delta: Vector2 = new_pos - drag_handle.global_position
		drag_handle.global_position = new_pos
		settings_icon.global_position += delta
		options_root.global_position += delta
		get_viewport().set_input_as_handled()

func _save_layout() -> void:
	var data := {
		"version": LAYOUT_VERSION,
		"drag_handle":   [drag_handle.position.x,   drag_handle.position.y],
		"settings_icon": [settings_icon.position.x, settings_icon.position.y],
		"options_root":  [options_root.position.x,  options_root.position.y],
	}
	_write_json_atomic(LAYOUT_FILE, data)

func _load_layout() -> void:
	var data := _read_json_safe(LAYOUT_FILE)
	if data.is_empty():
		return

	if _is_vec2_array(data.get("drag_handle")):
		drag_handle.position = _to_vec2(data["drag_handle"])
	if _is_vec2_array(data.get("settings_icon")):
		settings_icon.position = _to_vec2(data["settings_icon"])
	if _is_vec2_array(data.get("options_root")):
		options_root.position = _to_vec2(data["options_root"])

func _is_vec2_array(v) -> bool:
	return v is Array and v.size() == 2 and v[0] is float and v[1] is float

func _to_vec2(v: Array) -> Vector2:
	return Vector2(v[0], v[1])

func _save_mode() -> void:
	var mode := _selected_index
	var data := {
		"version": MODE_VERSION,
		"mode": mode,
		"hide_mask": mode == 1 or mode == 3,
		"hide_anquanku": mode == 2 or mode == 3,
	}
	_write_json_atomic(MODE_FILE, data)

func _load_mode() -> int:
	var data := _read_json_safe(MODE_FILE)
	if data.is_empty():
		return default_selected

	var raw = data.get("mode", default_selected)
	if not (raw is float or raw is int):
		push_warning("[KaguyaSilentRavenSkin]mode 字段类型错误，使用默认值")
		return default_selected
	var mode := int(raw)
	if mode < 0 or mode >= _options.size():
		push_warning("[KaguyaSilentRavenSkin]mode 值越界 (%d)，使用默认值" % mode)
		return default_selected

	return mode

func _load_saved() -> void:
	_load_layout()
	var idx := _load_mode()
	_select(idx, true)

func reset_layout() -> void:
	if FileAccess.file_exists(LAYOUT_FILE):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(LAYOUT_FILE))
	if FileAccess.file_exists(MODE_FILE):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(MODE_FILE))
