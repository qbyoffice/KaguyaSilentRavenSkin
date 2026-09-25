using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace SilentSkinMod.Core.Startup;

public partial class KaguyaStartupPopup : Control, IScreenContext
{
    private NVerticalPopup _panel = null!;
    private NPopupYesNoButton _understood = null!;
    private bool _closing;
    private bool _useChinese;
    public Control? DefaultFocusedControl => _understood;

    public override void _Ready()
    {
        _useChinese = LocManager.Instance.Language is "zhs" or "zht";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        _panel = PreloadManager.Cache.GetScene("res://scenes/ui/vertical_popup.tscn")
            .Instantiate<NVerticalPopup>();
        AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        _panel.OffsetLeft = -440;
        _panel.OffsetTop = -390;
        _panel.OffsetRight = 440;
        _panel.OffsetBottom = 390;

        var header = _panel.GetNode<MegaLabel>("Header");
        header.OffsetLeft = 55;
        header.OffsetRight = -55;
        header.MaxFontSize = 30;
        var body = _panel.GetNode<MegaRichTextLabel>("Description");
        body.OffsetLeft = 70;
        body.OffsetRight = -70;
        body.OffsetTop = 140;
        body.OffsetBottom = -210;
        body.ScrollActive = true;

        var live = CreateLiveModeButton();
        _understood = _panel.YesButton;
        ConfigureButton(_panel.NoButton, Localize("不再提醒", "Don't remind me again"), 65, 210, DisableReminder);
        ConfigureButton(_understood, Localize("已了解", "Got it"), 605, 210, Close);

        NButton[] buttons = [_panel.NoButton, live, _understood];
        for (int i = 0; i < buttons.Length; i++)
        {
            var previous = buttons[(i + buttons.Length - 1) % buttons.Length];
            var next = buttons[(i + 1) % buttons.Length];
            buttons[i].FocusNeighborLeft = buttons[i].GetPathTo(previous);
            buttons[i].FocusNeighborRight = buttons[i].GetPathTo(next);
            buttons[i].FocusPrevious = buttons[i].GetPathTo(previous);
            buttons[i].FocusNext = buttons[i].GetPathTo(next);
            buttons[i].FocusNeighborTop = buttons[i].GetPathTo(buttons[i]);
            buttons[i].FocusNeighborBottom = buttons[i].GetPathTo(buttons[i]);
        }
        UpdateText();
    }

