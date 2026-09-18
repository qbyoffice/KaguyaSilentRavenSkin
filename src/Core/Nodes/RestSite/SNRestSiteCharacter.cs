using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace SilentSkinMod.Core.Nodes.RestSite;

[GlobalClass]
public partial class SNRestSiteCharacter : NRestSiteCharacter
{
    private Node2D _spineOriginal;
    private Node2D _spineNsfw;

    public override void _Ready()
    {
        base._Ready();

        _spineOriginal = GetNodeOrNull<Node2D>("SpineSprite");
        _spineNsfw = GetNodeOrNull<Node2D>("SpineSprite_nsfw");

        if (_spineOriginal == null)
            GD.PrintErr("[SNRestSiteCharacter] 未找到 SpineSprite");
        if (_spineNsfw == null)
            GD.PrintErr("[SNRestSiteCharacter] 未找到 SpineSprite_nsfw");
        
        if (_spineNsfw != null)
            _spineNsfw.Visible = false;

        HeadVisibilityBus.OnModeChanged += OnModeChanged;
        ApplyMode(HeadVisibilityBus.CurrentMode);
    }

    private void OnModeChanged(CharacterMode mode) => ApplyMode(mode);

    private void ApplyMode(CharacterMode mode)
    {
        bool useNsfw = mode == CharacterMode.Nsfw || mode == CharacterMode.NsfwCat;

        if (_spineOriginal != null)
            _spineOriginal.Visible = !useNsfw;
        if (_spineNsfw != null)
            _spineNsfw.Visible = useNsfw;

        GD.Print($"[SNRestSiteCharacter] 模式 {mode} -> {(useNsfw ? "Nsfw" : "Original")}");
    }

    public override void _ExitTree()
    {
        HeadVisibilityBus.OnModeChanged -= OnModeChanged;
        base._ExitTree();
    }
}