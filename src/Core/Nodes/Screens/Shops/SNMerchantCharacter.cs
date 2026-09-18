using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace SilentSkinMod.Core.Nodes.Screens.Shops;

[GlobalClass]
public partial class SNMerchantCharacter : NMerchantCharacter
{
	private MegaSprite _spineMega;
	private MegaSkeletonDataResource _liveData;
	private MegaSkeletonDataResource _nekoData;
	private MegaSkeletonDataResource _nsfwData;
	private MegaSkeletonDataResource _nsfwCatData;

	public override void _Ready()
	{
		base._Ready();

		var spineNode = GetNode<Node2D>("SpineSprite");
		_spineMega = new MegaSprite(Variant.From(spineNode));
		_liveData = _spineMega.GetSkeleton()?.GetData();

		_nekoData    = ExtractSkeletonData("SpineSprite_neko");
		_nsfwData    = ExtractSkeletonData("SpineSprite_nsfw");
		_nsfwCatData = ExtractSkeletonData("SpineSprite_nsfw_cat");

		HeadVisibilityBus.OnModeChanged += OnModeChanged;
		ApplyMode(HeadVisibilityBus.CurrentMode);
	}

	private MegaSkeletonDataResource ExtractSkeletonData(string path)
	{
		var node = GetNodeOrNull<Node2D>(path);
		if (node == null) return null;
		node.Visible = false;
		return new MegaSprite(Variant.From(node)).GetSkeleton()?.GetData();
	}

	private void OnModeChanged(CharacterMode mode) => ApplyMode(mode);

	private void ApplyMode(CharacterMode mode)
	{
		if (_spineMega == null) return;

		MegaSkeletonDataResource target = mode switch
		{
			CharacterMode.Live     => _liveData,
			CharacterMode.LiveCat  => _nekoData,
			CharacterMode.Nsfw     => _nsfwData,
			CharacterMode.NsfwCat  => _nsfwCatData,
			_ => _liveData
		};

		if (target == null)
		{
			GD.PrintErr($"[SNMerchantCharacter] 模式 {mode} 的骨架数据不存在");
			return;
		}

		_spineMega.SetSkeletonDataRes(target);
		_spineMega.GetSkeleton()?.SetSlotsToSetupPose();
		PlayAnimation("relaxed_loop", loop: true);
		GD.Print($"[SNMerchantCharacter] 切换到 {mode}");
	}

	public override void _ExitTree()
	{
		HeadVisibilityBus.OnModeChanged -= OnModeChanged;
		base._ExitTree();
	}
}
