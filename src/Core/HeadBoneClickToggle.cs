using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace SilentSkinMod.Core.Nodes.Animation;

[GlobalClass]
[GodotClassName("HeadBoneClickToggle")]
public partial class HeadBoneClickToggle : Control
{
	private static readonly string[] MaskSlots = new string[]
	{
		"tougu mianju 0", "tougu mianju 1", "tougu mianju 2",
		"mianju yanjing 0", "mianju yanjing 6", "mianju yanjing 2",
		"mianju yanjing 3", "mianju yanjing 4", "mianju yanjing 5",
		"mianju yanjing 1", "mianju yinying 0"
	};

	private static readonly string[] AnquankuSlots = new string[]
	{
		"anquanku1_dei", "anquanku2_dei", "anquanku0_dei"
	};

	private readonly Dictionary<CharacterMode, Node2D> _spriteMap = new();
	private Node2D _currentSprite;
	private MegaSprite _currentMega;
	private MegaSkeleton _currentSkeleton;
	private readonly List<GodotObject> _maskSlotsCached = new();
	private readonly List<GodotObject> _anquankuSlotsCached = new();
	private readonly Dictionary<string, Color> _storedColors = new();

	public override void _Ready()
	{
		_spriteMap[CharacterMode.Live]     = GetNodeOrNull<Node2D>("../SpineSprite");
		_spriteMap[CharacterMode.LiveCat]  = GetNodeOrNull<Node2D>("../SpineSprite_neko");
		_spriteMap[CharacterMode.Nsfw]     = GetNodeOrNull<Node2D>("../SpineSprite_nsfw");
		_spriteMap[CharacterMode.NsfwCat]  = GetNodeOrNull<Node2D>("../SpineSprite_nsfw_cat");
		
		var menuRoot = GetNodeOrNull<Node>("../MenuRoot");
		if (menuRoot != null && menuRoot.HasSignal("option_selected"))
		{
			menuRoot.Connect("option_selected", new Callable(this, nameof(OnOptionSelected)));
			GD.Print("[KaguyaSilentRavenSkin][HeadBoneClickToggle] 已连接 MenuRoot.option_selected");
		}
		else
		{
			GD.PrintErr("[KaguyaSilentRavenSkin][HeadBoneClickToggle] 未找到 MenuRoot 或 option_selected 信号");
		}

		SwitchTo(HeadVisibilityBus.CurrentMode);
	}

	private void OnOptionSelected(int index, string optionName)
	{
		var mode = (CharacterMode)index;
		GD.Print($"[KaguyaSilentRavenSkin][HeadBoneClickToggle] 选项: {optionName} -> {mode}");
		HeadVisibilityBus.SetMode(mode);
		SwitchTo(mode);
	}

	private void SwitchTo(CharacterMode mode)
	{
		if (!_spriteMap.TryGetValue(mode, out var target) || target == null)
		{
			return;
		}

		if (target == _currentSprite)
		{
			ApplyVisibility();
			return;
		}
		
		foreach (var kv in _spriteMap)
			if (kv.Value != null) kv.Value.Visible = (kv.Value == target);
		
		if (_currentSprite != null && _currentSprite.HasSignal("world_transforms_changed"))
			_currentSprite.Disconnect("world_transforms_changed", new Callable(this, nameof(OnWorldTransformsChanged)));

		_currentSprite = target;
		_currentMega = new MegaSprite(Variant.From(target));
		_currentSkeleton = _currentMega.GetSkeleton();
		
		_maskSlotsCached.Clear();
		_anquankuSlotsCached.Clear();
		_storedColors.Clear();
		if (_currentSkeleton != null) CacheSlots();
		
		if (_currentSprite.HasSignal("world_transforms_changed"))
			_currentSprite.Connect("world_transforms_changed", new Callable(this, nameof(OnWorldTransformsChanged)));

		ApplyVisibility();
	}

	private void CacheSlots()
	{
		var slots = _currentSkeleton.BoundObject.Call("get_slots");
		if (slots.VariantType != Variant.Type.Array) return;

		var slotsArray = slots.As<Godot.Collections.Array>();
		foreach (var slotVariant in slotsArray)
		{
			if (slotVariant.VariantType != Variant.Type.Object) continue;
			var slot = slotVariant.AsGodotObject();
			string slotName = slot.Call("get_data").AsGodotObject().Call("get_name").AsString();

			if (IsInList(slotName, MaskSlots))
				_maskSlotsCached.Add(slot);
			else if (IsInList(slotName, AnquankuSlots))
				_anquankuSlotsCached.Add(slot);
		}
	}

	private void OnWorldTransformsChanged(Variant sprite) => ApplyVisibility();

	private void ApplyVisibility()
	{
		if (_currentSkeleton == null) return;

		var mode = HeadVisibilityBus.CurrentMode;

		bool hideMask     = mode == CharacterMode.LiveCat || mode == CharacterMode.NsfwCat;
		bool hideAnquanku = mode == CharacterMode.Nsfw    || mode == CharacterMode.NsfwCat;

		ApplySlotVisibility(_maskSlotsCached, hideMask);
		ApplySlotVisibility(_anquankuSlotsCached, hideAnquanku);
	}

	private void ApplySlotVisibility(List<GodotObject> slots, bool hide)
	{
		foreach (var slot in slots)
		{
			string slotName = slot.Call("get_data").AsGodotObject().Call("get_name").AsString();
			Color c = slot.Call("get_color").As<Color>();

			if (hide)
			{
				if (!_storedColors.ContainsKey(slotName)) _storedColors[slotName] = c;
				c.A = 0f;
				slot.Call("set_color", c);
			}
			else if (_storedColors.TryGetValue(slotName, out var stored))
			{
				slot.Call("set_color", stored);
				_storedColors.Remove(slotName);
			}
		}
	}

	private static bool IsInList(string name, string[] list)
	{
		foreach (var t in list) if (t == name) return true;
		return false;
	}

	public override void _ExitTree()
	{
		if (_currentSprite != null && _currentSprite.HasSignal("world_transforms_changed"))
			_currentSprite.Disconnect("world_transforms_changed", new Callable(this, nameof(OnWorldTransformsChanged)));

		_maskSlotsCached.Clear();
		_anquankuSlotsCached.Clear();
		_storedColors.Clear();
	}
}
