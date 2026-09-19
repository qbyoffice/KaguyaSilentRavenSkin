using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace SilentSkinMod.Core.Nodes.RestSite;

[GlobalClass]
public partial class SNRestSiteCharacter : NRestSiteCharacter
{
	private Node2D _spineLive;
	private Node2D _spineNsfw;

	public override void _Ready()
	{
		base._Ready();

		HeadVisibilityBus.EnsureInitialized();

		_spineLive = GetNodeOrNull<Node2D>("SpineSprite");
		_spineNsfw = GetNodeOrNull<Node2D>("SpineSprite_nsfw");

		if (_spineNsfw != null) _spineNsfw.Visible = false;

		HeadVisibilityBus.OnModeChanged += OnModeChanged;
		ApplyMode(HeadVisibilityBus.CurrentMode);
	}

	private void OnModeChanged(CharacterMode mode) => ApplyMode(mode);

	private void ApplyMode(CharacterMode mode)
	{
		bool useNsfw = mode == CharacterMode.Nsfw || mode == CharacterMode.NsfwCat;

		if (_spineLive != null) _spineLive.Visible = !useNsfw;
		if (_spineNsfw != null) _spineNsfw.Visible = useNsfw;
	}

	public override void _ExitTree()
	{
		HeadVisibilityBus.OnModeChanged -= OnModeChanged;
		base._ExitTree();
	}
}
