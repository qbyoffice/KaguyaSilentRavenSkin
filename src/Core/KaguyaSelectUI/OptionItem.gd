extends Control
class_name OptionItem

signal option_pressed(index: int)

const NORMAL_WIDTH   := 195.0
const SELECTED_WIDTH := 240.0

var index: int = 0
var selected := false

var _tex_normal: Texture2D
var _tex_hover: Texture2D
var _tex_pressed: Texture2D
var _tex_selected: Texture2D

var _hovered := false
var _mouse_down := false

var bg: TextureRect
var label: Label

func setup(i: int, text: String, h: float,
		tn: Texture2D, th: Texture2D, tp: Texture2D, ts: Texture2D) -> void:
	index = i
	_tex_normal = tn
	_tex_hover = th
	_tex_pressed = tp
	_tex_selected = ts

	size = Vector2(0, h)
	clip_contents = true
	mouse_filter = Control.MOUSE_FILTER_IGNORE

	bg = TextureRect.new()
	bg.texture = tn
	bg.position = Vector2.ZERO
	bg.expand_mode = TextureRect.EXPAND_KEEP_SIZE
	bg.stretch_mode = TextureRect.STRETCH_KEEP
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)
	if tn:
		bg.size = tn.get_size()

	label = Label.new()
	label.text = text
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(label)
	label.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

	mouse_entered.connect(_on_hover.bind(true))
	mouse_exited.connect(_on_hover.bind(false))
	gui_input.connect(_on_gui_input)

func get_target_width() -> float:
	return SELECTED_WIDTH if selected else NORMAL_WIDTH

func set_selected(v: bool) -> void:
	selected = v
	_refresh()

func _refresh() -> void:
	if _mouse_down:
		bg.texture = _tex_pressed
	elif selected:
		bg.texture = _tex_selected
	elif _hovered:
		bg.texture = _tex_hover
	else:
		bg.texture = _tex_normal

	if bg.texture:
		bg.size = bg.texture.get_size()

func _on_hover(entered: bool) -> void:
	_hovered = entered
	_refresh()

func _on_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_mouse_down = true
			_refresh()
		else:
			_mouse_down = false
			_refresh()
			if _hovered:
				option_pressed.emit(index)
		accept_event()
