using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System.Reflection;

namespace SilentSkinMod.Core.Nodes.Combat;

[GlobalClass]
public partial class SNCreatureVisuals : NCreatureVisuals
{
	private MegaSkeletonDataResource _liveData;
	private MegaSkeletonDataResource _nekoData;
	private MegaSkeletonDataResource _nsfwData;
	private MegaSkeletonDataResource _nsfwCatData;
	private CharacterMode _lastMode = (CharacterMode)(-1);

	public override void _Ready()
	{
		base._Ready();
		
		HeadVisibilityBus.EnsureInitialized();

		if (SpineBody != null)
			_liveData = SpineBody.GetSkeleton()?.GetData();

		_nekoData    = ExtractSkeletonData("%Visuals_neko");
		_nsfwData    = ExtractSkeletonData("%Visuals_nsfw");
		_nsfwCatData = ExtractSkeletonData("%Visuals_nsfw_cat");

		HeadVisibilityBus.OnModeChanged += OnModeChanged;
		
		ApplyMode(HeadVisibilityBus.CurrentMode, force: true);
	}

	private MegaSkeletonDataResource ExtractSkeletonData(string path)
	{
		var node = GetNodeOrNull<Node2D>(path);
		if (node == null || node.GetClass() != "SpineSprite") return null;
		node.Visible = false;
		return new MegaSprite(Variant.From(node)).GetSkeleton()?.GetData();
	}

	private void OnModeChanged(CharacterMode mode) => ApplyMode(mode);

	private void ApplyMode(CharacterMode mode, bool force = false)
	{
		if (SpineBody == null) return;
		if (!force && mode == _lastMode) return;

		MegaSkeletonDataResource target = mode switch
		{
			CharacterMode.Live    => _liveData,
			CharacterMode.LiveCat => _nekoData,
			CharacterMode.Nsfw    => _nsfwData,
			CharacterMode.NsfwCat => _nsfwCatData,
			_ => _liveData
		};

		if (target == null)
		{
			GD.PrintErr($"[KaguyaSilentRavenSkin][SNCreatureVisuals] 模式 {mode} 的骨架数据不存在");
			return;
		}

		SpineBody.SetSkeletonDataRes(target);
		_lastMode = mode;

		RefreshAnimator();
		GD.Print($"[KaguyaSilentRavenSkin][SNCreatureVisuals] 切换到 {mode}");
	}

	private void RefreshAnimator()
	{
		var parent = GetParent();
		if (parent == null) return;

		var entityProp = parent.GetType().GetProperty("Entity");
		if (entityProp == null) return;
		var creature = entityProp.GetValue(parent) as Creature;
		if (creature == null) return;

		object model = null;
		if (creature.Player != null) model = creature.Player.Character;
		else if (creature.Monster != null) model = creature.Monster;
		if (model == null) return;

		var genMethod = model.GetType().GetMethod("GenerateAnimator", new[] { typeof(MegaSprite), typeof(Creature) });
		if (genMethod == null) return;
		var newAnimator = genMethod.Invoke(model, new object[] { SpineBody, creature }) as CreatureAnimator;
		if (newAnimator == null) return;

		var animField = parent.GetType().GetField("_spineAnimator", BindingFlags.NonPublic | BindingFlags.Instance);
		if (animField == null) return;
		animField.SetValue(parent, newAnimator);

		parent.GetType().GetMethod("ConnectSpineAnimatorSignals", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(parent, null);
		parent.GetType().GetMethod("SetAnimationTrigger")?.Invoke(parent, new object[] { "Idle" });
	}

	public override void _ExitTree()
	{
		HeadVisibilityBus.OnModeChanged -= OnModeChanged;
		base._ExitTree();
	}
}
