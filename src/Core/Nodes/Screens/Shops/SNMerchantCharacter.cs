using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace SilentSkinMod.Core.Nodes.Screens.Shops;

[GlobalClass]
public partial class SNMerchantCharacter : NMerchantCharacter
{
    private MegaSprite _spineMega;               
    private Node2D _nekoNode;                    
    private MegaSkeletonDataResource _originalData;
    private MegaSkeletonDataResource _nekoData;
    private bool _isNekoActive = false;

    public override void _Ready()
    {
        base._Ready();

        var spineNode = GetNode<Node2D>("SpineSprite");
        _nekoNode = GetNode<Node2D>("SpineSprite_neko");
        
        
        _spineMega = new MegaSprite(spineNode);


        var skeleton = _spineMega.GetSkeleton();
        if (skeleton != null)
            _originalData = skeleton.GetData();

        var nekoSkeleton = new MegaSprite(_nekoNode).GetSkeleton();
        if (nekoSkeleton != null)
            _nekoData = nekoSkeleton.GetData();

        _nekoNode.Visible = false;

        HeadVisibilityBus.OnVisibilityChanged += OnVisibilityChanged;
        ApplyModelVisibility(HeadVisibilityBus.IsHidden);
    }

    private void OnVisibilityChanged(bool hidden) => ApplyModelVisibility(hidden);

    private void ApplyModelVisibility(bool hidden)
    {
        if (_spineMega == null || _nekoData == null || _originalData == null) return;

        if (hidden && !_isNekoActive)
        {
            _spineMega.SetSkeletonDataRes(_nekoData);
            _spineMega.GetSkeleton()?.SetSlotsToSetupPose();
            PlayAnimation("relaxed_loop", loop: true);
            _isNekoActive = true;
            GD.Print("[KaguyaSilentNMerchantCharacter] Switched to Neko data");
        }
        else if (!hidden && _isNekoActive)
        {
            _spineMega.SetSkeletonDataRes(_originalData);
            _spineMega.GetSkeleton()?.SetSlotsToSetupPose();
            PlayAnimation("relaxed_loop", loop: true);
            _isNekoActive = false;
            GD.Print("[KaguyaSilentNMerchantCharacter] Switched to Original data");
        }
    }

    public override void _ExitTree()
    {
        HeadVisibilityBus.OnVisibilityChanged -= OnVisibilityChanged;
        base._ExitTree();
    }
}