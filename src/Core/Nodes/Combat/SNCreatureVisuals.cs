using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Animation;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace SilentSkinMod.Core.Nodes.Combat;

internal static class NCreatureAccessors
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_body")]
    public extern static ref Node2D GetBodyField(NCreatureVisuals instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_spineAnimator")]
    public extern static ref CreatureAnimator GetSpineAnimatorField(NCreature instance);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ConnectSpineAnimatorSignals")]
    public extern static void ConnectSpineAnimatorSignals(NCreature instance);
}

[GlobalClass]
public partial class SNCreatureVisuals : NCreatureVisuals
{
    private Node2D _nekoVisuals;
    private MegaSkeletonDataResource _originalSkeletonData;
    private MegaSkeletonDataResource _nekoSkeletonData;
    private bool _isNekoActive = false;

    public override void _Ready()
    {
        base._Ready();

        if (SpineBody != null)
        {
            var skeleton = SpineBody.GetSkeleton();
            if (skeleton != null)
                _originalSkeletonData = skeleton.GetData();
        }

        _nekoVisuals = GetNodeOrNull<Node2D>("%Visuals_neko");
        if (_nekoVisuals == null)
        {
            GD.PrintErr("未找到 %Visuals_neko");
            return;
        }
        _nekoVisuals.Visible = false;

        if (_nekoVisuals.GetClass() == "SpineSprite")
        {
            var nekoSpine = new MegaSprite(Variant.From(_nekoVisuals));
            var skeleton = nekoSpine.GetSkeleton();
            if (skeleton != null)
                _nekoSkeletonData = skeleton.GetData();
        }

        HeadVisibilityBus.OnModeChanged += OnModeChanged;
        ApplyMode(HeadVisibilityBus.CurrentMode);
    }

    private void OnModeChanged(CharacterMode mode) => ApplyMode(mode);

    private void ApplyMode(CharacterMode mode)
    {
        if (_nekoVisuals == null || SpineBody == null) return;

        bool useNeko = mode == CharacterMode.LiveCat || mode == CharacterMode.NsfwCat;

        if (useNeko && !_isNekoActive)
        {
            if (_nekoSkeletonData != null)
            {
                SpineBody.SetSkeletonDataRes(_nekoSkeletonData);
                SpineBody.GetSkeleton()?.SetSlotsToSetupPose();
            }
            _nekoVisuals.Visible = false;
            _isNekoActive = true;
            RefreshAnimator();
        }
        else if (!useNeko && _isNekoActive)
        {
            if (_originalSkeletonData != null)
            {
                SpineBody.SetSkeletonDataRes(_originalSkeletonData);
                SpineBody.GetSkeleton()?.SetSlotsToSetupPose();
            }
            _isNekoActive = false;
            RefreshAnimator();
        }
    }

    private void RefreshAnimator()
    {
        var parent = GetParent() as NCreature;
        if (parent == null) return;
        
        var creature = parent.Entity;
        if (creature == null) return;

        object model = null;
        if (creature.Player != null) model = creature.Player.Character;
        else if (creature.Monster != null) model = creature.Monster;
        if (model == null) return;
        
        CreatureAnimator newAnimator = null;
        if (model is CharacterModel character)
            newAnimator = character.GenerateAnimator(SpineBody, creature);

        if (newAnimator == null) return;
        
        NCreatureAccessors.GetSpineAnimatorField(parent) = newAnimator;
        
        NCreatureAccessors.ConnectSpineAnimatorSignals(parent);
        
        parent.SetAnimationTrigger("Idle");
    }

    public override void _ExitTree()
    {
        HeadVisibilityBus.OnModeChanged -= OnModeChanged;
        base._ExitTree();
    }
}