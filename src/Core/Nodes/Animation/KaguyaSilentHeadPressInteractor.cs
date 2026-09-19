using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace SilentSkinMod.Core.Nodes.Animation;

[GlobalClass]
[GodotClassName("KaguyaSilentHeadPressInteractor")]
public partial class KaguyaSilentHeadPressInteractor : Control
{
	[Export] public float LongPressThreshold { get; set; } = 0.15f;

	[Export] public string IdleAnimation { get; set; } = "animation";
	[Export] public string PressStartAnim { get; set; } = "1/biyan";
	[Export] public string PressLoopAnim { get; set; } = "1/biyan_idle";
	[Export] public string ReleaseAnim { get; set; } = "1/kaiyan";
	[Export] public string ClickAnim { get; set; } = "1/zhayan";

	private const int TrackBody = 0;
	private const int TrackHead = 1;

	private bool _isPressing = false;
	private bool _isLongPressTriggered = false;
	private ulong _pressStartTime = 0;
	private MegaAnimationState _animationState = null;
	private Node _spineNode = null;

	private MegaAnimationState GetAnimationState()
	{
		if (_spineNode == null)
		{
			_spineNode = GetNode("../SpineSprite");
		}
		return new MegaSprite(Variant.From(_spineNode)).GetAnimationState();
	}

	private void PlayAnimation(int track, string name, bool loop)
	{
		if (_animationState == null) return;
		_animationState.BoundObject.Call("set_animation", name, loop, track);
	}

	public override void _Ready()
	{
		_animationState = GetAnimationState();
		if (_animationState != null)
		{
			PlayAnimation(TrackBody, IdleAnimation, true);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			bool isInside = GetGlobalRect().HasPoint(GetGlobalMousePosition());

			if (mouseEvent.Pressed && isInside)
			{
				_isPressing = true;
				_isLongPressTriggered = false;
				_pressStartTime = Time.GetTicksMsec();

				if (_animationState == null)
					_animationState = GetAnimationState();
				if (_animationState == null) return;
			}
			else if (!mouseEvent.Pressed && _isPressing)
			{
				_isPressing = false;
				float duration = (Time.GetTicksMsec() - _pressStartTime) / 1000.0f;

				if (_animationState == null) return;

				if (_isLongPressTriggered)
				{
					PlayAnimation(TrackHead, ReleaseAnim, false);
					_animationState.AddEmptyAnimation(TrackHead);
				}
				else
				{
					PlayAnimation(TrackHead, ClickAnim, false);
					_animationState.AddEmptyAnimation(TrackHead);
				}

				_isLongPressTriggered = false;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!_isPressing || _isLongPressTriggered || _animationState == null)
			return;

		float duration = (Time.GetTicksMsec() - _pressStartTime) / 1000.0f;
		if (duration >= LongPressThreshold)
		{
			_isLongPressTriggered = true;

			PlayAnimation(TrackHead, PressStartAnim, false);
		}
	}
}
