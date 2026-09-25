using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace SilentSkinMod.Core.Nodes.Animation;

[GlobalClass]
[GodotClassName("HeadBoneClickToggle")]
public partial class HeadBoneClickToggle : Control
{
	private const string ModeJsonPath =
		"user://Kugaya.skin/KaguyaSilentRavenSkin/kaguyaMode.json";

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

	private Node2D _spineSprite;
	private MegaSkeleton _skeleton;
	private readonly List<GodotObject> _maskSlots = new();
	private readonly List<GodotObject> _anquankuSlots = new();
	private readonly Dictionary<string, Color> _originalColors = new();

	private bool _hideMask;
	private bool _hideAnquanku;

	public override void _Ready()
	{
		_spineSprite = GetNodeOrNull<Node2D>("../SpineSprite");
		if (_spineSprite == null)
		{
			GD.PrintErr("[KaguyaSilentRavenSkin] 未找到 SpineSprite");
			return;
		}

		_skeleton = new MegaSprite(Variant.From(_spineSprite)).GetSkeleton();

		if (_spineSprite.HasSignal("world_transforms_changed"))
			_spineSprite.Connect("world_transforms_changed",
				new Callable(this, nameof(OnWorldTransformsChanged)));

		var menuRoot = GetNodeOrNull<Node>("../KuguyaLayer/MenuRoot");
		if (menuRoot != null && menuRoot.HasSignal("option_selected"))
			menuRoot.Connect("option_selected",
				new Callable(this, nameof(OnOptionSelected)));

		LoadFromJson(out var mode, out _hideMask, out _hideAnquanku);
		HeadVisibilityBus.SetMode(mode);

		CacheSlots();
		CaptureOriginalColors(); 
		ApplySlotVisibility();
	}

	private void OnOptionSelected(int index, string optionName)
	{
		if (index < 0 || index > 3) return;
		var mode = (CharacterMode)index;

		bool oldHideMask = _hideMask;
		bool oldHideAnquanku = _hideAnquanku;
		
		_hideMask     = mode == CharacterMode.LiveCat || mode == CharacterMode.NsfwCat;
		_hideAnquanku = mode == CharacterMode.Nsfw    || mode == CharacterMode.NsfwCat;

		SaveSlotFlags(mode, _hideMask, _hideAnquanku);
		HeadVisibilityBus.SetMode(mode);
		
		if (oldHideMask && !_hideMask)     RestoreSlots(_maskSlots);
		if (oldHideAnquanku && !_hideAnquanku) RestoreSlots(_anquankuSlots);
		
		ApplySlotVisibility();
	}

	private void CacheSlots()
	{
		if (_skeleton == null) return;
		var slots = _skeleton.BoundObject.Call("get_slots");
		if (slots.VariantType != Variant.Type.Array) return;

		var arr = slots.As<Godot.Collections.Array>();
		foreach (var v in arr)
		{
			if (v.VariantType != Variant.Type.Object) continue;
			var slot = v.AsGodotObject();
			string slotName = slot.Call("get_data").AsGodotObject().Call("get_name").AsString();

			if (IsInList(slotName, MaskSlots)) _maskSlots.Add(slot);
			else if (IsInList(slotName, AnquankuSlots)) _anquankuSlots.Add(slot);
		}
	}

	private void CaptureOriginalColors()
	{
		foreach (var slot in _maskSlots)
		{
			string name = slot.Call("get_data").AsGodotObject().Call("get_name").AsString();
			if (!_originalColors.ContainsKey(name))
				_originalColors[name] = slot.Call("get_color").As<Color>();
		}
		foreach (var slot in _anquankuSlots)
		{
			string name = slot.Call("get_data").AsGodotObject().Call("get_name").AsString();
			if (!_originalColors.ContainsKey(name))
				_originalColors[name] = slot.Call("get_color").As<Color>();
		}
	}

