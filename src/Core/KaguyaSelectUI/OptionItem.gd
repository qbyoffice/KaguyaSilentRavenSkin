extends Control
class_name OptionItem

signal option_pressed(index: int)

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

func setup(i: int, text: String, w: float, h: float,
		tn: Texture2D, th: Texture2D, tp: Texture2D, ts: Texture2D) -> void:
	index = i
	_tex_normal = tn
	_tex_hover = th
	_tex_pressed = tp
	_tex_selected = ts

	size = Vector2(0, h)          # 初始宽度 0，配合 clip 实现从左到右揭开
	clip_contents = true
	mouse_filter = Control.MOUSE_FILTER_IGNORE

	# 背景图：始终满宽，不随父节点宽度变化
	bg = TextureRect.new()
	bg.texture = tn
	bg.position = Vector2.ZERO
	bg.size = Vector2(w, h)
	bg.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	bg.stretch_mode = TextureRect.STRETCH_SCALE
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	# 文本：后 add，盖在背景图上
	label = Label.new()
	label.text = text
	label.position = Vector2.ZERO
	label.size = Vector2(w, h)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(label)

	mouse_entered.connect(_on_hover.bind(true))
	mouse_exited.connect(_on_hover.bind(false))
	gui_input.connect(_on_gui_input)

func set_selected(v: bool) -> void:
	selected = v
	_refresh()

# 优先级：按下 > 选中 > 悬停 > 默认
func _refresh() -> void:
	if _mouse_down:
		bg.texture = _tex_pressed
	elif selected:
		bg.texture = _tex_selected
	elif _hovered:
		bg.texture = _tex_hover
	else:
		bg.texture = _tex_normal

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
