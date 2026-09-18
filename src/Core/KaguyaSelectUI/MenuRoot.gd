extends Control

signal option_selected(index: int, option_name: String)

@export var option_names: Array[String] = ["直播模式", "直播模式_猫", "NSFW", "NSFW_猫"]

@export_group("选项图片差分")
@export var tex_normal_list:   Array[Texture2D] = []
@export var tex_hover_list:    Array[Texture2D] = []
@export var tex_pressed_list:  Array[Texture2D] = []
@export var tex_selected_list: Array[Texture2D] = []

@export_group("设置图标")
@export var icon_active: Texture2D   # 默认图在编辑器里给 SettingsIcon.texture_normal

@export_group("布局")
@export var option_width: float = 220.0
@export var option_height: float = 44.0
@export var option_gap: float = 6.0
@export var default_selected: int = 0

@export_group("动画")
@export var anim_duration: float = 0.22
@export var stagger_delay: float = 0.05

@onready var settings_icon: TextureButton = $SettingsIcon
@onready var options_root: Control = $OptionsRoot

var _options: Array[OptionItem] = []
var _is_open := false
var _selected_index := -1
var _icon_default: Texture2D
var _tween: Tween

func _ready() -> void:
	_icon_default = settings_icon.texture_normal
	settings_icon.pressed.connect(_toggle)

	_build_options()
	_select(default_selected, false)
	_set_open(false, true)

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
	settings_icon.texture_normal = icon_active if open else _icon_default

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
	_selected_index = index
	for i in _options.size():
		_options[i].set_selected(i == index)
	if emit:
		option_selected.emit(index, option_names[index])

func _on_option_pressed(index: int) -> void:
	_select(index)