	private void RestoreSlots(List<GodotObject> slots)
	{
		foreach (var slot in slots)
		{
			string name = slot.Call("get_data").AsGodotObject().Call("get_name").AsString();
			if (_originalColors.TryGetValue(name, out var orig))
				slot.Call("set_color", orig);
		}
	}

	private void OnWorldTransformsChanged(Variant sprite) => ApplySlotVisibility();

	private void ApplySlotVisibility()
	{
		if (_skeleton == null) return;
		if (_hideMask)     ForceHideSlots(_maskSlots);
		if (_hideAnquanku) ForceHideSlots(_anquankuSlots);
	}

	private void ForceHideSlots(List<GodotObject> slots)
	{
		foreach (var slot in slots)
		{
			Color c = slot.Call("get_color").As<Color>();
			c.A = 0f;
			slot.Call("set_color", c);
		}
	}

	private static bool IsInList(string name, string[] list)
	{
		foreach (var t in list) if (t == name) return true;
		return false;
	}

	private void SaveSlotFlags(CharacterMode mode, bool hideMask, bool hideAnquanku)
	{
		var dict = new Godot.Collections.Dictionary
		{
			{ "version", 2 },
			{ "mode", (int)mode },
			{ "hide_mask", hideMask },
			{ "hide_anquanku", hideAnquanku }
		};

		string tmp = ModeJsonPath + ".tmp";
		using (var f = Godot.FileAccess.Open(tmp, Godot.FileAccess.ModeFlags.Write))
		{
			if (f == null) { GD.PushWarning("[KaguyaSilentRavenSkin] 无法写入 " + tmp); return; }
			f.StoreString(Json.Stringify(dict, "\t"));
		}

		var da = DirAccess.Open("user://Kugaya.skin/KaguyaSilentRavenSkin");
		if (da == null) return;
		if (da.FileExists("kaguyaMode.json")) da.Remove("kaguyaMode.json");
		da.Rename("kaguyaMode.json.tmp", "kaguyaMode.json");
	}

	private void LoadFromJson(out CharacterMode mode, out bool hideMask, out bool hideAnquanku)
	{
		mode = CharacterMode.Live;
		hideMask = false;
		hideAnquanku = false;

		if (!Godot.FileAccess.FileExists(ModeJsonPath)) return;

		using var file = Godot.FileAccess.Open(ModeJsonPath, Godot.FileAccess.ModeFlags.Read);
		if (file == null) return;

		string content = file.GetAsText();
		if (string.IsNullOrWhiteSpace(content)) return;

		var json = new Json();
		if (json.Parse(content) != Error.Ok) return;

		var data = json.Data;
		if (data.VariantType != Variant.Type.Dictionary) return;
		var dict = data.AsGodotDictionary();

		if (dict.ContainsKey("mode"))
		{
			var mv = dict["mode"];
			if (mv.VariantType == Variant.Type.Int || mv.VariantType == Variant.Type.Float)
			{
				int m = (int)mv;
				if (m >= 0 && m <= 3) mode = (CharacterMode)m;
			}
		}

		hideMask     = mode == CharacterMode.LiveCat || mode == CharacterMode.NsfwCat;
		hideAnquanku = mode == CharacterMode.Nsfw    || mode == CharacterMode.NsfwCat;

		if (dict.ContainsKey("hide_mask"))
		{
			var v = dict["hide_mask"];
			if (v.VariantType == Variant.Type.Bool) hideMask = v.AsBool();
		}
		if (dict.ContainsKey("hide_anquanku"))
		{
			var v = dict["hide_anquanku"];
			if (v.VariantType == Variant.Type.Bool) hideAnquanku = v.AsBool();
		}
	}

	public override void _ExitTree()
	{
		if (_spineSprite != null && _spineSprite.HasSignal("world_transforms_changed"))
			_spineSprite.Disconnect("world_transforms_changed",
				new Callable(this, nameof(OnWorldTransformsChanged)));

		_maskSlots.Clear();
		_anquankuSlots.Clear();
		_originalColors.Clear();
	}
}
