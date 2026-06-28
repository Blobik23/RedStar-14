using Content.Shared.Copier;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Copier;

/// <summary>
///     Client-side BUI for the copier, wires window buttons to network messages.
/// </summary>
[UsedImplicitly]
public sealed class CopierBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CopierWindow? _window;

    public CopierBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = new CopierWindow();
        _window.OnClose += Close;

        _window.PrintModeButton.OnPressed += _ =>
        {
            _window.SetMode(CopierMode.Print);
            SendMessage(new CopierSetModeMessage(CopierMode.Print));
        };

        _window.CopyModeButton.OnPressed += _ =>
        {
            _window.SetMode(CopierMode.Copy);
            SendMessage(new CopierSetModeMessage(CopierMode.Copy));
        };

        _window.PrintButton.OnPressed += _ =>
        {
            if (_window.SelectedTemplateId is not null)
                SendMessage(new CopierPrintMessage(_window.SelectedTemplateId, _window.CopiesToPrint));
        };

        _window.CopyButton.OnPressed += _ =>
        {
            SendMessage(new CopierCopyMessage(_window.CopiesToPrint));
        };

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is CopierBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }
}