    private NMainMenuTextButton CreateLiveModeButton()
    {
        var button = new NMainMenuTextButton
        {
            Name = "LiveModeButton",
            Position = new Vector2(295, 615),
            Size = new Vector2(290, 72),
            FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        var label = new MegaLabel
        {
            Name = "Label",
            MaxFontSize = 26,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            SelfModulate = StsColors.cream
        };
        label.AddThemeFontOverride("font",
            _panel.YesButton.GetNode<MegaLabel>("Visuals/Label").GetThemeFont("font"));
        label.AddThemeColorOverride("font_color", Colors.White);

        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.5f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0));
        label.AddThemeConstantOverride("outline_size", 18);
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        label.AddThemeConstantOverride("shadow_outline_size", 1);
        button.AddChild(label);
        label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        label.PivotOffset = button.Size / 2;
        _panel.AddChild(button);
        label.SetTextAutoSize(Localize("切换为Live mode", "switch to Live mode"));
        button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => SwitchToLive()));
        return button;
    }

    private static void ConfigureButton(NPopupYesNoButton button, string text,
        float left, float width, Action action)
    {
        button.DisconnectHotkeys();
        // The 0.107.1 cancel-button scene does not contain HotkeyIcon. Keep this
        // optional so the popup works across the 0.107.1/0.111 scene variants.
        button.GetNodeOrNull<Control>("HotkeyIcon")?.Hide();
        button.FocusMode = FocusModeEnum.All;
        button.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        button.Position = new Vector2(left, 615);
        button.Size = new Vector2(width, 72);
        var visuals = button.GetNode<Control>("Visuals");
        visuals.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        visuals.PivotOffset = button.Size / 2;
        button.SetText(text);
        button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => action()));
    }

    private string Localize(string chinese, string english) => _useChinese ? chinese : english;

    private void UpdateText(string? error = null)
    {
        string mode = HeadVisibilityBus.CurrentMode switch
        {
            CharacterMode.LiveCat => "Live mode cat",
            CharacterMode.Nsfw => "Vainilla",
            CharacterMode.NsfwCat => "Vainilla cat",
            _ => "Live mode"
        };
        string body = Localize(
            $"感谢您使用本皮肤 mod\n目前你使用的模式：[b][color=#FFD700]{mode}[/color][/b]\n"
            + "[b][color= red]请确认当前模式是否适用于你所处的环境。[/color][/b]\n\n"
            + "我们在选角界面为您提供了以下四个选择模式：\n"
            + "[b][color=#FFD700]Live mode[/color][/b]·[b][color=#FFD700]Live mode cat[/color][/b]·[b][color=#FFD700]Vainilla[/color][/b]·[b][color=#FFD700]Vainilla cat[/color][/b]\n"
            + "\n[color= red]我们并未承诺过制作任何 R-18 模式\nVainilla模式只是不适合所有场景[/color]\n"
            + "\n如果遇到问题\n请在我的[b]GitHub仓库[/b]或[b]创意工坊[/b]提交 issues。\n",
            $"Thank you for using this skin mod!\n\nCurrent mode: [b][color=#FFD700]{mode}[/color][/b]\n"
            + "[color= red]make sure your selected mode is appropriate for your environment.[/color]\n\n"
            + "Four modes are available on the character selection screen:\n"
            + "[b][color=#FFD700]Live mode[/color][/b]·[b][color=#FFD700]Live mode cat[/color][/b]·[b][color=#FFD700]Vainilla[/color][/b]·[b][color=#FFD700]Vainilla cat[/color][/b]\n"
            + "\n[color= red]We never promised to make any R-18 mode.\nVainilla mode just isn't suitable for every situation.[/color]\n"
            + "\nIf you encounter any issues\nreport them on my GitHub repository or Steam Workshop page.\n");
        if (error != null) body += "\n[color=#ffaaaa]" + error + "[/color]";
        _panel.SetText(Localize("来自 KaguyaSilentRavenSkin 的内容", "A message from KaguyaSilentRavenSkin"), body);
    }

    private void DisableReminder()
    {
        if (_closing) return;
        try
        {
            StartupReminderSettings.Disable(ProjectSettings.GlobalizePath(StartupReminderSettings.SaveDirectory));
        }
        catch (Exception exception)
        {
            ReportSaveFailure(exception);
            return;
        }
        Close();
    }

    private void SwitchToLive()
    {
        if (_closing) return;
        try
        {
            StartupReminderSettings.SwitchToLive(ProjectSettings.GlobalizePath(StartupReminderSettings.SaveDirectory));
        }
        catch (Exception exception)
        {
            ReportSaveFailure(exception);
            return;
        }
        HeadVisibilityBus.EnsureInitialized();
        HeadVisibilityBus.SetMode(CharacterMode.Live);

        GetTree().CallGroup("kaguya_mode_selectors", "reload_saved_mode");
        Close();
    }

    private void ReportSaveFailure(Exception exception)
    {
        GD.PushError($"[KaguyaSilentRavenSkin] 保存配置失败：{exception}");
        UpdateText(Localize(
            "[KaguyaSilentRavenSkin]保存失败，设置尚未更改。请检查配置文件和写入权限后重试。",
            "[KaguyaSilentRavenSkin]Could not save your settings. No changes were made. Please check the configuration file and write permissions, then try again."));
    }

    private void Close()
    {
        if (_closing) return;
        _closing = true;
        if (NModalContainer.Instance?.OpenModal == this)
            NModalContainer.Instance.Clear();
    }
}
