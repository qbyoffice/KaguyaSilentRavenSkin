using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace SilentSkinMod.Core.Startup;

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class StartupReminderHook
{
    private static void Postfix(NMainMenu __instance)
    {
        Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(__instance) && __instance.IsInsideTree()
                && !__instance.IsQueuedForDeletion() && !StartupReminderGate.HandledThisLaunch)
                __instance.AddChild(new StartupReminderGate());
        }).CallDeferred();
    }
}

public partial class StartupReminderGate : Node
{
    internal static bool HandledThisLaunch { get; private set; }

    public override void _Ready()
    {
        try
        {
            if (StartupReminderSettings.IsDisabled(
                ProjectSettings.GlobalizePath(StartupReminderSettings.SaveDirectory)))
                HandledThisLaunch = true;
        }
        catch (Exception exception)
        {
            GD.PushWarning($"[KaguyaSilentRavenSkin] 无法读取提醒设置，将显示提醒：{exception.Message}");
        }
    }

    public override void _Process(double delta)
    {
        if (HandledThisLaunch)
        {
            SetProcess(false);
            QueueFree();
            return;
        }

        if (GetParent() is not NMainMenu menu || !menu.IsVisibleInTree()
            || menu.IsQueuedForDeletion() || NGame.Instance?.Transition.InTransition != false
            || NModalContainer.Instance is not { OpenModal: null } modal
            || !ActiveScreenContext.Instance.IsCurrent(menu)) return;
        
        try
        {
            HeadVisibilityBus.EnsureInitialized();
            modal.Add(new KaguyaStartupPopup());
            HandledThisLaunch = true;
        }
        catch (Exception exception)
        {
            GD.PushError($"[KaguyaSilentRavenSkin] 启动提醒创建失败：{exception}");
        }
        SetProcess(false);
        QueueFree();
    }
}
